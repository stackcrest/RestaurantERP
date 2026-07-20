namespace RestaurantERP.Application.DTOs.Public;

public class ThemeDto
{
    public string PrimaryColor { get; set; } = "#E63946";
    public string SecondaryColor { get; set; } = "#1D3557";
    public string AccentColor { get; set; } = "#F4A261";
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string SurfaceColor { get; set; } = "#F8F9FA";
    public string TextPrimary { get; set; } = "#212529";
    public string TextSecondary { get; set; } = "#6C757D";
    public string FontFamily { get; set; } = "Inter, system-ui, sans-serif";
    public string BorderRadius { get; set; } = "12px";
    public string ButtonStyle { get; set; } = "rounded";
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public bool DarkMode { get; set; }
    public string? RestaurantName { get; set; }
    public string? Tagline { get; set; }
}

public class NavigationMenuDto
{
    public string MenuKey { get; set; } = string.Empty;
    public List<NavigationItemDto> Items { get; set; } = new();
}

public class NavigationItemDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public List<NavigationItemDto> Children { get; set; } = new();
}

public class PublicPageDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public List<PageBlockDto> Blocks { get; set; } = new();
}

public class PageBlockDto
{
    public Guid Id { get; set; }
    public string BlockType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? LinkText { get; set; }
    public string? ConfigJson { get; set; }
    public int DisplayOrder { get; set; }
}
