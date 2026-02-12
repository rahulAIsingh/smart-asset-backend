namespace SmartAssetManager.Api.Domain.Entities;

public class AssetRequestAttachmentEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RequestId { get; set; } = string.Empty;
    public AssetRequestEntity? Request { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}

