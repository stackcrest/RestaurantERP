namespace RestaurantERP.Application.Common;

public static class WhatsAppHelper
{
    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length >= 10 ? digits : null;
    }

    public static string? BuildChatUrl(string? phone, string message)
    {
        var digits = NormalizePhone(phone);
        if (string.IsNullOrEmpty(digits) || string.IsNullOrWhiteSpace(message)) return null;
        return $"https://wa.me/{digits}?text={Uri.EscapeDataString(message)}";
    }

    public static string ApplyTemplate(string template, IReadOnlyDictionary<string, string> tokens)
    {
        var result = template;
        foreach (var (key, value) in tokens)
            result = result.Replace($"{{{key}}}", value ?? "", StringComparison.OrdinalIgnoreCase);
        return result;
    }

    public static bool IsEnabledFlag(string? value) =>
        string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase)
        || value?.Trim() == "1"
        || string.Equals(value?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);
}
