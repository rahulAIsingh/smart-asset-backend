namespace SmartAssetManager.Api.Domain.Entities;

public class AssetRequestCommentEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RequestId { get; set; } = string.Empty;
    public AssetRequestEntity? Request { get; set; }
    public string AuthorEmail { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

