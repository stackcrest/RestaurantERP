using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Web.Services;

namespace RestaurantERP.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class CategoriesController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly IUploadStorageService _uploads;

    public CategoriesController(IApplicationDbContext context, IUploadStorageService uploads)
    {
        _context = context;
        _uploads = uploads;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories
            .Include(c => c.MenuItems)
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
        return View(categories);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    public async Task<IActionResult> Create(
        string name, string? description, string? imageUrl, IFormFile? imageFile,
        int displayOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Category name is required.";
            return View();
        }

        var resolved = await ResolveImageAsync(imageFile, imageUrl);
        if (resolved.Error != null)
        {
            TempData["Error"] = resolved.Error;
            return View();
        }

        var category = new Category
        {
            Name = name.Trim(),
            Description = description,
            ImageUrl = resolved.Url,
            DisplayOrder = displayOrder,
            IsActive = isActive
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Category created successfully!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null || category.IsDeleted) return NotFound();
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    public async Task<IActionResult> Edit(
        Guid id, string name, string? description, string? imageUrl, IFormFile? imageFile,
        int displayOrder, bool isActive)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null || category.IsDeleted) return NotFound();

        var resolved = await ResolveImageAsync(imageFile, imageUrl, category.ImageUrl);
        if (resolved.Error != null)
        {
            TempData["Error"] = resolved.Error;
            return RedirectToAction(nameof(Edit), new { id });
        }

        var previousImage = category.ImageUrl;

        category.Name = name.Trim();
        category.Description = description;
        category.ImageUrl = resolved.Url;
        category.DisplayOrder = displayOrder;
        category.IsActive = isActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(previousImage)
            && previousImage != resolved.Url
            && (previousImage.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
                || previousImage.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)))
        {
            await _uploads.DeleteIfExistsAsync(previousImage);
        }

        TempData["Success"] = "Category updated!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null && !category.IsDeleted)
        {
            category.IsActive = !category.IsActive;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _context.Categories
            .Include(c => c.MenuItems)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return RedirectToAction(nameof(Index));

        if (category.MenuItems.Any(m => !m.IsDeleted))
        {
            TempData["Error"] = "Cannot delete category with menu items. Move or delete items first.";
            return RedirectToAction(nameof(Index));
        }

        category.IsDeleted = true;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Category deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<(string? Url, string? Error)> ResolveImageAsync(
        IFormFile? imageFile, string? imageUrl, string? currentUrl = null)
    {
        if (imageFile != null && imageFile.Length > 0)
            return await _uploads.SaveImageAsync(imageFile, "categories");

        if (!string.IsNullOrWhiteSpace(imageUrl))
            return (imageUrl.Trim(), null);

        return (currentUrl, null);
    }
}
