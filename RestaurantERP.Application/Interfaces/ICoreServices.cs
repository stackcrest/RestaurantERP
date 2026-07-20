namespace RestaurantERP.Application.Interfaces;

public interface IPermissionService
{
    bool IsSuperAdmin(IEnumerable<string> roles);
    bool IsAdminOrAbove(IEnumerable<string> roles);
    bool CanAccess(string permission, IEnumerable<string> roles);
    Task<bool> UserCanReadSectionAsync(IEnumerable<string> roles, string sectionKey);
    Task<bool> UserCanWriteSectionAsync(IEnumerable<string> roles, string sectionKey);
    Task<Dictionary<string, (bool CanRead, bool CanWrite)>> GetUserSectionAccessAsync(IEnumerable<string> roles);
    Task<Dictionary<string, (bool CanRead, bool CanWrite)>> GetRoleSectionAccessAsync(string roleName);
    Task SaveRoleSectionAccessAsync(string roleName, Dictionary<string, (bool CanRead, bool CanWrite)> access);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
}

public interface ITokenService
{
    Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(Domain.Entities.ApplicationUser user, IEnumerable<string> roles);
    Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt, Guid UserId)?> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string refreshToken);
}

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string? entityId, string? oldValues = null, string? newValues = null);
}
