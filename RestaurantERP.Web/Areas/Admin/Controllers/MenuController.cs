using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class MenuController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly IMenuExcelImportService _excelImport;

    public MenuController(IApplicationDbContext context, IMenuExcelImportService excelImport)
    {
        _context = context;
        _excelImport = excelImport;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _context.MenuItems.Include(m => m.Category)
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.Category.DisplayOrder).ThenBy(m => m.Name)
            .ToListAsync();
        ViewBag.Categories = await _context.Categories
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
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
    public async Task<IActionResult> Create(string name, string? description, decimal basePrice, Guid categoryId,
        bool isVeg, string spiceLevel, bool isFeatured, int preparationTime, string? imageUrl)
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
            ImageUrl = imageUrl,
            IsAvailable = true
        };
        _context.MenuItems.Add(item);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Menu item created successfully!";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await _context.MenuItems.FindAsync(id);
        if (item == null || item.IsDeleted) return NotFound();
        ViewBag.Categories = await _context.Categories.Where(c => c.IsActive && !c.IsDeleted).ToListAsync();
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, string name, string? description, decimal basePrice, Guid categoryId,
        bool isVeg, string spiceLevel, bool isFeatured, bool isAvailable, int preparationTime, string? imageUrl)
    {
        var item = await _context.MenuItems.FindAsync(id);
        if (item == null || item.IsDeleted) return NotFound();

        item.Name = name;
        item.Description = description;
        item.BasePrice = basePrice;
        item.CategoryId = categoryId;
        item.IsVeg = isVeg;
        item.SpiceLevel = Enum.Parse<SpiceLevel>(spiceLevel, true);
        item.IsFeatured = isFeatured;
        item.IsAvailable = isAvailable;
        item.PreparationTimeMinutes = preparationTime;
        item.ImageUrl = imageUrl;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Menu item updated!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleAvailability(Guid id)
    {
        var item = await _context.MenuItems.FindAsync(id);
        if (item != null && !item.IsDeleted)
        {
            item.IsAvailable = !item.IsAvailable;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _context.MenuItems.FindAsync(id);
        if (item != null)
        {
            item.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Menu item deleted.";
        }
        return RedirectToAction("Index");
    }
}
