using Microsoft.AspNetCore.Mvc;

namespace RestaurantERP.Web.Extensions;

public static class ControllerExtensions
{
    public static IActionResult ItemNotFound(this Controller controller, string message, string action = "Index")
    {
        controller.TempData["Error"] = message;
        return controller.RedirectToAction(action);
    }
}
