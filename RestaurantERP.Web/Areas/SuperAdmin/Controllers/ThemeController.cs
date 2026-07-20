using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class ThemeController : Controller
{
    private readonly IApplicationDbContext _context;

    public ThemeController(IApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var theme = await _context.ThemeSettings
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync() ?? new ThemeSetting();
        return View(theme);
    }

    [HttpPost]
    public async Task<IActionResult> Save(string restaurantName, string tagline, string primaryColor,
        string secondaryColor, string accentColor, string surfaceColor, string logoUrl, string faviconUrl)
    {
        var existing = await _context.ThemeSettings.OrderByDescending(t => t.Version).FirstOrDefaultAsync();
        var newVersion = (existing?.Version ?? 0) + 1;

        var theme = new ThemeSetting
        {
            RestaurantName = restaurantName,
            Tagline = tagline,
            PrimaryColor = primaryColor,
            SecondaryColor = secondaryColor,
            AccentColor = accentColor,
            SurfaceColor = surfaceColor,
            LogoUrl = logoUrl,
            FaviconUrl = faviconUrl,
            Version = newVersion,
            IsPublished = true
        };

        _context.ThemeSettings.Add(theme);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Theme saved and published!";
        return RedirectToAction("Index");
    }
}
