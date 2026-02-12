namespace SmartAssetManager.Api.Domain.Entities;

public class FinanceAssetOverrideEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string AssetId { get; set; } = string.Empty;
    public string Method { get; set; } = "straight_line";
    public int UsefulLifeMonths { get; set; } = 36;
    public string SalvageType { get; set; } = "percent";
    public decimal SalvageValue { get; set; } = 10;
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string CreatedBy { get; set; } = "system";
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
}
