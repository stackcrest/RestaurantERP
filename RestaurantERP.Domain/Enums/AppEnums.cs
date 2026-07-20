namespace RestaurantERP.Domain.Enums;

public enum OrderStatus
{
    Placed,
    Confirmed,
    Preparing,
    Ready,
    OutForDelivery,
    Served,
    Delivered,
    Completed,
    Cancelled
}

public enum OrderSource
{
    Web,
    POS
}

public enum BillSource
{
    Offline,
    Online
}

public enum OrderType
{
    DineIn,
    Takeaway,
    Delivery
}

public enum PaymentMethod
{
    Cash,
    UPI,
    Card
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

public enum TableStatus
{
    Available,
    Occupied,
    Reserved,
    Cleaning
}

public enum ReservationStatus
{
    Pending,
    Confirmed,
    Cancelled,
    NoShow,
    Completed
}

public enum KitchenItemStatus
{
    Queued,
    Preparing,
    Ready
}

public enum PageStatus
{
    Draft,
    Review,
    Published
}

public enum PageVisibility
{
    All,
    Customer,
    Admin,
    Guest
}

public enum SettlementCycle
{
    Daily,
    Weekly,
    Monthly
}

public enum SettlementStatus
{
    Pending,
    Processed,
    Paid
}

public enum SpiceLevel
{
    Mild,
    Medium,
    Hot,
    ExtraHot
}

public enum BlockType
{
    Text,
    Image,
    Video,
    Cta,
    Faq,
    Gallery,
    MenuShowcase,
    Testimonials,
    Hero
}

public enum AppContentType
{
    Text,
    Html,
    Url,
    Image,
    File
}

public enum ContactInquiryStatus
{
    New,
    InProgress,
    Resolved,
    Closed
}
