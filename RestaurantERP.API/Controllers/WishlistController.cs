using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.API.Controllers;

[Route("api/v1/wishlist")]
[Authorize(Policy = "CustomerOrAbove")]
public class WishlistController : ApiControllerBase
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService, ICurrentUserService currentUser)
        : base(currentUser) => _wishlistService = wishlistService;

    [HttpGet]
    public async Task<IActionResult> GetWishlist()
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _wishlistService.GetWishlistAsync(userId.Value));
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount()
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _wishlistService.GetWishlistCountAsync(userId.Value));
    }

    [HttpPost("toggle/{menuItemId:guid}")]
    public async Task<IActionResult> Toggle(Guid menuItemId)
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _wishlistService.ToggleWishlistAsync(userId.Value, menuItemId));
    }
}
