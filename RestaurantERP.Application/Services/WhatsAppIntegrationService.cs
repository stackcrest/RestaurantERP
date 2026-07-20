using RestaurantERP.Application.Common;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.Application.Services;

public class WhatsAppIntegrationService : IWhatsAppIntegrationService
{
    private readonly IApplicationContentService _content;

    public WhatsAppIntegrationService(IApplicationContentService content)
    {
        _content = content;
    }

    public bool IsEnabled() =>
        WhatsAppHelper.IsEnabledFlag(_content.Get("integrations.whatsapp.enabled", "true"));

    public string? GetPhoneNumber() =>
        WhatsAppHelper.NormalizePhone(_content.Get("integrations.whatsapp.number"));

    public string? GetSupportChatUrl(string? userName, string? userEmail, string? siteName)
    {
        if (!IsEnabled()) return null;

        var template = _content.Get(
            "integrations.whatsapp.greeting",
            "Hi! I'm {name}. I need help with my order at {siteName}.");

        var message = WhatsAppHelper.ApplyTemplate(template, new Dictionary<string, string>
        {
            ["name"] = userName ?? "Customer",
            ["email"] = userEmail ?? "",
            ["siteName"] = siteName ?? _content.Get("site.name", "Restaurant ERP")
        });

        return WhatsAppHelper.BuildChatUrl(_content.Get("integrations.whatsapp.number"), message);
    }

    public string? GetOrderChatUrl(string orderNumber, string? userName, string? userEmail, string? siteName)
    {
        if (!IsEnabled()) return null;

        var template = _content.Get(
            "integrations.whatsapp.order_message",
            "Hi, I'm {name} ({email}). I have a question about my order #{orderNumber}.");

        var message = WhatsAppHelper.ApplyTemplate(template, new Dictionary<string, string>
        {
            ["name"] = userName ?? "Customer",
            ["email"] = userEmail ?? "",
            ["orderNumber"] = orderNumber,
            ["siteName"] = siteName ?? _content.Get("site.name", "Restaurant ERP")
        });

        return WhatsAppHelper.BuildChatUrl(_content.Get("integrations.whatsapp.number"), message);
    }
}
