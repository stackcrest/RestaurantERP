using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class PosController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly IPosBillingService _posService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PosController(
        IApplicationDbContext context,
        IPosBillingService posService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _posService = posService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (!await _posService.IsPosEnabledAsync())
        {
            ViewBag.Disabled = true;
            return View();
        }

        var features = await _posService.GetFeatureStateAsync();
        if (!features.OfflineBilling)
        {
            ViewBag.Disabled = true;
            ViewBag.DisabledMessage = "Offline POS billing is disabled. Enable it in SuperAdmin → POS Settings.";
            return View();
        }

        var categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
        var items = await _context.MenuItems
            .Include(m => m.Category)
            .Include(m => m.Variants.Where(v => v.IsActive))
            .Where(m => m.IsAvailable)
            .OrderBy(m => m.Name)
            .ToListAsync();
        var tables = await _context.Tables.Include(t => t.Section).Where(t => t.Status == TableStatus.Available).ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.Items = items;
        ViewBag.Tables = tables;
        ViewBag.Features = features;
        return View();
    }

    public async Task<IActionResult> Bills(string? source)
    {
        var query = _context.Bills.Include(b => b.Order).OrderByDescending(b => b.CreatedAt).AsQueryable();

        if (!string.IsNullOrEmpty(source) && Enum.TryParse<BillSource>(source, out var s))
            query = query.Where(b => b.Source == s);

        ViewBag.CurrentSource = source;
        ViewBag.Features = await _posService.GetFeatureStateAsync();
        return View(await query.Take(100).ToListAsync());
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Checkout([FromBody] PosCheckoutRequest? request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (request == null || request.Items == null || !request.Items.Any())
            return Json(new { success = false, message = "Invalid request. Please add items and try again." });

        try
        {
            var result = await _posService.CreateOfflineBillAsync(request, user.Id, user.FullName);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            return Json(new { success = true, message = result.Message, billId = result.BillId, printUrl = Url.Action(nameof(Print), new { id = result.BillId }) });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Failed to generate bill: {ex.Message}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateBill(Guid orderId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var result = await _posService.GenerateOnlineBillAsync(orderId, user.Id, user.FullName);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction("Details", "Orders", new { area = "Admin", id = orderId });
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Print), new { id = result.BillId });
    }

    [HttpGet]
    public async Task<IActionResult> Print(Guid id)
    {
        var bill = await _posService.GetBillPrintAsync(id, incrementPrintCount: true);
        if (bill == null) return NotFound();
        return View(bill);
    }
}
