using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.API.Controllers;

[Route("api/v1/public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly IPublicService _publicService;

    public PublicController(IPublicService publicService) => _publicService = publicService;

    [HttpGet("theme")]
    public async Task<IActionResult> GetTheme() =>
        Ok(await _publicService.GetThemeAsync());

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings() =>
        Ok(await _publicService.GetPublicSettingsAsync());

    [HttpGet("navigation/{menuKey}")]
    public async Task<IActionResult> GetNavigation(string menuKey) =>
        Ok(await _publicService.GetNavigationAsync(menuKey));

    [HttpGet("pages/{slug}")]
    public async Task<IActionResult> GetPageBySlug(string slug) =>
        Ok(await _publicService.GetPageBySlugAsync(slug));

    [HttpGet("homepage")]
    public async Task<IActionResult> GetHomepage() =>
        Ok(await _publicService.GetHomepageAsync());
}
