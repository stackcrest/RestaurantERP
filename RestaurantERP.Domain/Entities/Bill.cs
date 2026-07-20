using RestaurantERP.Domain.Common;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Domain.Entities;

public class Bill : BaseEntity
{
    public string BillNumber { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public BillSource Source { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public Guid? GeneratedByUserId { get; set; }
    public string? GeneratedByName { get; set; }
    public int PrintCount { get; set; }
    public string? Notes { get; set; }

    public Order Order { get; set; } = null!;
}
