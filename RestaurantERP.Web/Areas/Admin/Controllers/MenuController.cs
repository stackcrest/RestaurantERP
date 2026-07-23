using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;
using RestaurantERP.Web.Services;

namespace RestaurantERP.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class MenuController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly IMenuExcelImportService _excelImport;
    private readonly IUploadStorageService _uploads;

    public MenuController(
        IApplicationDbContext context,
        IMenuExcelImportService excelImport,
        IUploadStorageService uploads)
    {
        _context = context;
        _excelImport = excelImport;
        _uploads = uploads;
    }

    public async Task<IActionResult> Index(int page = 1, Guid? categoryId = null, string? search = null)
    {
        const int pageSize = 20;
        if (page < 1) page = 1;

        var query = _context.MenuItems.Include(m => m.Category)
            .Where(m => !m.IsDeleted)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(m => m.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m =>
                m.Name.Contains(term)
                || (m.Description != null && m.Description.Contains(term))
                || m.Category.Name.Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(m => m.Category.DisplayOrder).ThenBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Categories = await _context.Categories
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        ViewBag.TotalCount = total;
        ViewBag.CategoryId = categoryId;
        ViewBag.Search = search;

        var qs = new List<string>();
        if (categoryId.HasValue) qs.Add($"categoryId={categoryId}");
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
        ViewBag.PaginationBaseUrl = "/Admin/Menu" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");

        return View(items);
    }

    public IActionResult Import()
    {
        return View();
    }

    [HttpGet]
    public IActionResult DownloadTemplate()
    {
        var bytes = _excelImport.GenerateTemplate();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "menu-import-template.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select an Excel file (.xlsx).";
            return RedirectToAction(nameof(Import));
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".xlsx" and not ".xlsm")
        {
            TempData["Error"] = "Only .xlsx Excel files are supported.";
            return RedirectToAction(nameof(Import));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _excelImport.ImportAsync(stream);

            if (result.TotalProcessed == 0 && result.HasErrors)
            {
                TempData["Error"] = string.Join(" ", result.Errors.Take(5));
                return RedirectToAction(nameof(Import));
            }

            var summary = string.Join(" | ", result.Messages);
            if (result.HasErrors)
                summary += " | Warnings: " + string.Join("; ", result.Errors.Take(8));

            TempData[result.HasErrors && result.TotalProcessed == 0 ? "Error" : "Success"] = summary;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Import failed: {ex.Message}";
            return RedirectToAction(nameof(Import));
        }
    }

    public async Task<IActionResult> Create()
    {
        var categories = await _context.Categories.Where(c => c.IsActive && !c.IsDeleted).OrderBy(c => c.DisplayOrder).ToListAsync();
        ViewBag.Categories = categories;
        ViewBag.HasCategories = categories.Any();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    public async Task<IActionResult> Create(string name, string? description, decimal basePrice, Guid categoryId,
        bool isVeg, string spiceLevel, bool isFeatured, int preparationTime, string? imageUrl, IFormFile? imageFile,
        bool enableHalfFull = false, decimal? halfPrice = null, decimal? fullPrice = null)
    {
        if (!await _context.Categories.AnyAsync(c => c.IsActive && !c.IsDeleted))
        {
            TempData["Error"] = "Please create a category first, or use Excel Import to upload menu with categories.";
            return RedirectToAction("Create");
        }

        if (categoryId == Guid.Empty)
        {
            TempData["Error"] = "Please select a category.";
            return RedirectToAction("Create");
        }

        if (enableHalfFull)
        {
            var sizeError = ValidateHalfFullPrices(halfPrice, fullPrice);
            if (sizeError != null)
            {
                TempData["Error"] = sizeError;
                return RedirectToAction("Create");
            }
            basePrice = halfPrice!.Value;
        }

        var resolvedImage = await ResolveImageAsync(imageFile, imageUrl);
        if (resolvedImage.Error != null)
        {
            TempData["Error"] = resolvedImage.Error;
            return RedirectToAction("Create");
        }

        var item = new MenuItem
        {
            Name = name,
            Description = description,
            BasePrice = basePrice,
            CategoryId = categoryId,
            IsVeg = isVeg,
            SpiceLevel = Enum.Parse<SpiceLevel>(spiceLevel, true),
            IsFeatured = isFeatured,
            PreparationTimeMinutes = preparationTime,
            ImageUrl = resolvedImage.Url,
            ThumbnailUrl = resolvedImage.Url,
            IsAvailable = true
        };
        _context.MenuItems.Add(item);
        await _context.SaveChangesAsync();
        await SyncHalfFullVariantsAsync(item, enableHalfFull, halfPrice, fullPrice);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Menu item created successfully!";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(Guid id, int page = 1, Guid? categoryId = null, string? search = null)
    {
        var item = await _context.MenuItems
            .Include(m => m.Variants)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
        if (item == null) return NotFound();

        var half = item.Variants.FirstOrDefault(v => v.IsActive && v.Name.Equals("Half", StringComparison.OrdinalIgnoreCase));
        var full = item.Variants.FirstOrDefault(v => v.IsActive && v.Name.Equals("Full", StringComparison.OrdinalIgnoreCase));
        ViewBag.EnableHalfFull = half != null && full != null;
        ViewBag.HalfPrice = half != null ? item.BasePrice + half.PriceAdjustment : (decimal?)null;
        ViewBag.FullPrice = full != null ? item.BasePrice + full.PriceAdjustment : (decimal?)null;

        ViewBag.Categories = await _context.Categories.Where(c => c.IsActive && !c.IsDeleted).ToListAsync();
        ViewBag.ReturnPage = page < 1 ? 1 : page;
        ViewBag.ReturnCategoryId = categoryId;
        ViewBag.ReturnSearch = search;
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    public async Task<IActionResult> Edit(Guid id, string name, string? description, decimal basePrice, Guid categoryId,
        bool isVeg, string spiceLevel, bool isFeatured, bool isAvailable, int preparationTime,
        string? imageUrl, IFormFile? imageFile,
        int returnPage = 1, Guid? returnCategoryId = null, string? returnSearch = null,
        bool enableHalfFull = false, decimal? halfPrice = null, decimal? fullPrice = null)
    {
        var item = await _context.MenuItems
            .Include(m => m.Variants)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
        if (item == null) return NotFound();

        if (enableHalfFull)
        {
            var sizeError = ValidateHalfFullPrices(halfPrice, fullPrice);
            if (sizeError != null)
            {
                TempData["Error"] = sizeError;
                return RedirectToAction("Edit", BuildEditRoute(id, returnPage, returnCategoryId, returnSearch));
            }
            basePrice = halfPrice!.Value;
        }

        var resolvedImage = await ResolveImageAsync(imageFile, imageUrl, item.ImageUrl);
        if (resolvedImage.Error != null)
        {
            TempData["Error"] = resolvedImage.Error;
            return RedirectToAction("Edit", BuildEditRoute(id, returnPage, returnCategoryId, returnSearch));
        }

        var previousImage = item.ImageUrl;

        item.Name = name;
        item.Description = description;
        item.BasePrice = basePrice;
        item.CategoryId = categoryId;
        item.IsVeg = isVeg;
        item.SpiceLevel = Enum.Parse<SpiceLevel>(spiceLevel, true);
        item.IsFeatured = isFeatured;
        item.IsAvailable = isAvailable;
        item.PreparationTimeMinutes = preparationTime;
        item.ImageUrl = resolvedImage.Url;
        item.ThumbnailUrl = resolvedImage.Url;
        item.UpdatedAt = DateTime.UtcNow;

        await SyncHalfFullVariantsAsync(item, enableHalfFull, halfPrice, fullPrice);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(previousImage)
            && previousImage != resolvedImage.Url
            && (previousImage.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
                || previousImage.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)))
        {
            await _uploads.DeleteIfExistsAsync(previousImage);
        }

        TempData["Success"] = "Menu item updated!";
        return RedirectToAction(nameof(Index), BuildIndexRoute(returnPage, returnCategoryId, returnSearch));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleAvailability(Guid id, int page = 1, Guid? categoryId = null, string? search = null)
    {
        var item = await _context.MenuItems.FindAsync(id);
        if (item != null && !item.IsDeleted)
        {
            item.IsAvailable = !item.IsAvailable;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index), BuildIndexRoute(page, categoryId, search));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, int page = 1, Guid? categoryId = null, string? search = null)
    {
        var item = await _context.MenuItems.FindAsync(id);
        if (item != null)
        {
            item.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Menu item deleted.";
        }
        return RedirectToAction(nameof(Index), BuildIndexRoute(page, categoryId, search));
    }

    private static object BuildIndexRoute(int page, Guid? categoryId, string? search)
    {
        var route = new Dictionary<string, object?>();
        if (page > 1) route["page"] = page;
        if (categoryId.HasValue) route["categoryId"] = categoryId.Value;
        if (!string.IsNullOrWhiteSpace(search)) route["search"] = search.Trim();
        return route;
    }

    private static object BuildEditRoute(Guid id, int page, Guid? categoryId, string? search)
    {
        var route = new Dictionary<string, object?> { ["id"] = id };
        if (page > 1) route["page"] = page;
        if (categoryId.HasValue) route["categoryId"] = categoryId.Value;
        if (!string.IsNullOrWhiteSpace(search)) route["search"] = search.Trim();
        return route;
    }

    private static string? ValidateHalfFullPrices(decimal? halfPrice, decimal? fullPrice)
    {
        if (!halfPrice.HasValue || halfPrice.Value <= 0)
            return "Enter a valid Half price.";
        if (!fullPrice.HasValue || fullPrice.Value <= 0)
            return "Enter a valid Full price.";
        if (fullPrice.Value < halfPrice.Value)
            return "Full price must be greater than or equal to Half price.";
        return null;
    }

    private async Task SyncHalfFullVariantsAsync(MenuItem item, bool enableHalfFull, decimal? halfPrice, decimal? fullPrice)
    {
        var variants = item.Variants?.ToList()
            ?? await _context.MenuItemVariants.Where(v => v.MenuItemId == item.Id).ToListAsync();

        var half = variants.FirstOrDefault(v => v.Name.Equals("Half", StringComparison.OrdinalIgnoreCase));
        var full = variants.FirstOrDefault(v => v.Name.Equals("Full", StringComparison.OrdinalIgnoreCase));

        if (!enableHalfFull)
        {
            if (half != null) { half.IsActive = false; half.UpdatedAt = DateTime.UtcNow; }
            if (full != null) { full.IsActive = false; full.UpdatedAt = DateTime.UtcNow; }
            return;
        }

        item.BasePrice = halfPrice!.Value;
        var fullAdj = fullPrice!.Value - halfPrice.Value;

        if (half == null)
        {
            _context.MenuItemVariants.Add(new MenuItemVariant
            {
                MenuItemId = item.Id,
                Name = "Half",
                PriceAdjustment = 0,
                IsDefault = true,
                IsActive = true
            });
        }
        else
        {
            half.PriceAdjustment = 0;
            half.IsDefault = true;
            half.IsActive = true;
            half.UpdatedAt = DateTime.UtcNow;
        }

        if (full == null)
        {
            _context.MenuItemVariants.Add(new MenuItemVariant
            {
                MenuItemId = item.Id,
                Name = "Full",
                PriceAdjustment = fullAdj,
                IsDefault = false,
                IsActive = true
            });
        }
        else
        {
            full.PriceAdjustment = fullAdj;
            full.IsDefault = false;
            full.IsActive = true;
            full.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<(string? Url, string? Error)> ResolveImageAsync(
        IFormFile? imageFile, string? imageUrl, string? currentUrl = null)
    {
        if (imageFile != null && imageFile.Length > 0)
            return await _uploads.SaveImageAsync(imageFile, "menu");

        if (!string.IsNullOrWhiteSpace(imageUrl))
            return (imageUrl.Trim(), null);

        return (currentUrl, null);
    }
}
