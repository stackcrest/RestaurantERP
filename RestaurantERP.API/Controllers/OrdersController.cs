using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.DTOs.Orders;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.API.Controllers;

[Route("api/v1/orders")]
[Authorize(Policy = "CustomerOrAbove")]
public class OrdersController : ApiControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService, ICurrentUserService currentUser)
        : base(currentUser) => _orderService = orderService;

    [HttpPost]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _orderService.PlaceOrderAsync(userId.Value, dto));
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] PagedRequest paging)
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _orderService.GetOrdersAsync(
            IsAdmin ? null : userId, IsAdmin, paging));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponseOrNotFound(await _orderService.GetOrderAsync(id, userId, IsAdmin));
    }

    [HttpGet("by-number/{orderNumber}")]
    public async Task<IActionResult> GetByNumber(string orderNumber)
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponseOrNotFound(await _orderService.GetOrderByNumberAsync(orderNumber, userId));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request) =>
        FromResponse(await _orderService.UpdateOrderStatusAsync(id, request.Status));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _orderService.CancelOrderAsync(id, userId.Value));
    }

    [HttpPost("{id:guid}/reorder")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> Reorder(Guid id)
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _orderService.ReorderAsync(id, userId.Value));
    }
}

public record UpdateStatusRequest(string Status);
