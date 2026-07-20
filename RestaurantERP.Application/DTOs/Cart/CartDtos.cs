namespace RestaurantERP.Application.DTOs.Cart;

public class AddToCartDto
{
    public Guid MenuItemId { get; set; }
    public Guid? VariantId { get; set; }
    public int Quantity { get; set; } = 1;
    public List<Guid>? AddOnIds { get; set; }
}

public class UpdateCartItemDto
{
    public Guid CartItemId { get; set; }
    public int Quantity { get; set; }
}

public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public List<string> AddOnNames { get; set; } = new();
}

public class CartDto
{
    public List<CartItemDto> Items { get; set; } = new();
    public int ItemCount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool DiscountEligible { get; set; }
    public string? DiscountMessage { get; set; }
}

public class CartSummaryDto
{
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
}
