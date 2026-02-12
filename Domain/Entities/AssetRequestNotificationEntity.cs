namespace SmartAssetManager.Api.Domain.Entities;

public class AssetRequestNotificationEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RequestId { get; set; } = string.Empty;
    public AssetRequestEntity? Request { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Channel { get; set; } = "in_app";
    public string Type { get; set; } = "request_submitted";
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "sent";
}

