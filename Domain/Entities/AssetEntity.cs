namespace SmartAssetManager.Api.Domain.Entities;

public class AssetEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? DeviceSerialNumber { get; set; }
    public string? Company { get; set; }
    public string? Model { get; set; }
    public string? Department { get; set; }
    public string? WarrantyStart { get; set; }
    public string? WarrantyEnd { get; set; }
    public string? WarrantyVendor { get; set; }
    public string? Configuration { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = "available";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
