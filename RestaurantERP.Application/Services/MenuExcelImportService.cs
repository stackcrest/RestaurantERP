using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.DTOs.Menu;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Services;

public class MenuExcelImportService : IMenuExcelImportService
{
    private readonly IApplicationDbContext _context;

    private static readonly string[] RequiredHeaders = ["Category", "Name", "BasePrice"];

    public MenuExcelImportService(IApplicationDbContext context) => _context = context;

    public byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Menu");

        var headers = new[]
        {
            "Category", "Name", "Description", "BasePrice", "IsVeg", "SpiceLevel",
            "IsFeatured", "IsAvailable", "PreparationTimeMinutes", "ImageUrl", "Allergens", "CategoryDisplayOrder"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1D3557");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Sample rows
        sheet.Cell(2, 1).Value = "Starters";
        sheet.Cell(2, 2).Value = "Paneer Tikka";
        sheet.Cell(2, 3).Value = "Grilled cottage cheese with spices";
        sheet.Cell(2, 4).Value = 249;
        sheet.Cell(2, 5).Value = "Yes";
        sheet.Cell(2, 6).Value = "Medium";
        sheet.Cell(2, 7).Value = "Yes";
        sheet.Cell(2, 8).Value = "Yes";
        sheet.Cell(2, 9).Value = 20;
        sheet.Cell(2, 10).Value = "";
        sheet.Cell(2, 11).Value = "Dairy";
        sheet.Cell(2, 12).Value = 1;

        sheet.Cell(3, 1).Value = "Main Course";
        sheet.Cell(3, 2).Value = "Butter Chicken";
        sheet.Cell(3, 3).Value = "Creamy tomato gravy with chicken";
        sheet.Cell(3, 4).Value = 349;
        sheet.Cell(3, 5).Value = "No";
        sheet.Cell(3, 6).Value = "Mild";
        sheet.Cell(3, 7).Value = "No";
        sheet.Cell(3, 8).Value = "Yes";
        sheet.Cell(3, 9).Value = 25;
        sheet.Cell(3, 10).Value = "";
        sheet.Cell(3, 11).Value = "";
        sheet.Cell(3, 12).Value = 2;

        var guide = workbook.Worksheets.Add("Instructions");
        guide.Cell(1, 1).Value = "Menu Excel Import Instructions";
        guide.Cell(1, 1).Style.Font.Bold = true;
        guide.Cell(2, 1).Value = "1. Category and Name and BasePrice are required.";
        guide.Cell(3, 1).Value = "2. Categories are created/updated automatically from the Category column — no need to create them in Categories section first.";
        guide.Cell(4, 1).Value = "3. Existing menu items are matched by Name (case-insensitive) and updated; otherwise a new item is created.";
        guide.Cell(5, 1).Value = "4. IsVeg / IsFeatured / IsAvailable: Yes/No, True/False, 1/0.";
        guide.Cell(6, 1).Value = "5. SpiceLevel: Mild, Medium, Hot, ExtraHot (default Mild).";
        guide.Cell(7, 1).Value = "6. CategoryDisplayOrder is optional; used when creating/updating the category.";
        guide.Columns().AdjustToContents();

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<MenuExcelImportResult> ImportAsync(Stream excelStream, CancellationToken cancellationToken = default)
    {
        var result = new MenuExcelImportResult();

        using var workbook = new XLWorkbook(excelStream);
        var sheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Equals("Menu", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheets.FirstOrDefault();

        if (sheet == null)
        {
            result.Errors.Add("Excel file has no worksheets.");
            return result;
        }

        var headerMap = BuildHeaderMap(sheet);
        foreach (var required in RequiredHeaders)
        {
            if (!headerMap.ContainsKey(required))
            {
                result.Errors.Add($"Missing required column: {required}");
            }
        }

        if (result.HasErrors)
            return result;

        var usedRange = sheet.RangeUsed();
        if (usedRange == null || usedRange.RowCount() < 2)
        {
            result.Errors.Add("Excel file has no data rows.");
            return result;
        }

        var categories = await _context.Categories.ToListAsync(cancellationToken);
        var categoryLookup = categories
            .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var menuItems = await _context.MenuItems.ToListAsync(cancellationToken);

        var itemLookup = menuItems
            .GroupBy(m => m.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var lastRow = usedRange.LastRow().RowNumber();

        for (var row = 2; row <= lastRow; row++)
        {
            try
            {
                var categoryName = GetCellString(sheet, row, headerMap, "Category")?.Trim();
                var itemName = GetCellString(sheet, row, headerMap, "Name")?.Trim();
                var priceText = GetCellString(sheet, row, headerMap, "BasePrice");

                if (string.IsNullOrWhiteSpace(categoryName)
                    && string.IsNullOrWhiteSpace(itemName)
                    && string.IsNullOrWhiteSpace(priceText))
                {
                    continue; // blank row
                }

                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    result.Errors.Add($"Row {row}: Category is required.");
                    result.RowsSkipped++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(itemName))
                {
                    result.Errors.Add($"Row {row}: Name is required.");
                    result.RowsSkipped++;
                    continue;
                }

                if (!TryParseDecimal(priceText, out var basePrice) || basePrice < 0)
                {
                    result.Errors.Add($"Row {row}: BasePrice must be a valid non-negative number.");
                    result.RowsSkipped++;
                    continue;
                }

                var category = await ResolveCategoryAsync(
                    categoryName,
                    GetCellString(sheet, row, headerMap, "CategoryDisplayOrder"),
                    categoryLookup,
                    result,
                    cancellationToken);

                var description = GetCellString(sheet, row, headerMap, "Description");
                var isVeg = ParseBool(GetCellString(sheet, row, headerMap, "IsVeg"), defaultValue: true);
                var spiceLevel = ParseSpiceLevel(GetCellString(sheet, row, headerMap, "SpiceLevel"));
                var isFeatured = ParseBool(GetCellString(sheet, row, headerMap, "IsFeatured"), defaultValue: false);
                var isAvailable = ParseBool(GetCellString(sheet, row, headerMap, "IsAvailable"), defaultValue: true);
                var prepTime = ParseInt(GetCellString(sheet, row, headerMap, "PreparationTimeMinutes"), defaultValue: 15);
                var imageUrl = NullIfEmpty(GetCellString(sheet, row, headerMap, "ImageUrl"));
                var allergens = NullIfEmpty(GetCellString(sheet, row, headerMap, "Allergens"));

                if (itemLookup.TryGetValue(itemName, out var existing))
                {
                    existing.Name = itemName;
                    existing.Description = description;
                    existing.BasePrice = basePrice;
                    existing.CategoryId = category.Id;
                    existing.IsVeg = isVeg;
                    existing.SpiceLevel = spiceLevel;
                    existing.IsFeatured = isFeatured;
                    existing.IsAvailable = isAvailable;
                    existing.PreparationTimeMinutes = prepTime;
                    if (imageUrl != null) existing.ImageUrl = imageUrl;
                    existing.Allergens = allergens;
                    existing.IsDeleted = false;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.ItemsUpdated++;
                }
                else
                {
                    var item = new MenuItem
                    {
                        Name = itemName,
                        Description = description,
                        BasePrice = basePrice,
                        CategoryId = category.Id,
                        IsVeg = isVeg,
                        SpiceLevel = spiceLevel,
                        IsFeatured = isFeatured,
                        IsAvailable = isAvailable,
                        PreparationTimeMinutes = prepTime,
                        ImageUrl = imageUrl,
                        Allergens = allergens
                    };
                    _context.MenuItems.Add(item);
                    itemLookup[itemName] = item;
                    result.ItemsCreated++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {row}: {ex.Message}");
                result.RowsSkipped++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        result.Messages.Add($"Categories created: {result.CategoriesCreated}, updated: {result.CategoriesUpdated}");
        result.Messages.Add($"Menu items created: {result.ItemsCreated}, updated: {result.ItemsUpdated}");
        if (result.RowsSkipped > 0)
            result.Messages.Add($"Rows skipped: {result.RowsSkipped}");

        return result;
    }

    private async Task<Category> ResolveCategoryAsync(
        string categoryName,
        string? displayOrderText,
        Dictionary<string, Category> categoryLookup,
        MenuExcelImportResult result,
        CancellationToken cancellationToken)
    {
        var hasOrder = int.TryParse(displayOrderText?.Trim(), out var displayOrder);

        if (categoryLookup.TryGetValue(categoryName, out var existing))
        {
            var changed = false;
            if (!string.Equals(existing.Name, categoryName, StringComparison.Ordinal))
            {
                existing.Name = categoryName;
                changed = true;
            }

            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                changed = true;
            }

            if (!existing.IsActive)
            {
                existing.IsActive = true;
                changed = true;
            }

            if (hasOrder && existing.DisplayOrder != displayOrder)
            {
                existing.DisplayOrder = displayOrder;
                changed = true;
            }

            if (changed)
            {
                existing.UpdatedAt = DateTime.UtcNow;
                result.CategoriesUpdated++;
            }

            return existing;
        }

        var maxOrder = categoryLookup.Values.Any()
            ? categoryLookup.Values.Max(c => c.DisplayOrder)
            : 0;

        var category = new Category
        {
            Name = categoryName,
            IsActive = true,
            DisplayOrder = hasOrder ? displayOrder : maxOrder + 1
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken); // need Id for menu FK in same batch

        categoryLookup[categoryName] = category;
        result.CategoriesCreated++;
        return category;
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet sheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (var col = 1; col <= lastCol; col++)
        {
            var header = sheet.Cell(1, col).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(header)) continue;
            map[NormalizeHeader(header)] = col;
        }
        return map;
    }

    private static string NormalizeHeader(string header) =>
        header.Replace(" ", "", StringComparison.Ordinal)
              .Replace("_", "", StringComparison.Ordinal)
              switch
              {
                  "CategoryName" => "Category",
                  "ItemName" or "MenuItem" or "MenuItemName" => "Name",
                  "Price" or "PricePerPerson" => "BasePrice",
                  "Veg" or "Vegetarian" => "IsVeg",
                  "Featured" => "IsFeatured",
                  "Available" => "IsAvailable",
                  "PrepTime" or "PreparationTime" => "PreparationTimeMinutes",
                  "Image" or "ImageURL" => "ImageUrl",
                  "DisplayOrder" or "CategoryOrder" => "CategoryDisplayOrder",
                  var h => h
              };

    private static string? GetCellString(IXLWorksheet sheet, int row, Dictionary<string, int> map, string header)
    {
        if (!map.TryGetValue(header, out var col)) return null;
        var cell = sheet.Cell(row, col);
        if (cell.IsEmpty()) return null;
        return cell.GetFormattedString()?.Trim();
    }

    private static bool TryParseDecimal(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        text = text.Replace("₹", "").Replace(",", "").Trim();
        return decimal.TryParse(text, out value);
    }

    private static bool ParseBool(string? text, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(text)) return defaultValue;
        text = text.Trim();
        if (text is "1" or "Y" or "y") return true;
        if (text is "0" or "N" or "n") return false;
        if (bool.TryParse(text, out var b)) return b;
        if (text.Equals("Yes", StringComparison.OrdinalIgnoreCase)
            || text.Equals("Veg", StringComparison.OrdinalIgnoreCase)
            || text.Equals("True", StringComparison.OrdinalIgnoreCase))
            return true;
        if (text.Equals("No", StringComparison.OrdinalIgnoreCase)
            || text.Equals("Non-Veg", StringComparison.OrdinalIgnoreCase)
            || text.Equals("NonVeg", StringComparison.OrdinalIgnoreCase)
            || text.Equals("False", StringComparison.OrdinalIgnoreCase))
            return false;
        return defaultValue;
    }

    private static SpiceLevel ParseSpiceLevel(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return SpiceLevel.Mild;
        text = text.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        if (Enum.TryParse<SpiceLevel>(text, true, out var level)) return level;
        return SpiceLevel.Mild;
    }

    private static int ParseInt(string? text, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(text)) return defaultValue;
        return int.TryParse(text.Trim(), out var n) && n >= 0 ? n : defaultValue;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
