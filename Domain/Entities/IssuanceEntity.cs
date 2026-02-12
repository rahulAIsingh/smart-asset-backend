namespace SmartAssetManager.Api.Domain.Entities;

public class IssuanceEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string AssetId { get; set; } = string.Empty;
    public AssetEntity? Asset { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTimeOffset IssueDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReturnDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
