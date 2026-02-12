namespace SmartAssetManager.Api.Domain.Entities;

public class UserEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
    public string? Department { get; set; }
    public string? Name { get; set; }
    public string? Avatar { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
