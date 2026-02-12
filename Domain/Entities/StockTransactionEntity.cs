namespace SmartAssetManager.Api.Domain.Entities;

public class StockTransactionEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string AssetId { get; set; } = string.Empty;
    public AssetEntity? Asset { get; set; }
    public string Type { get; set; } = "in";
    public decimal Quantity { get; set; } = 1;
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
