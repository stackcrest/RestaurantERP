using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly ICurrentUserService CurrentUser;

    protected ApiControllerBase(ICurrentUserService currentUser) => CurrentUser = currentUser;

    protected IActionResult FromResponse<T>(ApiResponse<T> response) =>
        response.Success ? Ok(response) : BadRequest(response);

    protected IActionResult FromResponseOrNotFound<T>(ApiResponse<T> response) =>
        response.Success ? Ok(response) : NotFound(response);

    protected Guid? RequireUserId() => CurrentUser.UserId;

    protected bool IsAdmin => CurrentUser.Roles.Any(r =>
        r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
        r.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));
}
