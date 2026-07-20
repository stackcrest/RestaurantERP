using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemVariant> MenuItemVariants => Set<MenuItemVariant>();
    public DbSet<AddOn> AddOns => Set<AddOn>();
    public DbSet<MenuItemAddOn> MenuItemAddOns => Set<MenuItemAddOn>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemAddOn> OrderItemAddOns => Set<OrderItemAddOn>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RestaurantTable> Tables => Set<RestaurantTable>();
    public DbSet<TableSection> TableSections => Set<TableSection>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<GRN> GRNs => Set<GRN>();
    public DbSet<KitchenStation> KitchenStations => Set<KitchenStation>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();
    public DbSet<ThemeSetting> ThemeSettings => Set<ThemeSetting>();
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
    public DbSet<CommissionLedger> CommissionLedgers => Set<CommissionLedger>();
    public DbSet<UiPage> UiPages => Set<UiPage>();
    public DbSet<UiPageBlock> UiPageBlocks => Set<UiPageBlock>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<NavigationMenu> NavigationMenus => Set<NavigationMenu>();
    public DbSet<NavigationMenuItem> NavigationMenuItems => Set<NavigationMenuItem>();
    public DbSet<AppContentItem> AppContentItems => Set<AppContentItem>();
    public DbSet<ContactInquiry> ContactInquiries => Set<ContactInquiry>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<RoleMenuAccess> RoleMenuAccesses => Set<RoleMenuAccess>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<MenuItemAddOn>()
            .HasOne(x => x.MenuItem).WithMany(m => m.MenuItemAddOns).HasForeignKey(x => x.MenuItemId);
        builder.Entity<MenuItemAddOn>()
            .HasOne(x => x.AddOn).WithMany(a => a.MenuItemAddOns).HasForeignKey(x => x.AddOnId);

        builder.Entity<WishlistItem>().HasIndex(x => new { x.UserId, x.MenuItemId }).IsUnique();
        builder.Entity<ApplicationSetting>().HasIndex(x => x.Key).IsUnique();
        builder.Entity<UiPage>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<BlogPost>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<BlogPost>().HasIndex(x => x.Status);
        builder.Entity<BlogPost>()
            .HasOne(b => b.Author)
            .WithMany()
            .HasForeignKey(b => b.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<NavigationMenu>().HasIndex(x => x.MenuKey).IsUnique();
        builder.Entity<AppContentItem>().HasIndex(x => x.ContentKey).IsUnique();
        builder.Entity<FeatureFlag>().HasIndex(x => x.FeatureKey).IsUnique();
        builder.Entity<Coupon>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<Order>().HasIndex(x => x.OrderNumber).IsUnique();
        builder.Entity<RoleMenuAccess>().HasIndex(x => new { x.RoleName, x.SectionKey }).IsUnique();
        builder.Entity<OrderStatusHistory>().HasIndex(x => x.OrderId);
        builder.Entity<Bill>().HasIndex(x => x.BillNumber).IsUnique();
        builder.Entity<Bill>().HasIndex(x => x.OrderId);

        builder.Entity<ContactInquiry>()
            .HasOne(c => c.HandledBy)
            .WithMany()
            .HasForeignKey(c => c.HandledByUserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Entity<ContactInquiry>().HasIndex(x => x.Status);
        builder.Entity<ContactInquiry>().HasIndex(x => x.CreatedAt);

        builder.Entity<NavigationMenuItem>()
            .HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RestaurantTable>().ToTable("Tables");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
