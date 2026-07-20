using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Web.Models;

namespace RestaurantERP.Web.Controllers;

public partial class HomeController
{
    [Route("Home/Error")]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var model = new ErrorViewModel
        {
            RequestId = HttpContext.TraceIdentifier,
            StatusCode = 500,
            Title = "Something went wrong",
            Message = "An unexpected error occurred. The application is still running — please try again or go back home.",
            IsDevelopment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
            Detail = feature?.Error.Message
        };
        Response.StatusCode = 500;
        return View(model);
    }

    [Route("Home/PageNotFound")]
    public IActionResult PageNotFound(int? code)
    {
        var model = new ErrorViewModel
        {
            RequestId = HttpContext.TraceIdentifier,
            StatusCode = code ?? 404,
            Title = "Page not found",
            Message = "The page or record you requested does not exist or may have been removed."
        };
        Response.StatusCode = 404;
        return View("PageNotFound", model);
    }
}
