using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Application.Options;

namespace RestaurantERP.Infrastructure.Services;

public class MailjetEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly MailjetOptions _options;
    private readonly ILogger<MailjetEmailSender> _logger;

    public MailjetEmailSender(HttpClient http, IOptions<MailjetOptions> options, ILogger<MailjetEmailSender> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string htmlBody, string? textBody = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            _logger.LogError("Mailjet ApiKey/SecretKey is missing. Email to {Email} was not sent.", toEmail);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            _logger.LogError("Mailjet FromEmail is missing. Email to {Email} was not sent.", toEmail);
            return false;
        }

        var payload = new
        {
            Messages = new[]
            {
                new
                {
                    From = new { Email = _options.FromEmail, Name = _options.FromName },
                    To = new[] { new { Email = toEmail } },
                    Subject = subject,
                    TextPart = textBody ?? StripHtml(htmlBody),
                    HTMLPart = htmlBody
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mailjet.com/v3.1/send");
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.ApiKey}:{_options.SecretKey}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Mailjet send failed ({Status}): {Body}", (int)response.StatusCode, body);
                return false;
            }

            _logger.LogInformation("Password reset email sent to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mailjet send threw for {Email}", toEmail);
            return false;
        }
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ").Trim();
    }
}
