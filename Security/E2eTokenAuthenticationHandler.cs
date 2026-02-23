using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SmartAssetManager.Api.Security;

public class E2eTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "E2E";
    private const string Prefix = "e2e::";

    public E2eTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) ||
            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = authHeader["Bearer ".Length..].Trim();
        if (!token.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid E2E token format."));
        }

        var parts = token.Split("::", StringSplitOptions.None);
        if (parts.Length != 3)
        {
            return Task.FromResult(AuthenticateResult.Fail("E2E token must match e2e::<email>::<role>."));
        }

        var email = parts[1].Trim().ToLowerInvariant();
        var role = NormalizeRole(parts[2]);
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return Task.FromResult(AuthenticateResult.Fail("E2E token email is invalid."));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return Task.FromResult(AuthenticateResult.Fail("E2E token role is invalid."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, email),
            new("preferred_username", email),
            new("upn", email),
            new("unique_name", email),
            new(ClaimTypes.Role, role),
            new("roles", role)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static string NormalizeRole(string value)
    {
        var role = value.Trim().ToLowerInvariant();
        return role switch
        {
            "admin" => "admin",
            "support" => "support",
            "pm" => "pm",
            "boss" => "boss",
            "user" => "user",
            "it" => "it",
            _ => string.Empty
        };
    }
}
