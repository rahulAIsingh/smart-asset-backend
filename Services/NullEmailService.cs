namespace SmartAssetManager.Api.Services;

public class NullEmailService : IEmailService
{
    private readonly ILogger<NullEmailService> _logger;

    public NullEmailService(ILogger<NullEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(SendEmailRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email request captured. To={To}, Subject={Subject}", request.To, request.Subject);
        return Task.CompletedTask;
    }
}
