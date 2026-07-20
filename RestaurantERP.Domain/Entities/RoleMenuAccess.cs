using RestaurantERP.Domain.Common;

namespace RestaurantERP.Domain.Entities;

public class RoleMenuAccess : BaseEntity
{
    public string RoleName { get; set; } = string.Empty;
    public string SectionKey { get; set; } = string.Empty;
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
}
