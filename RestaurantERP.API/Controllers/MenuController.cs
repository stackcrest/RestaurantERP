using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.DTOs.Menu;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.API.Controllers;

[Route("api/v1/menu")]
public class MenuController : ApiControllerBase
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService, ICurrentUserService currentUser)
        : base(currentUser) => _menuService = menuService;

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories() =>
        FromResponse(await _menuService.GetCategoriesAsync());

    [HttpGet("items")]
    [AllowAnonymous]
    public async Task<IActionResult> GetItems([FromQuery] MenuFilterDto filter) =>
        FromResponse(await _menuService.GetMenuItemsAsync(filter));

    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeatured() =>
        FromResponse(await _menuService.GetFeaturedItemsAsync());

    [HttpGet("items/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetItem(Guid id) =>
        FromResponseOrNotFound(await _menuService.GetMenuItemAsync(id));

    [HttpPost("items")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> CreateItem([FromBody] CreateMenuItemDto dto) =>
        FromResponse(await _menuService.CreateMenuItemAsync(dto));

    [HttpPut("items/{id:guid}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateMenuItemDto dto) =>
        FromResponse(await _menuService.UpdateMenuItemAsync(id, dto));

    [HttpDelete("items/{id:guid}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> DeleteItem(Guid id) =>
        FromResponse(await _menuService.DeleteMenuItemAsync(id));

    [HttpPatch("items/{id:guid}/availability")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> SetAvailability(Guid id, [FromBody] SetAvailabilityRequest request) =>
        FromResponse(await _menuService.SetAvailabilityAsync(id, request.IsAvailable));
}

public record SetAvailabilityRequest(bool IsAvailable);
