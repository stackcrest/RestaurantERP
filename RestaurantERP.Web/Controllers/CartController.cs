using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Web.Controllers;

[Authorize]
public class CartController : BaseController
{
    private readonly IConfiguration _config;

    public CartController(UserManager<ApplicationUser> userManager, IApplicationDbContext context, IConfiguration config)
        : base(userManager, context)
    {
        _config = config;
    }

    public async Task<IActionResult> Index()
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult();

        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");

        var items = await _context.CartItems
            .Include(c => c.MenuItem)
            .Include(c => c.Variant)
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        var addOns = await _context.AddOns.ToListAsync();
        
        decimal subTotal = 0;
        var cartItems = new List<dynamic>();
        
        foreach (var item in items)
        {
            var unitPrice = item.MenuItem.BasePrice + (item.Variant?.PriceAdjustment ?? 0);
            var itemAddOns = new List<AddOn>();
            
            if (!string.IsNullOrEmpty(item.AddOnIds))
            {
                var ids = item.AddOnIds.Split(',').Select(Guid.Parse).ToList();
                itemAddOns = addOns.Where(a => ids.Contains(a.Id)).ToList();
                unitPrice += itemAddOns.Sum(a => a.Price);
            }

            subTotal += unitPrice * item.Quantity;
            cartItems.Add(new { Item = item, UnitPrice = unitPrice, AddOns = itemAddOns });
        }

        var discountThreshold = _config.GetValue<decimal>("BusinessRules:DiscountThresholdAmount", 500);
        var discountPercent = _config.GetValue<decimal>("BusinessRules:DiscountPercentage", 5);
        var cgst = _config.GetValue<decimal>("BusinessRules:CGST", 2.5m);
        var sgst = _config.GetValue<decimal>("BusinessRules:SGST", 2.5m);

        var discount = subTotal >= discountThreshold ? Math.Round(subTotal * discountPercent / 100, 2) : 0;
        var taxable = subTotal - discount;
        var tax = Math.Round(taxable * (cgst + sgst) / 100, 2);
        var total = taxable + tax;

        ViewBag.CartItems = cartItems;
        ViewBag.SubTotal = subTotal;
        ViewBag.Discount = discount;
        ViewBag.Tax = tax;
        ViewBag.Total = total;
        ViewBag.DiscountThreshold = discountThreshold;
        ViewBag.DiscountPercent = discountPercent;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Add(Guid menuItemId, Guid? variantId, int quantity, List<Guid>? addOnIds)
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult();

        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");

        var addOnStr = addOnIds?.Any() == true ? string.Join(",", addOnIds) : null;

        var existing = await _context.CartItems.FirstOrDefaultAsync(c =>
            c.UserId == user.Id && c.MenuItemId == menuItemId &&
            c.VariantId == variantId && c.AddOnIds == addOnStr);

        if (existing != null)
        {
            existing.Quantity += quantity > 0 ? quantity : 1;
        }
        else
        {
            _context.CartItems.Add(new CartItem
            {
                UserId = user.Id,
                MenuItemId = menuItemId,
                VariantId = variantId,
                Quantity = quantity > 0 ? quantity : 1,
                AddOnIds = addOnStr
            });
        }
        await _context.SaveChangesAsync();

        TempData["Success"] = "Item added to cart!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Update(Guid id, int quantity)
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult();

        var user = await GetCurrentUserAsync();
        var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user!.Id);
        
        if (item != null)
        {
            if (quantity <= 0)
                _context.CartItems.Remove(item);
            else
                item.Quantity = quantity;
            
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Remove(Guid id)
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult();

        var user = await GetCurrentUserAsync();
        var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user!.Id);
        
        if (item != null)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult();

        var user = await GetCurrentUserAsync();
        var items = await _context.CartItems.Where(c => c.UserId == user!.Id).ToListAsync();
        _context.CartItems.RemoveRange(items);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [Authorize]
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> QuickAdd([FromBody] QuickCartRequest request)
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult(json: true);

        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var hasOptions = await _context.MenuItemVariants.AnyAsync(v => v.MenuItemId == request.MenuItemId && v.IsActive)
            || await _context.MenuItemAddOns.AnyAsync(a => a.MenuItemId == request.MenuItemId);

        if (hasOptions)
        {
            return Json(new { success = false, requiresOptions = true, detailUrl = $"/Menu/Details/{request.MenuItemId}" });
        }

        var existing = await _context.CartItems.FirstOrDefaultAsync(c =>
            c.UserId == user.Id && c.MenuItemId == request.MenuItemId && c.VariantId == null && (c.AddOnIds == null || c.AddOnIds == ""));

        if (existing != null)
            existing.Quantity += 1;
        else
            _context.CartItems.Add(new CartItem { UserId = user.Id, MenuItemId = request.MenuItemId, Quantity = 1 });

        await _context.SaveChangesAsync();
        return Json(await BuildCartResponseAsync(user.Id, request.MenuItemId));
    }

    [Authorize]
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> QuickUpdate([FromBody] QuickCartRequest request)
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult(json: true);

        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var item = await _context.CartItems.FirstOrDefaultAsync(c =>
            c.UserId == user.Id && c.MenuItemId == request.MenuItemId && c.VariantId == null && (c.AddOnIds == null || c.AddOnIds == ""));

        if (item != null)
        {
            if (request.Quantity <= 0)
                _context.CartItems.Remove(item);
            else
                item.Quantity = request.Quantity;
            await _context.SaveChangesAsync();
        }

        return Json(await BuildCartResponseAsync(user.Id, request.MenuItemId));
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiUpdateLine([FromBody] CartLineRequest request)
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult(json: true);

        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == user.Id);
        if (item != null)
        {
            if (request.Quantity <= 0)
                _context.CartItems.Remove(item);
            else
                item.Quantity = request.Quantity;
            await _context.SaveChangesAsync();
        }

        return Json(await BuildFullCartResponseAsync(user.Id));
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiRemoveLine([FromBody] CartLineRequest request)
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult(json: true);

        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == user.Id);
        if (item != null)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        return Json(await BuildFullCartResponseAsync(user.Id));
    }

    private async Task<object> BuildFullCartResponseAsync(Guid userId)
    {
        var cartItems = await _context.CartItems
            .Include(c => c.MenuItem)
            .Include(c => c.Variant)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        var addOns = await _context.AddOns.ToListAsync();
        decimal subTotal = 0;
        var lines = new List<object>();

        foreach (var ci in cartItems)
        {
            var unitPrice = ci.MenuItem.BasePrice + (ci.Variant?.PriceAdjustment ?? 0);
            if (!string.IsNullOrEmpty(ci.AddOnIds))
            {
                var ids = ci.AddOnIds.Split(',').Select(Guid.Parse);
                unitPrice += addOns.Where(a => ids.Contains(a.Id)).Sum(a => a.Price);
            }
            var lineTotal = unitPrice * ci.Quantity;
            subTotal += lineTotal;
            lines.Add(new { id = ci.Id, quantity = ci.Quantity, lineTotal });
        }

        var discountThreshold = _config.GetValue<decimal>("BusinessRules:DiscountThresholdAmount", 500);
        var discountPercent = _config.GetValue<decimal>("BusinessRules:DiscountPercentage", 5);
        var cgst = _config.GetValue<decimal>("BusinessRules:CGST", 2.5m);
        var sgst = _config.GetValue<decimal>("BusinessRules:SGST", 2.5m);

        var discount = subTotal >= discountThreshold ? Math.Round(subTotal * discountPercent / 100, 2) : 0;
        var taxable = subTotal - discount;
        var tax = Math.Round(taxable * (cgst + sgst) / 100, 2);
        var total = taxable + tax;

        return new
        {
            success = true,
            cartCount = cartItems.Sum(c => c.Quantity),
            cartTotal = subTotal,
            subTotal,
            discount,
            tax,
            total,
            discountThreshold,
            discountPercent,
            lines
        };
    }

    private async Task<object> BuildCartResponseAsync(Guid userId, Guid menuItemId)
    {
        var cartItems = await _context.CartItems.Include(c => c.MenuItem).Include(c => c.Variant).Where(c => c.UserId == userId).ToListAsync();
        var addOns = await _context.AddOns.ToListAsync();
        decimal total = 0;
        foreach (var ci in cartItems)
        {
            var price = ci.MenuItem.BasePrice + (ci.Variant?.PriceAdjustment ?? 0);
            if (!string.IsNullOrEmpty(ci.AddOnIds))
            {
                var ids = ci.AddOnIds.Split(',').Select(Guid.Parse);
                price += addOns.Where(a => ids.Contains(a.Id)).Sum(a => a.Price);
            }
            total += price * ci.Quantity;
        }

        var qty = cartItems
            .Where(c => c.MenuItemId == menuItemId && c.VariantId == null && (c.AddOnIds == null || c.AddOnIds == ""))
            .Sum(c => c.Quantity);

        return new
        {
            success = true,
            quantity = qty,
            cartCount = cartItems.Sum(c => c.Quantity),
            cartTotal = total
        };
    }
}

public class QuickCartRequest
{
    public Guid MenuItemId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class CartLineRequest
{
    public Guid Id { get; set; }
    public int Quantity { get; set; }
}
