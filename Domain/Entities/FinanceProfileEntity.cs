namespace SmartAssetManager.Api.Domain.Entities;

public class FinanceProfileEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Category { get; set; } = string.Empty;
    public string Method { get; set; } = "straight_line";
    public int UsefulLifeMonths { get; set; } = 36;
    public string SalvageType { get; set; } = "percent";
    public decimal SalvageValue { get; set; } = 10;
    public string Frequency { get; set; } = "monthly";
    public string? ExpenseGl { get; set; }
    public string? AccumDepGl { get; set; }
    public bool Active { get; set; } = true;
    public string CreatedBy { get; set; } = "system";
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedDate { get; set; }
}
