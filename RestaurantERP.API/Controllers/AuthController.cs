using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.DTOs.Auth;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.API.Controllers;

[Route("api/v1/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService, ICurrentUserService currentUser)
        : base(currentUser) => _authService = authService;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto) =>
        FromResponse(await _authService.RegisterAsync(dto));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto) =>
        FromResponse(await _authService.LoginAsync(dto));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request) =>
        FromResponse(await _authService.RefreshTokenAsync(request.RefreshToken));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request) =>
        FromResponse(await _authService.LogoutAsync(request.RefreshToken));

    [HttpGet("profile")]
    [Authorize(Policy = "CustomerOrAbove")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _authService.GetProfileAsync(userId.Value));
    }

    [HttpPut("profile")]
    [Authorize(Policy = "CustomerOrAbove")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _authService.UpdateProfileAsync(userId.Value, dto));
    }

    [HttpPut("fcm-token")]
    [Authorize(Policy = "CustomerOrAbove")]
    public async Task<IActionResult> UpdateFcmToken([FromBody] FcmTokenRequest request)
    {
        var userId = RequireUserId();
        if (userId == null) return Unauthorized();
        return FromResponse(await _authService.UpdateFcmTokenAsync(userId.Value, request.FcmToken));
    }
}

public record RefreshTokenRequest(string RefreshToken);
public record FcmTokenRequest(string FcmToken);
