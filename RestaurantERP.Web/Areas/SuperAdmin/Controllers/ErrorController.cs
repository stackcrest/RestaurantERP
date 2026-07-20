using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Web.Models;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class ErrorController : Controller
{
    [Route("SuperAdmin/NotFound")]
    public IActionResult PageNotFound()
    {
        Response.StatusCode = 404;
        return View("NotFound", new ErrorViewModel
        {
            StatusCode = 404,
            Title = "Not found",
            Message = "The user or record you requested does not exist or may have been removed."
        });
    }
}
