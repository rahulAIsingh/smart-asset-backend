using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace SmartAssetManager.Api.Services;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpEmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(SendEmailRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.To))
            throw new InvalidOperationException("Recipient email is required.");
        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new InvalidOperationException("Email subject is required.");
        if (string.IsNullOrWhiteSpace(_options.MailFrom) || string.IsNullOrWhiteSpace(_options.EPassword) || string.IsNullOrWhiteSpace(_options.Smtp))
            throw new InvalidOperationException("SMTP email settings are not configured.");

        using var message = new MailMessage
        {
            From = new MailAddress(_options.MailFrom),
            Subject = request.Subject,
            Body = request.Html ?? string.Empty,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(request.To));

        using var client = new SmtpClient(_options.Smtp, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.MailFrom, _options.EPassword),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent successfully. To={To} Subject={Subject}", request.To, request.Subject);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Email send cancelled. To={To} Subject={Subject}", request.To, request.Subject);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email. To={To} Subject={Subject}", request.To, request.Subject);
            throw;
        }
    }
}
