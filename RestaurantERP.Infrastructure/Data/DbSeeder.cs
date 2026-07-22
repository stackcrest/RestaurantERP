using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantERP.Application.Common;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Infrastructure.Data;

/// <summary>
/// Idempotent startup seeder. Never wipes or overwrites existing rows —
/// only inserts missing defaults so new features can be added without reseeding.
/// </summary>
public class DbSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<DbSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Ensuring seed data (additive only — existing rows are kept).");

        await SeedRolesAsync();
        await SeedUsersAsync();
        await SeedThemeAsync();
        await SeedCategoriesAsync();
        await SeedMenuItemsAsync();
        await SeedTablesAsync();
        await SeedInventoryAsync();
        await SeedRoleMenuAccessAsync();
        await SeedDeliverySettingsAsync();
        await SeedPosSettingsAsync();
        await SeedCommissionRulesAsync();
        await SeedWelcomeOfferAsync();
        await SeedBlogPostsAsync();
        await SeedAppContentAsync();
        await SeedNavigationMenusAsync();

        await _context.SaveChangesAsync();
        _logger.LogInformation("Database seed ensure completed.");
    }

    private async Task SeedRolesAsync()
    {
        string[] roles = ["SuperAdmin", "Admin", "Customer", "Kitchen", "Delivery"];
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    private async Task SeedUsersAsync()
    {
        await SeedUserAsync("superadmin@khalsarestaurant.com", "Super Admin", "SuperAdmin@2026!@", "SuperAdmin");
        await SeedUserAsync("admin@khalsarestaurant.com", "Restaurant Admin", "Admin@2026!@", "Admin");
        await SeedUserAsync("customer@khalsarestaurant.com", "Demo Customer", "Customer@2026!@", "Customer");
    }

    private async Task SeedUserAsync(string email, string fullName, string password, string role)
    {
        if (await _userManager.FindByEmailAsync(email) != null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await _userManager.AddToRoleAsync(user, role);
        else
            _logger.LogWarning("Failed to seed {Role} user ({Email}): {Errors}",
                role, email, string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    private async Task SeedCategoriesAsync()
    {
        if (await _context.Categories.AnyAsync())
        {
            _logger.LogDebug("Categories already exist — skipping sample category seed.");
            return;
        }

        _context.Categories.AddRange(
            new Category { Name = "Starters", Description = "Appetizers and small plates", DisplayOrder = 1 },
            new Category { Name = "Main Course", Description = "Hearty main dishes", DisplayOrder = 2 },
            new Category { Name = "Breads", Description = "Fresh breads and naans", DisplayOrder = 3 },
            new Category { Name = "Rice & Biryani", Description = "Rice specialties", DisplayOrder = 4 },
            new Category { Name = "Desserts", Description = "Sweet treats", DisplayOrder = 5 },
            new Category { Name = "Beverages", Description = "Drinks and refreshments", DisplayOrder = 6 }
        );
        await _context.SaveChangesAsync();
    }

    private async Task SeedMenuItemsAsync()
    {
        if (await _context.MenuItems.AnyAsync())
        {
            _logger.LogDebug("Menu items already exist — skipping sample menu seed.");
            return;
        }

        var categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        if (categories.Count == 0) return;

        var starters = categories.FirstOrDefault(c => c.Name == "Starters") ?? categories[0];
        var main = categories.FirstOrDefault(c => c.Name == "Main Course") ?? categories[0];
        var beverages = categories.FirstOrDefault(c => c.Name == "Beverages") ?? categories[^1];

        _context.MenuItems.AddRange(
            new MenuItem { Name = "Paneer Tikka", Description = "Grilled cottage cheese with spices", BasePrice = 220, CategoryId = starters.Id, IsVeg = true, IsAvailable = true, PreparationTimeMinutes = 15, IsFeatured = true },
            new MenuItem { Name = "Chicken Wings", Description = "Crispy fried chicken wings", BasePrice = 280, CategoryId = starters.Id, IsVeg = false, IsAvailable = true, PreparationTimeMinutes = 20 },
            new MenuItem { Name = "Butter Chicken", Description = "Creamy tomato curry with chicken", BasePrice = 320, CategoryId = main.Id, IsVeg = false, IsAvailable = true, PreparationTimeMinutes = 25, IsFeatured = true },
            new MenuItem { Name = "Dal Makhani", Description = "Slow-cooked black lentils", BasePrice = 180, CategoryId = main.Id, IsVeg = true, IsAvailable = true, PreparationTimeMinutes = 20 },
            new MenuItem { Name = "Veg Biryani", Description = "Fragrant rice with vegetables", BasePrice = 240, CategoryId = main.Id, IsVeg = true, IsAvailable = true, PreparationTimeMinutes = 30 },
            new MenuItem { Name = "Fresh Lime Soda", Description = "Refreshing lime drink", BasePrice = 60, CategoryId = beverages.Id, IsVeg = true, IsAvailable = true, PreparationTimeMinutes = 5 },
            new MenuItem { Name = "Cold Coffee", Description = "Iced blended coffee", BasePrice = 90, CategoryId = beverages.Id, IsVeg = true, IsAvailable = true, PreparationTimeMinutes = 5 }
        );
        await _context.SaveChangesAsync();
    }

    private async Task SeedTablesAsync()
    {
        if (await _context.Tables.AnyAsync())
        {
            _logger.LogDebug("Tables already exist — skipping sample table seed.");
            return;
        }

        var ground = await _context.TableSections.FirstOrDefaultAsync(s => s.Name == "Ground Floor");
        if (ground == null)
        {
            ground = new TableSection { Name = "Ground Floor", Description = "Main dining area" };
            _context.TableSections.Add(ground);
        }

        var rooftop = await _context.TableSections.FirstOrDefaultAsync(s => s.Name == "Rooftop");
        if (rooftop == null)
        {
            rooftop = new TableSection { Name = "Rooftop", Description = "Outdoor seating" };
            _context.TableSections.Add(rooftop);
        }

        await _context.SaveChangesAsync();

        _context.Tables.AddRange(
            new RestaurantTable { TableNumber = "T-01", Capacity = 2, SectionId = ground.Id, Status = TableStatus.Available },
            new RestaurantTable { TableNumber = "T-02", Capacity = 4, SectionId = ground.Id, Status = TableStatus.Available },
            new RestaurantTable { TableNumber = "T-03", Capacity = 4, SectionId = ground.Id, Status = TableStatus.Available },
            new RestaurantTable { TableNumber = "T-04", Capacity = 6, SectionId = ground.Id, Status = TableStatus.Available },
            new RestaurantTable { TableNumber = "R-01", Capacity = 4, SectionId = rooftop.Id, Status = TableStatus.Available },
            new RestaurantTable { TableNumber = "R-02", Capacity = 6, SectionId = rooftop.Id, Status = TableStatus.Available }
        );
        await _context.SaveChangesAsync();
    }

    private async Task SeedInventoryAsync()
    {
        if (await _context.Ingredients.AnyAsync())
        {
            _logger.LogDebug("Ingredients already exist — skipping sample inventory seed.");
            return;
        }

        if (!await _context.Suppliers.AnyAsync(s => s.Name == "Fresh Foods Pvt Ltd"))
        {
            _context.Suppliers.Add(new Supplier
            {
                Name = "Fresh Foods Pvt Ltd",
                ContactPerson = "Rajesh Kumar",
                Phone = "+91 98765 11111",
                Email = "orders@freshfoods.com",
                IsActive = true
            });
        }

        _context.Ingredients.AddRange(
            new Ingredient { Name = "Chicken (boneless)", Unit = "kg", CurrentStock = 25, LowStockThreshold = 5, UnitCost = 280, IsPerishable = true },
            new Ingredient { Name = "Basmati Rice", Unit = "kg", CurrentStock = 50, LowStockThreshold = 10, UnitCost = 90, IsPerishable = false },
            new Ingredient { Name = "Paneer", Unit = "kg", CurrentStock = 8, LowStockThreshold = 3, UnitCost = 320, IsPerishable = true },
            new Ingredient { Name = "Cooking Oil", Unit = "ltr", CurrentStock = 20, LowStockThreshold = 5, UnitCost = 150, IsPerishable = false },
            new Ingredient { Name = "Onions", Unit = "kg", CurrentStock = 15, LowStockThreshold = 5, UnitCost = 40, IsPerishable = true },
            new Ingredient { Name = "Tomatoes", Unit = "kg", CurrentStock = 3, LowStockThreshold = 5, UnitCost = 35, IsPerishable = true }
        );
        await _context.SaveChangesAsync();
    }

    private async Task SeedDeliverySettingsAsync()
    {
        var defaults = new Dictionary<string, (string Value, string Description)>
        {
            [DeliverySettingKeys.RestaurantLatitude] = ("30.294356201894", "Restaurant latitude"),
            [DeliverySettingKeys.RestaurantLongitude] = ("77.997803321754", "Restaurant longitude"),
            [DeliverySettingKeys.RadiusKm] = ("3.5", "Delivery radius in km"),
            [DeliverySettingKeys.RestaurantAddress] = ("St. Jude's Chowk,Dehradun, Uttarakhand", "Restaurant address")
        };

        foreach (var (key, (value, description)) in defaults)
        {
            if (await _context.ApplicationSettings.AnyAsync(s => s.Key == key))
                continue;

            _context.ApplicationSettings.Add(new ApplicationSetting
            {
                Key = key,
                Value = value,
                Group = "Delivery",
                Description = description,
                IsPublic = true
            });
        }
    }

    private async Task SeedRoleMenuAccessAsync()
    {
        // Additive only: insert missing role/section rows; never replace existing permissions.
        foreach (var role in AdminMenuSections.ManageableRoles)
        {
            foreach (var section in AdminMenuSections.All)
            {
                if (await _context.RoleMenuAccesses.AnyAsync(r => r.RoleName == role && r.SectionKey == section.Key))
                    continue;

                var canRead = role == "Admin"
                    || (role == "Kitchen" && section.Key is AdminMenuSections.Orders or AdminMenuSections.MenuItems)
                    || (role == "Delivery" && section.Key == AdminMenuSections.Orders);

                var canWrite = role == "Admin"
                    || (role == "Kitchen" && section.Key == AdminMenuSections.Orders);

                _context.RoleMenuAccesses.Add(new RoleMenuAccess
                {
                    RoleName = role,
                    SectionKey = section.Key,
                    CanRead = canRead,
                    CanWrite = canWrite
                });
            }
        }
    }

    private async Task SeedThemeAsync()
    {
        if (await _context.ThemeSettings.AnyAsync())
        {
            _logger.LogDebug("Theme settings already exist — skipping theme seed.");
            return;
        }

        _context.ThemeSettings.Add(new ThemeSetting
        {
            PrimaryColor = "#E63946",
            SecondaryColor = "#1D3557",
            AccentColor = "#F4A261",
            RestaurantName = "KhalsaFamilyRestaurant",
            Tagline = "Delicious food, delivered fast",
            Version = 1
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedPosSettingsAsync()
    {
        var posDefaults = new Dictionary<string, (string Value, string Description)>
        {
            [PosSettingKeys.Gstin] = ("", "Restaurant GSTIN for bills"),
            [PosSettingKeys.BillPrefix] = ("BILL", "Bill number prefix"),
            [PosSettingKeys.RestaurantPhone] = ("+91 8449895240", "Phone on printed bills"),
            [PosSettingKeys.BillFooterNote] = ("Thank you! Visit again.", "Footer text on bills")
        };

        foreach (var (key, (value, description)) in posDefaults)
        {
            if (await _context.ApplicationSettings.AnyAsync(s => s.Key == key))
                continue;

            _context.ApplicationSettings.Add(new ApplicationSetting
            {
                Key = key,
                Value = value,
                Group = "POS",
                Description = description,
                IsPublic = false
            });
        }

        var posFlags = new[]
        {
            (PosFeatureKeys.PosEnabled, "POS System", "Enable POS terminal and billing"),
            (PosFeatureKeys.OfflineBilling, "Offline POS Billing", "Walk-in counter bill generation"),
            (PosFeatureKeys.OnlineBilling, "Online Bill Generation", "Generate bills for web orders")
        };

        foreach (var (key, name, desc) in posFlags)
        {
            if (await _context.FeatureFlags.AnyAsync(f => f.FeatureKey == key))
                continue;

            _context.FeatureFlags.Add(new FeatureFlag
            {
                FeatureKey = key,
                Name = name,
                Description = desc,
                IsEnabled = true
            });
        }
    }

    private async Task SeedCommissionRulesAsync()
    {
        if (await _context.CommissionRules.AnyAsync())
        {
            _logger.LogDebug("Commission rules already exist — skipping sample commission seed.");
            return;
        }

        _context.CommissionRules.Add(new CommissionRule
        {
            Name = "Platform Commission",
            Percentage = 5,
            Priority = 1,
            IsActive = true
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedWelcomeOfferAsync()
    {
        if (await _context.Coupons.AnyAsync(c => c.Code == "WELCOME10" && !c.IsDeleted))
            return;

        _context.Coupons.Add(new Coupon
        {
            Code = "WELCOME10",
            Title = "Welcome Offer",
            Subtitle = "Sign up now and get 10% off on your first order!",
            Description = "Default first-order welcome promotion",
            BadgeText = "10% OFF",
            CtaText = "Create Account",
            DiscountPercentage = 10,
            ValidFrom = DateTime.UtcNow.Date,
            ValidTo = DateTime.UtcNow.Date.AddYears(1),
            IsActive = true,
            ShowOnHomepage = true,
            FirstOrderOnly = true,
            Priority = 100
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedBlogPostsAsync()
    {
        var author = await _userManager.FindByEmailAsync("superadmin@khalsarestaurant.com")
            ?? await _context.Users.OrderBy(u => u.CreatedAt).FirstOrDefaultAsync();
        if (author == null) return;

        var samples = new[]
        {
            new BlogPost
            {
                Title = "5 Secrets to Perfect Biryani at Home",
                Slug = "perfect-biryani-secrets",
                Summary = "Learn the tips our chefs use to make aromatic, fluffy biryani every single time.",
                Content = "<p>Biryani is more than a dish — it's an experience. Start with aged basmati rice, marinate your protein overnight, and layer flavors patiently.</p><h3>Key Tips</h3><ul><li>Soak rice for 30 minutes before cooking</li><li>Use whole spices tempered in ghee</li><li>Dum cook on low heat for 25 minutes</li></ul><p>Visit our restaurant to taste our signature dum biryani!</p>",
                FeaturedImageUrl = "https://images.unsplash.com/photo-1563379091339-03246963d96c?w=800&h=500&fit=crop",
                Status = PageStatus.Published,
                AuthorUserId = author.Id,
                AuthorName = author.FullName,
                PublishedAt = DateTime.UtcNow.AddDays(-7),
                MetaDescription = "Chef secrets for making perfect biryani at home.",
                Tags = "recipes, biryani, tips"
            },
            new BlogPost
            {
                Title = "Why Fresh Ingredients Matter",
                Slug = "fresh-ingredients-matter",
                Summary = "From farm to plate — how we source the freshest produce for every dish.",
                Content = "<p>At Restaurant ERP, we partner with local suppliers to bring you the freshest ingredients daily.</p><blockquote>Great food starts with great ingredients.</blockquote><p>Our kitchen team inspects every delivery to ensure quality standards are met before anything hits the pan.</p>",
                FeaturedImageUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999?w=800&h=500&fit=crop",
                Status = PageStatus.Published,
                AuthorUserId = author.Id,
                AuthorName = author.FullName,
                PublishedAt = DateTime.UtcNow.AddDays(-3),
                MetaDescription = "How we source fresh ingredients for our restaurant.",
                Tags = "food, quality, sustainability"
            }
        };

        foreach (var sample in samples)
        {
            if (await _context.BlogPosts.AnyAsync(p => p.Slug == sample.Slug && !p.IsDeleted))
                continue;

            _context.BlogPosts.Add(sample);
        }
    }

    private async Task SeedAppContentAsync()
    {
        foreach (var seed in AppContentDefaults.GetAll())
        {
            if (await _context.AppContentItems.AnyAsync(c => c.ContentKey == seed.Key))
                continue;

            _context.AppContentItems.Add(new AppContentItem
            {
                ContentKey = seed.Key,
                Value = seed.Value,
                ContentType = seed.Type,
                Section = seed.Section,
                Label = seed.Label,
                Description = seed.Description,
                SortOrder = seed.SortOrder,
                IsPublished = true,
                IsSystem = true
            });
        }
    }

    private async Task SeedNavigationMenusAsync()
    {
        var mainMenu = await EnsureNavigationMenuAsync("main", "Main Navigation");
        var footerMenu = await EnsureNavigationMenuAsync("footer", "Footer Quick Links");
        await _context.SaveChangesAsync();

        var mainItems = new[]
        {
            ("Home", "/", "bi-house", 1),
            ("Menu", "/Menu", "bi-grid", 2),
            ("Blog", "/Blog", "bi-journal-richtext", 3),
            ("About Us", "/Home/About", "bi-info-circle", 4),
            ("Contact", "/Home/Contact", "bi-envelope", 5)
        };

        foreach (var (label, url, icon, order) in mainItems)
            await EnsureNavigationItemAsync(mainMenu.Id, label, url, icon, order);

        var footerItems = new[]
        {
            ("Home", "/", 1),
            ("Menu", "/Menu", 2),
            ("About Us", "/Home/About", 3),
            ("Contact", "/Home/Contact", 4)
        };

        foreach (var (label, url, order) in footerItems)
            await EnsureNavigationItemAsync(footerMenu.Id, label, url, null, order);
    }

    private async Task<NavigationMenu> EnsureNavigationMenuAsync(string menuKey, string name)
    {
        var existing = await _context.NavigationMenus.FirstOrDefaultAsync(m => m.MenuKey == menuKey);
        if (existing != null) return existing;

        var menu = new NavigationMenu { Name = name, MenuKey = menuKey, IsActive = true };
        _context.NavigationMenus.Add(menu);
        return menu;
    }

    private async Task EnsureNavigationItemAsync(Guid menuId, string label, string url, string? icon, int order)
    {
        if (await _context.NavigationMenuItems.AnyAsync(i => i.MenuId == menuId && i.Url == url))
            return;

        _context.NavigationMenuItems.Add(new NavigationMenuItem
        {
            MenuId = menuId,
            Label = label,
            Url = url,
            Icon = icon,
            DisplayOrder = order,
            IsActive = true
        });
    }
}
