namespace RestaurantERP.Application.DTOs.Auth;

public record RegisterDto(string Email, string Password, string FullName, string? PhoneNumber);
public record LoginDto(string Email, string Password);
public record UpdateProfileDto(string FullName, string? PhoneNumber, string? Address, string? City, string? PinCode, string? AvatarUrl);

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserProfileDto User { get; set; } = null!;
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PinCode { get; set; }
    public int LoyaltyPoints { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UserListDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
