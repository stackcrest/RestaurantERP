using RestaurantERP.Domain.Common;

namespace RestaurantERP.Domain.Entities;

public class KitchenStation : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
