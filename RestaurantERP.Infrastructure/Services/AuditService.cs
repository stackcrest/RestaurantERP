using Microsoft.AspNetCore.Http;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(IApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string entityType, string? entityId, string? oldValues = null, string? newValues = null)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = user?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "system";

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId != null ? Guid.Parse(userId) : null,
            UserEmail = email,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
        await _context.SaveChangesAsync();
    }
}
