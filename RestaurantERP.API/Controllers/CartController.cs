using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.DTOs.Cart;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.API.Controllers;

[Route("api/v1/cart")]
[Authorize(Policy = "CustomerOnly")]
public class CartController : ApiControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService, ICurrentUserService currentUser)
        : base(currentUser) => _cartService = cartService;

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _cartService.GetCartAsync(userId.Value));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _cartService.GetCartSummaryAsync(userId.Value));
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _cartService.AddToCartAsync(userId.Value, dto));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemDto dto)
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _cartService.UpdateCartItemAsync(userId.Value, dto));
    }

    [HttpDelete("{cartItemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid cartItemId)
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _cartService.RemoveFromCartAsync(userId.Value, cartItemId));
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _cartService.ClearCartAsync(userId.Value));
    }
}
