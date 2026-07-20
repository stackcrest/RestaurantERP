using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.DTOs.Cart;
using RestaurantERP.Application.DTOs.Menu;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.Application.Services;

public class CartService : ICartService
{
    private readonly IApplicationDbContext _context;
    private readonly OrderCalculationHelper _calc;

    public CartService(IApplicationDbContext context, OrderCalculationHelper calc)
    {
        _context = context;
        _calc = calc;
    }

    public async Task<ApiResponse<CartDto>> GetCartAsync(Guid userId) =>
        ApiResponse<CartDto>.Ok(await BuildCart(userId));

    public async Task<ApiResponse<CartSummaryDto>> GetCartSummaryAsync(Guid userId)
    {
        var cart = await BuildCart(userId);
        return ApiResponse<CartSummaryDto>.Ok(new CartSummaryDto
        {
            ItemCount = cart.ItemCount,
            TotalAmount = cart.TotalAmount
        });
    }

    public async Task<ApiResponse<CartDto>> AddToCartAsync(Guid userId, AddToCartDto dto)
    {
        var menuItem = await _context.MenuItems.FindAsync(dto.MenuItemId);
        if (menuItem == null || !menuItem.IsAvailable)
            return ApiResponse<CartDto>.Fail("This item is out of stock or unavailable.");

        var addOnIds = dto.AddOnIds != null ? string.Join(",", dto.AddOnIds) : null;
        var existing = await _context.CartItems.FirstOrDefaultAsync(c =>
            c.UserId == userId && c.MenuItemId == dto.MenuItemId &&
            c.VariantId == dto.VariantId && c.AddOnIds == addOnIds);

        if (existing != null)
            existing.Quantity += dto.Quantity;
        else
            _context.CartItems.Add(new Domain.Entities.CartItem
            {
                UserId = userId, MenuItemId = dto.MenuItemId,
                VariantId = dto.VariantId, Quantity = dto.Quantity, AddOnIds = addOnIds
            });

        await _context.SaveChangesAsync();
        return ApiResponse<CartDto>.Ok(await BuildCart(userId), "Item added to cart.");
    }

    public async Task<ApiResponse<CartDto>> UpdateCartItemAsync(Guid userId, UpdateCartItemDto dto)
    {
        var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == dto.CartItemId && c.UserId == userId);
        if (item == null) return ApiResponse<CartDto>.Fail("Cart item not found.");

        if (dto.Quantity <= 0)
            _context.CartItems.Remove(item);
        else
            item.Quantity = dto.Quantity;

        await _context.SaveChangesAsync();
        return ApiResponse<CartDto>.Ok(await BuildCart(userId));
    }

    public async Task<ApiResponse<CartDto>> RemoveFromCartAsync(Guid userId, Guid cartItemId)
    {
        var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
        if (item == null) return ApiResponse<CartDto>.Fail("Cart item not found.");
        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();
        return ApiResponse<CartDto>.Ok(await BuildCart(userId));
    }

    public async Task<ApiResponse<bool>> ClearCartAsync(Guid userId)
    {
        var items = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();
        _context.CartItems.RemoveRange(items);
        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true);
    }

    private async Task<CartDto> BuildCart(Guid userId)
    {
        var items = await _context.CartItems
            .Include(c => c.MenuItem)
            .Include(c => c.Variant)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        var cartItems = new List<CartItemDto>();
        decimal subTotal = 0;

        foreach (var item in items)
        {
            var unitPrice = item.MenuItem.BasePrice + (item.Variant?.PriceAdjustment ?? 0);
            var addOnNames = new List<string>();

            if (!string.IsNullOrEmpty(item.AddOnIds))
            {
                var ids = item.AddOnIds.Split(',').Select(Guid.Parse).ToList();
                var addOns = await _context.AddOns.Where(a => ids.Contains(a.Id)).ToListAsync();
                unitPrice += addOns.Sum(a => a.Price);
                addOnNames = addOns.Select(a => a.Name).ToList();
            }

            var total = unitPrice * item.Quantity;
            subTotal += total;
            cartItems.Add(new CartItemDto
            {
                Id = item.Id, MenuItemId = item.MenuItemId, MenuItemName = item.MenuItem.Name,
                ImageUrl = item.MenuItem.ImageUrl, VariantId = item.VariantId,
                VariantName = item.Variant?.Name, Quantity = item.Quantity,
                UnitPrice = unitPrice, TotalPrice = total, AddOnNames = addOnNames
            });
        }

        var (discount, tax, totalAmount) = _calc.Calculate(subTotal);
        var threshold = _calc.GetDiscountThreshold();

        return new CartDto
        {
            Items = cartItems,
            ItemCount = cartItems.Sum(i => i.Quantity),
            SubTotal = subTotal,
            DiscountAmount = discount,
            TaxAmount = tax,
            TotalAmount = totalAmount,
            DiscountEligible = subTotal >= threshold,
            DiscountMessage = subTotal >= threshold
                ? $"You get {_calc.GetDiscountPercentage()}% off on orders ≥ ₹{threshold}!"
                : $"Add ₹{threshold - subTotal:F0} more for {_calc.GetDiscountPercentage()}% discount"
        };
    }
}
