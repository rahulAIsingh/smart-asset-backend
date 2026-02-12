using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SmartAssetManager.Api.Models.Compat;
using SmartAssetManager.Api.Services;

namespace SmartAssetManager.Api.Controllers;

[ApiController]
[Route("api/compat/db")]
public class CompatDbController : ControllerBase
{
    private readonly ICompatDbService _service;

    public CompatDbController(ICompatDbService service)
    {
        _service = service;
    }

    [HttpPost("{entity}/list")]
    public async Task<IActionResult> List(string entity, [FromBody] CompatListRequest? request, CancellationToken cancellationToken)
    {
        var rows = await _service.ListAsync(entity, request ?? new CompatListRequest(), cancellationToken);
        return Ok(rows);
    }

    [HttpPost("{entity}/create")]
    public async Task<IActionResult> Create(string entity, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(entity, payload, cancellationToken);
        return Ok(created);
    }

    [HttpPatch("{entity}/{id}")]
    public async Task<IActionResult> Update(string entity, string id, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateAsync(entity, id, payload, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{entity}/{id}")]
    public async Task<IActionResult> Delete(string entity, string id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(entity, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("batch")]
    public async Task<IActionResult> Batch([FromBody] CompatBatchRequest request, CancellationToken cancellationToken)
    {
        var affectedRows = await _service.ExecuteBatchAsync(request, cancellationToken);
        return Ok(new { affectedRows });
    }
}
