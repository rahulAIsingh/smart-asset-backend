namespace SmartAssetManager.Api.Models.Requests;

public record CreateAssetRequestDto(
    string RequestType,
    string RequesterEmail,
    string? RequesterName,
    string? RequesterUserId,
    string? RequestedForEmail,
    string Department,
    string? CostCenter,
    string Location,
    string BusinessJustification,
    string Urgency,
    string PmApproverEmail,
    string BossApproverEmail,
    string? DestinationUserEmail,
    string? DestinationManagerEmail,
    string? RelatedAssetId,
    string? RequestedCategory,
    string? RequestedConfigurationJson,
    DateTimeOffset? IncidentDate,
    string? IncidentLocation,
    string? PoliceReportNumber
);

public record ListAssetRequestsDto(
    string? RequesterEmail,
    string? ApproverEmail,
    string? Status,
    string? RequestType,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo,
    int? Limit
);

public record RequestActionDto(
    string ActorEmail,
    string? ActorRole,
    string? Comment
);

public record ItFulfillRequestDto(
    string ActorEmail,
    string? ActorRole,
    string? Comment,
    string? AssignedAssetId
);

public record RequestCommentDto(
    string AuthorEmail,
    string Comment
);

public record NotifyRequestDto(
    string RecipientEmail,
    string Channel,
    string Type,
    string? Subject,
    string? Html,
    string? Status
);

public record AuditListRequestDto(
    string? RequestId,
    string? RequestNumber,
    string? RequestType,
    string? RequesterEmail,
    string? ApproverEmail,
    string? PmApproverEmail,
    string? BossApproverEmail,
    string? Action,
    string? ActorEmail,
    string? Decision,
    string? Status,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo,
    int? Limit
);
