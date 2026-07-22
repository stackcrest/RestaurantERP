namespace RestaurantERP.Application.DTOs.Orders;

public class PlaceOrderDto
{
    public string OrderType { get; set; } = "Takeaway";
    public Guid? TableId { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? CouponCode { get; set; }
}

public class OrderListDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CustomerName { get; set; }
}

public class OrderDetailDto : OrderListDto
{
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal DeliveryFee { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
    public string? TableNumber { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public List<OrderStatusStepDto> StatusTimeline { get; set; } = new();
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string KitchenStatus { get; set; } = string.Empty;
    public List<string> AddOns { get; set; } = new();
}

public class OrderStatusStepDto
{
    public string Status { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsCurrent { get; set; }
}

public class CreateReservationDto
{
    public Guid? TableId { get; set; }
    public DateTime ReservationDate { get; set; }
    public string ReservationTime { get; set; } = string.Empty;
    public int PartySize { get; set; }
    public string? SpecialRequests { get; set; }
}

public class ReservationDto
{
    public Guid Id { get; set; }
    public DateTime ReservationDate { get; set; }
    public string ReservationTime { get; set; } = string.Empty;
    public int PartySize { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TableNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? SpecialRequests { get; set; }
}

public class TableDto
{
    public Guid Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
}

public class CreateCouponDto
{
    public string Code { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Subtitle { get; set; }
    public string? BadgeText { get; set; }
    public string? CtaText { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int? UsageLimit { get; set; }
    public bool ShowOnHomepage { get; set; }
    public bool FirstOrderOnly { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }
}

public class CouponDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Subtitle { get; set; }
    public string? BadgeText { get; set; }
    public string? CtaText { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
    public bool ShowOnHomepage { get; set; }
    public bool FirstOrderOnly { get; set; }
    public int Priority { get; set; }
    public decimal? CalculatedDiscount { get; set; }
    public bool IsCurrentlyValid { get; set; }
}

public class CreateReviewDto
{
    public Guid MenuItemId { get; set; }
    public Guid? OrderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class ReviewDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DashboardSummaryDto
{
    public decimal TodaySales { get; set; }
    public int TodayOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int PendingOrders { get; set; }
    public int LowStockItems { get; set; }
    public int ActiveTables { get; set; }
    public int TotalTables { get; set; }
    public List<TopItemDto> TopItems { get; set; } = new();
}

public class TopItemDto
{
    public string Name { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}
