using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartAssetManager.Api.Services;

namespace SmartAssetManager.Api.Controllers.Test;

public record SeedScenarioRequest(string? Scenario);

[ApiController]
[Route("api/test")]
[Authorize]
[DisableRateLimiting]
[ApiExplorerSettings(IgnoreApi = true)]
public class TestHarnessController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly TestDataSeeder _seeder;

    public TestHarnessController(IWebHostEnvironment environment, TestDataSeeder seeder)
    {
        _environment = environment;
        _seeder = seeder;
    }

    [HttpPost("reset-seed")]
    public async Task<IActionResult> ResetSeed(CancellationToken cancellationToken)
    {
        if (!_environment.IsEnvironment("Testing"))
        {
            return NotFound();
        }

        await _seeder.ResetAndSeedAsync(cancellationToken);
        return Ok(new { ok = true, mode = "baseline" });
    }

    [HttpPost("seed-scenario")]
    public async Task<IActionResult> SeedScenario([FromBody] SeedScenarioRequest? request, CancellationToken cancellationToken)
    {
        if (!_environment.IsEnvironment("Testing"))
        {
            return NotFound();
        }

        var scenario = request?.Scenario ?? "baseline";
        await _seeder.SeedScenarioAsync(scenario, cancellationToken);
        return Ok(new { ok = true, scenario });
    }
}
