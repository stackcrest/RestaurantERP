using RestaurantERP.Application.Common;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Interfaces;

public class PosCartItemDto
{
    public Guid MenuItemId { get; set; }
    public Guid? VariantId { get; set; }
    public int Quantity { get; set; } = 1;
    public List<Guid>? AddOnIds { get; set; }
}

public class PosCheckoutRequest
{
    public OrderType OrderType { get; set; } = OrderType.Takeaway;
    public Guid? TableId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? Notes { get; set; }
    public List<PosCartItemDto> Items { get; set; } = new();
}

public class BillPrintDto
{
    public Guid Id { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public BillSource Source { get; set; }
    public OrderType OrderType { get; set; }
    public string? RestaurantName { get; set; }
    public string? RestaurantAddress { get; set; }
    public string? RestaurantPhone { get; set; }
    public string? Gstin { get; set; }
    public string? BillFooterNote { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CashierName { get; set; }
    public string? TableNumber { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime BillDate { get; set; }
    public string? Notes { get; set; }
    public List<BillLineItemDto> Items { get; set; } = new();
}

public class BillLineItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public List<string> AddOns { get; set; } = new();
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public interface IPosBillingService
{
    Task<PosFeatureState> GetFeatureStateAsync();
    Task<PosConfig> GetConfigAsync();
    Task SaveConfigAsync(PosConfig config);
    Task<bool> IsPosEnabledAsync();
    Task<(bool Success, string Message, Guid? BillId)> CreateOfflineBillAsync(PosCheckoutRequest request, Guid userId, string userName);
    Task<(bool Success, string Message, Guid? BillId)> GenerateOnlineBillAsync(Guid orderId, Guid userId, string userName);
    Task<BillPrintDto?> GetBillPrintAsync(Guid billId, bool incrementPrintCount = false);
}
