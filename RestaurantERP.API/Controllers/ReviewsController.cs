using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.DTOs.Orders;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.API.Controllers;

[Route("api/v1/reviews")]
public class ReviewsController : ApiControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService, ICurrentUserService currentUser)
        : base(currentUser) => _reviewService = reviewService;

    [HttpPost]
    [Authorize(Policy = "CustomerOrAbove")]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _reviewService.CreateReviewAsync(userId.Value, dto));
    }

    [HttpGet("items/{menuItemId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetItemReviews(Guid menuItemId, [FromQuery] PagedRequest paging) =>
        FromResponse(await _reviewService.GetItemReviewsAsync(menuItemId, paging));
}
