using RestaurantERP.Domain.Common;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Domain.Entities;

public class AppContentItem : BaseEntity
{
    public string ContentKey { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public AppContentType ContentType { get; set; } = AppContentType.Text;
    public string Section { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool IsSystem { get; set; } = true;
}
