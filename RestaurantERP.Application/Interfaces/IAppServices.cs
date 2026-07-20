using RestaurantERP.Application.Common;
using RestaurantERP.Application.DTOs.Auth;
using RestaurantERP.Application.DTOs.Cart;
using RestaurantERP.Application.DTOs.Menu;
using RestaurantERP.Application.DTOs.Orders;
using RestaurantERP.Application.DTOs.Public;
using RestaurantERP.Application.DTOs.SuperAdmin;

namespace RestaurantERP.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto);
    Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(string refreshToken);
    Task<ApiResponse<bool>> LogoutAsync(string refreshToken);
    Task<ApiResponse<UserProfileDto>> GetProfileAsync(Guid userId);
    Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
    Task<ApiResponse<bool>> UpdateFcmTokenAsync(Guid userId, string fcmToken);
}

public interface IMenuService
{
    Task<ApiResponse<List<CategoryDto>>> GetCategoriesAsync();
    Task<ApiResponse<List<MenuItemListDto>>> GetMenuItemsAsync(MenuFilterDto filter);
    Task<ApiResponse<MenuItemDetailDto>> GetMenuItemAsync(Guid id);
    Task<ApiResponse<List<MenuItemListDto>>> GetFeaturedItemsAsync();
    Task<ApiResponse<MenuItemDetailDto>> CreateMenuItemAsync(CreateMenuItemDto dto);
    Task<ApiResponse<MenuItemDetailDto>> UpdateMenuItemAsync(Guid id, UpdateMenuItemDto dto);
    Task<ApiResponse<bool>> DeleteMenuItemAsync(Guid id);
    Task<ApiResponse<bool>> SetAvailabilityAsync(Guid id, bool isAvailable);
}

public interface ICartService
{
    Task<ApiResponse<CartDto>> GetCartAsync(Guid userId);
    Task<ApiResponse<CartSummaryDto>> GetCartSummaryAsync(Guid userId);
    Task<ApiResponse<CartDto>> AddToCartAsync(Guid userId, AddToCartDto dto);
    Task<ApiResponse<CartDto>> UpdateCartItemAsync(Guid userId, UpdateCartItemDto dto);
    Task<ApiResponse<CartDto>> RemoveFromCartAsync(Guid userId, Guid cartItemId);
    Task<ApiResponse<bool>> ClearCartAsync(Guid userId);
}

public interface IWishlistService
{
    Task<ApiResponse<List<MenuItemListDto>>> GetWishlistAsync(Guid userId);
    Task<ApiResponse<bool>> ToggleWishlistAsync(Guid userId, Guid menuItemId);
    Task<ApiResponse<int>> GetWishlistCountAsync(Guid userId);
}

public interface IOrderService
{
    Task<ApiResponse<OrderDetailDto>> PlaceOrderAsync(Guid userId, PlaceOrderDto dto);
    Task<ApiResponse<List<OrderListDto>>> GetOrdersAsync(Guid? userId, bool isAdmin, PagedRequest paging);
    Task<ApiResponse<OrderDetailDto>> GetOrderAsync(Guid id, Guid? userId, bool isAdmin);
    Task<ApiResponse<OrderDetailDto>> GetOrderByNumberAsync(string orderNumber, Guid? userId);
    Task<ApiResponse<OrderDetailDto>> UpdateOrderStatusAsync(Guid id, string status);
    Task<ApiResponse<bool>> CancelOrderAsync(Guid id, Guid userId);
    Task<ApiResponse<OrderDetailDto>> ReorderAsync(Guid orderId, Guid userId);
}

public interface IPublicService
{
    Task<ApiResponse<ThemeDto>> GetThemeAsync();
    Task<ApiResponse<Dictionary<string, object>>> GetPublicSettingsAsync();
    Task<ApiResponse<List<NavigationMenuDto>>> GetNavigationAsync(string menuKey);
    Task<ApiResponse<PublicPageDto>> GetPageBySlugAsync(string slug);
    Task<ApiResponse<PublicPageDto>> GetHomepageAsync();
}

public interface ISuperAdminService
{
    Task<ApiResponse<ThemeDto>> GetThemeSettingsAsync();
    Task<ApiResponse<ThemeDto>> UpdateThemeAsync(UpdateThemeDto dto);
    Task<ApiResponse<List<ApplicationSettingDto>>> GetSettingsAsync();
    Task<ApiResponse<bool>> UpdateSettingsAsync(List<ApplicationSettingDto> settings);
    Task<ApiResponse<List<CommissionRuleDto>>> GetCommissionRulesAsync();
    Task<ApiResponse<CommissionRuleDto>> CreateCommissionRuleAsync(CreateCommissionRuleDto dto);
    Task<ApiResponse<List<UiPageDto>>> GetPagesAsync();
    Task<ApiResponse<UiPageDto>> CreatePageAsync(CreateUiPageDto dto);
    Task<ApiResponse<UiPageDto>> UpdatePageAsync(Guid id, UpdateUiPageDto dto);
    Task<ApiResponse<bool>> PublishPageAsync(Guid id);
    Task<ApiResponse<List<FeatureFlagDto>>> GetFeatureFlagsAsync();
    Task<ApiResponse<FeatureFlagDto>> UpdateFeatureFlagAsync(string key, bool enabled);
    Task<ApiResponse<List<UserListDto>>> GetAllUsersAsync();
    Task<ApiResponse<List<AuditLogDto>>> GetAuditLogsAsync(PagedRequest paging);
}

public interface IDashboardService
{
    Task<ApiResponse<DashboardSummaryDto>> GetSummaryAsync();
    Task<ApiResponse<Dictionary<string, object>>> GetSalesChartAsync(int days = 7);
}

public interface ITableService
{
    Task<ApiResponse<List<TableDto>>> GetTablesAsync();
    Task<ApiResponse<TableDto>> UpdateTableStatusAsync(Guid id, string status);
}

public interface IReservationService
{
    Task<ApiResponse<ReservationDto>> CreateReservationAsync(Guid userId, CreateReservationDto dto);
    Task<ApiResponse<List<ReservationDto>>> GetReservationsAsync(Guid? userId, bool isAdmin);
    Task<ApiResponse<ReservationDto>> UpdateReservationStatusAsync(Guid id, string status);
}

public interface ICouponService
{
    Task<ApiResponse<CouponDto>> ValidateCouponAsync(string code, decimal orderAmount);
    Task<ApiResponse<List<CouponDto>>> GetCouponsAsync();
    Task<ApiResponse<CouponDto>> CreateCouponAsync(CreateCouponDto dto);
}

public interface IReviewService
{
    Task<ApiResponse<ReviewDto>> CreateReviewAsync(Guid userId, CreateReviewDto dto);
    Task<ApiResponse<List<ReviewDto>>> GetItemReviewsAsync(Guid menuItemId, PagedRequest paging);
}
