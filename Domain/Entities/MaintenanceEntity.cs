namespace SmartAssetManager.Api.Domain.Entities;

public class MaintenanceEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string AssetId { get; set; } = string.Empty;
    public AssetEntity? Asset { get; set; }
    public string Type { get; set; } = "maintenance";
    public string Description { get; set; } = string.Empty;
    public decimal? Cost { get; set; }
    public DateOnly? WarrantyExpiry { get; set; }
    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
    public string PerformedBy { get; set; } = "system";
}
