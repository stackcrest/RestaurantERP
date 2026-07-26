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
        string restaurantName, string tagline,
        string primaryColor, string secondaryColor, string accentColor,
        string backgroundColor, string surfaceColor,
        string textPrimary, string textSecondary,
        string fontFamily, string headingFontFamily,
        string borderRadius, string buttonStyle,
        string shadowIntensity, string motionStyle,
        string colorGrade, string heroStyle,
        bool enableAnimations, bool darkMode,
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

            void Apply(ThemeSetting t)
            {
                t.RestaurantName = restaurantName?.Trim();
                t.Tagline = tagline?.Trim();
                t.PrimaryColor = NormalizeColor(primaryColor, "#E63946");
                t.SecondaryColor = NormalizeColor(secondaryColor, "#1D3557");
                t.AccentColor = NormalizeColor(accentColor, "#F4A261");
                t.BackgroundColor = NormalizeColor(backgroundColor, "#FFFFFF");
                t.SurfaceColor = NormalizeColor(surfaceColor, "#F8F9FA");
                t.TextPrimary = NormalizeColor(textPrimary, "#212529");
                t.TextSecondary = NormalizeColor(textSecondary, "#6C757D");
                t.FontFamily = string.IsNullOrWhiteSpace(fontFamily)
                    ? "'Source Sans 3', system-ui, sans-serif"
                    : fontFamily.Trim();
                t.HeadingFontFamily = string.IsNullOrWhiteSpace(headingFontFamily)
                    ? "'Cormorant Garamond', Georgia, serif"
                    : headingFontFamily.Trim();
                t.BorderRadius = string.IsNullOrWhiteSpace(borderRadius) ? "12px" : borderRadius.Trim();
                t.ButtonStyle = NormalizeOption(buttonStyle, "rounded", "rounded", "soft", "pill", "square");
                t.ShadowIntensity = NormalizeOption(shadowIntensity, "medium", "none", "soft", "medium", "strong");
                t.MotionStyle = NormalizeOption(motionStyle, "subtle", "none", "subtle", "smooth", "lively");
                t.ColorGrade = NormalizeOption(colorGrade, "none", "none", "warm", "cool", "vintage", "vibrant");
                t.HeroStyle = NormalizeOption(heroStyle, "gradient", "gradient", "solid", "soft-glow");
                t.EnableAnimations = enableAnimations;
                t.DarkMode = darkMode;
                t.LogoUrl = resolvedLogo.Url;
                t.FaviconUrl = resolvedFavicon.Url;
                t.Version = newVersion;
                t.IsPublished = true;
                t.UpdatedAt = DateTime.UtcNow;
            }

            if (existing != null)
            {
                Apply(existing);
            }
            else
            {
                var theme = new ThemeSetting();
                Apply(theme);
                _context.ThemeSettings.Add(theme);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "UI theme published. Open the public site to see colors, fonts, and effects.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save theme settings");
            TempData["Error"] = "Could not save theme. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    private static string NormalizeColor(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string NormalizeOption(string? value, string fallback, params string[] allowed)
    {
        var v = (value ?? "").Trim().ToLowerInvariant();
        return allowed.Contains(v) ? v : fallback;
    }

    private async Task<(string? Url, string? Error)> ResolveMediaAsync(
        IFormFile? file, string? url, string? currentUrl)
    {
        if (file != null && file.Length > 0)
            return await _uploads.SaveImageAsync(file, "theme");

        if (!string.IsNullOrWhiteSpace(url))
            return (url.Trim(), null);

        return (currentUrl, null);
    }
}
