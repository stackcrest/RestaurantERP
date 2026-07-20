using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.DTOs.Menu;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Services;

public class MenuService : IMenuService
{
    private readonly IApplicationDbContext _context;

    public MenuService(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<List<CategoryDto>>> GetCategoriesAsync()
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                DisplayOrder = c.DisplayOrder,
                ItemCount = c.MenuItems.Count(m => m.IsAvailable)
            }).ToListAsync();

        return ApiResponse<List<CategoryDto>>.Ok(categories);
    }

    public async Task<ApiResponse<List<MenuItemListDto>>> GetMenuItemsAsync(MenuFilterDto filter)
    {
        var query = _context.MenuItems.Include(m => m.Category).AsQueryable();

        if (filter.CategoryId.HasValue) query = query.Where(m => m.CategoryId == filter.CategoryId);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(m => m.Name.Contains(filter.Search) || (m.Description != null && m.Description.Contains(filter.Search)));
        if (filter.IsVeg.HasValue) query = query.Where(m => m.IsVeg == filter.IsVeg);
        if (filter.MinPrice.HasValue) query = query.Where(m => m.BasePrice >= filter.MinPrice);
        if (filter.MaxPrice.HasValue) query = query.Where(m => m.BasePrice <= filter.MaxPrice);
        if (filter.IsAvailable.HasValue) query = query.Where(m => m.IsAvailable == filter.IsAvailable);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(m => m.Name)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(m => MapListItem(m))
            .ToListAsync();

        return ApiResponse<List<MenuItemListDto>>.Ok(items, pagination: new PaginationInfo
        {
            Page = filter.Page, PageSize = filter.PageSize, TotalCount = total
        });
    }

    public async Task<ApiResponse<MenuItemDetailDto>> GetMenuItemAsync(Guid id)
    {
        var item = await _context.MenuItems
            .Include(m => m.Category)
            .Include(m => m.Variants)
            .Include(m => m.MenuItemAddOns).ThenInclude(ma => ma.AddOn)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (item == null) return ApiResponse<MenuItemDetailDto>.Fail("Menu item not found.");

        var dto = MapListItem(item);
        var detail = new MenuItemDetailDto
        {
            Id = dto.Id, Name = dto.Name, Description = dto.Description, BasePrice = dto.BasePrice,
            ImageUrl = dto.ImageUrl, ThumbnailUrl = dto.ThumbnailUrl, IsVeg = dto.IsVeg,
            SpiceLevel = dto.SpiceLevel, IsAvailable = dto.IsAvailable, IsFeatured = dto.IsFeatured,
            AverageRating = dto.AverageRating, ReviewCount = dto.ReviewCount,
            CategoryId = dto.CategoryId, CategoryName = dto.CategoryName,
            PreparationTimeMinutes = dto.PreparationTimeMinutes,
            Allergens = item.Allergens,
            Variants = item.Variants.Where(v => v.IsActive).Select(v => new VariantDto
            {
                Id = v.Id, Name = v.Name, PriceAdjustment = v.PriceAdjustment,
                IsDefault = v.IsDefault, TotalPrice = item.BasePrice + v.PriceAdjustment
            }).ToList(),
            AddOns = item.MenuItemAddOns.Select(ma => new AddOnDto
            {
                Id = ma.AddOn.Id, Name = ma.AddOn.Name, Price = ma.AddOn.Price
            }).ToList()
        };
        return ApiResponse<MenuItemDetailDto>.Ok(detail);
    }

    public async Task<ApiResponse<List<MenuItemListDto>>> GetFeaturedItemsAsync()
    {
        var items = await _context.MenuItems.Include(m => m.Category)
            .Where(m => m.IsFeatured && m.IsAvailable)
            .Take(10).Select(m => MapListItem(m)).ToListAsync();
        return ApiResponse<List<MenuItemListDto>>.Ok(items);
    }

    public async Task<ApiResponse<MenuItemDetailDto>> CreateMenuItemAsync(CreateMenuItemDto dto)
    {
        var item = new MenuItem
        {
            Name = dto.Name, Description = dto.Description, BasePrice = dto.BasePrice,
            ImageUrl = dto.ImageUrl, IsVeg = dto.IsVeg,
            SpiceLevel = Enum.Parse<SpiceLevel>(dto.SpiceLevel, true),
            CategoryId = dto.CategoryId, IsFeatured = dto.IsFeatured,
            PreparationTimeMinutes = dto.PreparationTimeMinutes
        };
        _context.MenuItems.Add(item);
        await _context.SaveChangesAsync();
        return await GetMenuItemAsync(item.Id);
    }

    public async Task<ApiResponse<MenuItemDetailDto>> UpdateMenuItemAsync(Guid id, UpdateMenuItemDto dto)
    {
        var item = await _context.MenuItems.FindAsync(id);
        if (item == null) return ApiResponse<MenuItemDetailDto>.Fail("Menu item not found.");

        item.Name = dto.Name; item.Description = dto.Description; item.BasePrice = dto.BasePrice;
        item.ImageUrl = dto.ImageUrl; item.IsVeg = dto.IsVeg;
        item.SpiceLevel = Enum.Parse<SpiceLevel>(dto.SpiceLevel, true);
        item.CategoryId = dto.CategoryId; item.IsFeatured = dto.IsFeatured;
        item.IsAvailable = dto.IsAvailable; item.PreparationTimeMinutes = dto.PreparationTimeMinutes;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetMenuItemAsync(id);
    }

    public async Task<ApiResponse<bool>> DeleteMenuItemAsync(Guid id)
    {
        var item = await _context.MenuItems.FindAsync(id);
        if (item == null) return ApiResponse<bool>.Fail("Menu item not found.");
        item.IsDeleted = true;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Menu item deleted.");
    }

    public async Task<ApiResponse<bool>> SetAvailabilityAsync(Guid id, bool isAvailable)
    {
        var item = await _context.MenuItems.FindAsync(id);
        if (item == null) return ApiResponse<bool>.Fail("Menu item not found.");
        item.IsAvailable = isAvailable;
        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true);
    }

    private static MenuItemListDto MapListItem(MenuItem m) => new()
    {
        Id = m.Id, Name = m.Name, Description = m.Description, BasePrice = m.BasePrice,
        ImageUrl = m.ImageUrl, ThumbnailUrl = m.ThumbnailUrl, IsVeg = m.IsVeg,
        SpiceLevel = m.SpiceLevel.ToString(), IsAvailable = m.IsAvailable, IsFeatured = m.IsFeatured,
        AverageRating = m.AverageRating, ReviewCount = m.ReviewCount,
        CategoryId = m.CategoryId, CategoryName = m.Category.Name,
        PreparationTimeMinutes = m.PreparationTimeMinutes
    };
}
