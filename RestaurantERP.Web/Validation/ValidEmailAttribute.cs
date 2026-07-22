using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace RestaurantERP.Web.Validation;

/// <summary>Requires a real-looking email (format + public domain). Rejects .local / disposable placeholders.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ValidEmailAttribute : ValidationAttribute
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> BlockedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "quick.local",
        "localhost",
        "example.com",
        "example.org",
        "example.net",
        "test.com",
        "mailinator.com",
        "tempmail.com",
        "guerrillamail.com",
        "10minutemail.com",
        "yopmail.com"
    };

    public ValidEmailAttribute()
        : base("Enter a valid email address (e.g. name@gmail.com).")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return false;
        var email = value.ToString()?.Trim() ?? string.Empty;
        if (email.Length is < 5 or > 254) return false;
        if (email.Contains(' ') || email.Count(c => c == '@') != 1) return false;
        if (!EmailRegex.IsMatch(email)) return false;

        var domain = email[(email.IndexOf('@') + 1)..];
        if (domain.StartsWith('.') || domain.EndsWith('.') || domain.Contains("..")) return false;
        if (domain.EndsWith(".local", StringComparison.OrdinalIgnoreCase)) return false;
        if (BlockedDomains.Contains(domain)) return false;

        return true;
    }
}
