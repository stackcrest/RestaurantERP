using RestaurantERP.Domain.Common;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Domain.Entities;

public class MenuItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsVeg { get; set; }
    public SpiceLevel SpiceLevel { get; set; } = SpiceLevel.Mild;
    public string? Allergens { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsFeatured { get; set; }
    public int PreparationTimeMinutes { get; set; } = 15;
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public Guid CategoryId { get; set; }

    public Category Category { get; set; } = null!;
    public ICollection<MenuItemVariant> Variants { get; set; } = new List<MenuItemVariant>();
    public ICollection<MenuItemAddOn> MenuItemAddOns { get; set; } = new List<MenuItemAddOn>();
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
