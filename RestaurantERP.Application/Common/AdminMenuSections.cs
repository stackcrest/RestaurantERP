namespace RestaurantERP.Application.Common;

public static class AdminMenuSections
{
    public const string Dashboard = "dashboard";
    public const string Orders = "orders";
    public const string Categories = "categories";
    public const string MenuItems = "menu_items";
    public const string Tables = "tables";
    public const string Inventory = "inventory";
    public const string Reports = "reports";
    public const string Pos = "pos";
    public const string Contact = "contact";
    public const string Offers = "offers";

    public static readonly IReadOnlyList<(string Key, string Name, string Icon)> All =
    [
        (Dashboard, "Dashboard", "speedometer2"),
        (Orders, "Orders", "receipt"),
        (Pos, "POS / Billing", "cash-stack"),
        (Categories, "Categories", "folder"),
        (MenuItems, "Menu Items", "grid"),
        (Offers, "Offers", "percent"),
        (Tables, "Tables", "columns-gap"),
        (Inventory, "Inventory", "box-seam"),
        (Reports, "Reports", "bar-chart"),
        (Contact, "Contact Us", "envelope")
    ];

    public static readonly string[] ManageableRoles = ["Admin", "Kitchen", "Delivery"];

    public static string? GetSectionForController(string controllerName) => controllerName switch
    {
        "Dashboard" => Dashboard,
        "Orders" => Orders,
        "Categories" => Categories,
        "Menu" => MenuItems,
        "Offers" => Offers,
        "Tables" => Tables,
        "Inventory" => Inventory,
        "Reports" => Reports,
        "Pos" => Pos,
        "Contact" => Contact,
        _ => null
    };

    public static bool IsWriteAction(string httpMethod, string actionName)
    {
        if (string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase)
            || string.Equals(httpMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
            return false;

        return !actionName.Equals("Index", StringComparison.OrdinalIgnoreCase)
            && !actionName.Equals("Details", StringComparison.OrdinalIgnoreCase);
    }
}
