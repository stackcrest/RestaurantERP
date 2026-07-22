using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.DTOs.Orders;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Web.Controllers;

public class MenuController : BaseController
{
    private readonly IReviewService _reviewService;
    private readonly IWishlistService _wishlistService;

    public MenuController(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        IReviewService reviewService,
        IWishlistService wishlistService)
        : base(userManager, context)
    {
        _reviewService = reviewService;
        _wishlistService = wishlistService;
    }

    public async Task<IActionResult> Index(Guid? category, string? search, bool? veg, int page = 1)
    {
        const int pageSize = 12;
        if (page < 1) page = 1;

        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var query = _context.MenuItems
            .Include(m => m.Category)
            .Where(m => m.IsAvailable && !m.IsDeleted)
            .AsQueryable();

        if (category.HasValue)
            query = query.Where(m => m.CategoryId == category.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Name.Contains(search) || (m.Description != null && m.Description.Contains(search)));

        if (veg == true)
            query = query.Where(m => m.IsVeg);

        var total = await query.CountAsync();
        var items = await query.OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.SelectedCategory = category;
        ViewBag.Search = search;
        ViewBag.VegOnly = veg ?? false;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        ViewBag.TotalCount = total;
        ViewBag.CartQuantities = await GetCartQuantitiesAsync();
        ViewBag.ItemsWithOptions = await GetMenuItemsWithOptionsAsync();

        var qs = new List<string>();
        if (category.HasValue) qs.Add($"category={category}");
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
        if (veg == true) qs.Add("veg=true");
        ViewBag.PaginationBaseUrl = "/Menu" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");

        return View(items);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var item = await _context.MenuItems
            .Include(m => m.Category)
            .Include(m => m.Variants.Where(v => v.IsActive))
            .Include(m => m.MenuItemAddOns).ThenInclude(ma => ma.AddOn)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (item == null) return NotFound();

        var reviews = await _reviewService.GetItemReviewsAsync(id, new PagedRequest { Page = 1, PageSize = 20 });
        ViewBag.Reviews = reviews.Data ?? new List<ReviewDto>();

        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            ViewBag.IsLiked = await _context.WishlistItems
                .AnyAsync(w => w.UserId == user.Id && w.MenuItemId == id);
        }

        return View(item);
    }

    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Options(Guid id)
    {
        var item = await _context.MenuItems
            .Include(m => m.Variants.Where(v => v.IsActive))
            .Include(m => m.MenuItemAddOns).ThenInclude(ma => ma.AddOn)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted && m.IsAvailable);

        if (item == null)
            return Json(new { success = false, message = "Item not found." });

        return Json(new
        {
            success = true,
            id = item.Id,
            name = item.Name,
            basePrice = item.BasePrice,
            imageUrl = item.ImageUrl,
            variants = item.Variants.OrderByDescending(v => v.IsDefault).ThenBy(v => v.Name).Select(v => new
            {
                id = v.Id,
                name = v.Name,
                priceAdjustment = v.PriceAdjustment,
                isDefault = v.IsDefault
            }),
            addOns = item.MenuItemAddOns
                .Where(ma => ma.AddOn != null && ma.AddOn.IsActive)
                .Select(ma => new
                {
                    id = ma.AddOn.Id,
                    name = ma.AddOn.Name,
                    price = ma.AddOn.Price
                })
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(Guid menuItemId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        var result = await _wishlistService.ToggleWishlistAsync(user.Id, menuItemId);
        if (Request.Headers.Accept.ToString().Contains("application/json"))
            return Json(new { success = result.Success, liked = result.Data, message = result.Message });

        TempData[result.Success ? "Success" : "Error"] = result.Data
            ? "Added to favourites."
            : "Removed from favourites.";
        return RedirectToAction(nameof(Details), new { id = menuItemId });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(Guid menuItemId, int rating, string? comment)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        if (rating < 1 || rating > 5)
        {
            TempData["Error"] = "Please choose a rating from 1 to 5.";
            return RedirectToAction(nameof(Details), new { id = menuItemId });
        }

        var result = await _reviewService.CreateReviewAsync(user.Id, new CreateReviewDto
        {
            MenuItemId = menuItemId,
            Rating = rating,
            Comment = comment
        });

        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Thanks for your review!"
            : result.Message;
        return RedirectToAction(nameof(Details), new { id = menuItemId });
    }
}
