# Restaurant Management System ERP

A comprehensive Restaurant ERP system built with ASP.NET Core 8.0 MVC, featuring mobile-first responsive UI and SQL Server database.

## Features

### Customer Features
- **Menu Browsing**: Browse categories, search dishes, filter by veg/non-veg
- **Cart Management**: Add/remove items with variants and add-ons
- **Order Placement**: Dine-in, Takeaway, or Delivery with table selection
- **Order Tracking**: Real-time order status updates
- **Profile Management**: View order history, loyalty points

### Admin Features
- **Dashboard**: Sales analytics, order statistics, table status
- **Order Management**: View, confirm, prepare, and complete orders
- **Menu Management**: CRUD operations for menu items, categories
- **Table Management**: Track table availability

### SuperAdmin Features
- **Theme Management**: Customize restaurant branding, colors, logo
- **Application Settings**: Configure business rules, feature flags
- **User Management**: Manage users and roles (SuperAdmin, Admin, Customer)
- **CMS Pages**: Create and manage dynamic content pages
- **Commission Rules**: Configure commission structures
- **Audit Logs**: Track all system activities

## Technology Stack

- **Backend**: ASP.NET Core 8.0
- **Frontend**: ASP.NET Core MVC with Razor Views
- **Database**: Microsoft SQL Server (LocalDB for development)
- **ORM**: Entity Framework Core 8.0
- **Authentication**: ASP.NET Core Identity
- **UI Framework**: Bootstrap 5.3 (Mobile-first responsive)
- **Icons**: Bootstrap Icons

## Project Structure

```
RestaurantERP/
├── RestaurantERP.Domain/          # Domain entities and enums
├── RestaurantERP.Application/     # Business logic, services, DTOs
├── RestaurantERP.Infrastructure/  # Database, Identity, seeding
├── RestaurantERP.API/             # REST API (for future mobile apps)
├── RestaurantERP.Web/             # MVC Web Application
│   ├── Controllers/               # Customer-facing controllers
│   ├── Views/                     # Razor views
│   ├── Areas/
│   │   ├── Admin/                 # Admin panel
│   │   └── SuperAdmin/            # SuperAdmin panel
│   └── Models/                    # View models
└── RestaurantERP.Tests/           # Unit tests
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code

### Setup

1. Clone the repository
2. Update connection string in `appsettings.json` if needed
3. Run the application:

```bash
cd RestaurantERP
dotnet run --project RestaurantERP.Web
```

The application will:
- Automatically create and migrate the database
- Seed default data (users, menu items, categories, tables)

### Default Users

| Role | Email | Password |
|------|-------|----------|
| SuperAdmin | superadmin@restauranterp.com | SuperAdmin@123 |
| Admin | admin@restaurant.com | Admin@123 |
| Customer | customer@restaurant.com | Customer@123 |

## Application URLs

- **Customer Site**: https://localhost:5001 (or http://localhost:5000)
- **Admin Panel**: https://localhost:5001/Admin
- **SuperAdmin Panel**: https://localhost:5001/SuperAdmin

## Database Schema

### Core Entities
- **Users**: ApplicationUser (extends IdentityUser)
- **Menu**: Categories, MenuItems, MenuItemVariants, AddOns
- **Orders**: Orders, OrderItems, OrderItemAddOns
- **Tables**: TableSections, RestaurantTables, Reservations
- **Inventory**: Ingredients, Recipes, Suppliers, GRN

### Platform Entities
- **Settings**: ApplicationSettings, ThemeSettings, FeatureFlags
- **CMS**: UiPages, UiPageBlocks, NavigationMenus
- **Finance**: CommissionRules, Payments

## Configuration

### Business Rules (appsettings.json)

```json
{
  "BusinessRules": {
    "DiscountThresholdAmount": 500,
    "DiscountPercentage": 5,
    "CGST": 2.5,
    "SGST": 2.5,
    "DeliveryFee": 40,
    "MinimumOrderAmount": 100
  }
}
```

## Mobile-First Design

The UI is optimized for mobile devices:
- Bottom navigation for easy thumb access
- Floating cart bar showing items and total
- Touch-friendly buttons and inputs
- Responsive cards and layouts
- Max container width of 600px for optimal mobile experience

## Future Enhancements

- Mobile apps (Flutter/React Native) using the REST API
- Kitchen Display System (KDS)
- Real-time order notifications via SignalR
- Payment gateway integration
- Inventory management with low stock alerts
- Customer loyalty program

## License

MIT License
