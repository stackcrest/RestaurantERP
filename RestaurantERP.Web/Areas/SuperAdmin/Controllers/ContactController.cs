using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class ContactController : Controller
{
    public IActionResult Index() => Redirect("/Admin/Contact");

    public IActionResult Details(Guid id) => Redirect($"/Admin/Contact/Details/{id}");
}
