using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RetailOS.Application.Ai.Interfaces;
using RetailOS.Application.Common.Exceptions;

namespace RetailOS.Infrastructure.Ai;

public class GeminiClient : IGeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RawExtractedInvoice> AnalyzeInvoiceImageAsync(
        byte[] imageBytes, 
        string mimeType, 
        CancellationToken cancellationToken = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
                     ?? _configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new DomainException(
                "GEMINI_API_KEY_MISSING", 
                "مفتاح Google Gemini API غير معرف في إعدادات النظام. يرجى ضبط Gemini:ApiKey أو المتغير GEMINI_API_KEY.", 
                500);
        }

        var model = _configuration["Gemini:Model"] ?? "gemini-1.5-pro";
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new
                        {
                            text = "Analyze this supplier / vendor invoice image carefully. " +
                                   "Extract: supplierName, invoiceNumber, invoiceDate (ISO 8601 string if readable), totalAmount, taxAmount, discountAmount, and line items. " +
                                   "For each item, extract: name (product description), barcode (if visible), category (appropriate broad category e.g. المنظفات، العناية الشخصية), unit (e.g. قطعة، كجم، لتر، عبوة، كرتونة), quantity, unitCost, total. " +
                                   "Ensure all Arabic / Eastern numerals (١، ٢، ٣...) are converted into standard decimal numbers. If barcode is not found, return null."
                        },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = Convert.ToBase64String(imageBytes)
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                response_mime_type = "application/json",
                response_schema = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        supplierName = new { type = "STRING" },
                        invoiceNumber = new { type = "STRING" },
                        invoiceDate = new { type = "STRING" },
                        totalAmount = new { type = "NUMBER" },
                        taxAmount = new { type = "NUMBER" },
                        discountAmount = new { type = "NUMBER" },
                        items = new
                        {
                            type = "ARRAY",
                            items = new
                            {
                                type = "OBJECT",
                                properties = new
                                {
                                    name = new { type = "STRING" },
                                    barcode = new { type = "STRING" },
                                    category = new { type = "STRING" },
                                    unit = new { type = "STRING" },
                                    quantity = new { type = "NUMBER" },
                                    unitCost = new { type = "NUMBER" },
                                    total = new { type = "NUMBER" }
                                },
                                required = new[] { "name", "quantity", "unitCost", "total" }
                            }
                        }
                    },
                    required = new[] { "items", "totalAmount" }
                }
            }
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody), 
            Encoding.UTF8, 
            "application/json");

        _logger.LogInformation("Calling Google Gemini Vision API ({Model})...", model);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(endpoint, jsonContent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Google Gemini Vision API.");
            throw new DomainException("AI_SERVICE_CONNECTION_ERROR", "فشل الاتصال بخدمة الذكاء الاصطناعي. يرجى التحقق من اتصال الإنترنت.", 503);
        }

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API returned error: {StatusCode} - {Response}", response.StatusCode, responseString);
            throw new DomainException("AI_SERVICE_ERROR", $"تعذر تحليل الفاتورة عبر الذكاء الاصطناعي ({response.StatusCode}).", 502);
        }

        try
        {
            using var doc = JsonDocument.Parse(responseString);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                throw new DomainException("AI_EMPTY_RESPONSE", "لم يتمكن الذكاء الاصطناعي من قراءة أي بيانات في الصورة.", 422);
            }

            var textContent = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(textContent))
            {
                throw new DomainException("AI_EMPTY_TEXT", "الاستجابة المستلمة من الذكاء الاصطناعي فارغة.", 422);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var parsed = JsonSerializer.Deserialize<RawExtractedInvoice>(textContent, options);
            if (parsed == null || parsed.Items == null)
            {
                throw new DomainException("AI_PARSE_ERROR", "فشل في تحويل بيانات الفاتورة المقروءة.", 422);
            }

            return parsed;
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Gemini structured response: {Response}", responseString);
            throw new DomainException("AI_DESERIALIZATION_FAILED", "حدث خطأ أثناء معالجة بيانات الفاتورة المستخرجة.", 500);
        }
    }
}
