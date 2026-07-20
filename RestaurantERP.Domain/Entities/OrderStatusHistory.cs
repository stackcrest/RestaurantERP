using RestaurantERP.Domain.Common;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Domain.Entities;

public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public string? Note { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? UpdatedByName { get; set; }

    public Order Order { get; set; } = null!;
}
