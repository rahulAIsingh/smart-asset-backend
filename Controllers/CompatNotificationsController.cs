using Microsoft.AspNetCore.Mvc;
using SmartAssetManager.Api.Services;

namespace SmartAssetManager.Api.Controllers;

public record CompatEmailBody(string To, string Subject, string Html);

[ApiController]
[Route("api/compat/notifications")]
public class CompatNotificationsController : ControllerBase
{
    private readonly IEmailService _emailService;

    public CompatNotificationsController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("email")]
    public async Task<IActionResult> Email([FromBody] CompatEmailBody request, CancellationToken cancellationToken)
    {
        await _emailService.SendAsync(new SendEmailRequest(request.To, request.Subject, request.Html), cancellationToken);
        return Ok(new { ok = true });
    }
}
