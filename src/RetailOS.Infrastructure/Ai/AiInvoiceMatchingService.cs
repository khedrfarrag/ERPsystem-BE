using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Ai.DTOs;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Ai;

public interface IAiInvoiceMatchingService
{
    Task<ProductMatchResult> MatchProductAsync(
        string rawName, 
        string? barcode, 
        Guid storeId, 
        CancellationToken cancellationToken = default);
}

public record ProductMatchResult(
    Product? MatchedProduct,
    decimal ConfidenceScore,
    bool IsExactMatch
);

public class AiInvoiceMatchingService : IAiInvoiceMatchingService
{
    private readonly AppDbContext _context;

    public AiInvoiceMatchingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProductMatchResult> MatchProductAsync(
        string rawName, 
        string? barcode, 
        Guid storeId, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return new ProductMatchResult(null, 0m, false);
        }

        var cleanBarcode = barcode?.Trim();
        var cleanName = NormalizeString(rawName);

        // 1. Tier 1: Barcode match (Exact, highest confidence)
        if (!string.IsNullOrWhiteSpace(cleanBarcode))
        {
            var productByBarcode = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Unit)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.StoreId == storeId && p.Barcode == cleanBarcode && p.IsActive , cancellationToken);

            if (productByBarcode != null)
            {
                return new ProductMatchResult(productByBarcode, 1.0m, true);
            }
        }

        // Fetch active products in this store to match in memory
        var storeProducts = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .AsNoTracking()
            .Where(p => p.StoreId == storeId && p.IsActive )
            .ToListAsync(cancellationToken);

        // 2. Tier 2: Exact Name match (Normalized)
        var exactMatch = storeProducts.FirstOrDefault(p => NormalizeString(p.Name) == cleanName);
        if (exactMatch != null)
        {
            return new ProductMatchResult(exactMatch, 0.95m, true);
        }

        // 3. Tier 3: Fuzzy matching (Token Overlap & Levenshtein Similarity)
        Product? bestMatch = null;
        decimal bestScore = 0m;

        foreach (var prod in storeProducts)
        {
            var prodNorm = NormalizeString(prod.Name);
            var similarity = CalculateSimilarity(cleanName, prodNorm);

            if (similarity > bestScore && similarity >= 0.70m)
            {
                bestScore = similarity;
                bestMatch = prod;
            }
        }

        if (bestMatch != null)
        {
            return new ProductMatchResult(bestMatch, Math.Round(bestScore, 2), false);
        }

        return new ProductMatchResult(null, 0m, false);
    }

    private static string NormalizeString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var normalized = input.Trim().ToLowerInvariant()
            .Replace("أ", "ا")
            .Replace("إ", "ا")
            .Replace("آ", "ا")
            .Replace("ة", "ه")
            .Replace("ى", "ي");

        return normalized;
    }

    private static decimal CalculateSimilarity(string s1, string s2)
    {
        if (s1 == s2) return 1.0m;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0m;

        // Token overlap (Dice coefficient)
        var tokens1 = s1.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var tokens2 = s2.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        int intersection = tokens1.Intersect(tokens2).Count();
        decimal tokenScore = (2.0m * intersection) / (tokens1.Count + tokens2.Count);

        // Levenshtein distance
        int distance = ComputeLevenshteinDistance(s1, s2);
        int maxLen = Math.Max(s1.Length, s2.Length);
        decimal levScore = maxLen == 0 ? 1.0m : 1.0m - ((decimal)distance / maxLen);

        // Weighted blend (60% token overlap, 40% character edit distance)
        return (tokenScore * 0.6m) + (levScore * 0.4m);
    }

    private static int ComputeLevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
