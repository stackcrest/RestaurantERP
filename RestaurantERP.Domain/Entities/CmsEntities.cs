using RestaurantERP.Domain.Common;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Domain.Entities;

public class UiPage : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public PageStatus Status { get; set; } = PageStatus.Draft;
    public PageVisibility Visibility { get; set; } = PageVisibility.All;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? OgImageUrl { get; set; }
    public bool IsHomePage { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<UiPageBlock> Blocks { get; set; } = new List<UiPageBlock>();
}

public class UiPageBlock : BaseEntity
{
    public Guid PageId { get; set; }
    public BlockType BlockType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? LinkText { get; set; }
    public string? ConfigJson { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public UiPage Page { get; set; } = null!;
}

public class NavigationMenu : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string MenuKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<NavigationMenuItem> Items { get; set; } = new List<NavigationMenuItem>();
}

public class NavigationMenuItem : BaseEntity
{
    public Guid MenuId { get; set; }
    public Guid? ParentId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public PageVisibility Visibility { get; set; } = PageVisibility.All;
    public bool IsActive { get; set; } = true;
    public bool OpenInNewTab { get; set; }

    public NavigationMenu Menu { get; set; } = null!;
    public NavigationMenuItem? Parent { get; set; }
    public ICollection<NavigationMenuItem> Children { get; set; } = new List<NavigationMenuItem>();
}

public class FeatureFlag : BaseEntity
{
    public string FeatureKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? ConfigJson { get; set; }
}

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
}

public class PageRequest : BaseEntity
{
    public Guid RequestedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PageStatus Status { get; set; } = PageStatus.Draft;
    public string? AdminNotes { get; set; }
    public Guid? ResultingPageId { get; set; }

    public ApplicationUser RequestedBy { get; set; } = null!;
}
