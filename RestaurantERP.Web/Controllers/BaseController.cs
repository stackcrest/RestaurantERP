using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Web.Controllers;

public class BaseController : Controller
{
    protected readonly UserManager<ApplicationUser> _userManager;
    protected readonly IApplicationDbContext _context;

    public BaseController(UserManager<ApplicationUser> userManager, IApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    ViewBag.CanPlaceOrders = !roles.Contains("Admin") && !roles.Contains("SuperAdmin");

                    var cart = await _context.CartItems
                        .Include(c => c.MenuItem)
                        .Include(c => c.Variant)
                        .Where(c => c.UserId == user.Id)
                        .ToListAsync();

                    var addOns = await _context.AddOns.ToListAsync();
                    decimal cartTotal = 0;
                    foreach (var ci in cart)
                    {
                        var price = ci.MenuItem.BasePrice + (ci.Variant?.PriceAdjustment ?? 0);
                        if (!string.IsNullOrEmpty(ci.AddOnIds))
                        {
                            var ids = ci.AddOnIds.Split(',').Select(Guid.Parse);
                            price += addOns.Where(a => ids.Contains(a.Id)).Sum(a => a.Price);
                        }
                        cartTotal += price * ci.Quantity;
                    }

                    ViewBag.CartCount = cart.Sum(c => c.Quantity);
                    ViewBag.CartTotal = cartTotal;
                }
            }
            catch
            {
                ViewBag.CartCount = 0;
                ViewBag.CartTotal = 0m;
                ViewBag.CanPlaceOrders = false;
            }
        }
        await next();
    }

    protected async Task<bool> CanPlaceCustomerOrdersAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return false;

        var roles = await _userManager.GetRolesAsync(user);
        return !roles.Contains("Admin") && !roles.Contains("SuperAdmin");
    }

    protected IActionResult StaffOrderBlockedResult(bool json = false)
    {
        const string message = "Admin and SuperAdmin accounts cannot add to cart or place orders. Please use a customer account.";
        if (json || Request.Headers.Accept.ToString().Contains("application/json"))
            return Json(new { success = false, message });

        TempData["Error"] = message;
        return RedirectToAction("Index", "Home");
    }

    protected async Task<ApplicationUser?> GetCurrentUserAsync() =>
        User.Identity?.IsAuthenticated == true ? await _userManager.GetUserAsync(User) : null;

    protected async Task<Dictionary<Guid, int>> GetCartQuantitiesAsync()
    {
        if (User.Identity?.IsAuthenticated != true) return new();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return new();

        return await _context.CartItems
            .Where(c => c.UserId == user.Id && c.VariantId == null && (c.AddOnIds == null || c.AddOnIds == ""))
            .GroupBy(c => c.MenuItemId)
            .Select(g => new { MenuItemId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.MenuItemId, x => x.Quantity);
    }

    protected async Task<HashSet<Guid>> GetMenuItemsWithOptionsAsync()
    {
        var withVariants = await _context.MenuItemVariants.Where(v => v.IsActive).Select(v => v.MenuItemId).Distinct().ToListAsync();
        var withAddOns = await _context.MenuItemAddOns.Select(a => a.MenuItemId).Distinct().ToListAsync();
        return withVariants.Concat(withAddOns).Distinct().ToHashSet();
    }
}
