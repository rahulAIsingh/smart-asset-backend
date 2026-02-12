namespace SmartAssetManager.Api.Domain.Entities;

public class AssetRequestAuditEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RequestId { get; set; } = string.Empty;
    public string RequestNumber { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ActorEmail { get; set; } = string.Empty;
    public string? ActorRole { get; set; }
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public string? Decision { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
