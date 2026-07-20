using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.DTOs.Menu;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.Application.Services;

public class WishlistService : IWishlistService
{
    private readonly IApplicationDbContext _context;

    public WishlistService(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<List<MenuItemListDto>>> GetWishlistAsync(Guid userId)
    {
        var items = await _context.WishlistItems
            .Include(w => w.MenuItem).ThenInclude(m => m.Category)
            .Where(w => w.UserId == userId)
            .Select(w => new MenuItemListDto
            {
                Id = w.MenuItem.Id, Name = w.MenuItem.Name, Description = w.MenuItem.Description,
                BasePrice = w.MenuItem.BasePrice, ImageUrl = w.MenuItem.ImageUrl,
                IsVeg = w.MenuItem.IsVeg, SpiceLevel = w.MenuItem.SpiceLevel.ToString(),
                IsAvailable = w.MenuItem.IsAvailable, IsFeatured = w.MenuItem.IsFeatured,
                AverageRating = w.MenuItem.AverageRating, ReviewCount = w.MenuItem.ReviewCount,
                CategoryId = w.MenuItem.CategoryId, CategoryName = w.MenuItem.Category.Name
            }).ToListAsync();

        return ApiResponse<List<MenuItemListDto>>.Ok(items);
    }

    public async Task<ApiResponse<bool>> ToggleWishlistAsync(Guid userId, Guid menuItemId)
    {
        var existing = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.MenuItemId == menuItemId);

        if (existing != null)
        {
            _context.WishlistItems.Remove(existing);
            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(false, "Removed from wishlist.");
        }

        _context.WishlistItems.Add(new Domain.Entities.WishlistItem { UserId = userId, MenuItemId = menuItemId });
        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Added to wishlist.");
    }

    public async Task<ApiResponse<int>> GetWishlistCountAsync(Guid userId)
    {
        var count = await _context.WishlistItems.CountAsync(w => w.UserId == userId);
        return ApiResponse<int>.Ok(count);
    }
}
