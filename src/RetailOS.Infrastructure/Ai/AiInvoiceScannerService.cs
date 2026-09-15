using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RetailOS.Application.Ai.DTOs;
using RetailOS.Application.Ai.Interfaces;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Helpers;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Domain.Enums;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Ai;

public class AiInvoiceScannerService : IAiInvoiceScannerService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IUserContext _userContext;
    private readonly IGeminiClient _geminiClient;
    private readonly IAiInvoiceMatchingService _matchingService;
    private readonly IInvoiceVerificationService _verificationService;
    private readonly IInvoiceStorageService _storageService;
    private readonly ILogger<AiInvoiceScannerService> _logger;

    public AiInvoiceScannerService(
        AppDbContext context,
        IStoreContext storeContext,
        IUserContext userContext,
        IGeminiClient geminiClient,
        IAiInvoiceMatchingService matchingService,
        IInvoiceVerificationService verificationService,
        IInvoiceStorageService storageService,
        ILogger<AiInvoiceScannerService> logger)
    {
        _context = context;
        _storeContext = storeContext;
        _userContext = userContext;
        _geminiClient = geminiClient;
        _matchingService = matchingService;
        _verificationService = verificationService;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<InvoiceScanPreviewDto> ScanAndExtractAsync(
        Stream fileStream, 
        string fileName, 
        decimal defaultMarkupPercent = 25m, 
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var storeId = _storeContext.CurrentStoreId!.Value;

        // 1. Read bytes & detect MIME type
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        if (bytes.Length == 0)
            throw new DomainException("EMPTY_FILE", "الملف المرفوع فارغ.", 400);

        if (bytes.Length > 10 * 1024 * 1024)
            throw new DomainException("FILE_TOO_LARGE", "حجم الملف يتجاوز الحد الأقصى المسموح به (10 ميجابايت).", 400);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var mimeType = ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "image/jpeg"
        };

        // 2. Save temporary image for later archiving / preview
        memoryStream.Seek(0, SeekOrigin.Begin);
        var tempKey = await _storageService.SaveTemporaryImageAsync(memoryStream, fileName, storeId, cancellationToken);

        // 3. Call Gemini Vision API
        var rawInvoice = await _geminiClient.AnalyzeInvoiceImageAsync(bytes, mimeType, cancellationToken);

        // 4. Supplier matching
        Guid? matchedSupplierId = null;
        string? matchedSupplierName = rawInvoice.SupplierName;

        if (!string.IsNullOrWhiteSpace(rawInvoice.SupplierName))
        {
            var rawSuppName = rawInvoice.SupplierName.Trim();
            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.StoreId == storeId && s.IsActive)
                .ToListAsync(cancellationToken);

            var supplier = suppliers.FirstOrDefault(s =>
                s.Name.Equals(rawSuppName, StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains(rawSuppName, StringComparison.OrdinalIgnoreCase) ||
                rawSuppName.Contains(s.Name, StringComparison.OrdinalIgnoreCase));

            if (supplier != null)
            {
                matchedSupplierId = supplier.Id;
                matchedSupplierName = supplier.Name;
            }
        }

        // 5. Line items matching & markup pricing
        var previewItems = new List<InvoiceScanLineItemDto>();
        int lineNumber = 1;

        foreach (var rawItem in rawInvoice.Items)
        {
            var cleanName = rawItem.Name?.Trim() ?? string.Empty;
            var cleanBarcode = string.IsNullOrWhiteSpace(rawItem.Barcode) ? null : rawItem.Barcode.Trim();
            var qty = rawItem.Quantity > 0 ? rawItem.Quantity : 1m;
            var cost = rawItem.UnitCost;
            var subTotal = rawItem.Total > 0 ? rawItem.Total : Math.Round(qty * cost, 2, MidpointRounding.AwayFromZero);

            var matchResult = await _matchingService.MatchProductAsync(cleanName, cleanBarcode, storeId, cancellationToken);

            Guid? matchedProdId = null;
            string? matchedProdName = null;
            bool isNew = true;
            decimal confidence = 0m;
            string categoryName = !string.IsNullOrWhiteSpace(rawItem.Category) ? rawItem.Category.Trim() : "عام";
            string unitSymbol = !string.IsNullOrWhiteSpace(rawItem.Unit) ? rawItem.Unit.Trim() : "قطعة";
            decimal? sellingPrice;

            if (matchResult.MatchedProduct != null)
            {
                matchedProdId = matchResult.MatchedProduct.Id;
                matchedProdName = matchResult.MatchedProduct.Name;
                isNew = false;
                confidence = matchResult.ConfidenceScore;

                if (matchResult.MatchedProduct.Category != null)
                    categoryName = matchResult.MatchedProduct.Category.Name;

                if (matchResult.MatchedProduct.Unit != null)
                    unitSymbol = matchResult.MatchedProduct.Unit.Symbol;

                sellingPrice = matchResult.MatchedProduct.SellingPrice > 0
                    ? matchResult.MatchedProduct.SellingPrice
                    : PricingCalculator.CalculateSellingPrice(cost, defaultMarkupPercent);
            }
            else
            {
                sellingPrice = PricingCalculator.CalculateSellingPrice(cost, defaultMarkupPercent);
            }

            var isCostMissingOrZero = cost <= 0m;
            var isSellingBelowCost = PricingCalculator.IsSellingBelowCost(sellingPrice, cost);

            previewItems.Add(new InvoiceScanLineItemDto(
                LineNumber: lineNumber++,
                RawItemName: cleanName,
                Barcode: cleanBarcode,
                MatchedProductId: matchedProdId,
                MatchedProductName: matchedProdName,
                IsNewProduct: isNew,
                ConfidenceScore: confidence,
                CategoryName: categoryName,
                UnitSymbol: unitSymbol,
                Quantity: qty,
                UnitCost: cost,
                SellingPrice: sellingPrice,
                SubTotal: subTotal,
                IsCostMissingOrZero: isCostMissingOrZero,
                IsSellingBelowCost: isSellingBelowCost
            ));
        }

        // 6. Duplicate Invoice Check
        DateTimeOffset? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(rawInvoice.InvoiceDate) && DateTimeOffset.TryParse(rawInvoice.InvoiceDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        {
            parsedDate = d;
        }

        var duplicateWarning = await _verificationService.CheckDuplicateAsync(
            storeId, 
            matchedSupplierId, 
            rawInvoice.InvoiceNumber, 
            rawInvoice.TotalAmount, 
            previewItems, 
            cancellationToken);

        return new InvoiceScanPreviewDto(
            SupplierName: matchedSupplierName,
            MatchedSupplierId: matchedSupplierId,
            InvoiceNumber: rawInvoice.InvoiceNumber,
            InvoiceDate: parsedDate,
            TotalAmount: rawInvoice.TotalAmount,
            TaxAmount: rawInvoice.TaxAmount,
            DiscountAmount: rawInvoice.DiscountAmount,
            ImageTempKey: tempKey,
            DuplicateWarning: duplicateWarning,
            Items: previewItems
        );
    }

    public async Task<CommitAiInvoiceResponse> CommitInvoiceAsync(
        CommitAiInvoiceRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var storeId = _storeContext.CurrentStoreId!.Value;
        var userId = _userContext.CurrentUserId ?? Guid.Empty;

        if (request.Items == null || request.Items.Count == 0)
            throw new DomainException("NO_ITEMS", "لا يمكن حفظ فاتورة لا تحتوي على أصناف.", 400);

        // 1. Guardrail against identical duplicates unless override confirmed
        var duplicateWarning = await _verificationService.CheckDuplicateAsync(
            storeId, 
            request.SupplierId, 
            request.InvoiceNumber, 
            request.Items.Sum(i => i.Quantity * i.UnitCost), 
            request.Items, 
            cancellationToken);

        if (duplicateWarning != null && duplicateWarning.IsIdentical && !request.AllowDuplicateOverride)
        {
            throw new DomainException(
                "DUPLICATE_INVOICE_RESTRICTED", 
                duplicateWarning.WarningMessage, 
                409);
        }

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        // 2. Resolve or provision Categories & Units
        var activeCategories = await _context.Categories
            .Where(c => c.StoreId == storeId && c.IsActive )
            .ToDictionaryAsync(c => c.Name.Trim().ToLowerInvariant(), c => c.Id, cancellationToken);

        var activeUnits = await _context.Units
            .Where(u => u.StoreId == storeId && u.IsActive )
            .ToListAsync(cancellationToken);

        var unitMap = new Dictionary<string, Guid>();
        foreach (var u in activeUnits)
        {
            if (!string.IsNullOrWhiteSpace(u.Symbol)) unitMap[u.Symbol.Trim().ToLowerInvariant()] = u.Id;
            if (!string.IsNullOrWhiteSpace(u.Name)) unitMap[u.Name.Trim().ToLowerInvariant()] = u.Id;
        }

        var newCategories = new Dictionary<string, Category>();
        var newUnits = new Dictionary<string, Unit>();

        Guid ResolveCategory(string name)
        {
            var catName = string.IsNullOrWhiteSpace(name) ? "عام" : name.Trim();
            var catKey = catName.ToLowerInvariant();
            if (activeCategories.TryGetValue(catKey, out var existingId))
                return existingId;

            if (!newCategories.TryGetValue(catKey, out var newCat))
            {
                newCat = new Category
                {
                    StoreId = storeId,
                    Name = catName,
                    IsActive = true
                };
                newCategories[catKey] = newCat;
                activeCategories[catKey] = newCat.Id;
            }
            return newCat.Id;
        }

        Guid ResolveUnit(string symbol)
        {
            var unitSymbol = string.IsNullOrWhiteSpace(symbol) ? "قطعة" : symbol.Trim();
            var unitKey = unitSymbol.ToLowerInvariant();
            if (unitMap.TryGetValue(unitKey, out var existingId))
                return existingId;

            if (!newUnits.TryGetValue(unitKey, out var newUnit))
            {
                newUnit = new Unit
                {
                    StoreId = storeId,
                    Name = unitSymbol,
                    Symbol = unitSymbol,
                    IsActive = true
                };
                newUnits[unitKey] = newUnit;
                unitMap[unitKey] = newUnit.Id;
            }
            return newUnit.Id;
        }

        // 3. Resolve Supplier (if mode == "Purchase" or SupplierName specified)
        Guid? supplierId = request.SupplierId;
        if (!supplierId.HasValue && !string.IsNullOrWhiteSpace(request.SupplierName))
        {
            var rawSupName = request.SupplierName.Trim();
            var existingSupplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.StoreId == storeId && s.Name.ToLower() == rawSupName.ToLower() , cancellationToken);

            if (existingSupplier != null)
            {
                supplierId = existingSupplier.Id;
            }
            else
            {
                var newSupplier = new Supplier
                {
                    StoreId = storeId,
                    Name = rawSupName,
                    Phone = "0000000000",
                    IsActive = true
                };
                _context.Suppliers.Add(newSupplier);
                supplierId = newSupplier.Id;
            }
        }

        // 4. Resolve Products
        var productsCreated = 0;
        var productsUpdated = 0;
        var resolvedProducts = new List<(CommitAiInvoiceItemDto Item, Product Product)>();

        var existingProducts = await _context.Products
            .Where(p => p.StoreId == storeId )
            .ToListAsync(cancellationToken);

        var productDictById = existingProducts.ToDictionary(p => p.Id);
        var productDictByBarcode = existingProducts
            .Where(p => !string.IsNullOrWhiteSpace(p.Barcode))
            .ToDictionary(p => p.Barcode!.Trim().ToLowerInvariant());
        var productDictByName = existingProducts
            .ToDictionary(p => p.Name.Trim().ToLowerInvariant());

        foreach (var item in request.Items)
        {
            Product? targetProduct = null;

            if (item.ProductId.HasValue && productDictById.TryGetValue(item.ProductId.Value, out var prodById))
            {
                targetProduct = prodById;
            }
            else if (!string.IsNullOrWhiteSpace(item.Barcode) && productDictByBarcode.TryGetValue(item.Barcode.Trim().ToLowerInvariant(), out var prodByBc))
            {
                targetProduct = prodByBc;
            }
            else if (productDictByName.TryGetValue(item.Name.Trim().ToLowerInvariant(), out var prodByName))
            {
                targetProduct = prodByName;
            }

            var catId = ResolveCategory(item.CategoryName);
            var uId = ResolveUnit(item.UnitSymbol);

            if (targetProduct != null)
            {
                if (item.SellingPrice > 0)
                {
                    targetProduct.SellingPrice = item.SellingPrice;
                }
                productsUpdated++;
            }
            else
            {
                targetProduct = new Product
                {
                    StoreId = storeId,
                    CategoryId = catId,
                    UnitId = uId,
                    Name = item.Name.Trim(),
                    Barcode = string.IsNullOrWhiteSpace(item.Barcode) ? null : item.Barcode.Trim(),
                    PurchaseCost = item.UnitCost > 0 ? item.UnitCost : null,
                    SellingPrice = item.SellingPrice,
                    IsActive = true
                };
                _context.Products.Add(targetProduct);
                productDictById[targetProduct.Id] = targetProduct;
                if (!string.IsNullOrWhiteSpace(targetProduct.Barcode))
                    productDictByBarcode[targetProduct.Barcode.ToLowerInvariant()] = targetProduct;
                productDictByName[targetProduct.Name.ToLowerInvariant()] = targetProduct;
                productsCreated++;
            }

            resolvedProducts.Add((item, targetProduct));
        }

        if (newCategories.Count > 0)
        {
            _context.Categories.AddRange(newCategories.Values);
        }
        if (newUnits.Count > 0)
        {
            _context.Units.AddRange(newUnits.Values);
        }

        // 5. Digital Invoice Archiving
        string? archivedImageUrl = null;
        var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId, cancellationToken);
        var shouldArchive = request.SaveToArchive ?? (store?.EnableInvoiceArchiving ?? true);

        if (shouldArchive && !string.IsNullOrWhiteSpace(request.ImageTempKey))
        {
            using var tempStream = await _storageService.GetTemporaryImageAsync(request.ImageTempKey, storeId, cancellationToken);
            if (tempStream != null)
            {
                archivedImageUrl = await _storageService.SaveArchiveImageAsync(tempStream, "invoice.webp", storeId, cancellationToken);
            }
        }

        // 6. Branching by Mode: "Purchase" vs "CatalogOnly"
        Guid? createdPurchaseId = null;
        string? createdPurchaseNumber = null;
        var inventoryItemsIncreased = 0;
        decimal totalAmount = 0m;

        if (string.Equals(request.Mode, "Purchase", StringComparison.OrdinalIgnoreCase))
        {
            if (!supplierId.HasValue)
            {
                // Assign or create a generic supplier if not identified
                var genericSupplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.StoreId == storeId && s.Name == "مورد عام", cancellationToken);
                if (genericSupplier == null)
                {
                    genericSupplier = new Supplier
                    {
                        StoreId = storeId,
                        Name = "مورد عام",
                        Phone = "0000000000",
                        IsActive = true
                    };
                    _context.Suppliers.Add(genericSupplier);
                }
                supplierId = genericSupplier.Id;
            }

            createdPurchaseNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20];
            totalAmount = request.Items.Sum(i => i.Quantity * i.UnitCost);

            var purchase = new Purchase
            {
                StoreId = storeId,
                SupplierId = supplierId.Value,
                PurchaseNumber = createdPurchaseNumber,
                InvoiceNumber = request.InvoiceNumber?.Trim(),
                PurchaseDate = request.InvoiceDate ?? DateTimeOffset.UtcNow,
                Status = PurchaseStatus.Confirmed,
                TotalAmount = totalAmount,
                Notes = request.Notes?.Trim(),
                InvoiceImageUrl = archivedImageUrl,
                CreatedBy = userId
            };

            _context.Purchases.Add(purchase);
            createdPurchaseId = purchase.Id;

            // Fetch current stocks for WAC recalculation
            var productIds = resolvedProducts.Select(rp => rp.Product.Id).Distinct().ToList();
            var currentStocks = await _context.InventoryTransactions
                .Where(it => it.StoreId == storeId && productIds.Contains(it.ProductId))
                .GroupBy(it => it.ProductId)
                .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Stock, cancellationToken);

            foreach (var (item, prod) in resolvedProducts)
            {
                var subTotal = Math.Round(item.Quantity * item.UnitCost, 2, MidpointRounding.AwayFromZero);

                purchase.LineItems.Add(new PurchaseLineItem
                {
                    StoreId = storeId,
                    ProductId = prod.Id,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    Discount = 0m,
                    SubTotal = subTotal
                });

                // Weighted Average Cost calculation
                var curStock = currentStocks.TryGetValue(prod.Id, out var s) ? s : 0m;
                var curCost = prod.PurchaseCost ?? item.UnitCost;
                decimal newWac;
                if (curStock <= 0m)
                {
                    newWac = item.UnitCost;
                }
                else
                {
                    var totalCostSum = (curStock * curCost) + (item.Quantity * item.UnitCost);
                    var totalQtySum = curStock + item.Quantity;
                    newWac = Math.Round(totalCostSum / totalQtySum, 6, MidpointRounding.AwayFromZero);
                }
                prod.PurchaseCost = newWac;

                // Increment stock via inventory transaction
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    StoreId = storeId,
                    ProductId = prod.Id,
                    Quantity = item.Quantity,
                    CostPerUnit = item.UnitCost,
                    Reason = InventoryTransactionReason.Purchase,
                    ReferenceId = purchase.Id,
                    Notes = $"AI Invoice Scan {purchase.PurchaseNumber}",
                    CreatedBy = userId
                });

                inventoryItemsIncreased += (int)Math.Max(1, Math.Floor(item.Quantity));
            }

            // Supplier ledger transaction
            _context.SupplierAccountTransactions.Add(new SupplierAccountTransaction
            {
                StoreId = storeId,
                SupplierId = purchase.SupplierId,
                Type = SupplierTransactionType.Purchase,
                Amount = totalAmount,
                ReferenceId = purchase.Id,
                Notes = $"AI Invoice {purchase.InvoiceNumber ?? purchase.PurchaseNumber}",
                CreatedBy = userId
            });
        }
        else
        {
            // CatalogOnly mode: update purchase cost without creating purchase invoice or inventory transaction
            foreach (var (item, prod) in resolvedProducts)
            {
                if (item.UnitCost > 0)
                    prod.PurchaseCost = item.UnitCost;
                totalAmount += item.Quantity * item.UnitCost;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        // 7. Cleanup temp image
        if (!string.IsNullOrWhiteSpace(request.ImageTempKey))
        {
            await _storageService.CleanupTemporaryImageAsync(request.ImageTempKey, cancellationToken);
        }

        var message = string.Equals(request.Mode, "Purchase", StringComparison.OrdinalIgnoreCase)
            ? $"تم تسجيل فاتورة الشراء بنجاح ({createdPurchaseNumber}) وتحديث المخزون والأسعار."
            : $"تم استيراد {productsCreated} صنف جديد وتحديث {productsUpdated} صنف في دليل المنتجات بنجاح.";

        return new CommitAiInvoiceResponse(
            Success: true,
            Message: message,
            PurchaseId: createdPurchaseId,
            PurchaseNumber: createdPurchaseNumber,
            InvoiceImageUrl: archivedImageUrl,
            ProductsCreated: productsCreated,
            ProductsUpdated: productsUpdated,
            InventoryItemsIncreased: inventoryItemsIncreased,
            TotalProcessedAmount: totalAmount
        );
    }
}
