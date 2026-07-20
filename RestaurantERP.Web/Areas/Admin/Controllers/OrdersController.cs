using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class OrdersController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly IOrderTrackingService _trackingService;
    private readonly IPosBillingService _posService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(
        IApplicationDbContext context,
        IOrderTrackingService trackingService,
        IPosBillingService posService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _trackingService = trackingService;
        _posService = posService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? status, string? type)
    {
        var query = _context.Orders.Include(o => o.User).Include(o => o.Items).AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var s))
            query = query.Where(o => o.Status == s);
        else if (status == "active")
            query = query.Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled);

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<OrderType>(type, out var t))
            query = query.Where(o => o.OrderType == t);

        var orders = await query.OrderByDescending(o => o.CreatedAt).Take(100).ToListAsync();
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentType = type;
        ViewBag.AreaPrefix = "/Admin";
        return View(orders);
    }

    public async Task<IActionResult> Tracking(string? type)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled);

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<OrderType>(type, out var t))
            query = query.Where(o => o.OrderType == t);

        var orders = await query.OrderByDescending(o => o.CreatedAt).Take(200).ToListAsync();
        ViewBag.CurrentType = type;
        ViewBag.AreaPrefix = "/Admin";
        return View(orders);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.AddOns)
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        ViewBag.Timeline = await _trackingService.GetTimelineAsync(id);
        ViewBag.AreaPrefix = "/Admin";
        ViewBag.OrderArea = "Admin";
        ViewBag.ExistingBill = await _context.Bills.FirstOrDefaultAsync(b => b.OrderId == id);
        ViewBag.PosFeatures = await _posService.GetFeatureStateAsync();
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(Guid id, string status, string? note)
    {
        if (!Enum.TryParse<OrderStatus>(status, out var newStatus))
        {
            TempData["Error"] = "Invalid status.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var user = await _userManager.GetUserAsync(User);
        var result = await _trackingService.UpdateStatusAsync(id, newStatus, user?.Id, user?.FullName, note);

        if (result.Success)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateBill(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        try
        {
            var result = await _posService.GenerateOnlineBillAsync(id, user.Id, user.FullName);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = result.Message;
            return RedirectToAction("Print", "Pos", new { area = "Admin", id = result.BillId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to generate bill: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
