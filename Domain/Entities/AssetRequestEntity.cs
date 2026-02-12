namespace SmartAssetManager.Api.Domain.Entities;

public class AssetRequestEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RequestNumber { get; set; } = string.Empty;
    public string RequestType { get; set; } = "new_asset";
    public string RequesterEmail { get; set; } = string.Empty;
    public string? RequesterName { get; set; }
    public string? RequesterUserId { get; set; }
    public string? RequestedForEmail { get; set; }
    public string Department { get; set; } = string.Empty;
    public string? CostCenter { get; set; }
    public string Location { get; set; } = string.Empty;
    public string BusinessJustification { get; set; } = string.Empty;
    public string Urgency { get; set; } = "medium";
    public string Status { get; set; } = "pending_pm";
    public string CurrentApprovalLevel { get; set; } = "pm";
    public string PmApproverEmail { get; set; } = string.Empty;
    public string BossApproverEmail { get; set; } = string.Empty;
    public string? DestinationUserEmail { get; set; }
    public string? DestinationManagerEmail { get; set; }
    public string? RelatedAssetId { get; set; }
    public string? RequestedCategory { get; set; }
    public string? RequestedConfigurationJson { get; set; }
    public bool SecurityIncidentFlag { get; set; }
    public DateTimeOffset? IncidentDate { get; set; }
    public string? IncidentLocation { get; set; }
    public string? PoliceReportNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; }
}

