using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RestaurantERP.Application.DTOs.Orders;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Web.Filters;

public class SiteDataFilter : IAsyncActionFilter
{
    private readonly IApplicationContentService _content;
    private readonly ICouponService _offers;

    public SiteDataFilter(IApplicationContentService content, ICouponService offers)
    {
        _content = content;
        _offers = offers;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.RouteData.Values.ContainsKey("area"))
        {
            await next();
            return;
        }

        var action = context.RouteData.Values["action"]?.ToString();
        if (action is "PageNotFound" or "Error")
        {
            await next();
            return;
        }

        if (context.Controller is Controller controller)
        {
            try
            {
                controller.ViewBag.SiteTheme = await _content.GetPublishedThemeAsync();
                var menus = await _content.GetNavigationMenusAsync();
                var isAuthenticated = context.HttpContext.User.Identity?.IsAuthenticated == true;
                controller.ViewBag.MainNav = GetTopLevelNavItems(menus, "main", isAuthenticated);
                controller.ViewBag.MainNavChildren = GetChildNavLookup(menus, "main", isAuthenticated);
                controller.ViewBag.FooterNav = GetTopLevelNavItems(menus, "footer", isAuthenticated);
                controller.ViewBag.ActiveOffer = await _offers.GetActiveHomepageOfferAsync();
            }
            catch
            {
                controller.ViewBag.MainNav = new List<NavigationMenuItem>();
                controller.ViewBag.MainNavChildren = new Dictionary<Guid, List<NavigationMenuItem>>();
                controller.ViewBag.FooterNav = new List<NavigationMenuItem>();
            }
        }

        await next();
    }

    private static List<NavigationMenuItem> GetTopLevelNavItems(List<NavigationMenu> menus, string key, bool isAuthenticated)
    {
        var menu = menus.FirstOrDefault(m => m.MenuKey == key && m.IsActive);
        if (menu == null) return [];

        return menu.Items
            .Where(i => i.IsActive && i.ParentId == null && IsVisible(i.Visibility, isAuthenticated))
            .OrderBy(i => i.DisplayOrder)
            .ToList();
    }

    private static Dictionary<Guid, List<NavigationMenuItem>> GetChildNavLookup(
        List<NavigationMenu> menus, string key, bool isAuthenticated)
    {
        var menu = menus.FirstOrDefault(m => m.MenuKey == key && m.IsActive);
        if (menu == null) return new();

        return menu.Items
            .Where(i => i.IsActive && i.ParentId != null && IsVisible(i.Visibility, isAuthenticated))
            .GroupBy(i => i.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.DisplayOrder).ToList());
    }

    private static bool IsVisible(PageVisibility visibility, bool isAuthenticated) => visibility switch
    {
        PageVisibility.All => true,
        PageVisibility.Guest => !isAuthenticated,
        PageVisibility.Customer => isAuthenticated,
        PageVisibility.Admin => false,
        _ => true
    };
}
