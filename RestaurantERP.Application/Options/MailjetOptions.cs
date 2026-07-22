namespace RestaurantERP.Application.Options;

public class MailjetOptions
{
    public const string SectionName = "Mailjet";

    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Restaurant ERP";
}
