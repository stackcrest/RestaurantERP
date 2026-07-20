using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class SettingsController : Controller
{
    private readonly IApplicationDbContext _context;

    public SettingsController(IApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var settings = await _context.ApplicationSettings.OrderBy(s => s.Group).ThenBy(s => s.Key).ToListAsync();
        var features = await _context.FeatureFlags.OrderBy(f => f.Name).ToListAsync();
        ViewBag.Features = features;
        return View(settings);
    }

    [HttpPost]
    public async Task<IActionResult> SaveSetting(string key, string value)
    {
        var setting = await _context.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting != null)
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleFeature(Guid id)
    {
        var feature = await _context.FeatureFlags.FindAsync(id);
        if (feature != null)
        {
            feature.IsEnabled = !feature.IsEnabled;
            feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> AddSetting(string key, string value, string? group, string? description)
    {
        var setting = new ApplicationSetting
        {
            Key = key,
            Value = value,
            Group = group ?? "General",
            Description = description
        };
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Setting added!";
        return RedirectToAction("Index");
    }
}
