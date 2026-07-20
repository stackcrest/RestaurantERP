using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.DTOs.Public;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Services;

public class PublicService : IPublicService
{
    private readonly IApplicationDbContext _context;

    public PublicService(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<ThemeDto>> GetThemeAsync()
    {
        var theme = await _context.ThemeSettings
            .Where(t => t.IsPublished).OrderByDescending(t => t.Version).FirstOrDefaultAsync();

        if (theme == null) return ApiResponse<ThemeDto>.Ok(new ThemeDto());
        return ApiResponse<ThemeDto>.Ok(MapTheme(theme));
    }

    public async Task<ApiResponse<Dictionary<string, object>>> GetPublicSettingsAsync()
    {
        var settings = await _context.ApplicationSettings
            .Where(s => s.IsPublic).ToListAsync();

        var dict = settings.ToDictionary(s => s.Key, s => (object)s.Value);
        dict["appName"] = settings.FirstOrDefault(s => s.Key == "AppName")?.Value ?? "RestaurantERP";
        dict["currency"] = settings.FirstOrDefault(s => s.Key == "Currency")?.Value ?? "INR";
        return ApiResponse<Dictionary<string, object>>.Ok(dict);
    }

    public async Task<ApiResponse<List<NavigationMenuDto>>> GetNavigationAsync(string menuKey)
    {
        var menu = await _context.NavigationMenus
            .Include(m => m.Items.Where(i => i.IsActive && i.ParentId == null))
            .ThenInclude(i => i.Children.Where(c => c.IsActive))
            .FirstOrDefaultAsync(m => m.MenuKey == menuKey && m.IsActive);

        if (menu == null) return ApiResponse<List<NavigationMenuDto>>.Ok(new());

        var dto = new NavigationMenuDto
        {
            MenuKey = menu.MenuKey,
            Items = menu.Items.OrderBy(i => i.DisplayOrder).Select(MapNavItem).ToList()
        };
        return ApiResponse<List<NavigationMenuDto>>.Ok(new List<NavigationMenuDto> { dto });
    }

    public async Task<ApiResponse<PublicPageDto>> GetPageBySlugAsync(string slug)
    {
        var page = await _context.UiPages
            .Include(p => p.Blocks.Where(b => b.IsActive))
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == PageStatus.Published);

        if (page == null) return ApiResponse<PublicPageDto>.Fail("Page not found.");
        return ApiResponse<PublicPageDto>.Ok(MapPage(page));
    }

    public async Task<ApiResponse<PublicPageDto>> GetHomepageAsync()
    {
        var page = await _context.UiPages
            .Include(p => p.Blocks.Where(b => b.IsActive))
            .FirstOrDefaultAsync(p => p.IsHomePage && p.Status == PageStatus.Published);

        if (page == null) return ApiResponse<PublicPageDto>.Fail("Homepage not configured.");
        return ApiResponse<PublicPageDto>.Ok(MapPage(page));
    }

    private static ThemeDto MapTheme(Domain.Entities.ThemeSetting t) => new()
    {
        PrimaryColor = t.PrimaryColor, SecondaryColor = t.SecondaryColor, AccentColor = t.AccentColor,
        BackgroundColor = t.BackgroundColor, SurfaceColor = t.SurfaceColor,
        TextPrimary = t.TextPrimary, TextSecondary = t.TextSecondary,
        FontFamily = t.FontFamily, BorderRadius = t.BorderRadius, ButtonStyle = t.ButtonStyle,
        LogoUrl = t.LogoUrl, FaviconUrl = t.FaviconUrl, DarkMode = t.DarkMode,
        RestaurantName = t.RestaurantName, Tagline = t.Tagline
    };

    private static NavigationItemDto MapNavItem(Domain.Entities.NavigationMenuItem i) => new()
    {
        Id = i.Id, Label = i.Label, Url = i.Url, Icon = i.Icon,
        Children = i.Children.OrderBy(c => c.DisplayOrder).Select(MapNavItem).ToList()
    };

    private static PublicPageDto MapPage(Domain.Entities.UiPage p) => new()
    {
        Id = p.Id, Title = p.Title, Slug = p.Slug,
        MetaTitle = p.MetaTitle, MetaDescription = p.MetaDescription,
        Blocks = p.Blocks.OrderBy(b => b.DisplayOrder).Select(b => new PageBlockDto
        {
            Id = b.Id, BlockType = b.BlockType.ToString(), Title = b.Title,
            Content = b.Content, ImageUrl = b.ImageUrl, LinkUrl = b.LinkUrl,
            LinkText = b.LinkText, ConfigJson = b.ConfigJson, DisplayOrder = b.DisplayOrder
        }).ToList()
    };
}
