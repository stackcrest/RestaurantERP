using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Web.Services;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class ThemeController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly IUploadStorageService _uploads;
    private readonly ILogger<ThemeController> _logger;

    public ThemeController(
        IApplicationDbContext context,
        IUploadStorageService uploads,
        ILogger<ThemeController> logger)
    {
        _context = context;
        _uploads = uploads;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var theme = await _context.ThemeSettings
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync() ?? new ThemeSetting();
        return View(theme);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    public async Task<IActionResult> Save(
        string restaurantName, string tagline, string primaryColor,
        string secondaryColor, string accentColor, string surfaceColor,
        string? logoUrl, string? faviconUrl,
        IFormFile? logoFile, IFormFile? faviconFile)
    {
        try
        {
            var existing = await _context.ThemeSettings
                .Where(t => !t.IsDeleted)
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync();

            var newVersion = (existing?.Version ?? 0) + 1;

            var resolvedLogo = await ResolveMediaAsync(logoFile, logoUrl, existing?.LogoUrl);
            if (resolvedLogo.Error != null)
            {
                TempData["Error"] = resolvedLogo.Error;
                return RedirectToAction(nameof(Index));
            }

            var resolvedFavicon = await ResolveMediaAsync(faviconFile, faviconUrl, existing?.FaviconUrl);
            if (resolvedFavicon.Error != null)
            {
                TempData["Error"] = resolvedFavicon.Error;
                return RedirectToAction(nameof(Index));
            }

            // Update the latest row in place so the admin page always reloads the saved photos immediately.
            if (existing != null)
            {
                existing.RestaurantName = restaurantName?.Trim();
                existing.Tagline = tagline?.Trim();
                existing.PrimaryColor = primaryColor;
                existing.SecondaryColor = secondaryColor;
                existing.AccentColor = accentColor;
                existing.SurfaceColor = surfaceColor;
                existing.LogoUrl = resolvedLogo.Url;
                existing.FaviconUrl = resolvedFavicon.Url;
                existing.Version = newVersion;
                existing.IsPublished = true;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ThemeSettings.Add(new ThemeSetting
                {
                    RestaurantName = restaurantName?.Trim(),
                    Tagline = tagline?.Trim(),
                    PrimaryColor = primaryColor,
                    SecondaryColor = secondaryColor,
                    AccentColor = accentColor,
                    SurfaceColor = surfaceColor,
                    LogoUrl = resolvedLogo.Url,
                    FaviconUrl = resolvedFavicon.Url,
                    Version = newVersion,
                    IsPublished = true
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = string.IsNullOrEmpty(resolvedLogo.Url) && logoFile == null
                ? "Theme saved and published!"
                : "Theme saved! Logo/favicon updated.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save theme settings");
            TempData["Error"] = "Could not save theme. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<(string? Url, string? Error)> ResolveMediaAsync(
        IFormFile? file, string? url, string? currentUrl)
    {
        // Prefer newly uploaded file over URL text.
        if (file != null && file.Length > 0)
            return await _uploads.SaveImageAsync(file, "theme");

        if (!string.IsNullOrWhiteSpace(url))
            return (url.Trim(), null);

        return (currentUrl, null);
    }
}
