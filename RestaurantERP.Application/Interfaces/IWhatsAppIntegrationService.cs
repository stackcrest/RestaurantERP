namespace RestaurantERP.Application.Interfaces;

public interface IWhatsAppIntegrationService
{
    bool IsEnabled();
    string? GetPhoneNumber();
    string? GetSupportChatUrl(string? userName, string? userEmail, string? siteName);
    string? GetOrderChatUrl(string orderNumber, string? userName, string? userEmail, string? siteName);
}
