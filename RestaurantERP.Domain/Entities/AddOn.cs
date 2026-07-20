using RestaurantERP.Domain.Common;

namespace RestaurantERP.Domain.Entities;

public class AddOn : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MenuItemAddOn> MenuItemAddOns { get; set; } = new List<MenuItemAddOn>();
}

public class MenuItemAddOn : BaseEntity
{
    public Guid MenuItemId { get; set; }
    public Guid AddOnId { get; set; }

    public MenuItem MenuItem { get; set; } = null!;
    public AddOn AddOn { get; set; } = null!;
}
