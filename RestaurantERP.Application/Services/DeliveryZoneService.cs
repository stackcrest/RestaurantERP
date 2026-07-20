using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.Application.Services;

public class DeliveryZoneService : IDeliveryZoneService
{
    private readonly IApplicationDbContext _context;

    public DeliveryZoneService(IApplicationDbContext context) => _context = context;

    public async Task<DeliveryConfig> GetConfigAsync()
    {
        var settings = await _context.ApplicationSettings
            .Where(s => DeliverySettingKeys.All.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        return new DeliveryConfig(
            ParseDouble(settings, DeliverySettingKeys.RestaurantLatitude, 28.6139),
            ParseDouble(settings, DeliverySettingKeys.RestaurantLongitude, 77.2090),
            ParseDouble(settings, DeliverySettingKeys.RadiusKm, 5),
            settings.GetValueOrDefault(DeliverySettingKeys.RestaurantAddress));
    }

    public async Task SaveConfigAsync(double latitude, double longitude, double radiusKm, string? restaurantAddress)
    {
        await UpsertAsync(DeliverySettingKeys.RestaurantLatitude, latitude.ToString("F6"), "Delivery");
        await UpsertAsync(DeliverySettingKeys.RestaurantLongitude, longitude.ToString("F6"), "Delivery");
        await UpsertAsync(DeliverySettingKeys.RadiusKm, radiusKm.ToString("F1"), "Delivery");
        await UpsertAsync(DeliverySettingKeys.RestaurantAddress, restaurantAddress ?? "", "Delivery");
        await _context.SaveChangesAsync();
    }

    public async Task<(double DistanceKm, bool CanDeliver, string Message)> ValidateDeliveryAsync(double userLatitude, double userLongitude)
    {
        var config = await GetConfigAsync();
        var distance = GeoHelper.GetDistanceKm(config.RestaurantLatitude, config.RestaurantLongitude, userLatitude, userLongitude);
        var canDeliver = distance <= config.RadiusKm;

        var message = canDeliver
            ? $"Great! You're {distance:F1} km away — we deliver to your location."
            : $"Unable to deliver to your location. You are {distance:F1} km away. We only deliver within {config.RadiusKm:F1} km.";

        return (distance, canDeliver, message);
    }

    private async Task UpsertAsync(string key, string value, string group)
    {
        var setting = await _context.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            _context.ApplicationSettings.Add(new Domain.Entities.ApplicationSetting
            {
                Key = key,
                Value = value,
                Group = group,
                IsPublic = true
            });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static double ParseDouble(Dictionary<string, string> settings, string key, double fallback) =>
        settings.TryGetValue(key, out var value) && double.TryParse(value, out var parsed) ? parsed : fallback;
}
