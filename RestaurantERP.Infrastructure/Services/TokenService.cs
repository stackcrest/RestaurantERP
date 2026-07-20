using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Infrastructure.Data;
using RestaurantERP.Infrastructure.Identity;

namespace RestaurantERP.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;

    public TokenService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(ApplicationUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        });
        await _context.SaveChangesAsync();

        return (accessToken, refreshToken, expires);
    }

    public async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt, Guid UserId)?> RefreshTokenAsync(string refreshToken)
    {
        var stored = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshToken && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow);

        if (stored == null) return null;

        stored.IsRevoked = true;
        await _context.SaveChangesAsync();
        var roles = await _userManager.GetRolesAsync(stored.User);
        var tokens = await GenerateTokensAsync(stored.User, roles);
        return (tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt, stored.UserId);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken);
        if (stored != null)
        {
            stored.IsRevoked = true;
            await _context.SaveChangesAsync();
        }
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
