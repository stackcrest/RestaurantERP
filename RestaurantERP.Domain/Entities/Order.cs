using RestaurantERP.Domain.Common;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid? TableId { get; set; }
    public OrderType OrderType { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Placed;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CouponCode { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryCity { get; set; }
    public string? DeliveryPinCode { get; set; }
    public string? DeliveryLandmark { get; set; }
    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }
    public double? DeliveryDistanceKm { get; set; }
    public string? Notes { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public OrderSource OrderSource { get; set; } = OrderSource.Web;

    public ApplicationUser User { get; set; } = null!;
    public RestaurantTable? Table { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<CommissionLedger> CommissionEntries { get; set; } = new List<CommissionLedger>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public KitchenItemStatus KitchenStatus { get; set; } = KitchenItemStatus.Queued;
    public Guid? KitchenStationId { get; set; }

    public Order Order { get; set; } = null!;
    public MenuItem MenuItem { get; set; } = null!;
    public KitchenStation? KitchenStation { get; set; }
    public ICollection<OrderItemAddOn> AddOns { get; set; } = new List<OrderItemAddOn>();
}

public class OrderItemAddOn : BaseEntity
{
    public Guid OrderItemId { get; set; }
    public Guid AddOnId { get; set; }
    public string AddOnName { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public OrderItem OrderItem { get; set; } = null!;
}

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public string? TransactionReference { get; set; }

    public Order Order { get; set; } = null!;
}
