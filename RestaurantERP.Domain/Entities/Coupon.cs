using RestaurantERP.Domain.Common;

namespace RestaurantERP.Domain.Entities;

public class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Review : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid? OrderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public MenuItem MenuItem { get; set; } = null!;
}
