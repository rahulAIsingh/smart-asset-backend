using System.IdentityModel.Tokens.Jwt;
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
                    ?? User.FindFirstValue("upn")
                    ?? User.FindFirstValue("unique_name");
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

    [Authorize]
    [HttpGet("debug-token")]
    public IActionResult DebugToken()
    {
        var auth = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Missing bearer token" });
        }

        var token = auth["Bearer ".Length..].Trim();
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return Ok(new
            {
                aud = jwt.Audiences.FirstOrDefault(),
                iss = jwt.Issuer,
                tid = jwt.Claims.FirstOrDefault(c => c.Type == "tid")?.Value,
                scp = jwt.Claims.FirstOrDefault(c => c.Type == "scp")?.Value,
                roles = jwt.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToArray(),
                exp = jwt.Claims.FirstOrDefault(c => c.Type == "exp")?.Value
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Token parse failed", detail = ex.Message });
        }
    }
}
