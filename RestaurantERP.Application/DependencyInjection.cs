using Microsoft.Extensions.DependencyInjection;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Application.Services;

namespace RestaurantERP.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IMenuExcelImportService, MenuExcelImportService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPublicService, PublicService>();
        services.AddScoped<ISuperAdminService, SuperAdminService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITableService, TableService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IDeliveryZoneService, DeliveryZoneService>();
        services.AddScoped<IOrderTrackingService, OrderTrackingService>();
        services.AddScoped<IPosBillingService, PosBillingService>();
        services.AddScoped<ICommissionService, CommissionService>();
        services.AddScoped<IApplicationContentService, ApplicationContentService>();
        services.AddScoped<IContactInquiryService, ContactInquiryService>();
        services.AddScoped<IWhatsAppIntegrationService, WhatsAppIntegrationService>();
        services.AddSingleton<OrderCalculationHelper>();
        return services;
    }
}
