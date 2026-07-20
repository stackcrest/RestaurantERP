using RestaurantERP.Domain.Common;

namespace RestaurantERP.Domain.Entities;

public class CartItem : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid? VariantId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? AddOnIds { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public MenuItem MenuItem { get; set; } = null!;
    public MenuItemVariant? Variant { get; set; }
}

public class WishlistItem : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid MenuItemId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public MenuItem MenuItem { get; set; } = null!;
}
