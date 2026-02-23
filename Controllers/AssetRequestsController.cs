using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SmartAssetManager.Api.Data;
using SmartAssetManager.Api.Domain.Entities;
using SmartAssetManager.Api.Models.Requests;
using SmartAssetManager.Api.Services;

namespace SmartAssetManager.Api.Controllers;

[ApiController]
[Route("api/requests")]
[Authorize]
public class AssetRequestsController : ControllerBase
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "new_asset",
        "upgrade",
        "replacement",
        "transfer",
        "return",
        "loss_theft",
        "damage",
        "accessory_peripheral",
        "temporary_loan"
    };

    private static readonly HashSet<string> AllowedUrgencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "low", "medium", "high", "critical"
    };

    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;

    public AssetRequestsController(AppDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateAssetRequestDto dto, CancellationToken cancellationToken)
    {
        var normalizedType = Normalize(dto.RequestType);
        var normalizedUrgency = Normalize(dto.Urgency);

        if (!AllowedTypes.Contains(normalizedType))
        {
            return BadRequest(new { error = "Invalid requestType." });
        }

        if (!AllowedUrgencies.Contains(normalizedUrgency))
        {
            return BadRequest(new { error = "Invalid urgency." });
        }

        var validationError = ValidateByType(normalizedType, dto);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        var requesterRole = await GetActorRoleAsync(dto.RequesterEmail.Trim(), cancellationToken);
        var startsAtBossApproval = string.Equals(requesterRole, "pm", StringComparison.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;
        var request = new AssetRequestEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            RequestNumber = await GenerateRequestNumberAsync(now, cancellationToken),
            RequestType = normalizedType,
            RequesterEmail = dto.RequesterEmail.Trim(),
            RequesterName = TrimOrNull(dto.RequesterName),
            RequesterUserId = TrimOrNull(dto.RequesterUserId),
            RequestedForEmail = TrimOrNull(dto.RequestedForEmail),
            Department = dto.Department.Trim(),
            CostCenter = TrimOrNull(dto.CostCenter),
            Location = dto.Location.Trim(),
            BusinessJustification = dto.BusinessJustification.Trim(),
            Urgency = normalizedUrgency,
            Status = startsAtBossApproval ? "pending_boss" : "pending_pm",
            CurrentApprovalLevel = startsAtBossApproval ? "boss" : "pm",
            PmApproverEmail = dto.PmApproverEmail.Trim(),
            BossApproverEmail = dto.BossApproverEmail.Trim(),
            DestinationUserEmail = TrimOrNull(dto.DestinationUserEmail),
            DestinationManagerEmail = TrimOrNull(dto.DestinationManagerEmail),
            RelatedAssetId = TrimOrNull(dto.RelatedAssetId),
            RequestedCategory = TrimOrNull(dto.RequestedCategory),
            RequestedConfigurationJson = TrimOrNull(dto.RequestedConfigurationJson),
            SecurityIncidentFlag = normalizedType == "loss_theft",
            IncidentDate = dto.IncidentDate,
            IncidentLocation = TrimOrNull(dto.IncidentLocation),
            PoliceReportNumber = TrimOrNull(dto.PoliceReportNumber),
            CreatedAt = now,
            UpdatedAt = now
        };

        if (normalizedType == "loss_theft")
        {
            request.Urgency = "critical";
            if (!request.IncidentDate.HasValue)
            {
                request.IncidentDate = now;
            }

            if (!string.IsNullOrWhiteSpace(request.RelatedAssetId))
            {
                var asset = await _db.Assets.FindAsync([request.RelatedAssetId], cancellationToken);
                if (asset is not null)
                {
                    asset.Status = "at_risk";
                    asset.UpdatedAt = now;
                }
            }
        }

        _db.AssetRequests.Add(request);

        if (startsAtBossApproval)
        {
            _db.AssetRequestApprovals.Add(NewApproval(
                request.Id,
                "pm",
                request.PmApproverEmail,
                "approved",
                "PM stage auto-approved because requester role is PM."
            ));
            await AddNotificationAsync(request.Id, request.BossApproverEmail, "in_app", "request_submitted", cancellationToken);
            await AddNotificationAsync(request.Id, request.RequesterEmail, "in_app", "pm_approved", cancellationToken);
        }
        else
        {
            await AddNotificationAsync(request.Id, request.PmApproverEmail, "in_app", "request_submitted", cancellationToken);
        }
        await AddNotificationAsync(request.Id, request.RequesterEmail, "in_app", "request_submitted", cancellationToken);

        if (normalizedType == "loss_theft")
        {
            await AddNotificationAsync(request.Id, request.BossApproverEmail, "in_app", "security_escalation", cancellationToken);
            await AddNotificationAsync(request.Id, "it.support@company.com", "in_app", "security_escalation", cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(
            request,
            "created",
            request.RequesterEmail,
            requesterRole,
            null,
            request.Status,
            "submitted",
            "Request submitted",
            cancellationToken
        );

        if (startsAtBossApproval)
        {
            await WriteAuditAsync(
                request,
                "approved",
                request.PmApproverEmail,
                "pm",
                "pending_pm",
                "pending_boss",
                "approved",
                "PM stage auto-approved because requester role is PM.",
                cancellationToken
            );
        }

        return Ok(request);
    }

    [HttpPost("list")]
    public async Task<IActionResult> List([FromBody] ListAssetRequestsDto dto, CancellationToken cancellationToken)
    {
        var query = _db.AssetRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(dto.RequesterEmail))
        {
            var requester = dto.RequesterEmail.Trim();
            query = query.Where(x => x.RequesterEmail == requester);
        }

        if (!string.IsNullOrWhiteSpace(dto.ApproverEmail))
        {
            var approver = dto.ApproverEmail.Trim();
            query = query.Where(x => x.PmApproverEmail == approver || x.BossApproverEmail == approver);
        }

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var status = Normalize(dto.Status);
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(dto.RequestType))
        {
            var type = Normalize(dto.RequestType);
            query = query.Where(x => x.RequestType == type);
        }

        if (dto.CreatedFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= dto.CreatedFrom.Value);
        }

        if (dto.CreatedTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= dto.CreatedTo.Value);
        }

        query = query.OrderByDescending(x => x.UpdatedAt);

        if (dto.Limit.HasValue && dto.Limit.Value > 0)
        {
            query = query.Take(dto.Limit.Value);
        }

        var rows = await query.ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPost("audit/list")]
    public async Task<IActionResult> AuditList([FromBody] AuditListRequestDto dto, CancellationToken cancellationToken)
    {
        var query = _db.AssetRequestAudits.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(dto.RequestId))
        {
            var requestId = dto.RequestId.Trim();
            query = query.Where(x => x.RequestId == requestId);
        }

        if (!string.IsNullOrWhiteSpace(dto.RequestNumber))
        {
            var requestNumber = dto.RequestNumber.Trim();
            query = query.Where(x => x.RequestNumber == requestNumber);
        }

        if (!string.IsNullOrWhiteSpace(dto.RequestType))
        {
            var requestType = Normalize(dto.RequestType);
            query = query.Where(x => x.RequestType == requestType);
        }

        if (!string.IsNullOrWhiteSpace(dto.Action))
        {
            var action = Normalize(dto.Action);
            query = query.Where(x => x.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(dto.ActorEmail))
        {
            var actorEmail = dto.ActorEmail.Trim();
            query = query.Where(x => x.ActorEmail == actorEmail);
        }

        if (!string.IsNullOrWhiteSpace(dto.Decision))
        {
            var decision = Normalize(dto.Decision);
            query = query.Where(x => x.Decision == decision);
        }

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var status = Normalize(dto.Status);
            query = query.Where(x => x.ToStatus == status || x.FromStatus == status);
        }

        if (dto.CreatedFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= dto.CreatedFrom.Value);
        }

        if (dto.CreatedTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= dto.CreatedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(dto.RequesterEmail) || !string.IsNullOrWhiteSpace(dto.ApproverEmail) || !string.IsNullOrWhiteSpace(dto.PmApproverEmail) || !string.IsNullOrWhiteSpace(dto.BossApproverEmail))
        {
            var requesterEmail = dto.RequesterEmail?.Trim();
            var approverEmail = dto.ApproverEmail?.Trim();
            var pmApproverEmail = dto.PmApproverEmail?.Trim();
            var bossApproverEmail = dto.BossApproverEmail?.Trim();

            query = from audit in query
                    join request in _db.AssetRequests.AsNoTracking() on audit.RequestId equals request.Id
                    where (string.IsNullOrWhiteSpace(requesterEmail) || request.RequesterEmail == requesterEmail)
                       && (string.IsNullOrWhiteSpace(approverEmail) || request.PmApproverEmail == approverEmail || request.BossApproverEmail == approverEmail)
                       && (string.IsNullOrWhiteSpace(pmApproverEmail) || request.PmApproverEmail == pmApproverEmail)
                       && (string.IsNullOrWhiteSpace(bossApproverEmail) || request.BossApproverEmail == bossApproverEmail)
                    select audit;
        }

        query = query.OrderByDescending(x => x.CreatedAt);

        if (dto.Limit.HasValue && dto.Limit.Value > 0)
        {
            query = query.Take(dto.Limit.Value);
        }

        var rows = await query.ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var request = await _db.AssetRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (request is null) return NotFound();

        var approvals = await _db.AssetRequestApprovals.AsNoTracking()
            .Where(x => x.RequestId == id)
            .OrderBy(x => x.DecidedAt)
            .ToListAsync(cancellationToken);

        var comments = await _db.AssetRequestComments.AsNoTracking()
            .Where(x => x.RequestId == id)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var notifications = await _db.AssetRequestNotifications.AsNoTracking()
            .Where(x => x.RequestId == id)
            .OrderByDescending(x => x.SentAt)
            .ToListAsync(cancellationToken);

        return Ok(new { request, approvals, comments, notifications });
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(string id, [FromBody] RequestActionDto dto, CancellationToken cancellationToken)
    {
        var request = await _db.AssetRequests.FindAsync([id], cancellationToken);
        if (request is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Comment))
        {
            return BadRequest(new { error = "Approval reason is required." });
        }

        var actor = GetActorEmail();
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized(new { error = "Authenticated user email is required." });
        var role = await GetActorRoleAsync(actor, cancellationToken);

        if (request.Status == "pending_pm")
        {
            if (!IsSameEmail(actor, request.PmApproverEmail) && role != "admin")
            {
                return Forbid();
            }

            var fromStatus = request.Status;
            request.Status = "pending_boss";
            request.CurrentApprovalLevel = "boss";
            request.UpdatedAt = DateTimeOffset.UtcNow;
            _db.AssetRequestApprovals.Add(NewApproval(id, "pm", actor, "approved", dto.Comment));

            await AddNotificationAsync(id, request.BossApproverEmail, "in_app", "pm_approved", cancellationToken);
            await AddNotificationAsync(id, request.RequesterEmail, "in_app", "pm_approved", cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(request, "approved", actor, role, fromStatus, request.Status, "approved", dto.Comment, cancellationToken);
            return Ok(request);
        }

        if (request.Status == "pending_boss")
        {
            if (!IsSameEmail(actor, request.BossApproverEmail) && role != "admin")
            {
                return Forbid();
            }

            var fromStatus = request.Status;
            request.Status = "pending_it_fulfillment";
            request.CurrentApprovalLevel = "it";
            request.UpdatedAt = DateTimeOffset.UtcNow;
            _db.AssetRequestApprovals.Add(NewApproval(id, "boss", actor, "approved", dto.Comment));

            await AddNotificationAsync(id, request.RequesterEmail, "in_app", "boss_approved", cancellationToken);
            await AddNotificationAsync(id, "it.support@company.com", "in_app", "pending_it_fulfillment", cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(request, "approved", actor, role, fromStatus, request.Status, "approved", dto.Comment, cancellationToken);
            return Ok(request);
        }

        return BadRequest(new { error = "Approve is not valid for current request status." });
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(string id, [FromBody] RequestActionDto dto, CancellationToken cancellationToken)
    {
        var request = await _db.AssetRequests.FindAsync([id], cancellationToken);
        if (request is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Comment))
        {
            return BadRequest(new { error = "Rejection reason is required." });
        }

        var actor = GetActorEmail();
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized(new { error = "Authenticated user email is required." });
        var role = await GetActorRoleAsync(actor, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var fromStatus = request.Status;

        if (request.Status == "pending_pm")
        {
            if (!IsSameEmail(actor, request.PmApproverEmail) && role != "admin") return Forbid();
            request.Status = "rejected_pm";
            request.CurrentApprovalLevel = "closed";
            request.ClosedAt = now;
            request.UpdatedAt = now;
            _db.AssetRequestApprovals.Add(NewApproval(id, "pm", actor, "rejected", dto.Comment));
        }
        else if (request.Status == "pending_boss")
        {
            if (!IsSameEmail(actor, request.BossApproverEmail) && role != "admin") return Forbid();
            request.Status = "rejected_boss";
            request.CurrentApprovalLevel = "closed";
            request.ClosedAt = now;
            request.UpdatedAt = now;
            _db.AssetRequestApprovals.Add(NewApproval(id, "boss", actor, "rejected", dto.Comment));
        }
        else if (request.Status == "pending_it_fulfillment")
        {
            if (!IsIt(role)) return Forbid();
            request.Status = "rejected_it";
            request.CurrentApprovalLevel = "closed";
            request.ClosedAt = now;
            request.UpdatedAt = now;
            _db.AssetRequestApprovals.Add(NewApproval(id, "it", actor, "rejected", dto.Comment));
        }
        else
        {
            return BadRequest(new { error = "Reject is not valid for current request status." });
        }

        await AddNotificationAsync(id, request.RequesterEmail, "in_app", "request_rejected", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(request, "rejected", actor, role, fromStatus, request.Status, "rejected", dto.Comment, cancellationToken);
        return Ok(request);
    }

    [HttpPost("{id}/return-for-info")]
    public async Task<IActionResult> ReturnForInfo(string id, [FromBody] RequestActionDto dto, CancellationToken cancellationToken)
    {
        var request = await _db.AssetRequests.FindAsync([id], cancellationToken);
        if (request is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Comment))
        {
            return BadRequest(new { error = "Return reason is required." });
        }

        var actor = GetActorEmail();
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized(new { error = "Authenticated user email is required." });
        var role = await GetActorRoleAsync(actor, cancellationToken);

        if (request.Status is not ("pending_pm" or "pending_boss" or "pending_it_fulfillment"))
        {
            return BadRequest(new { error = "Return-for-info is not valid for current request status." });
        }

        if (request.Status == "pending_pm" && !IsSameEmail(actor, request.PmApproverEmail) && role != "admin") return Forbid();
        if (request.Status == "pending_boss" && !IsSameEmail(actor, request.BossApproverEmail) && role != "admin") return Forbid();
        if (request.Status == "pending_it_fulfillment" && !IsIt(role)) return Forbid();

        var fromStatus = request.Status;
        request.Status = "returned_for_info";
        request.UpdatedAt = DateTimeOffset.UtcNow;

        _db.AssetRequestApprovals.Add(NewApproval(
            id,
            request.CurrentApprovalLevel,
            actor,
            "returned_for_info",
            dto.Comment
        ));

        await AddNotificationAsync(id, request.RequesterEmail, "in_app", "returned_for_info", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(request, "returned_for_info", actor, role, fromStatus, request.Status, "returned_for_info", dto.Comment, cancellationToken);
        return Ok(request);
    }

    [HttpPost("{id}/it-fulfill")]
    public async Task<IActionResult> ItFulfill(string id, [FromBody] ItFulfillRequestDto dto, CancellationToken cancellationToken)
    {
        var request = await _db.AssetRequests.FindAsync([id], cancellationToken);
        if (request is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Comment))
        {
            return BadRequest(new { error = "Fulfillment note is required." });
        }

        if (request.Status != "pending_it_fulfillment")
        {
            return BadRequest(new { error = "IT fulfill is only valid when status is pending_it_fulfillment." });
        }

        var actor = GetActorEmail();
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized(new { error = "Authenticated user email is required." });
        var role = await GetActorRoleAsync(actor, cancellationToken);

        if (!IsIt(role))
        {
            return Forbid();
        }

        var now = DateTimeOffset.UtcNow;
        var fromStatus = request.Status;
        var isReturnRequest = string.Equals(request.RequestType, "return", StringComparison.OrdinalIgnoreCase);
        request.Status = isReturnRequest ? "closed" : "fulfilled";
        request.CurrentApprovalLevel = isReturnRequest ? "closed" : "it";
        request.ClosedAt = isReturnRequest ? now : request.ClosedAt;
        request.UpdatedAt = now;

        if (!string.IsNullOrWhiteSpace(dto.AssignedAssetId))
        {
            request.RelatedAssetId = dto.AssignedAssetId.Trim();
        }

        if (request.RequestType == "transfer" && !string.IsNullOrWhiteSpace(request.RelatedAssetId) && !string.IsNullOrWhiteSpace(request.DestinationUserEmail))
        {
            var asset = await _db.Assets.FindAsync([request.RelatedAssetId], cancellationToken);
            if (asset is not null)
            {
                asset.Location = request.DestinationUserEmail;
                asset.Status = "issued";
                asset.UpdatedAt = now;
            }
        }
        else if (isReturnRequest && !string.IsNullOrWhiteSpace(request.RelatedAssetId))
        {
            var asset = await _db.Assets.FindAsync([request.RelatedAssetId], cancellationToken);
            if (asset is not null)
            {
                asset.Location = "IT_RETURN_RECEIVED";
                asset.Status = "available";
                asset.UpdatedAt = now;
            }

            var activeIssuances = await _db.Issuances
                .Where(x =>
                    x.AssetId == request.RelatedAssetId
                    && x.UserEmail == request.RequesterEmail
                    && x.Status == "active")
                .ToListAsync(cancellationToken);

            foreach (var issuance in activeIssuances)
            {
                issuance.Status = "returned";
                issuance.ReturnDate ??= now;
                issuance.UpdatedAt = now;
            }
        }

        _db.AssetRequestApprovals.Add(NewApproval(id, "it", actor, "approved", dto.Comment));
        if (isReturnRequest)
        {
            await AddNotificationAsync(id, request.RequesterEmail, "in_app", "closed", cancellationToken);
            await AddNotificationAsync(id, request.PmApproverEmail, "in_app", "closed", cancellationToken);
            await AddNotificationAsync(id, request.BossApproverEmail, "in_app", "closed", cancellationToken);
        }
        else
        {
            await AddNotificationAsync(id, request.RequesterEmail, "in_app", "fulfilled", cancellationToken);
        }
        await _db.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(
            request,
            isReturnRequest ? "it_received_return" : "it_fulfilled",
            actor,
            role,
            fromStatus,
            request.Status,
            "approved",
            dto.Comment,
            cancellationToken);

        return Ok(request);
    }

    [HttpPost("{id}/it-close")]
    public async Task<IActionResult> ItClose(string id, [FromBody] RequestActionDto dto, CancellationToken cancellationToken)
    {
        var request = await _db.AssetRequests.FindAsync([id], cancellationToken);
        if (request is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Comment))
        {
            return BadRequest(new { error = "Closure note is required." });
        }

        if (request.Status is not ("fulfilled" or "pending_it_fulfillment"))
        {
            return BadRequest(new { error = "IT close is only valid when fulfilled or pending_it_fulfillment." });
        }

        var actor = GetActorEmail();
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized(new { error = "Authenticated user email is required." });
        var role = await GetActorRoleAsync(actor, cancellationToken);

        if (!IsIt(role))
        {
            return Forbid();
        }

        var now = DateTimeOffset.UtcNow;
        var fromStatus = request.Status;
        request.Status = "closed";
        request.CurrentApprovalLevel = "closed";
        request.ClosedAt = now;
        request.UpdatedAt = now;

        _db.AssetRequestApprovals.Add(NewApproval(id, "it", actor, "approved", dto.Comment));

        await AddNotificationAsync(id, request.RequesterEmail, "in_app", "closed", cancellationToken);
        await AddNotificationAsync(id, request.PmApproverEmail, "in_app", "closed", cancellationToken);
        await AddNotificationAsync(id, request.BossApproverEmail, "in_app", "closed", cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(request, "it_closed", actor, role, fromStatus, request.Status, "approved", dto.Comment, cancellationToken);
        return Ok(request);
    }

    [HttpPost("{id}/comment")]
    public async Task<IActionResult> Comment(string id, [FromBody] RequestCommentDto dto, CancellationToken cancellationToken)
    {
        var request = await _db.AssetRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (request is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Comment))
        {
            return BadRequest(new { error = "Comment is required." });
        }

        var actor = GetActorEmail();
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized(new { error = "Authenticated user email is required." });
        var role = await GetActorRoleAsync(actor, cancellationToken);

        var row = new AssetRequestCommentEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            RequestId = id,
            AuthorEmail = actor,
            Comment = dto.Comment.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.AssetRequestComments.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(request, "commented", actor, role, request.Status, request.Status, null, dto.Comment, cancellationToken);

        return Ok(row);
    }

    [HttpPost("{id}/notify")]
    public async Task<IActionResult> Notify(string id, [FromBody] NotifyRequestDto dto, CancellationToken cancellationToken)
    {
        var request = await _db.AssetRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (request is null) return NotFound();

        var notification = await AddNotificationAsync(
            id,
            dto.RecipientEmail,
            string.IsNullOrWhiteSpace(dto.Channel) ? "in_app" : dto.Channel,
            dto.Type,
            cancellationToken,
            string.IsNullOrWhiteSpace(dto.Status) ? "queued" : dto.Status
        );

        if (string.Equals(dto.Channel, "email", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(dto.Subject) && !string.IsNullOrWhiteSpace(dto.Html))
        {
            await _emailService.SendAsync(new SendEmailRequest(dto.RecipientEmail, dto.Subject, dto.Html), cancellationToken);
            notification.Status = "sent";
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(notification);
    }

    [HttpGet("pending/me")]
    public async Task<IActionResult> PendingMe([FromQuery] string? role, CancellationToken cancellationToken)
    {
        var actor = GetActorEmail();
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized(new { error = "Authenticated user email is required." });

        var normalizedRole = string.IsNullOrWhiteSpace(role)
            ? await GetActorRoleAsync(actor, cancellationToken)
            : Normalize(role);
        var query = _db.AssetRequests.AsNoTracking().AsQueryable();

        if (IsIt(normalizedRole))
        {
            query = query.Where(x => x.Status == "pending_it_fulfillment");
        }
        else
        {
            query = query.Where(x =>
                (x.Status == "pending_pm" && x.PmApproverEmail == actor)
                || (x.Status == "pending_boss" && x.BossApproverEmail == actor)
            );
        }

        var rows = await query.OrderByDescending(x => x.UpdatedAt).ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("summary/me")]
    public async Task<IActionResult> SummaryMe([FromQuery] string? role, CancellationToken cancellationToken)
    {
        var actor = GetActorEmail();
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized(new { error = "Authenticated user email is required." });
        var normalizedRole = string.IsNullOrWhiteSpace(role)
            ? await GetActorRoleAsync(actor, cancellationToken)
            : Normalize(role);

        var mine = await _db.AssetRequests.AsNoTracking().CountAsync(x => x.RequesterEmail == actor, cancellationToken);
        var pendingMine = await _db.AssetRequests.AsNoTracking().CountAsync(
            x => x.RequesterEmail == actor && (x.Status == "pending_pm" || x.Status == "pending_boss" || x.Status == "pending_it_fulfillment"),
            cancellationToken
        );

        var pendingApprovals = 0;
        if (IsIt(normalizedRole))
        {
            pendingApprovals = await _db.AssetRequests.AsNoTracking().CountAsync(x => x.Status == "pending_it_fulfillment", cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(actor))
        {
            pendingApprovals = await _db.AssetRequests.AsNoTracking().CountAsync(
                x => (x.Status == "pending_pm" && x.PmApproverEmail == actor)
                     || (x.Status == "pending_boss" && x.BossApproverEmail == actor),
                cancellationToken
            );
        }

        return Ok(new
        {
            mine,
            pendingMine,
            pendingApprovals
        });
    }

    private async Task<string> GenerateRequestNumberAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var yearStart = new DateTimeOffset(new DateTime(now.Year, 1, 1), TimeSpan.Zero);
        var yearEnd = yearStart.AddYears(1);
        var count = await _db.AssetRequests.CountAsync(x => x.CreatedAt >= yearStart && x.CreatedAt < yearEnd, cancellationToken);
        return $"REQ-{now.Year}-{(count + 1):0000}";
    }

    private static AssetRequestApprovalEntity NewApproval(string requestId, string level, string approverEmail, string decision, string? comment)
    {
        return new AssetRequestApprovalEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            RequestId = requestId,
            Level = Normalize(level),
            ApproverEmail = approverEmail.Trim(),
            Decision = Normalize(decision),
            Comment = TrimOrNull(comment),
            DecidedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task<AssetRequestNotificationEntity> AddNotificationAsync(
        string requestId,
        string recipientEmail,
        string channel,
        string type,
        CancellationToken cancellationToken,
        string status = "queued")
    {
        var row = new AssetRequestNotificationEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            RequestId = requestId,
            RecipientEmail = recipientEmail.Trim(),
            Channel = Normalize(channel),
            Type = Normalize(type),
            Status = Normalize(status),
            SentAt = DateTimeOffset.UtcNow
        };

        _db.AssetRequestNotifications.Add(row);
        await Task.CompletedTask;
        return row;
    }

    private async Task WriteAuditAsync(
        AssetRequestEntity request,
        string action,
        string actorEmail,
        string? actorRole,
        string? fromStatus,
        string? toStatus,
        string? decision,
        string? comment,
        CancellationToken cancellationToken)
    {
        var row = new AssetRequestAuditEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            RequestId = request.Id,
            RequestNumber = request.RequestNumber,
            RequestType = request.RequestType,
            Action = Normalize(action),
            ActorEmail = actorEmail.Trim(),
            ActorRole = TrimOrNull(actorRole),
            FromStatus = TrimOrNull(fromStatus),
            ToStatus = TrimOrNull(toStatus),
            Decision = TrimOrNull(decision),
            Comment = TrimOrNull(comment),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.AssetRequestAudits.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string? ValidateByType(string requestType, CreateAssetRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RequestType)) return "Request type is required.";
        if (string.IsNullOrWhiteSpace(dto.RequesterEmail)) return "Requester email is required.";
        if (string.IsNullOrWhiteSpace(dto.PmApproverEmail)) return "PM approver email is required.";
        if (string.IsNullOrWhiteSpace(dto.BossApproverEmail)) return "Boss approver email is required.";
        if (string.IsNullOrWhiteSpace(dto.Department)) return "Department is required.";
        if (string.IsNullOrWhiteSpace(dto.Location)) return "Location is required.";
        if (string.IsNullOrWhiteSpace(dto.BusinessJustification)) return "Business justification is required.";
        if (string.IsNullOrWhiteSpace(dto.Urgency)) return "Urgency is required.";
        if (string.IsNullOrWhiteSpace(dto.RequestedCategory)) return "Request category is required.";

        if (requestType is "new_asset" or "upgrade")
        {
            if (string.IsNullOrWhiteSpace(dto.RequestedConfigurationJson)) return "Requested configuration is required.";
        }

        if (requestType is "replacement" or "transfer" or "return" or "loss_theft" or "damage")
        {
            if (string.IsNullOrWhiteSpace(dto.RelatedAssetId)) return "Related asset is required.";
        }

        if (requestType == "transfer")
        {
            if (string.IsNullOrWhiteSpace(dto.DestinationUserEmail)) return "Destination user is required for transfer.";
            if (string.IsNullOrWhiteSpace(dto.DestinationManagerEmail)) return "Destination manager is required for transfer.";
        }

        if (requestType == "loss_theft")
        {
            if (string.IsNullOrWhiteSpace(dto.IncidentLocation)) return "Incident location is required for loss/theft.";
        }

        return null;
    }

    private static bool IsIt(string? role)
    {
        return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)
               || string.Equals(role, "support", StringComparison.OrdinalIgnoreCase)
               || string.Equals(role, "it", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameEmail(string left, string right)
    {
        return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private static string? TrimOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }

    private string? GetActorEmail()
    {
        return User.FindFirstValue(ClaimTypes.Email)
               ?? User.FindFirstValue("preferred_username")
               ?? User.FindFirstValue("upn")
               ?? User.FindFirstValue("unique_name");
    }

    private async Task<string> GetActorRoleAsync(string actorEmail, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(actorEmail)) return "user";
        var role = await _db.Users.AsNoTracking()
            .Where(x => x.Email == actorEmail)
            .Select(x => x.Role)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(role) ? "user" : Normalize(role);
    }
}
