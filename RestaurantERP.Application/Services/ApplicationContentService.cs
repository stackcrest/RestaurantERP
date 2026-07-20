using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Services;

public class ApplicationContentService : IApplicationContentService
{
    private readonly IApplicationDbContext _context;
    private Dictionary<string, string>? _content;
    private Dictionary<string, AppContentType>? _types;

    public ApplicationContentService(IApplicationDbContext context)
    {
        _context = context;
    }

    public string Get(string key, string fallback = "")
    {
        EnsureLoaded();
        return _content!.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : fallback;
    }

    public IReadOnlyDictionary<string, string> GetAll()
    {
        EnsureLoaded();
        return _content!;
    }

    public IReadOnlyDictionary<string, AppContentType> GetAllTypes()
    {
        EnsureLoaded();
        return _types!;
    }

    public async Task<List<AppContentItem>> GetSectionItemsAsync(string section, CancellationToken ct = default) =>
        await _context.AppContentItems
            .Where(c => !c.IsDeleted && c.Section == section)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Label)
            .ToListAsync(ct);

    public async Task<List<AppContentItem>> GetAllItemsAsync(CancellationToken ct = default) =>
        await _context.AppContentItems
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Section).ThenBy(c => c.SortOrder).ThenBy(c => c.Label)
            .ToListAsync(ct);

    public async Task<AppContentItem?> GetItemByKeyAsync(string key, CancellationToken ct = default) =>
        await _context.AppContentItems.FirstOrDefaultAsync(c => !c.IsDeleted && c.ContentKey == key, ct);

    public async Task UpdateItemAsync(Guid id, string value, bool isPublished, CancellationToken ct = default)
    {
        var item = await _context.AppContentItems.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct)
            ?? throw new InvalidOperationException("Content item not found.");

        item.Value = value;
        item.IsPublished = isPublished;
        await _context.SaveChangesAsync(ct);
        InvalidateCache();
    }

    public async Task<AppContentItem> CreateItemAsync(string key, string value, AppContentType type, string section, string label, string? description, CancellationToken ct = default)
    {
        if (await _context.AppContentItems.AnyAsync(c => !c.IsDeleted && c.ContentKey == key, ct))
            throw new InvalidOperationException($"Content key '{key}' already exists.");

        var maxOrder = await _context.AppContentItems
            .Where(c => !c.IsDeleted && c.Section == section)
            .Select(c => (int?)c.SortOrder)
            .MaxAsync(ct) ?? 0;

        var item = new AppContentItem
        {
            ContentKey = key.Trim(),
            Value = value,
            ContentType = type,
            Section = section,
            Label = label,
            Description = description,
            SortOrder = maxOrder + 1,
            IsPublished = true,
            IsSystem = false
        };

        _context.AppContentItems.Add(item);
        await _context.SaveChangesAsync(ct);
        InvalidateCache();
        return item;
    }

    public async Task DeleteItemAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _context.AppContentItems.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct)
            ?? throw new InvalidOperationException("Content item not found.");

        if (item.IsSystem)
            throw new InvalidOperationException("System content items cannot be deleted.");

        item.IsDeleted = true;
        await _context.SaveChangesAsync(ct);
        InvalidateCache();
    }

    public async Task<List<NavigationMenu>> GetNavigationMenusAsync(CancellationToken ct = default) =>
        await _context.NavigationMenus
            .Include(m => m.Items.Where(i => !i.IsDeleted))
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.Name)
            .ToListAsync(ct);

    public async Task SaveNavigationItemAsync(Guid? id, Guid menuId, string label, string url, string? icon, int displayOrder, bool isActive, bool openInNewTab, CancellationToken ct = default)
    {
        if (id.HasValue)
        {
            var existing = await _context.NavigationMenuItems.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, ct)
                ?? throw new InvalidOperationException("Navigation item not found.");
            existing.Label = label;
            existing.Url = url;
            existing.Icon = icon;
            existing.DisplayOrder = displayOrder;
            existing.IsActive = isActive;
            existing.OpenInNewTab = openInNewTab;
        }
        else
        {
            _context.NavigationMenuItems.Add(new NavigationMenuItem
            {
                MenuId = menuId,
                Label = label,
                Url = url,
                Icon = icon,
                DisplayOrder = displayOrder,
                IsActive = isActive,
                OpenInNewTab = openInNewTab
            });
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteNavigationItemAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _context.NavigationMenuItems.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, ct)
            ?? throw new InvalidOperationException("Navigation item not found.");
        item.IsDeleted = true;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<ThemeSetting?> GetPublishedThemeAsync(CancellationToken ct = default) =>
        await _context.ThemeSettings
            .Where(t => t.IsPublished && !t.IsDeleted)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(ct);

    public void InvalidateCache()
    {
        _content = null;
        _types = null;
    }

    private void EnsureLoaded()
    {
        if (_content != null) return;

        var items = _context.AppContentItems
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.IsPublished)
            .ToList();

        _content = items.ToDictionary(c => c.ContentKey, c => c.Value);
        _types = items.ToDictionary(c => c.ContentKey, c => c.ContentType);
    }
}
