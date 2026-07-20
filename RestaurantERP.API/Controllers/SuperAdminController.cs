using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.DTOs.SuperAdmin;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.API.Controllers;

[Route("api/v1/superadmin")]
[Authorize(Policy = "SuperAdminOnly")]
public class SuperAdminController : ApiControllerBase
{
    private readonly ISuperAdminService _superAdminService;

    public SuperAdminController(ISuperAdminService superAdminService, ICurrentUserService currentUser)
        : base(currentUser) => _superAdminService = superAdminService;

    [HttpGet("theme")]
    public async Task<IActionResult> GetTheme() =>
        FromResponse(await _superAdminService.GetThemeSettingsAsync());

    [HttpPut("theme")]
    public async Task<IActionResult> UpdateTheme([FromBody] UpdateThemeDto dto) =>
        FromResponse(await _superAdminService.UpdateThemeAsync(dto));

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings() =>
        FromResponse(await _superAdminService.GetSettingsAsync());

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] List<ApplicationSettingDto> settings) =>
        FromResponse(await _superAdminService.UpdateSettingsAsync(settings));

    [HttpGet("commission-rules")]
    public async Task<IActionResult> GetCommissionRules() =>
        FromResponse(await _superAdminService.GetCommissionRulesAsync());

    [HttpPost("commission-rules")]
    public async Task<IActionResult> CreateCommissionRule([FromBody] CreateCommissionRuleDto dto) =>
        FromResponse(await _superAdminService.CreateCommissionRuleAsync(dto));

    [HttpGet("pages")]
    public async Task<IActionResult> GetPages() =>
        FromResponse(await _superAdminService.GetPagesAsync());

    [HttpPost("pages")]
    public async Task<IActionResult> CreatePage([FromBody] CreateUiPageDto dto) =>
        FromResponse(await _superAdminService.CreatePageAsync(dto));

    [HttpPut("pages/{id:guid}")]
    public async Task<IActionResult> UpdatePage(Guid id, [FromBody] UpdateUiPageDto dto) =>
        FromResponse(await _superAdminService.UpdatePageAsync(id, dto));

    [HttpPost("pages/{id:guid}/publish")]
    public async Task<IActionResult> PublishPage(Guid id) =>
        FromResponse(await _superAdminService.PublishPageAsync(id));

    [HttpGet("feature-flags")]
    public async Task<IActionResult> GetFeatureFlags() =>
        FromResponse(await _superAdminService.GetFeatureFlagsAsync());

    [HttpPut("feature-flags/{key}")]
    public async Task<IActionResult> UpdateFeatureFlag(string key, [FromBody] FeatureFlagUpdateRequest request) =>
        FromResponse(await _superAdminService.UpdateFeatureFlagAsync(key, request.Enabled));

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() =>
        FromResponse(await _superAdminService.GetAllUsersAsync());

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] PagedRequest paging) =>
        FromResponse(await _superAdminService.GetAuditLogsAsync(paging));
}

public record FeatureFlagUpdateRequest(bool Enabled);
