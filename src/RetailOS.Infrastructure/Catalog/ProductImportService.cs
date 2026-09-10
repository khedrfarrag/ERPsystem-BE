using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Products.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Catalog;

public class ProductImportService : IProductImportService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;

    public ProductImportService(AppDbContext context, IStoreContext storeContext)
    {
        _context = context;
        _storeContext = storeContext;
    }

    private record RawRow(
        int RowNumber,
        string Name,
        string? Barcode,
        string CategoryName,
        string UnitSymbol,
        string SellingPrice,
        string? PurchaseCost,
        string? MinStockLevel,
        string? Description,
        string? WholesalePrice = null,
        string? IsWholesaleAvailable = null
    );

    private record ParsedValidProduct(
        string Name,
        string? Barcode,
        Guid CategoryId,
        Guid UnitId,
        decimal SellingPrice,
        decimal? PurchaseCost,
        decimal? MinStockLevel,
        string? Description,
        decimal? WholesalePrice = null,
        bool IsWholesaleAvailable = false
    );

    public async Task<ImportPreviewResponse> PreviewImportAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var rawRows = ParseFile(fileStream, fileName);
        if (rawRows.Count == 0)
            throw new DomainException("EMPTY_IMPORT_FILE", "The import file contains no data rows.", 400);

        var (validItems, errors) = await ValidateRowsAsync(rawRows, cancellationToken);

        return new ImportPreviewResponse(
            TotalRows: rawRows.Count,
            ValidRows: validItems.Count,
            ErrorRows: errors.Count,
            Errors: errors
        );
    }

    public async Task<ImportCommitResponse> CommitImportAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var rawRows = ParseFile(fileStream, fileName);
        if (rawRows.Count == 0)
            throw new DomainException("EMPTY_IMPORT_FILE", "The import file contains no data rows.", 400);

        var (validItems, errors) = await ValidateRowsAsync(rawRows, cancellationToken);
        var skippedInvalid = errors.Count;

        // Fetch current store products for duplicate checking
        var existingNames = (await _context.Products
            .AsNoTracking()
            .Select(p => p.Name.ToLower())
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var existingBarcodes = (await _context.Products
            .AsNoTracking()
            .Where(p => p.Barcode != null)
            .Select(p => p.Barcode!)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var batchToCreate = new List<Product>();
        var skippedDuplicates = 0;

        foreach (var item in validItems)
        {
            var lowerName = item.Name.ToLower();
            if (existingNames.Contains(lowerName))
            {
                skippedDuplicates++;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(item.Barcode) && existingBarcodes.Contains(item.Barcode))
            {
                skippedDuplicates++;
                continue;
            }

            var product = new Product
            {
                StoreId = _storeContext.CurrentStoreId!.Value,
                CategoryId = item.CategoryId,
                UnitId = item.UnitId,
                Name = item.Name,
                Barcode = string.IsNullOrWhiteSpace(item.Barcode) ? null : item.Barcode.Trim(),
                Description = item.Description?.Trim(),
                SellingPrice = item.SellingPrice,
                WholesalePrice = item.WholesalePrice,
                IsWholesaleAvailable = item.IsWholesaleAvailable,
                PurchaseCost = item.PurchaseCost,
                MinStockLevel = item.MinStockLevel,
                IsActive = true
            };

            batchToCreate.Add(product);
            existingNames.Add(lowerName);
            if (!string.IsNullOrWhiteSpace(item.Barcode))
                existingBarcodes.Add(item.Barcode);
        }

        if (batchToCreate.Count > 0)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            _context.Products.AddRange(batchToCreate);
            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }

        return new ImportCommitResponse(
            TotalProcessed: rawRows.Count,
            Created: batchToCreate.Count,
            SkippedDuplicate: skippedDuplicates,
            SkippedInvalid: skippedInvalid
        );
    }

    private async Task<(List<ParsedValidProduct> Valid, List<ImportRowError> Errors)> ValidateRowsAsync(
        List<RawRow> rows,
        CancellationToken cancellationToken)
    {
        var activeCategories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);

        var categoryMap = activeCategories
            .ToDictionary(c => c.Name.Trim().ToLower(), c => c.Id);

        var activeUnits = await _context.Units
            .AsNoTracking()
            .Where(u => u.IsActive)
            .ToListAsync(cancellationToken);

        var unitMap = activeUnits
            .ToDictionary(u => u.Symbol.Trim().ToLower(), u => u.Id);

        var valid = new List<ParsedValidProduct>();
        var errors = new List<ImportRowError>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Name))
            {
                errors.Add(new ImportRowError(row.RowNumber, "name", "Product name is required."));
                continue;
            }

            if (row.Name.Trim().Length > 300)
            {
                errors.Add(new ImportRowError(row.RowNumber, "name", "Product name cannot exceed 300 characters."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.CategoryName) || !categoryMap.TryGetValue(row.CategoryName.Trim().ToLower(), out var categoryId))
            {
                errors.Add(new ImportRowError(row.RowNumber, "category_name", $"Category '{row.CategoryName}' does not exist. Please create it first."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.UnitSymbol) || !unitMap.TryGetValue(row.UnitSymbol.Trim().ToLower(), out var unitId))
            {
                errors.Add(new ImportRowError(row.RowNumber, "unit_symbol", $"Unit '{row.UnitSymbol}' does not exist. Please create it first."));
                continue;
            }

            if (!decimal.TryParse(row.SellingPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0)
            {
                errors.Add(new ImportRowError(row.RowNumber, "selling_price", "Selling price must be greater than zero."));
                continue;
            }

            decimal? cost = null;
            if (!string.IsNullOrWhiteSpace(row.PurchaseCost))
            {
                if (!decimal.TryParse(row.PurchaseCost, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedCost) || parsedCost < 0)
                {
                    errors.Add(new ImportRowError(row.RowNumber, "purchase_cost", "Purchase cost cannot be negative."));
                    continue;
                }
                cost = parsedCost;
            }

            decimal? minStock = null;
            if (!string.IsNullOrWhiteSpace(row.MinStockLevel))
            {
                if (!decimal.TryParse(row.MinStockLevel, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedStock) || parsedStock < 0)
                {
                    errors.Add(new ImportRowError(row.RowNumber, "min_stock_level", "Minimum stock level cannot be negative."));
                    continue;
                }
                minStock = parsedStock;
            }

            decimal? wholesalePrice = null;
            if (!string.IsNullOrWhiteSpace(row.WholesalePrice))
            {
                if (decimal.TryParse(row.WholesalePrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedWs) && parsedWs >= 0)
                {
                    wholesalePrice = parsedWs;
                }
            }

            var isWholesaleAvailable = false;
            if (!string.IsNullOrWhiteSpace(row.IsWholesaleAvailable))
            {
                var val = row.IsWholesaleAvailable.Trim().ToLowerInvariant();
                isWholesaleAvailable = val is "1" or "true" or "yes" or "نعم" or "متاح";
            }

            valid.Add(new ParsedValidProduct(
                row.Name.Trim(),
                string.IsNullOrWhiteSpace(row.Barcode) ? null : row.Barcode.Trim(),
                categoryId,
                unitId,
                price,
                cost,
                minStock,
                row.Description?.Trim(),
                wholesalePrice,
                isWholesaleAvailable
            ));
        }

        return (valid, errors);
    }

    private static List<RawRow> ParseFile(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".csv" => ParseCsv(stream),
            ".xlsx" => ParseXlsx(stream),
            _ => throw new DomainException("UNSUPPORTED_FILE_FORMAT", "Only CSV and XLSX files are supported.", 400)
        };
    }

    private static List<RawRow> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);
        if (!csv.Read() || !csv.ReadHeader())
            return new List<RawRow>();

        var rows = new List<RawRow>();
        var rowNum = 2;

        while (csv.Read())
        {
            var name = csv.GetField("name") ?? csv.GetField(0) ?? string.Empty;
            var barcode = csv.GetField("barcode") ?? csv.GetField(1);
            var category = csv.GetField("category_name") ?? csv.GetField(2) ?? string.Empty;
            var unit = csv.GetField("unit_symbol") ?? csv.GetField(3) ?? string.Empty;
            var price = csv.GetField("selling_price") ?? csv.GetField(4) ?? string.Empty;
            var cost = csv.GetField("purchase_cost") ?? csv.GetField(5);
            var minStock = csv.GetField("min_stock_level") ?? csv.GetField(6);
            var desc = csv.GetField("description") ?? csv.GetField(7);
            var wsPrice = csv.GetField("wholesale_price") ?? csv.GetField(8);
            var wsAvail = csv.GetField("is_wholesale_available") ?? csv.GetField(9);

            rows.Add(new RawRow(rowNum++, name, barcode, category, unit, price, cost, minStock, desc, wsPrice, wsAvail));
        }

        return rows;
    }

    private static List<RawRow> ParseXlsx(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            return new List<RawRow>();

        var rows = new List<RawRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var name = row.Cell(1).GetString();
            var barcode = row.Cell(2).GetString();
            var category = row.Cell(3).GetString();
            var unit = row.Cell(4).GetString();
            var price = row.Cell(5).GetString();
            var cost = row.Cell(6).GetString();
            var minStock = row.Cell(7).GetString();
            var desc = row.Cell(8).GetString();
            var wsPrice = row.Cell(9).GetString();
            var wsAvail = row.Cell(10).GetString();

            rows.Add(new RawRow(rowNum, name, barcode, category, unit, price, cost, minStock, desc, wsPrice, wsAvail));
        }

        return rows;
    }
}
