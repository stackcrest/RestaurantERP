using Microsoft.EntityFrameworkCore;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<MenuItem> MenuItems { get; }
    DbSet<MenuItemVariant> MenuItemVariants { get; }
    DbSet<AddOn> AddOns { get; }
    DbSet<MenuItemAddOn> MenuItemAddOns { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<WishlistItem> WishlistItems { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderItemAddOn> OrderItemAddOns { get; }
    DbSet<OrderStatusHistory> OrderStatusHistories { get; }
    DbSet<Bill> Bills { get; }
    DbSet<Payment> Payments { get; }
    DbSet<RestaurantTable> Tables { get; }
    DbSet<TableSection> TableSections { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<Ingredient> Ingredients { get; }
    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeIngredient> RecipeIngredients { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<GRN> GRNs { get; }
    DbSet<KitchenStation> KitchenStations { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<Review> Reviews { get; }
    DbSet<ApplicationSetting> ApplicationSettings { get; }
    DbSet<ThemeSetting> ThemeSettings { get; }
    DbSet<CommissionRule> CommissionRules { get; }
    DbSet<CommissionLedger> CommissionLedgers { get; }
    DbSet<UiPage> UiPages { get; }
    DbSet<UiPageBlock> UiPageBlocks { get; }
    DbSet<BlogPost> BlogPosts { get; }
    DbSet<NavigationMenu> NavigationMenus { get; }
    DbSet<NavigationMenuItem> NavigationMenuItems { get; }
    DbSet<AppContentItem> AppContentItems { get; }
    DbSet<ContactInquiry> ContactInquiries { get; }
    DbSet<FeatureFlag> FeatureFlags { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<RoleMenuAccess> RoleMenuAccesses { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
