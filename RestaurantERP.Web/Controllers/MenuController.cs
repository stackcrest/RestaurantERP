using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Web.Controllers;

public class MenuController : BaseController
{
    public MenuController(UserManager<ApplicationUser> userManager, IApplicationDbContext context)
        : base(userManager, context) { }

    public async Task<IActionResult> Index(Guid? category, string? search, bool? veg)
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var query = _context.MenuItems
            .Include(m => m.Category)
            .Where(m => m.IsAvailable)
            .AsQueryable();

        if (category.HasValue)
            query = query.Where(m => m.CategoryId == category.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Name.Contains(search) || (m.Description != null && m.Description.Contains(search)));

        if (veg == true)
            query = query.Where(m => m.IsVeg);

        var items = await query.OrderBy(m => m.Name).Take(50).ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.SelectedCategory = category;
        ViewBag.Search = search;
        ViewBag.VegOnly = veg ?? false;
        ViewBag.CartQuantities = await GetCartQuantitiesAsync();
        ViewBag.ItemsWithOptions = await GetMenuItemsWithOptionsAsync();
        return View(items);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var item = await _context.MenuItems
            .Include(m => m.Category)
            .Include(m => m.Variants.Where(v => v.IsActive))
            .Include(m => m.MenuItemAddOns).ThenInclude(ma => ma.AddOn)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (item == null) return NotFound();
        return View(item);
    }
}
