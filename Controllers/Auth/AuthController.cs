using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartAssetManager.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirstValue("preferred_username")
                    ?? User.FindFirstValue("upn");
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? email ?? "unknown";
        var displayName = User.FindFirstValue("name") ?? User.Identity?.Name ?? email;

        var roles = User.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(new
        {
            user = new
            {
                id,
                email,
                displayName,
                role = roles.FirstOrDefault() ?? "user",
                roles
            }
        });
    }
}
