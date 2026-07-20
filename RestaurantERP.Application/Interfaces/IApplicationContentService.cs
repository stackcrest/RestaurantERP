using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Interfaces;

public interface IApplicationContentService
{
    string Get(string key, string fallback = "");
    IReadOnlyDictionary<string, string> GetAll();
    IReadOnlyDictionary<string, AppContentType> GetAllTypes();
    Task<List<AppContentItem>> GetSectionItemsAsync(string section, CancellationToken ct = default);
    Task<List<AppContentItem>> GetAllItemsAsync(CancellationToken ct = default);
    Task<AppContentItem?> GetItemByKeyAsync(string key, CancellationToken ct = default);
    Task UpdateItemAsync(Guid id, string value, bool isPublished, CancellationToken ct = default);
    Task<AppContentItem> CreateItemAsync(string key, string value, AppContentType type, string section, string label, string? description, CancellationToken ct = default);
    Task DeleteItemAsync(Guid id, CancellationToken ct = default);
    Task<List<NavigationMenu>> GetNavigationMenusAsync(CancellationToken ct = default);
    Task SaveNavigationItemAsync(Guid? id, Guid menuId, string label, string url, string? icon, int displayOrder, bool isActive, bool openInNewTab, CancellationToken ct = default);
    Task DeleteNavigationItemAsync(Guid id, CancellationToken ct = default);
    Task<ThemeSetting?> GetPublishedThemeAsync(CancellationToken ct = default);
    void InvalidateCache();
}
