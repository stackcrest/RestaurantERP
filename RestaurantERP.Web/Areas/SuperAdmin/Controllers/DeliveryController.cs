using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class DeliveryController : Controller
{
    private readonly IDeliveryZoneService _deliveryZoneService;

    public DeliveryController(IDeliveryZoneService deliveryZoneService) => _deliveryZoneService = deliveryZoneService;

    public async Task<IActionResult> Index()
    {
        ViewBag.Config = await _deliveryZoneService.GetConfigAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Save(double latitude, double longitude, double radiusKm, string? restaurantAddress)
    {
        if (radiusKm <= 0 || radiusKm > 100)
        {
            TempData["Error"] = "Delivery radius must be between 0.1 and 100 km.";
            return RedirectToAction(nameof(Index));
        }

        await _deliveryZoneService.SaveConfigAsync(latitude, longitude, radiusKm, restaurantAddress);
        TempData["Success"] = $"Delivery zone updated. Radius set to {radiusKm} km.";
        return RedirectToAction(nameof(Index));
    }
}
