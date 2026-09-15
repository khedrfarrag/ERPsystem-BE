using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        var base64Data = Convert.ToBase64String(imageBytes);
        var dataUri = $"data:{mimeType};base64,{base64Data}";

        // Strategy 1: Try Groq Vision (Ultra-fast, high accuracy, OpenAI-compatible)
        var groqKey = _configuration["Groq:ApiKey"];
        if (!string.IsNullOrWhiteSpace(groqKey))
        {
            try
            {
                _logger.LogInformation("Attempting invoice scan via Groq Vision API...");
                var result = await TryScanWithGroqAsync(dataUri, groqKey, cancellationToken);
                if (result != null && result.Items != null) return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Groq vision scan failed or throttled. Falling back to OpenRouter.");
            }
        }

        // Strategy 2: Try OpenRouter Free Vision (ling-3.0-flash-vl / openrouter/free)
        var orKey = _configuration["OpenRouter:ApiKey"];
        if (!string.IsNullOrWhiteSpace(orKey))
        {
            try
            {
                _logger.LogInformation("Attempting invoice scan via OpenRouter Vision API...");
                var result = await TryScanWithOpenRouterAsync(dataUri, orKey, cancellationToken);
                if (result != null && result.Items != null) return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenRouter vision scan failed. Falling back to Google Gemini.");
            }
        }

        // Strategy 3: Try Google Gemini
        var geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? _configuration["Gemini:ApiKey"];
        if (!string.IsNullOrWhiteSpace(geminiKey))
        {
            return await TryScanWithGeminiAsync(imageBytes, mimeType, geminiKey, cancellationToken);
        }

        throw new DomainException(
            "AI_SERVICE_UNAVAILABLE",
            "لم تتمكن خدمات الذكاء الاصطناعي من معالجة الفاتورة حالياً. يرجى مراجعة حالة الاتصال.",
            503);
    }

    private async Task<RawExtractedInvoice?> TryScanWithGroqAsync(string dataUri, string apiKey, CancellationToken cancellationToken)
    {
        var model = _configuration["Groq:Model"] ?? "qwen/qwen3.8-27b";
        var prompt = GetExtractionPrompt();

        var requestObj = new
        {
            model = model,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new { type = "image_url", image_url = new { url = dataUri } }
                    }
                }
            },
            temperature = 0.1,
            response_format = new { type = "json_object" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(requestObj), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Groq API returned error: {Status} - {Response}", response.StatusCode, err);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return ParseInvoiceJson(content);
    }

    private async Task<RawExtractedInvoice?> TryScanWithOpenRouterAsync(string dataUri, string apiKey, CancellationToken cancellationToken)
    {
        var model = _configuration["OpenRouter:Model"] ?? "inclusionai/ling-3.0-flash-vl:free";
        var prompt = GetExtractionPrompt();

        var requestObj = new
        {
            model = model,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new { type = "image_url", image_url = new { url = dataUri } }
                    }
                }
            },
            temperature = 0.1
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(requestObj), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("OpenRouter API returned error: {Status} - {Response}", response.StatusCode, err);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return ParseInvoiceJson(content);
    }

    private async Task<RawExtractedInvoice> TryScanWithGeminiAsync(byte[] imageBytes, string mimeType, string apiKey, CancellationToken cancellationToken)
    {
        var model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = GetExtractionPrompt() },
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
                response_mime_type = "application/json"
            }
        };

        var response = await _httpClient.PostAsync(
            endpoint, 
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), 
            cancellationToken);

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new DomainException("AI_SERVICE_ERROR", $"Google Gemini API error: {response.StatusCode}", (int)response.StatusCode);
        }

        using var doc = JsonDocument.Parse(responseString);
        var textContent = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return ParseInvoiceJson(textContent) 
            ?? throw new DomainException("AI_PARSE_ERROR", "فشل استخراج البيانات من استجابة Gemini.", 422);
    }

    private static string GetExtractionPrompt()
    {
        return @"Analyze this supplier / vendor invoice image carefully.
Extract: supplierName, invoiceNumber, invoiceDate (YYYY-MM-DD string if readable, else null), totalAmount, taxAmount, discountAmount, and line items.
For each item in the items array, extract: name (clean product description), barcode (if visible, else null), category (broad category in Arabic), unit (e.g. قطعة, كرتونة, علبة, كجم), quantity, unitCost, total.
Ensure all Eastern Arabic numerals (١, ٢, ٣...) are converted into standard decimal numbers.
Return ONLY valid JSON matching this schema:
{
  ""supplierName"": ""string or null"",
  ""invoiceNumber"": ""string or null"",
  ""invoiceDate"": ""string or null"",
  ""totalAmount"": 0.0,
  ""taxAmount"": 0.0,
  ""discountAmount"": 0.0,
  ""items"": [
    {
      ""name"": ""string"",
      ""barcode"": ""string or null"",
      ""category"": ""string"",
      ""unit"": ""string"",
      ""quantity"": 1.0,
      ""unitCost"": 0.0,
      ""total"": 0.0
    }
  ]
}";
    }

    private static RawExtractedInvoice? ParseInvoiceJson(string? rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent)) return null;

        var cleaned = rawContent.Trim();
        if (cleaned.StartsWith("```json"))
            cleaned = cleaned.Substring(7);
        else if (cleaned.StartsWith("```"))
            cleaned = cleaned.Substring(3);

        if (cleaned.EndsWith("```"))
            cleaned = cleaned.Substring(0, cleaned.Length - 3);

        cleaned = cleaned.Trim();

        var jsonMatch = Regex.Match(cleaned, @"\{[\s\S]*\}");
        if (jsonMatch.Success)
        {
            cleaned = jsonMatch.Value;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<RawExtractedInvoice>(cleaned, options);
    }
}
