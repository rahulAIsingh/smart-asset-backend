namespace SmartAssetManager.Api.Domain.Entities;

public class AssetRequestApprovalEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RequestId { get; set; } = string.Empty;
    public AssetRequestEntity? Request { get; set; }
    public string Level { get; set; } = "pm";
    public string ApproverEmail { get; set; } = string.Empty;
    public string Decision { get; set; } = "approved";
    public string? Comment { get; set; }
    public DateTimeOffset DecidedAt { get; set; } = DateTimeOffset.UtcNow;
}

