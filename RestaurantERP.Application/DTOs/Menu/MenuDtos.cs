namespace RestaurantERP.Application.DTOs.Menu;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public int ItemCount { get; set; }
}

public class MenuItemListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsVeg { get; set; }
    public string SpiceLevel { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public bool IsFeatured { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int PreparationTimeMinutes { get; set; }
}

public class MenuItemDetailDto : MenuItemListDto
{
    public string? Allergens { get; set; }
    public List<VariantDto> Variants { get; set; } = new();
    public List<AddOnDto> AddOns { get; set; } = new();
}

public class VariantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceAdjustment { get; set; }
    public bool IsDefault { get; set; }
    public decimal TotalPrice { get; set; }
}

public class AddOnDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class MenuFilterDto : Common.PagedRequest
{
    public Guid? CategoryId { get; set; }
    public string? Search { get; set; }
    public bool? IsVeg { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? IsAvailable { get; set; }
}

public class CreateMenuItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsVeg { get; set; }
    public string SpiceLevel { get; set; } = "Mild";
    public Guid CategoryId { get; set; }
    public bool IsFeatured { get; set; }
    public int PreparationTimeMinutes { get; set; } = 15;
}

public class UpdateMenuItemDto : CreateMenuItemDto
{
    public bool IsAvailable { get; set; } = true;
}
