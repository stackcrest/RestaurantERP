using RestaurantERP.Application.Common;

namespace RestaurantERP.Application.Interfaces;

public interface IDeliveryZoneService
{
    Task<DeliveryConfig> GetConfigAsync();
    Task SaveConfigAsync(double latitude, double longitude, double radiusKm, string? restaurantAddress);
    Task<(double DistanceKm, bool CanDeliver, string Message)> ValidateDeliveryAsync(double userLatitude, double userLongitude);
}
