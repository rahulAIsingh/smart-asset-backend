using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartAssetManager.Api.Data;
using SmartAssetManager.Api.Models.Common;
using SmartAssetManager.Api.Security;
using SmartAssetManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var isTesting = builder.Environment.IsEnvironment("Testing");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .Select(x => new
            {
                field = x.Key,
                messages = x.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage)
            });

        return new BadRequestObjectResult(ApiEnvelope.Fail("Validation failed.", errors));
    };
});
var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var fallbackCorsOrigins = new[]
{
    "http://localhost:3000",
    "http://127.0.0.1:3000",
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "http://localhost:5174",
    "http://127.0.0.1:5174"
};
var corsOrigins = configuredCorsOrigins.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
if (corsOrigins.Length == 0)
{
    corsOrigins = fallbackCorsOrigins;
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins(corsOrigins);
    });
});
if (!isTesting)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsJsonAsync(
                ApiEnvelope.Fail("Too many requests. Please retry later."),
                cancellationToken);
        };
        options.AddFixedWindowLimiter("api", limiter =>
        {
            limiter.PermitLimit = 100;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 0;
        });
    });
}
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ICompatDbService, CompatDbService>();
builder.Services.AddScoped<TestDataSeeder>();
builder.Services.Configure<SmtpEmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailService>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SmtpEmailOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.MailFrom)
        && !string.IsNullOrWhiteSpace(options.EPassword)
        && !string.IsNullOrWhiteSpace(options.Smtp)
        && options.Port > 0)
    {
        return ActivatorUtilities.CreateInstance<SmtpEmailService>(sp);
    }

    return ActivatorUtilities.CreateInstance<NullEmailService>(sp);
});

if (isTesting)
{
    builder.Services
        .AddAuthentication(E2eTokenAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, E2eTokenAuthenticationHandler>(
            E2eTokenAuthenticationHandler.SchemeName,
            _ => { });
}
else
{
    var authority = builder.Configuration["Auth:Authority"];
    var audience = builder.Configuration["Auth:Audience"];
    var audienceGuid = audience?.StartsWith("api://", StringComparison.OrdinalIgnoreCase) == true
        ? audience["api://".Length..]
        : audience;
    var tenantId = builder.Configuration["Auth:TenantId"];
    if (string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(authority))
    {
        try
        {
            var uri = new Uri(authority);
            tenantId = uri.Segments
                .Select(s => s.Trim('/'))
                .FirstOrDefault(s => Guid.TryParse(s, out _));
        }
        catch
        {
            // Ignore parse failure and let standard authority validation handle it.
        }
    }

    var validIssuers = new List<string>();
    if (!string.IsNullOrWhiteSpace(tenantId))
    {
        validIssuers.Add($"https://login.microsoftonline.com/{tenantId}/v2.0");
        validIssuers.Add($"https://sts.windows.net/{tenantId}/");
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.IncludeErrorDetails = builder.Environment.IsDevelopment();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                RoleClaimType = "roles",
                NameClaimType = "name",
                ValidAudiences = new[] { audience, audienceGuid }.Where(v => !string.IsNullOrWhiteSpace(v)),
                ValidIssuers = validIssuers.Count > 0 ? validIssuers : null
            };
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtAuth")
                        .LogWarning(context.Exception, "JWT authentication failed for {Path}", context.HttpContext.Request.Path);
                    return Task.CompletedTask;
                },
                OnChallenge = async context =>
                {
                    context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtAuth")
                        .LogWarning("JWT challenge for {Path}. Error={Error}; Description={Description}",
                            context.HttpContext.Request.Path, context.Error, context.ErrorDescription);

                    if (!context.Response.HasStarted)
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(ApiEnvelope.Fail("Unauthorized."));
                    }
                },
                OnForbidden = async context =>
                {
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(ApiEnvelope.Fail("Forbidden."));
                    }
                }
            };
        });
}

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(handler =>
{
    handler.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var ex = feature?.Error;

        var status = ex switch
        {
            JsonException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            DbUpdateException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        var message = status == StatusCodes.Status500InternalServerError
            ? "An unexpected server error occurred."
            : ex?.Message ?? "Request failed.";

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiEnvelope.Fail(message));
    });
});

app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    if (!response.HasStarted && string.IsNullOrWhiteSpace(response.ContentType))
    {
        response.ContentType = "application/json";
        await response.WriteAsJsonAsync(ApiEnvelope.Fail($"Request failed with status {response.StatusCode}."));
    }
});

app.UseHttpsRedirection();
app.UseCors("DevCors");
if (!isTesting)
{
    app.UseRateLimiter();
}
app.UseAuthentication();
app.UseAuthorization();
if (isTesting)
{
    app.MapControllers();
}
else
{
    app.MapControllers().RequireRateLimiting("api");
}
app.MapGet("/", () => Results.Ok(ApiEnvelope.Ok(new { service = "smart-asset-manager-api" }, "API is running."))).AllowAnonymous();
app.MapGet("/health", () => Results.Ok(ApiEnvelope.Ok(new { status = "ok" }, "Healthy"))).AllowAnonymous();

app.Run();
