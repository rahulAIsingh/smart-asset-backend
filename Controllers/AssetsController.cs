using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAssetManager.Api.Data;
using SmartAssetManager.Api.Domain.Entities;
using SmartAssetManager.Api.Models.Assets;
using SmartAssetManager.Api.Models.Common;

namespace SmartAssetManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("assets")]
[Route("api/assets")]
public class AssetsController : ControllerBase
{
    private static readonly Regex IdPattern = new("^[a-zA-Z0-9_-]{1,128}$", RegexOptions.Compiled);
    private static readonly Regex HtmlPattern = new("<[^>]+>", RegexOptions.Compiled);
    private readonly AppDbContext _db;

    public AssetsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var rows = await _db.Assets.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(ApiEnvelope.Ok(rows, "Assets fetched."));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        if (!IsSafeId(id))
        {
            return BadRequest(ApiEnvelope.Fail("Invalid asset id format."));
        }

        var row = await _db.Assets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound(ApiEnvelope.Fail("Asset not found."));
        }

        return Ok(ApiEnvelope.Ok(row, "Asset fetched."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssetDto dto, CancellationToken cancellationToken)
    {
        var unsafeField = FindUnsafeTextField(dto);
        if (unsafeField is not null)
        {
            return BadRequest(ApiEnvelope.Fail($"Invalid text in '{unsafeField}'. HTML/script input is not allowed."));
        }

        var now = DateTimeOffset.UtcNow;
        var row = new AssetEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = dto.Name.Trim(),
            Category = dto.Category.Trim(),
            SerialNumber = Normalize(dto.SerialNumber),
            DeviceSerialNumber = Normalize(dto.DeviceSerialNumber),
            Company = Normalize(dto.Company),
            Model = Normalize(dto.Model),
            Department = Normalize(dto.Department),
            WarrantyStart = Normalize(dto.WarrantyStart),
            WarrantyEnd = Normalize(dto.WarrantyEnd),
            WarrantyVendor = Normalize(dto.WarrantyVendor),
            Configuration = Normalize(dto.Configuration),
            Location = Normalize(dto.Location),
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "available" : dto.Status.Trim().ToLowerInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Assets.Add(row);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = row.Id }, ApiEnvelope.Ok(row, "Asset created."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateAssetDto dto, CancellationToken cancellationToken)
    {
        if (!IsSafeId(id))
        {
            return BadRequest(ApiEnvelope.Fail("Invalid asset id format."));
        }

        var unsafeField = FindUnsafeTextField(dto);
        if (unsafeField is not null)
        {
            return BadRequest(ApiEnvelope.Fail($"Invalid text in '{unsafeField}'. HTML/script input is not allowed."));
        }

        var row = await _db.Assets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound(ApiEnvelope.Fail("Asset not found."));
        }

        row.Name = dto.Name.Trim();
        row.Category = dto.Category.Trim();
        row.SerialNumber = Normalize(dto.SerialNumber);
        row.DeviceSerialNumber = Normalize(dto.DeviceSerialNumber);
        row.Company = Normalize(dto.Company);
        row.Model = Normalize(dto.Model);
        row.Department = Normalize(dto.Department);
        row.WarrantyStart = Normalize(dto.WarrantyStart);
        row.WarrantyEnd = Normalize(dto.WarrantyEnd);
        row.WarrantyVendor = Normalize(dto.WarrantyVendor);
        row.Configuration = Normalize(dto.Configuration);
        row.Location = Normalize(dto.Location);
        row.Status = string.IsNullOrWhiteSpace(dto.Status) ? row.Status : dto.Status.Trim().ToLowerInvariant();
        row.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiEnvelope.Ok(row, "Asset updated."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (!IsSafeId(id))
        {
            return BadRequest(ApiEnvelope.Fail("Invalid asset id format."));
        }

        var row = await _db.Assets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound(ApiEnvelope.Fail("Asset not found."));
        }

        _db.Assets.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiEnvelope.Ok(new { id }, "Asset deleted."));
    }

    private static bool IsSafeId(string id) => !string.IsNullOrWhiteSpace(id) && IdPattern.IsMatch(id);

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }

    private static string? FindUnsafeTextField(CreateAssetDto dto)
    {
        return FindUnsafeTextFieldCore(
            ("name", dto.Name),
            ("category", dto.Category),
            ("serialNumber", dto.SerialNumber),
            ("deviceSerialNumber", dto.DeviceSerialNumber),
            ("company", dto.Company),
            ("model", dto.Model),
            ("department", dto.Department),
            ("warrantyStart", dto.WarrantyStart),
            ("warrantyEnd", dto.WarrantyEnd),
            ("warrantyVendor", dto.WarrantyVendor),
            ("configuration", dto.Configuration),
            ("location", dto.Location),
            ("status", dto.Status)
        );
    }

    private static string? FindUnsafeTextField(UpdateAssetDto dto)
    {
        return FindUnsafeTextFieldCore(
            ("name", dto.Name),
            ("category", dto.Category),
            ("serialNumber", dto.SerialNumber),
            ("deviceSerialNumber", dto.DeviceSerialNumber),
            ("company", dto.Company),
            ("model", dto.Model),
            ("department", dto.Department),
            ("warrantyStart", dto.WarrantyStart),
            ("warrantyEnd", dto.WarrantyEnd),
            ("warrantyVendor", dto.WarrantyVendor),
            ("configuration", dto.Configuration),
            ("location", dto.Location),
            ("status", dto.Status)
        );
    }

    private static string? FindUnsafeTextFieldCore(params (string Name, string? Value)[] fields)
    {
        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.Value)) continue;
            if (HtmlPattern.IsMatch(field.Value))
            {
                return field.Name;
            }
        }

        return null;
    }
}
