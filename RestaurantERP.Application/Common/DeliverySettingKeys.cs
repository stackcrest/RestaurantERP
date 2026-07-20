namespace RestaurantERP.Application.Common;

public static class DeliverySettingKeys
{
    public const string RestaurantLatitude = "Delivery.RestaurantLatitude";
    public const string RestaurantLongitude = "Delivery.RestaurantLongitude";
    public const string RadiusKm = "Delivery.RadiusKm";
    public const string RestaurantAddress = "Delivery.RestaurantAddress";

    public static readonly string[] All =
    [
        RestaurantLatitude,
        RestaurantLongitude,
        RadiusKm,
        RestaurantAddress
    ];
}

public record DeliveryConfig(double RestaurantLatitude, double RestaurantLongitude, double RadiusKm, string? RestaurantAddress);
