using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.API.Controllers;

[Route("api/v1/tables")]
public class TablesController : ApiControllerBase
{
    private readonly ITableService _tableService;

    public TablesController(ITableService tableService, ICurrentUserService currentUser)
        : base(currentUser) => _tableService = tableService;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetTables() =>
        FromResponse(await _tableService.GetTablesAsync());

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTableStatusRequest request) =>
        FromResponse(await _tableService.UpdateTableStatusAsync(id, request.Status));
}

public record UpdateTableStatusRequest(string Status);
