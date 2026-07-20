using Microsoft.Extensions.Configuration;

namespace RestaurantERP.Application.Services;

public class OrderCalculationHelper
{
    private readonly IConfiguration _config;

    public OrderCalculationHelper(IConfiguration config) => _config = config;

    public decimal GetDiscountThreshold() => _config.GetValue("BusinessRules:DiscountThresholdAmount", 500m);
    public decimal GetDiscountPercentage() => _config.GetValue("BusinessRules:DiscountPercentage", 5m);
    public decimal GetCgstRate() => _config.GetValue("BusinessRules:CGST", 2.5m);
    public decimal GetSgstRate() => _config.GetValue("BusinessRules:SGST", 2.5m);
    public decimal GetDeliveryFee() => _config.GetValue("BusinessRules:DeliveryFee", 40m);

    public (decimal Discount, decimal Tax, decimal Total) Calculate(decimal subTotal, decimal couponDiscount = 0, bool applyAutoDiscount = true, bool includeDelivery = false)
    {
        var discount = couponDiscount;
        if (applyAutoDiscount && subTotal >= GetDiscountThreshold())
            discount += Math.Round(subTotal * GetDiscountPercentage() / 100, 2);

        var taxable = Math.Max(0, subTotal - discount);
        var tax = Math.Round(taxable * (GetCgstRate() + GetSgstRate()) / 100, 2);
        var delivery = includeDelivery ? GetDeliveryFee() : 0;
        var total = taxable + tax + delivery;
        return (discount, tax, total);
    }
}
