using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.DTOs.Orders;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.API.Controllers;

[Route("api/v1/coupons")]
public class CouponsController : ApiControllerBase
{
    private readonly ICouponService _couponService;

    public CouponsController(ICouponService couponService, ICurrentUserService currentUser)
        : base(currentUser) => _couponService = couponService;

    [HttpGet("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> Validate([FromQuery] string code, [FromQuery] decimal orderAmount) =>
        FromResponse(await _couponService.ValidateCouponAsync(code, orderAmount));

    [HttpGet]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> GetCoupons() =>
        FromResponse(await _couponService.GetCouponsAsync());

    [HttpPost]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> Create([FromBody] CreateCouponDto dto) =>
        FromResponse(await _couponService.CreateCouponAsync(dto));
}
