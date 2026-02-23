using Microsoft.AspNetCore.Mvc;
using SmartAssetManager.Api.Services;

using Microsoft.AspNetCore.Authorization;

namespace SmartAssetManager.Api.Controllers;

public record CompatEmailBody(string To, string Subject, string Html);
public record CompatEmailTestBody(string To);

[ApiController]
[Route("api/compat/notifications")]
[Authorize]
public class CompatNotificationsController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<CompatNotificationsController> _logger;

    public CompatNotificationsController(IEmailService emailService, ILogger<CompatNotificationsController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("email")]
    public async Task<IActionResult> Email([FromBody] CompatEmailBody request, CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendAsync(new SendEmailRequest(request.To, request.Subject, request.Html), cancellationToken);
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email send failed. To={To}, Subject={Subject}", request.To, request.Subject);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                ok = false,
                error = "Email send failed",
                detail = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }

    [HttpPost("email/test")]
    public async Task<IActionResult> EmailTest([FromBody] CompatEmailTestBody request, CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var html = $@"
                <div style=""font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:auto;border:1px solid #e2e8f0;border-radius:10px;padding:20px"">
                    <h2 style=""margin:0 0 10px;color:#0d9488"">Smart Asset Manager - SMTP Test</h2>
                    <p style=""margin:0 0 8px"">Email delivery is working.</p>
                    <p style=""margin:0;color:#64748b;font-size:13px"">Generated at: {now:yyyy-MM-dd HH:mm:ss} UTC</p>
                </div>";

            await _emailService.SendAsync(
                new SendEmailRequest(request.To, "Smart Asset Manager - SMTP Test", html),
                cancellationToken
            );

            return Ok(new
            {
                ok = true,
                message = "Test email sent successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email test failed. To={To}", request.To);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                ok = false,
                error = "Email test failed",
                detail = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }
}
