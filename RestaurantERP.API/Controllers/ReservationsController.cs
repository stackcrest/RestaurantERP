using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.DTOs.Orders;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.API.Controllers;

[Route("api/v1/reservations")]
[Authorize(Policy = "CustomerOrAbove")]
public class ReservationsController : ApiControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService, ICurrentUserService currentUser)
        : base(currentUser) => _reservationService = reservationService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _reservationService.CreateReservationAsync(userId.Value, dto));
    }

    [HttpGet]
    public async Task<IActionResult> GetReservations()
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _reservationService.GetReservationsAsync(
            IsAdmin ? null : userId, IsAdmin));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateReservationStatusRequest request) =>
        FromResponse(await _reservationService.UpdateReservationStatusAsync(id, request.Status));
}

public record UpdateReservationStatusRequest(string Status);
