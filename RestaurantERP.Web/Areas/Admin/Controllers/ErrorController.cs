using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Web.Models;

namespace RestaurantERP.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ErrorController : Controller
{
    [Route("Admin/NotFound")]
    public IActionResult PageNotFound()
    {
        Response.StatusCode = 404;
        return View("NotFound", new ErrorViewModel
        {
            StatusCode = 404,
            Title = "Not found",
            Message = "The record you requested does not exist or may have been removed."
        });
    }
}
