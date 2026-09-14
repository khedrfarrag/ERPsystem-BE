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
            throw new DomainException("EMPTY_IMPORT_FILE", "ملف الاستيراد فارغ ولا يحتوي على صفوف بيانات.", 400);

        var (validItems, errors, _, _) = await ValidateRowsAsync(rawRows, cancellationToken);

        var errorDict = errors
            .GroupBy(e => e.Row)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToList());

        var previewRows = rawRows.Select(r =>
        {
            var hasErrors = errorDict.TryGetValue(r.RowNumber, out var rowErrors);
            decimal.TryParse(r.SellingPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var price);
            decimal? cost = null;
            if (!string.IsNullOrWhiteSpace(r.PurchaseCost) && decimal.TryParse(r.PurchaseCost, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedCost))
                cost = parsedCost;

            return new ImportRowPreviewDto(
                r.RowNumber,
                r.Name,
                r.Barcode,
                r.CategoryName,
                r.UnitSymbol,
                price,
                cost,
                !hasErrors || rowErrors!.Count == 0,
                rowErrors ?? (IReadOnlyList<string>)Array.Empty<string>()
            );
        }).ToList();

        return new ImportPreviewResponse(
            TotalRows: rawRows.Count,
            ValidRows: validItems.Count,
            ErrorRows: errors.Count,
            Errors: errors,
            Rows: previewRows,
            ValidRowsCount: validItems.Count,
            InvalidRowsCount: errors.Count
        );
    }

    public async Task<ImportCommitResponse> CommitImportAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var rawRows = ParseFile(fileStream, fileName);
        if (rawRows.Count == 0)
            throw new DomainException("EMPTY_IMPORT_FILE", "ملف الاستيراد فارغ ولا يحتوي على صفوف بيانات.", 400);

        var (validItems, errors, newCategories, newUnits) = await ValidateRowsAsync(rawRows, cancellationToken);
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

        if (newCategories.Count > 0 || newUnits.Count > 0 || batchToCreate.Count > 0)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            if (newCategories.Count > 0)
            {
                _context.Categories.AddRange(newCategories);
            }
            if (newUnits.Count > 0)
            {
                _context.Units.AddRange(newUnits);
            }
            if (batchToCreate.Count > 0)
            {
                _context.Products.AddRange(batchToCreate);
            }
            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }

        return new ImportCommitResponse(
            TotalProcessed: rawRows.Count,
            Created: batchToCreate.Count,
            SkippedDuplicate: skippedDuplicates,
            SkippedInvalid: skippedInvalid,
            ImportedCount: batchToCreate.Count,
            SkippedCount: skippedDuplicates + skippedInvalid
        );
    }

    private async Task<(List<ParsedValidProduct> Valid, List<ImportRowError> Errors, List<Category> NewCategories, List<Unit> NewUnits)> ValidateRowsAsync(
        List<RawRow> rows,
        CancellationToken cancellationToken)
    {
        var currentStoreId = _storeContext.CurrentStoreId!.Value;

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

        var unitMap = new Dictionary<string, Guid>();
        foreach (var u in activeUnits)
        {
            if (!string.IsNullOrWhiteSpace(u.Symbol))
                unitMap[u.Symbol.Trim().ToLower()] = u.Id;
            if (!string.IsNullOrWhiteSpace(u.Name))
                unitMap[u.Name.Trim().ToLower()] = u.Id;
        }

        var newCategories = new Dictionary<string, Category>();
        var newUnits = new Dictionary<string, Unit>();

        var valid = new List<ParsedValidProduct>();
        var errors = new List<ImportRowError>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Name))
            {
                errors.Add(new ImportRowError(row.RowNumber, "name", "اسم الصنف مطلوب."));
                continue;
            }

            if (row.Name.Trim().Length > 300)
            {
                errors.Add(new ImportRowError(row.RowNumber, "name", "اسم الصنف لا يمكن أن يتجاوز 300 حرف."));
                continue;
            }

            // Category: Match or Auto-create
            var catName = string.IsNullOrWhiteSpace(row.CategoryName) ? "عام" : row.CategoryName.Trim();
            var catKey = catName.ToLower();
            if (!categoryMap.TryGetValue(catKey, out var categoryId))
            {
                if (!newCategories.TryGetValue(catKey, out var newCat))
                {
                    newCat = new Category
                    {
                        StoreId = currentStoreId,
                        Name = catName,
                        IsActive = true
                    };
                    categoryId = newCat.Id;
                    newCategories[catKey] = newCat;
                    categoryMap[catKey] = categoryId;
                }
                else
                {
                    categoryId = newCat.Id;
                }
            }

            // Unit: Match or Auto-create
            var unitSymbol = string.IsNullOrWhiteSpace(row.UnitSymbol) ? "قطعة" : row.UnitSymbol.Trim();
            var unitKey = unitSymbol.ToLower();
            if (!unitMap.TryGetValue(unitKey, out var unitId))
            {
                if (!newUnits.TryGetValue(unitKey, out var newUnit))
                {
                    newUnit = new Unit
                    {
                        StoreId = currentStoreId,
                        Name = unitSymbol,
                        Symbol = unitSymbol,
                        IsActive = true
                    };
                    unitId = newUnit.Id;
                    newUnits[unitKey] = newUnit;
                    unitMap[unitKey] = unitId;
                }
                else
                {
                    unitId = newUnit.Id;
                }
            }

            if (!decimal.TryParse(row.SellingPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0)
            {
                errors.Add(new ImportRowError(row.RowNumber, "selling_price", "سعر البيع يجب أن يكون أكبر من صفر."));
                continue;
            }

            decimal? cost = null;
            if (!string.IsNullOrWhiteSpace(row.PurchaseCost))
            {
                if (!decimal.TryParse(row.PurchaseCost, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedCost) || parsedCost < 0)
                {
                    errors.Add(new ImportRowError(row.RowNumber, "purchase_cost", "سعر التكلفة لا يمكن أن يكون سالباً."));
                    continue;
                }
                cost = parsedCost;
            }

            decimal? minStock = null;
            if (!string.IsNullOrWhiteSpace(row.MinStockLevel))
            {
                if (!decimal.TryParse(row.MinStockLevel, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedStock) || parsedStock < 0)
                {
                    errors.Add(new ImportRowError(row.RowNumber, "min_stock_level", "الحد الأدنى للمخزون لا يمكن أن يكون سالباً."));
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

        return (valid, errors, newCategories.Values.ToList(), newUnits.Values.ToList());
    }

    private static List<RawRow> ParseFile(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".csv" => ParseCsv(stream),
            ".xlsx" => ParseXlsx(stream),
            _ => throw new DomainException("UNSUPPORTED_FILE_FORMAT", "صيغة الملف غير مدعومة. الصيغ المدعومة هي CSV و XLSX فقط.", 400)
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

    public byte[] GenerateTemplateXlsx()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("قالب_الأصناف");
        worksheet.RightToLeft = true;

        var headers = new[]
        {
            "اسم الصنف (إجباري)",
            "الباركود",
            "الفئة",
            "الوحدة",
            "سعر البيع (إجباري)",
            "سعر التكلفة",
            "الحد الأدنى للمخزون",
            "الوصف",
            "سعر الجملة",
            "متاح جملة (1 أو 0)"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#059669");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        var sampleData = new object[][]
        {
            new object[] { "صابون سائل ديتول 500 مل", "6221234567890", "المنظفات", "قطعة", 35.00, 25.00, 10, "صابون سائل معقم لليدين", 30.00, 1 },
            new object[] { "مسحوق غسيل أوتوماتيك 3 كجم", "6221234567891", "المنظفات", "كجم", 180.00, 140.00, 5, "مسحوق تنظيف ملابس عالي الرغوة", 160.00, 1 },
            new object[] { "معطر جو روز 300 مل", "6221234567892", "المعطرات", "عبوة", 45.00, 32.00, 8, "معطر جو برائحة الورد المنعش", 38.00, 1 },
            new object[] { "كلور مبيض 1 لتر", "6221234567893", "المنظفات", "لتر", 22.00, 16.00, 15, "مبيض ومنظف أسطح متعدد الاستخدامات", 19.00, 1 },
            new object[] { "منظف زجاج ومرايا 500 مل", "6221234567894", "المنظفات", "علبة", 28.00, 20.00, 8, "ملمع ومنظف للزجاج فائق اللمعان", 24.00, 1 }
        };

        for (int r = 0; r < sampleData.Length; r++)
        {
            for (int c = 0; c < sampleData[r].Length; c++)
            {
                var cell = worksheet.Cell(r + 2, c + 1);
                var val = sampleData[r][c];
                if (val is double d)
                    cell.Value = d;
                else if (val is int intVal)
                    cell.Value = intVal;
                else
                    cell.Value = val?.ToString() ?? "";
            }
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
