namespace RestaurantERP.Application.Interfaces;

public interface IEmailSender
{
    Task<bool> SendAsync(string toEmail, string subject, string htmlBody, string? textBody = null, CancellationToken cancellationToken = default);
}
