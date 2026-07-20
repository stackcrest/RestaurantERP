using RestaurantERP.Domain.Common;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Domain.Entities;

public class BlogPost : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? FeaturedImageUrl { get; set; }
    public PageStatus Status { get; set; } = PageStatus.Draft;
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public string? MetaDescription { get; set; }
    public string? Tags { get; set; }

    public ApplicationUser Author { get; set; } = null!;
}
