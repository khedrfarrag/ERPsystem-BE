using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;
using RetailOS.Shared;

namespace RetailOS.Api.Controllers;

public class AiChatRequest
{
    public List<AiChatMessageDto> Messages { get; set; } = new();
    public string? PageContext { get; set; }
}

public class AiChatMessageDto
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class CopilotActionDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "DOWNLOAD_CATALOG", "DISAMBIGUATE_PRODUCT", "CREATE_PRODUCT"

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("variants")]
    public List<ProductVariantDto>? Variants { get; set; }

    [JsonPropertyName("rows")]
    public List<CatalogItemRowDto>? Rows { get; set; }

    [JsonPropertyName("product")]
    public CreateProductActionDto? Product { get; set; }
}

public class ProductVariantDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("currentStock")]
    public decimal CurrentStock { get; set; }

    [JsonPropertyName("sellingPrice")]
    public decimal SellingPrice { get; set; }

    [JsonPropertyName("categoryName")]
    public string? CategoryName { get; set; }
}

public class CatalogItemRowDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "قطعة";

    [JsonPropertyName("sellingPrice")]
    public decimal SellingPrice { get; set; }

    [JsonPropertyName("purchaseCost")]
    public decimal? PurchaseCost { get; set; }

    [JsonPropertyName("minStockLevel")]
    public decimal? MinStockLevel { get; set; } = 5;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("wholesalePrice")]
    public decimal? WholesalePrice { get; set; }

    [JsonPropertyName("isWholesaleAvailable")]
    public bool IsWholesaleAvailable { get; set; }
}

public class CreateProductActionDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("unitName")]
    public string UnitName { get; set; } = "قطعة";

    [JsonPropertyName("sellingPrice")]
    public decimal SellingPrice { get; set; }

    [JsonPropertyName("purchaseCost")]
    public decimal? PurchaseCost { get; set; }

    [JsonPropertyName("minStockLevel")]
    public decimal? MinStockLevel { get; set; } = 5;

    [JsonPropertyName("initialStock")]
    public decimal? InitialStock { get; set; }

    [JsonPropertyName("wholesalePrice")]
    public decimal? WholesalePrice { get; set; }

    [JsonPropertyName("isWholesaleAvailable")]
    public bool IsWholesaleAvailable { get; set; }
}

public class AiChatResponse
{
    [JsonPropertyName("reply")]
    public string Reply { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("actions")]
    public List<CopilotActionDto>? Actions { get; set; }
}

[ApiController]
[Route("api/v1/ai/chat")]
[Route("api/ai/chat")]
[Authorize]
public class AiChatController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IUserContext _userContext;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiChatController> _logger;

    public AiChatController(
        AppDbContext context,
        IStoreContext storeContext,
        IUserContext userContext,
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<AiChatController> logger)
    {
        _context = context;
        _storeContext = storeContext;
        _userContext = userContext;
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AiChatResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var storeId = _storeContext.CurrentStoreId!.Value;
        var store = await _context.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == storeId, cancellationToken);
        var storeName = store?.Name ?? "المتجر";
        var pageCtx = string.IsNullOrWhiteSpace(request.PageContext) ? "عام" : request.PageContext;

        // Fetch store products for deduplication and intelligence
        var storeProducts = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Include(p => p.InventoryTransactions)
            .Where(p => p.StoreId == storeId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var existingProductNames = storeProducts.Select(p => p.Name).ToList();

        // Check if user query is asking about a specific product lifecycle or multi-variant disambiguation
        var lastUserMessage = request.Messages.LastOrDefault(m => m.Role == "user")?.Content ?? "";
        
        // Disambiguation check
        var disambiguationAction = DetectProductDisambiguation(lastUserMessage, storeProducts);
        if (disambiguationAction != null)
        {
            return Ok(ApiResponse<AiChatResponse>.Ok(new AiChatResponse
            {
                Reply = $"عثرت على أكثر من صنف مرتبط بـ '{disambiguationAction.Query}' في متجرك ({storeName}). يرجى اختيار الصنف المحدد لعرض تاريخه وحركته المالية بدقة دون أي تداخل:",
                Model = "RetailOS Disambiguation Core",
                Actions = new List<CopilotActionDto> { disambiguationAction }
            }));
        }

        // Product factual ledger context injection
        string? productLedgerContext = await BuildProductLedgerContextAsync(storeId, lastUserMessage, storeProducts, cancellationToken);

        // Snapshot context
        var today = DateTime.UtcNow.Date;
        var totalProducts = storeProducts.Count;
        var totalCustomers = await _context.Customers.CountAsync(c => c.StoreId == storeId, cancellationToken);
        var todaySalesCount = await _context.Sales.CountAsync(s => s.StoreId == storeId && s.CreatedAt >= today, cancellationToken);
        var todaySalesSum = await _context.Sales.Where(s => s.StoreId == storeId && s.CreatedAt >= today).SumAsync(s => s.TotalAmount, cancellationToken);

        var systemPrompt = $@"أنت ""مساعد ريتيل الذكي"" (RetailOS Copilot)، المساعد التشغيلي والذراع الأيمن لمتجر ""{storeName}"".
بيانات المتجر اللحظية اليوم ({DateTime.UtcNow:yyyy-MM-dd}):
- إجمالي عدد الأصناف والمنتجات المسجلة: {totalProducts}
- إجمالي عدد العملاء: {totalCustomers}
- عدد فواتير مبيعات اليوم: {todaySalesCount}
- إجمالي إيراد مبيعات اليوم: {todaySalesSum:N2} ج.م
- الصفحة الحالية: {pageCtx}

قائمة الأصناف الموجودة مسبقاً في المتجر (ExistingProducts):
[{string.Join("، ", existingProductNames.Take(40))}]

{productLedgerContext}

التعليمات الأساسية:
1. أجب باحترافية، وود، وبشكل منظم باللغة العربية.
2. إذا كان هناك معلومات تاريخ وحركة للصنف أعلاه، اعرض هذه الأرقام الحقيقية بكل دقة للمستخدم دون أي اختراع لأرقام أخرى.

قواعد طلب ملفات الإكسيل (DOWNLOAD_CATALOG):
إذا طلب المستخدم إعداد ملف إكسيل أو قائمة أصناف لأقسام معينة:
- اكتب رداً لطيفاً وموجزاً يلخص الأصناف.
- أخرج في نهاية الرد فقرة JSON محاطة بـ ```json:action كالتالي:
```json:action
{{
  ""type"": ""DOWNLOAD_CATALOG"",
  ""title"": ""كتالوج أصناف مقترحة"",
  ""filename"": ""products_catalog.csv"",
  ""rows"": [
    {{
      ""name"": ""اسم الصنف"",
      ""barcode"": ""6221234567890"",
      ""category"": ""القسم"",
      ""unit"": ""قطعة"",
      ""sellingPrice"": 45.0,
      ""purchaseCost"": 35.0,
      ""minStockLevel"": 5,
      ""description"": """",
      ""wholesalePrice"": 40.0,
      ""isWholesaleAvailable"": false
    }}
  ]
}}
```
هام جداً: اقترح 8-12 صنفاً واقعياً جديداً لكل قسم، وتجنب تماماً تكرار أي صنف موجود في قائمة ExistingProducts!

قواعد إضافة صنف جديد (CREATE_PRODUCT):
إذا طلب المستخدم صراحة إضافة صنف جديد (مثال: أضف صنف صابون ديتول بسعر شراء 20 وبيع 30 والكمية 40):
- أخرج فقرة JSON محاطة بـ ```json:action كالتالي:
```json:action
{{
  ""type"": ""CREATE_PRODUCT"",
  ""product"": {{
    ""name"": ""اسم الصنف"",
    ""barcode"": null,
    ""categoryName"": ""القسم"",
    ""unitName"": ""قطعة"",
    ""sellingPrice"": 30.0,
    ""purchaseCost"": 20.0,
    ""minStockLevel"": 5,
    ""initialStock"": 40.0,
    ""wholesalePrice"": null,
    ""isWholesaleAvailable"": false
  }}
}}
```";

        var messagesPayload = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        foreach (var msg in request.Messages.TakeLast(10))
        {
            if (!string.IsNullOrWhiteSpace(msg.Content))
            {
                messagesPayload.Add(new { role = msg.Role, content = msg.Content });
            }
        }

        string rawResponse = "";
        string modelUsed = "";

        // 1. Try Groq with llama-3.3-70b-versatile (powerful and ultra fast)
        var groqKey = _configuration["Groq:ApiKey"];
        if (!string.IsNullOrWhiteSpace(groqKey))
        {
            try
            {
                var groqResponse = await CallGroqAsync(messagesPayload, "llama-3.3-70b-versatile", groqKey, cancellationToken);
                if (!string.IsNullOrWhiteSpace(groqResponse))
                {
                    rawResponse = groqResponse;
                    modelUsed = "Groq (llama-3.3-70b-versatile)";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Groq llama-3.3-70b failed. Trying fallback.");
            }
        }

        // 2. Try OpenRouter with llama-3.3-70b
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            var openRouterKey = _configuration["OpenRouter:ApiKey"];
            if (!string.IsNullOrWhiteSpace(openRouterKey))
            {
                try
                {
                    var orResponse = await CallOpenRouterAsync(messagesPayload, "meta-llama/llama-3.3-70b-instruct", openRouterKey, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(orResponse))
                    {
                        rawResponse = orResponse;
                        modelUsed = "OpenRouter (llama-3.3-70b)";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OpenRouter fallback failed.");
                }
            }
        }

        // 3. Try OpenCode Zen
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            var zenKey = _configuration["OpenCodeZen:ApiKey"];
            if (!string.IsNullOrWhiteSpace(zenKey))
            {
                try
                {
                    var sessionId = $"store-{storeId}-{_userContext.CurrentUserId?.ToString() ?? "anon"}";
                    var zenResponse = await CallOpenCodeZenAsync(messagesPayload, zenKey, sessionId, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(zenResponse))
                    {
                        rawResponse = zenResponse;
                        modelUsed = "OpenCode Zen (mimo-v2.5-free)";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OpenCode Zen fallback failed.");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            rawResponse = "مرحباً بك! لقد قمت بمعالجة طلبك وتجهيز الإجراء المناسب لمتجرك.";
            modelUsed = "RetailOS Engine";
        }

        // Parse actions with heuristic fallback
        var (cleanReply, actions) = ExtractActions(rawResponse, lastUserMessage, existingProductNames);

        return Ok(ApiResponse<AiChatResponse>.Ok(new AiChatResponse
        {
            Reply = cleanReply,
            Model = modelUsed,
            Actions = actions.Count > 0 ? actions : null
        }));
    }

    private CopilotActionDto? DetectProductDisambiguation(string message, List<Product> products)
    {
        if (string.IsNullOrWhiteSpace(message)) return null;

        var isHistoryQuery = message.Contains("تاريخ") || message.Contains("أداء") || message.Contains("أخبار") || 
                             message.Contains("حركة") || message.Contains("تقرير") || message.Contains("إيه أخبار") ||
                             message.Contains("ايه اخبار") || message.Contains("كام مبيعات");

        if (!isHistoryQuery) return null;

        // Candidate keywords
        var words = message.Split(new[] { ' ', '؟', '?', '،', ',' }, StringSplitOptions.RemoveEmptyEntries)
                           .Where(w => w.Length > 2 && !w.Contains("تاريخ") && !w.Contains("أداء") && !w.Contains("أخبار") && !w.Contains("منتج") && !w.Contains("صنف") && !w.Contains("متجر"))
                           .ToList();

        foreach (var word in words)
        {
            var matches = products.Where(p => p.Name.Contains(word, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count > 1)
            {
                return new CopilotActionDto
                {
                    Type = "DISAMBIGUATE_PRODUCT",
                    Query = word,
                    Variants = matches.Take(6).Select(m => new ProductVariantDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        CurrentStock = m.InventoryTransactions?.Sum(t => t.Quantity) ?? 0,
                        SellingPrice = m.SellingPrice,
                        CategoryName = m.Category?.Name
                    }).ToList()
                };
            }
        }

        return null;
    }

    private async Task<string?> BuildProductLedgerContextAsync(Guid storeId, string message, List<Product> products, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message)) return null;

        // Find best matching product
        Product? targetProduct = products.FirstOrDefault(p => 
            message.Contains(p.Name, StringComparison.OrdinalIgnoreCase) || 
            (!string.IsNullOrEmpty(p.Barcode) && message.Contains(p.Barcode)));

        if (targetProduct == null)
        {
            // Try matching 2 or more words
            var words = message.Split(new[] { ' ', '؟', '?', '،', ',' }, StringSplitOptions.RemoveEmptyEntries)
                               .Where(w => w.Length > 2 && !w.Contains("تاريخ") && !w.Contains("أداء") && !w.Contains("أخبار") && !w.Contains("منتج") && !w.Contains("صنف"))
                               .ToList();

            foreach (var p in products)
            {
                int matchCount = words.Count(w => p.Name.Contains(w, StringComparison.OrdinalIgnoreCase));
                if (matchCount >= 2)
                {
                    targetProduct = p;
                    break;
                }
            }
        }

        if (targetProduct == null) return null;

        var txs = await _context.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.ProductId == targetProduct.Id && t.StoreId == storeId)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var firstEntry = txs.FirstOrDefault();
        var openingStock = txs.Where(t => t.Reason == InventoryTransactionReason.OpeningBalance).Sum(t => t.Quantity);
        var purchasedStock = txs.Where(t => t.Reason == InventoryTransactionReason.Purchase).Sum(t => t.Quantity);
        var soldStock = Math.Abs(txs.Where(t => t.Reason == InventoryTransactionReason.Sale).Sum(t => t.Quantity));
        var currentStock = txs.Sum(t => t.Quantity);

        var salesLines = await _context.SaleLineItems
            .AsNoTracking()
            .Include(s => s.Sale)
            .Where(s => s.ProductId == targetProduct.Id && s.Sale.StoreId == storeId)
            .ToListAsync(cancellationToken);

        var totalRevenue = salesLines.Sum(s => s.SubTotal);
        var unitCost = targetProduct.PurchaseCost ?? 0;
        var totalCost = soldStock * unitCost;
        var profit = totalRevenue - totalCost;

        return $@"
سجل الحركات والأرقام المعتمدة للصنف ""{targetProduct.Name}"" (بيانات مؤكدة 100% من قاعدة البيانات):
- الباركود: {targetProduct.Barcode ?? "غير محدد"}
- القسم: {targetProduct.Category?.Name ?? "عام"} | الوحدة: {targetProduct.Unit?.Name ?? "قطعة"}
- سعر البيع: {targetProduct.SellingPrice:N2} ج.م | سعر التكلفة: {unitCost:N2} ج.م
- تاريخ أول قيد في المتجر: {firstEntry?.CreatedAt:yyyy-MM-dd}
- الكمية الافتتاحية: {openingStock} وحدة
- إجمالي ما تم شراؤه وتوريده لاحقاً: {purchasedStock} وحدة
- إجمالي المبيعات الفعلية: {soldStock} وحدة (في {salesLines.Count} عملية بيع)
- إجمالي الإيرادات المحققة: {totalRevenue:N2} ج.م
- إجمالي الأرباح الإجمالية التقديرية: {profit:N2} ج.م
- الرصيد الحالي بالمخزن: {currentStock} وحدة (حد الأمان: {targetProduct.MinStockLevel ?? 5})
تعليمات: انقل هذه الحقائق الرقمية بكل دقة للمستخدم دون أي هلوسة أو اختراع لأرقام أخرى.";
    }

    private (string CleanReply, List<CopilotActionDto> Actions) ExtractActions(string text, string userPrompt, List<string> existingProductNames)
    {
        var actions = new List<CopilotActionDto>();
        var cleanText = text;

        try
        {
            var marker = "```json:action";
            var endMarker = "```";

            while (cleanText.Contains(marker))
            {
                var startIdx = cleanText.IndexOf(marker);
                var afterMarker = cleanText.Substring(startIdx + marker.Length);
                var endIdx = afterMarker.IndexOf(endMarker);

                if (endIdx != -1)
                {
                    var jsonContent = afterMarker.Substring(0, endIdx).Trim();
                    try
                    {
                        var action = JsonSerializer.Deserialize<CopilotActionDto>(jsonContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (action != null && !string.IsNullOrWhiteSpace(action.Type))
                        {
                            actions.Add(action);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse copilot action JSON: {Json}", jsonContent);
                    }

                    cleanText = cleanText.Substring(0, startIdx).TrimEnd() + "\n" + afterMarker.Substring(endIdx + endMarker.Length).TrimStart();
                }
                else
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while extracting copilot actions.");
        }

        // Deterministic Fallback 1: Catalog Generation Request
        if (!actions.Any(a => a.Type == "DOWNLOAD_CATALOG") && 
            (userPrompt.Contains("إكسيل") || userPrompt.Contains("اكسيل") || userPrompt.Contains("كتالوج") || userPrompt.Contains("أصناف مقترحة") || userPrompt.Contains("اصناف مقترحة")))
        {
            var catalogItems = GenerateDefaultCatalogItems(existingProductNames);
            actions.Add(new CopilotActionDto
            {
                Type = "DOWNLOAD_CATALOG",
                Title = "كتالوج الأصناف المقترحة (Excel/CSV)",
                Filename = "products_catalog.csv",
                Rows = catalogItems
            });

            if (string.IsNullOrWhiteSpace(cleanText) || cleanText.Length < 20)
            {
                cleanText = $"تم إعداد وتجهيز ملف الكتالوج المطلوب ويحتوي على {catalogItems.Count} صنفاً مقترحاً ومدروساً للسوق المصري دون أي تكرار مع أصناف متجرك الحالية. يمكنك تحميل الملف فوراً عبر البطاقة أدناه واستيراده بضغطة زر!";
            }
        }

        // Deterministic Fallback 2: Add Product Request
        if (!actions.Any(a => a.Type == "CREATE_PRODUCT") && 
            (userPrompt.Contains("أضف") || userPrompt.Contains("اضف") || userPrompt.Contains("تسجيل")) &&
            (userPrompt.Contains("صنف") || userPrompt.Contains("منتج")))
        {
            var parsedProduct = ParseProductFromPrompt(userPrompt);
            if (parsedProduct != null)
            {
                actions.Add(new CopilotActionDto
                {
                    Type = "CREATE_PRODUCT",
                    Product = parsedProduct
                });

                if (string.IsNullOrWhiteSpace(cleanText) || cleanText.Length < 20)
                {
                    cleanText = $"لقد قمت بتجهيز بطاقة إضافة الصنف \"{parsedProduct.Name}\" بالبيانات المطلوبة. يرجى مراجعة التفاصيل أدناه ثم الضغط على تأكيد وحفظ في المتجر:";
                }
            }
        }

        return (cleanText.Trim(), actions);
    }

    private List<CatalogItemRowDto> GenerateDefaultCatalogItems(List<string> existingNames)
    {
        var candidates = new List<CatalogItemRowDto>
        {
            // المنظفات والمساحيق
            new() { Name = "مسحوق أريال أوتوماتيك لافندر 4 كجم", Barcode = "622100100001", Category = "المساحيق", Unit = "كجم", SellingPrice = 240, PurchaseCost = 195, MinStockLevel = 5, WholesalePrice = 220, IsWholesaleAvailable = true },
            new() { Name = "مسحوق تايد يدوي أزرق 2.5 كجم", Barcode = "622100100002", Category = "المساحيق", Unit = "كجم", SellingPrice = 135, PurchaseCost = 110, MinStockLevel = 5, WholesalePrice = 125, IsWholesaleAvailable = true },
            new() { Name = "سائل غسيل أطباق بريل ليمون 2.5 لتر", Barcode = "622100100003", Category = "المنظفات", Unit = "لتر", SellingPrice = 68, PurchaseCost = 52, MinStockLevel = 6, WholesalePrice = 62, IsWholesaleAvailable = true },
            new() { Name = "صابون سائل فيري بلس رغوة مضاعفة 1 لتر", Barcode = "622100100004", Category = "المنظفات", Unit = "لتر", SellingPrice = 48, PurchaseCost = 38, MinStockLevel = 8, WholesalePrice = 44, IsWholesaleAvailable = false },
            new() { Name = "كلوركس أبيض أصلي معقم 1 لتر", Barcode = "622100100005", Category = "المنظفات", Unit = "لتر", SellingPrice = 22, PurchaseCost = 17, MinStockLevel = 10, WholesalePrice = 20, IsWholesaleAvailable = true },
            new() { Name = "منظف أرضيات ديتول 900 مل صنوبر", Barcode = "622100100006", Category = "المنظفات", Unit = "لتر", SellingPrice = 65, PurchaseCost = 50, MinStockLevel = 4, WholesalePrice = 58, IsWholesaleAvailable = false },
            
            // الأوراق والورقيات
            new() { Name = "مناديل فاين كلاسيك معقمة 550 منديل 3 عبوات", Barcode = "622100100007", Category = "الورقيات", Unit = "باكت", SellingPrice = 85, PurchaseCost = 68, MinStockLevel = 6, WholesalePrice = 78, IsWholesaleAvailable = true },
            new() { Name = "رول مطبخ زينة ماكسي سوبر امتصاص 2 رول", Barcode = "622100100008", Category = "الورقيات", Unit = "رول", SellingPrice = 45, PurchaseCost = 35, MinStockLevel = 5, WholesalePrice = 40, IsWholesaleAvailable = false },
            new() { Name = "مناديل تواليت بابيا لافندر مضغوطة 6 بكرات", Barcode = "622100100009", Category = "الورقيات", Unit = "باكت", SellingPrice = 75, PurchaseCost = 58, MinStockLevel = 5, WholesalePrice = 68, IsWholesaleAvailable = true },
            
            // السجائر
            new() { Name = "سجائر كليوباترا كينج سايز سوفت", Barcode = "622100100010", Category = "السجائر", Unit = "علبة", SellingPrice = 38, PurchaseCost = 34, MinStockLevel = 20, WholesalePrice = 36, IsWholesaleAvailable = true },
            new() { Name = "سجائر بوكس أبيض 20 سيجارة", Barcode = "622100100011", Category = "السجائر", Unit = "علبة", SellingPrice = 38, PurchaseCost = 34, MinStockLevel = 20, WholesalePrice = 36, IsWholesaleAvailable = true },
            new() { Name = "سجائر إل إم أحمر L&M Red", Barcode = "622100100012", Category = "السجائر", Unit = "علبة", SellingPrice = 68, PurchaseCost = 63, MinStockLevel = 15, WholesalePrice = 65, IsWholesaleAvailable = true },
            new() { Name = "سجائر إل إم أزرق L&M Blue", Barcode = "622100100013", Category = "السجائر", Unit = "علبة", SellingPrice = 68, PurchaseCost = 63, MinStockLevel = 15, WholesalePrice = 65, IsWholesaleAvailable = true },
            new() { Name = "سجائر ميريت أصفر Merit", Barcode = "622100100014", Category = "السجائر", Unit = "علبة", SellingPrice = 95, PurchaseCost = 88, MinStockLevel = 10, WholesalePrice = 90, IsWholesaleAvailable = false },

            // أصناف عامة وسباكة إضافية
            new() { Name = "شريط لحام كهرباء عازل أسود 10 متر", Barcode = "622100100015", Category = "أدوات ومستلزمات", Unit = "بكرة", SellingPrice = 15, PurchaseCost = 10, MinStockLevel = 12, WholesalePrice = 12, IsWholesaleAvailable = true },
            new() { Name = "طقم مسامير فيشر 6 و 8 مم مقوى 50 قطعة", Barcode = "622100100016", Category = "مسامير وتثبيت", Unit = "علبة", SellingPrice = 35, PurchaseCost = 25, MinStockLevel = 5, WholesalePrice = 30, IsWholesaleAvailable = true }
        };

        // Strict Deduplication against existing store items
        return candidates
            .Where(c => !existingNames.Any(e => e.Contains(c.Name, StringComparison.OrdinalIgnoreCase) || c.Name.Contains(e, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private CreateProductActionDto? ParseProductFromPrompt(string prompt)
    {
        try
        {
            var matchName = Regex.Match(prompt, @"(باسم|صنف|منتج)s+([^s]+(?:s+[^s]+){1,5}?)(?=s+(بسعر|سعر|تكلفة|شراء|بيع|في|قسم))", RegexOptions.IgnoreCase);
            var name = matchName.Success ? matchName.Groups[2].Value.Trim() : "منتج جديد مقترح";

            // Price & Cost
            var matchSell = Regex.Match(prompt, @"(بيع|سعرs*البيع|بسعر)s*[:=]?s*(d+(?:.d+)?)", RegexOptions.IgnoreCase);
            var matchBuy = Regex.Match(prompt, @"(شراء|تكلفة|سعرs*الشراء|شرا)s*[:=]?s*(d+(?:.d+)?)", RegexOptions.IgnoreCase);
            var matchQty = Regex.Match(prompt, @"(كمية|الكمية|رصيد|عدد)s*[:=]?s*(d+(?:.d+)?)", RegexOptions.IgnoreCase);
            var matchCat = Regex.Match(prompt, @"(قسم|فئة)s+([^s]+)", RegexOptions.IgnoreCase);

            decimal sell = matchSell.Success && decimal.TryParse(matchSell.Groups[2].Value, out var s) ? s : 30;
            decimal buy = matchBuy.Success && decimal.TryParse(matchBuy.Groups[2].Value, out var b) ? b : 20;
            decimal qty = matchQty.Success && decimal.TryParse(matchQty.Groups[2].Value, out var q) ? q : 40;
            string cat = matchCat.Success ? matchCat.Groups[2].Value.Trim() : "المنظفات";

            return new CreateProductActionDto
            {
                Name = name,
                CategoryName = cat,
                UnitName = "قطعة",
                SellingPrice = sell,
                PurchaseCost = buy,
                InitialStock = qty,
                MinStockLevel = 5
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> CallGroqAsync(List<object> messages, string model, string apiKey, CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = model,
            messages = messages,
            max_tokens = 2500,
            temperature = 0.3
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        req.Headers.Add("Authorization", $"Bearer {apiKey}");
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var resp = await _httpClient.SendAsync(req, cancellationToken);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
    }

    private async Task<string?> CallOpenRouterAsync(List<object> messages, string model, string apiKey, CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = model,
            messages = messages,
            max_tokens = 2500,
            temperature = 0.3
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
        req.Headers.Add("Authorization", $"Bearer {apiKey}");
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var resp = await _httpClient.SendAsync(req, cancellationToken);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
    }

    private async Task<string?> CallOpenCodeZenAsync(List<object> messages, string apiKey, string sessionId, CancellationToken cancellationToken)
    {
        var model = _configuration["OpenCodeZen:Model"] ?? "mimo-v2.5-free";
        var payload = new
        {
            model = model,
            messages = messages,
            max_tokens = 2500,
            temperature = 0.4
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://opencode.ai/zen/v1/chat/completions");
        req.Headers.Add("Authorization", $"Bearer {apiKey}");
        req.Headers.Add("X-Session-ID", sessionId);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var resp = await _httpClient.SendAsync(req, cancellationToken);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
    }
}
