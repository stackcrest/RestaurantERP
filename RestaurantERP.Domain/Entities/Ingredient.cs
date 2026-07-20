using RestaurantERP.Domain.Common;

namespace RestaurantERP.Domain.Entities;

public class Ingredient : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "kg";
    public decimal CurrentStock { get; set; }
    public decimal LowStockThreshold { get; set; } = 10;
    public DateTime? ExpiryDate { get; set; }
    public decimal UnitCost { get; set; }
    public bool IsPerishable { get; set; }
}

public class Recipe : BaseEntity
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;

    public MenuItem MenuItem { get; set; } = null!;
    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
}

public class RecipeIngredient : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Guid IngredientId { get; set; }
    public decimal QuantityRequired { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public Ingredient Ingredient { get; set; } = null!;
}
