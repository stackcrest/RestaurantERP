using RestaurantERP.Domain.Common;

namespace RestaurantERP.Domain.Entities;

public class MenuItemVariant : BaseEntity
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceAdjustment { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public MenuItem MenuItem { get; set; } = null!;
}
