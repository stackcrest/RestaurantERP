using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class CategoriesController : Controller
{
    private readonly IApplicationDbContext _context;

    public CategoriesController(IApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories
            .Include(c => c.MenuItems)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
        return View(categories);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(string name, string? description, string? imageUrl, int displayOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("", "Category name is required.");
            return View();
        }

        var category = new Category
        {
            Name = name.Trim(),
            Description = description,
            ImageUrl = imageUrl,
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
        if (category == null) return NotFound();
        return View(category);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, string name, string? description, string? imageUrl, int displayOrder, bool isActive)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        category.Name = name.Trim();
        category.Description = description;
        category.ImageUrl = imageUrl;
        category.DisplayOrder = displayOrder;
        category.IsActive = isActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Category updated!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            category.IsActive = !category.IsActive;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
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
}
