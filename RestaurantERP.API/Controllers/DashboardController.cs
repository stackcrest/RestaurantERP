using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.API.Controllers;

[Route("api/v1/dashboard")]
[Authorize(Policy = "AdminOrAbove")]
public class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService, ICurrentUserService currentUser)
        : base(currentUser) => _dashboardService = dashboardService;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary() =>
        FromResponse(await _dashboardService.GetSummaryAsync());

    [HttpGet("sales-chart")]
    public async Task<IActionResult> GetSalesChart([FromQuery] int days = 7) =>
        FromResponse(await _dashboardService.GetSalesChartAsync(days));
}
