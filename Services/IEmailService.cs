namespace SmartAssetManager.Api.Services;

public record SendEmailRequest(string To, string Subject, string Html);

public interface IEmailService
{
    Task SendAsync(SendEmailRequest request, CancellationToken cancellationToken);
}
