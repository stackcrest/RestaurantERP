using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.Web.Filters;

public class AdminSectionAccessFilter : IAsyncActionFilter
{
    private readonly IPermissionService _permissionService;

    public AdminSectionAccessFilter(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.RouteData.Values["area"]?.ToString() != "Admin")
        {
            await next();
            return;
        }

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var roles = user.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (_permissionService.IsSuperAdmin(roles))
        {
            SetViewData(context, AdminMenuSections.All.ToDictionary(s => s.Key, _ => (true, true)));
            await next();
            return;
        }

        var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
        var action = context.RouteData.Values["action"]?.ToString() ?? "";
        var sectionKey = AdminMenuSections.GetSectionForController(controller);

        var userAccess = await _permissionService.GetUserSectionAccessAsync(roles);
        SetViewData(context, userAccess);

        if (sectionKey == null)
        {
            await next();
            return;
        }

        var requiresWrite = AdminMenuSections.IsWriteAction(context.HttpContext.Request.Method, action);
        var allowed = requiresWrite
            ? await _permissionService.UserCanWriteSectionAsync(roles, sectionKey)
            : await _permissionService.UserCanReadSectionAsync(roles, sectionKey);

        if (!allowed)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", new { area = "" });
            return;
        }

        if (context.Controller is Controller controllerInstance)
        {
            controllerInstance.ViewBag.CanWriteSection = userAccess.GetValueOrDefault(sectionKey).CanWrite;
        }

        await next();
    }

    private static void SetViewData(ActionExecutingContext context, Dictionary<string, (bool CanRead, bool CanWrite)> access)
    {
        if (context.Controller is Controller controller)
            controller.ViewData["AdminSectionAccess"] = access;
    }
}
