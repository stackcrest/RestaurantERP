using Microsoft.AspNetCore.Identity;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.DTOs.Auth;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto)
    {
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing != null)
            return ApiResponse<AuthResponseDto>.Fail("An account with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return ApiResponse<AuthResponseDto>.Fail("Registration failed.", result.Errors.Select(e => e.Description).ToList());

        await _userManager.AddToRoleAsync(user, "Customer");
        return await BuildAuthResponse(user, "Registration successful.");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !user.IsActive)
            return ApiResponse<AuthResponseDto>.Fail("Invalid email or password.");

        if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            return ApiResponse<AuthResponseDto>.Fail("Invalid email or password.");

        return await BuildAuthResponse(user, "Login successful.");
    }

    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(string refreshToken)
    {
        var result = await _tokenService.RefreshTokenAsync(refreshToken);
        if (result == null)
            return ApiResponse<AuthResponseDto>.Fail("Invalid or expired refresh token.");

        var user = await _userManager.FindByIdAsync(result.Value.UserId.ToString());
        if (user == null) return ApiResponse<AuthResponseDto>.Fail("User not found.");

        var roles = await _userManager.GetRolesAsync(user);
        return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
        {
            AccessToken = result.Value.AccessToken,
            RefreshToken = result.Value.RefreshToken,
            ExpiresAt = result.Value.ExpiresAt,
            User = MapUser(user, roles.ToList())
        });
    }

    public async Task<ApiResponse<bool>> LogoutAsync(string refreshToken)
    {
        await _tokenService.RevokeTokenAsync(refreshToken);
        return ApiResponse<bool>.Ok(true, "Logged out successfully.");
    }

    public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ApiResponse<UserProfileDto>.Fail("User not found.");
        var roles = await _userManager.GetRolesAsync(user);
        return ApiResponse<UserProfileDto>.Ok(MapUser(user, roles.ToList()));
    }

    public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ApiResponse<UserProfileDto>.Fail("User not found.");

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;
        user.Address = dto.Address;
        user.City = dto.City;
        user.PinCode = dto.PinCode;
        user.AvatarUrl = dto.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        return ApiResponse<UserProfileDto>.Ok(MapUser(user, roles.ToList()), "Profile updated.");
    }

    public async Task<ApiResponse<bool>> UpdateFcmTokenAsync(Guid userId, string fcmToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ApiResponse<bool>.Fail("User not found.");
        user.FcmToken = fcmToken;
        await _userManager.UpdateAsync(user);
        return ApiResponse<bool>.Ok(true);
    }

    private async Task<ApiResponse<AuthResponseDto>> BuildAuthResponse(ApplicationUser user, string message)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var tokens = await _tokenService.GenerateTokensAsync(user, roles);
        return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresAt = tokens.ExpiresAt,
            User = MapUser(user, roles.ToList())
        }, message);
    }

    private static UserProfileDto MapUser(ApplicationUser user, List<string> roles) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = user.AvatarUrl,
        Address = user.Address,
        City = user.City,
        PinCode = user.PinCode,
        LoyaltyPoints = user.LoyaltyPoints,
        Roles = roles
    };
}
