using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly IApplicationDbContext _context;

    private static readonly HashSet<string> AdminPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        "menu.manage", "orders.manage", "inventory.manage", "reports.view",
        "tables.manage", "reservations.manage", "kitchen.manage", "coupons.manage"
    };

    private static readonly HashSet<string> CustomerPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        "menu.view", "cart.manage", "orders.place", "wishlist.manage", "reviews.create"
    };

    public PermissionService(IApplicationDbContext context) => _context = context;

    public bool IsSuperAdmin(IEnumerable<string> roles) =>
        roles.Any(r => r.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));

    public bool IsAdminOrAbove(IEnumerable<string> roles) =>
        IsSuperAdmin(roles) || roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase));

    public bool CanAccess(string permission, IEnumerable<string> roles)
    {
        if (IsSuperAdmin(roles)) return true;
        if (IsAdminOrAbove(roles) && AdminPermissions.Contains(permission)) return true;
        return CustomerPermissions.Contains(permission);
    }

    public async Task<bool> UserCanReadSectionAsync(IEnumerable<string> roles, string sectionKey)
    {
        if (IsSuperAdmin(roles)) return true;

        foreach (var role in roles)
        {
            if (await RoleHasReadAccessAsync(role, sectionKey))
                return true;
        }

        return false;
    }

    public async Task<bool> UserCanWriteSectionAsync(IEnumerable<string> roles, string sectionKey)
    {
        if (IsSuperAdmin(roles)) return true;

        foreach (var role in roles)
        {
            if (await RoleHasWriteAccessAsync(role, sectionKey))
                return true;
        }

        return false;
    }

    public async Task<Dictionary<string, (bool CanRead, bool CanWrite)>> GetUserSectionAccessAsync(IEnumerable<string> roles)
    {
        var roleList = roles.ToList();
        if (IsSuperAdmin(roleList))
            return AdminMenuSections.All.ToDictionary(s => s.Key, _ => (true, true));

        var merged = AdminMenuSections.All.ToDictionary(s => s.Key, _ => (CanRead: false, CanWrite: false));

        foreach (var role in roleList)
        {
            var roleAccess = await GetRoleSectionAccessAsync(role);
            foreach (var (key, access) in roleAccess)
            {
                merged[key] = (merged[key].CanRead || access.CanRead, merged[key].CanWrite || access.CanWrite);
            }
        }

        return merged;
    }

    public async Task<Dictionary<string, (bool CanRead, bool CanWrite)>> GetRoleSectionAccessAsync(string roleName)
    {
        var entries = await _context.RoleMenuAccesses
            .Where(a => a.RoleName == roleName)
            .ToListAsync();

        var result = AdminMenuSections.All.ToDictionary(s => s.Key, _ => (false, false));

        foreach (var entry in entries)
        {
            if (result.ContainsKey(entry.SectionKey))
                result[entry.SectionKey] = (entry.CanRead, entry.CanWrite);
        }

        return result;
    }

    public async Task SaveRoleSectionAccessAsync(string roleName, Dictionary<string, (bool CanRead, bool CanWrite)> access)
    {
        var existing = await _context.RoleMenuAccesses
            .Where(a => a.RoleName == roleName)
            .ToListAsync();

        _context.RoleMenuAccesses.RemoveRange(existing);

        foreach (var section in AdminMenuSections.All)
        {
            access.TryGetValue(section.Key, out var rights);
            _context.RoleMenuAccesses.Add(new RoleMenuAccess
            {
                RoleName = roleName,
                SectionKey = section.Key,
                CanRead = rights.CanRead,
                CanWrite = rights.CanWrite
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task<bool> RoleHasReadAccessAsync(string roleName, string sectionKey)
    {
        if (roleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return true;

        var entry = await _context.RoleMenuAccesses
            .FirstOrDefaultAsync(a => a.RoleName == roleName && a.SectionKey == sectionKey);

        return entry?.CanRead ?? false;
    }

    private async Task<bool> RoleHasWriteAccessAsync(string roleName, string sectionKey)
    {
        if (roleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return true;

        var entry = await _context.RoleMenuAccesses
            .FirstOrDefaultAsync(a => a.RoleName == roleName && a.SectionKey == sectionKey);

        return entry?.CanWrite ?? false;
    }
}
