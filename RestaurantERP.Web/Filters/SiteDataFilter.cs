using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Web.Filters;

public class SiteDataFilter : IAsyncActionFilter
{
    private readonly IApplicationContentService _content;

    public SiteDataFilter(IApplicationContentService content)
    {
        _content = content;
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
                controller.ViewBag.MainNav = GetNavItems(menus, "main");
                controller.ViewBag.FooterNav = GetNavItems(menus, "footer");
            }
            catch
            {
                controller.ViewBag.MainNav = new List<NavigationMenuItem>();
                controller.ViewBag.FooterNav = new List<NavigationMenuItem>();
            }
        }

        await next();
    }

    private static List<NavigationMenuItem> GetNavItems(List<NavigationMenu> menus, string key) =>
        menus.FirstOrDefault(m => m.MenuKey == key && m.IsActive)?
            .Items.Where(i => i.IsActive)
            .OrderBy(i => i.DisplayOrder)
            .ToList() ?? [];
}
