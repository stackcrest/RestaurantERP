using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class PosController : Controller
{
    private readonly IPosBillingService _posService;
    private readonly IApplicationDbContext _context;

    public PosController(IPosBillingService posService, IApplicationDbContext context)
    {
        _posService = posService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Config = await _posService.GetConfigAsync();
        ViewBag.Features = await _posService.GetFeatureStateAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SaveSettings(string gstin, string billPrefix, string restaurantPhone, string billFooterNote)
    {
        await _posService.SaveConfigAsync(new PosConfig(gstin, billPrefix ?? "BILL", restaurantPhone, billFooterNote));
        TempData["Success"] = "POS settings saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleFeature(string featureKey, bool enabled)
    {
        var feature = await _context.FeatureFlags.FirstOrDefaultAsync(f => f.FeatureKey == featureKey);
        if (feature != null)
        {
            feature.IsEnabled = enabled;
            feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"{feature.Name} {(enabled ? "enabled" : "disabled")}.";
        }
        return RedirectToAction(nameof(Index));
    }
}
