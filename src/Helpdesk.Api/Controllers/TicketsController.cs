using Helpdesk.Api.Extensions;
using Helpdesk.Api.Hubs;
using Helpdesk.Api.Services;
using Helpdesk.Application.Tickets;
using Helpdesk.Domain.Assets;
using Helpdesk.Domain.Audit;
using Helpdesk.Domain.Common;
using Helpdesk.Domain.Tickets;
using Helpdesk.Domain.Users;
using Helpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets")]
public sealed class TicketsController(
    HelpdeskDbContext db,
    IHubContext<TicketUpdatesHub> ticketHub,
    TicketEmailNotificationService notifications) : ControllerBase
{
    private const long MaxAttachmentSize = 5 * 1024 * 1024;

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyCollection<TicketCategoryResponse>>> Categories(CancellationToken cancellationToken)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .Select(category => new TicketCategoryResponse(category.Id, category.Name, category.Description, category.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("categories/manage")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyCollection<TicketCategoryResponse>>> ManageCategories(CancellationToken cancellationToken)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new TicketCategoryResponse(category.Id, category.Name, category.Description, category.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpPost("categories")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TicketCategoryResponse>> CreateCategory(CreateTicketCategoryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { message = "Category name and description are required." });
        }

        var normalized = request.Name.Trim().ToLowerInvariant();
        var exists = await db.Categories.AnyAsync(category => category.Name.ToLower() == normalized, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "Category with this name already exists." });
        }

        var category = new TicketCategory(request.Name, request.Description);
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(ManageCategories),
            new TicketCategoryResponse(category.Id, category.Name, category.Description, category.IsActive));
    }

    [HttpPut("categories/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TicketCategoryResponse>> UpdateCategory(Guid id, UpdateTicketCategoryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { message = "Category name and description are required." });
        }

        var category = await db.Categories.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var normalized = request.Name.Trim().ToLowerInvariant();
        var exists = await db.Categories.AnyAsync(candidate => candidate.Id != id && candidate.Name.ToLower() == normalized, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "Category with this name already exists." });
        }

        category.Update(request.Name, request.Description, request.IsActive);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new TicketCategoryResponse(category.Id, category.Name, category.Description, category.IsActive));
    }

    [HttpGet("tags")]
    public async Task<ActionResult<IReadOnlyCollection<TicketTagResponse>>> Tags(CancellationToken cancellationToken)
    {
        var query = db.TicketTags.AsNoTracking().AsQueryable();
        if (User.GetUserRole() != UserRole.Admin)
        {
            query = query.Where(tag => tag.IsActive);
        }

        var tags = await query
            .OrderBy(tag => tag.Name)
            .Select(tag => new TicketTagResponse(tag.Id, tag.Name, tag.Color, tag.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(tags);
    }

    [HttpPost("tags")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TicketTagResponse>> CreateTag(CreateTicketTagRequest request, CancellationToken cancellationToken)
    {
        if (!IsValidTagRequest(request.Name, request.Color))
        {
            return BadRequest(new { message = "Tag name and hex color are required." });
        }

        var normalized = request.Name.Trim().ToLowerInvariant();
        var exists = await db.TicketTags.AnyAsync(tag => tag.Name.ToLower() == normalized, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "Tag with this name already exists." });
        }

        var now = DateTimeOffset.UtcNow;
        var tag = new TicketTag(request.Name, request.Color, now);
        db.TicketTags.Add(tag);
        db.AuditLogs.Add(new AuditLog("TicketTag", tag.Id, "Tag created", null, tag.Name, User.GetUserId(), now));
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Tags), new TicketTagResponse(tag.Id, tag.Name, tag.Color, tag.IsActive));
    }

    [HttpPut("tags/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TicketTagResponse>> UpdateTag(Guid id, UpdateTicketTagRequest request, CancellationToken cancellationToken)
    {
        if (!IsValidTagRequest(request.Name, request.Color))
        {
            return BadRequest(new { message = "Tag name and hex color are required." });
        }

        var tag = await db.TicketTags.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (tag is null)
        {
            return NotFound();
        }

        var normalized = request.Name.Trim().ToLowerInvariant();
        var exists = await db.TicketTags.AnyAsync(candidate => candidate.Id != id && candidate.Name.ToLower() == normalized, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "Tag with this name already exists." });
        }

        var oldValue = $"{tag.Name} ({tag.Color})";
        tag.Update(request.Name, request.Color, request.IsActive);
        db.AuditLogs.Add(new AuditLog("TicketTag", tag.Id, "Tag updated", oldValue, $"{tag.Name} ({tag.Color})", User.GetUserId(), DateTimeOffset.UtcNow));
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new TicketTagResponse(tag.Id, tag.Name, tag.Color, tag.IsActive));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<TicketListItemResponse>>> List(
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? assignedTechnicianId,
        [FromQuery] Guid[]? tagIds,
        [FromQuery] bool onlyOverdue,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();

        var query = db.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Category)
            .AsQueryable();

        if (role == UserRole.Employee)
        {
            query = query.Where(ticket => ticket.CreatedByUserId == currentUserId);
        }

        if (status is not null)
        {
            query = query.Where(ticket => ticket.Status == status);
        }

        if (priority is not null)
        {
            query = query.Where(ticket => ticket.Priority == priority);
        }

        if (categoryId is not null)
        {
            query = query.Where(ticket => ticket.CategoryId == categoryId);
        }

        if (assignedTechnicianId is not null)
        {
            query = query.Where(ticket => ticket.AssignedTechnicianId == assignedTechnicianId);
        }

        if (tagIds is { Length: > 0 })
        {
            var distinctTagIds = tagIds.Distinct().ToArray();
            query = query.Where(ticket => db.TicketTagAssignments
                .Any(assignment => assignment.TicketId == ticket.Id && distinctTagIds.Contains(assignment.TagId)));
        }

        if (onlyOverdue)
        {
            query = query.Where(ticket =>
                ticket.Status != TicketStatus.Resolved
                && ticket.Status != TicketStatus.Closed
                && ticket.Status != TicketStatus.Cancelled
                && ticket.SlaDueAt < now);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(ticket => ticket.Title.ToLower().Contains(normalized));
        }

        var tickets = await query
            .OrderByDescending(ticket => ticket.UpdatedAt)
            .Take(250)
            .ToListAsync(cancellationToken);

        var userIds = tickets
            .Select(ticket => ticket.CreatedByUserId)
            .Concat(tickets.Where(ticket => ticket.AssignedTechnicianId.HasValue).Select(ticket => ticket.AssignedTechnicianId!.Value))
            .Distinct()
            .ToArray();
        var assetIds = tickets
            .Where(ticket => ticket.RelatedAssetId.HasValue)
            .Select(ticket => ticket.RelatedAssetId!.Value)
            .Distinct()
            .ToArray();

        var users = await db.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);
        var assets = await db.Assets
            .AsNoTracking()
            .Where(asset => assetIds.Contains(asset.Id))
            .ToDictionaryAsync(asset => asset.Id, cancellationToken);
        var tagsByTicket = await LoadTagsByTicketAsync(tickets.Select(ticket => ticket.Id).ToArray(), cancellationToken);

        return Ok(tickets.Select(ticket => new TicketListItemResponse(
            ticket.Id,
            FormatReference(ticket),
            ticket.Title,
            ticket.Category?.Name ?? "Unknown",
            ticket.Priority,
            ticket.Status,
            users[ticket.CreatedByUserId].FullName,
            ticket.AssignedTechnicianId is null ? null : users[ticket.AssignedTechnicianId.Value].FullName,
            ticket.RelatedAssetId is null ? null : assets[ticket.RelatedAssetId.Value].AssetTag,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.SlaDueAt,
            ticket.IsOverdue(now),
            tagsByTicket.TryGetValue(ticket.Id, out var tags) ? tags : [])).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketDetailsResponse>> Details(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanAccess(ticket))
        {
            return Forbid();
        }

        return Ok(await MapDetailsAsync(ticket, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<ActionResult<TicketDetailsResponse>> Create(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var now = DateTimeOffset.UtcNow;

        var categoryExists = await db.Categories.AnyAsync(category => category.Id == request.CategoryId && category.IsActive, cancellationToken);
        if (!categoryExists)
        {
            return BadRequest(new { message = "Unknown ticket category." });
        }

        var ticket = Ticket.Create(request.Title, request.Description, request.CategoryId, request.Priority, userId, now);
        if (request.RelatedAssetId is not null)
        {
            var assetExists = await db.Assets.AnyAsync(asset => asset.Id == request.RelatedAssetId, cancellationToken);
            if (!assetExists)
            {
                return BadRequest(new { message = "Unknown asset." });
            }

            ticket.LinkAsset(request.RelatedAssetId.Value, now);
        }

        db.Tickets.Add(ticket);
        db.AuditLogs.Add(new AuditLog("Ticket", ticket.Id, "Ticket created", null, ticket.Priority.ToString(), userId, now));
        await db.SaveChangesAsync(cancellationToken);
        await PublishTicketChangedAsync(ticket, "created", cancellationToken);

        var created = await LoadTicketAsync(ticket.Id, cancellationToken);
        return CreatedAtAction(nameof(Details), new { id = ticket.Id }, await MapDetailsAsync(created!, cancellationToken));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<TicketDetailsResponse>> ChangeStatus(Guid id, ChangeTicketStatusRequest request, CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanChangeStatus(ticket, request.Status))
        {
            return Forbid();
        }

        var oldStatus = ticket.Status;
        var oldValue = oldStatus.ToString();
        var now = DateTimeOffset.UtcNow;

        try
        {
            ticket.ChangeStatus(request.Status, now);
        }
        catch (DomainException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        var statusChanged = oldStatus != ticket.Status;
        if (statusChanged)
        {
            db.AuditLogs.Add(new AuditLog("Ticket", ticket.Id, "Status changed", oldValue, ticket.Status.ToString(), User.GetUserId(), now));
            await notifications.QueueStatusChangedAsync(ticket, FormatReference(ticket), oldStatus, ticket.Status, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        if (statusChanged)
        {
            await PublishTicketChangedAsync(ticket, "status_changed", cancellationToken);
        }

        return Ok(await MapDetailsAsync(ticket, cancellationToken));
    }

    [HttpPatch("{id:guid}/assign")]
    [Authorize(Roles = "Technician,Admin")]
    public async Task<ActionResult<TicketDetailsResponse>> Assign(Guid id, AssignTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        var targetTechnicianId = User.GetUserRole() == UserRole.Technician
            ? User.GetUserId()
            : request.TechnicianId;

        if (targetTechnicianId is null)
        {
            return BadRequest(new { message = "Technician id is required for admin assignment." });
        }

        var technician = await db.Users.SingleOrDefaultAsync(
            user => user.Id == targetTechnicianId && user.Role == UserRole.Technician && user.IsActive,
            cancellationToken);
        if (technician is null)
        {
            return BadRequest(new { message = "Target user is not an active technician." });
        }

        var oldValue = ticket.AssignedTechnicianId?.ToString();
        var now = DateTimeOffset.UtcNow;

        try
        {
            ticket.AssignTo(technician.Id, now);
        }
        catch (DomainException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        db.AuditLogs.Add(new AuditLog("Ticket", ticket.Id, "Assigned technician changed", oldValue, technician.FullName, User.GetUserId(), now));
        await db.SaveChangesAsync(cancellationToken);
        await PublishTicketChangedAsync(ticket, "assigned", cancellationToken);
        return Ok(await MapDetailsAsync(ticket, cancellationToken));
    }

    [HttpPatch("{id:guid}/priority")]
    [Authorize(Roles = "Technician,Admin")]
    public async Task<ActionResult<TicketDetailsResponse>> ChangePriority(Guid id, ChangeTicketPriorityRequest request, CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        var oldValue = ticket.Priority.ToString();
        var now = DateTimeOffset.UtcNow;

        try
        {
            ticket.ChangePriority(request.Priority, now);
        }
        catch (DomainException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        db.AuditLogs.Add(new AuditLog("Ticket", ticket.Id, "Priority changed", oldValue, ticket.Priority.ToString(), User.GetUserId(), now));
        await db.SaveChangesAsync(cancellationToken);
        await PublishTicketChangedAsync(ticket, "priority_changed", cancellationToken);
        return Ok(await MapDetailsAsync(ticket, cancellationToken));
    }

    [HttpPatch("{id:guid}/asset")]
    [Authorize(Roles = "Technician,Admin")]
    public async Task<ActionResult<TicketDetailsResponse>> LinkAsset(Guid id, LinkTicketAssetRequest request, CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        var asset = await db.Assets.SingleOrDefaultAsync(candidate => candidate.Id == request.AssetId, cancellationToken);
        if (asset is null)
        {
            return BadRequest(new { message = "Unknown asset." });
        }

        var oldValue = ticket.RelatedAssetId?.ToString();
        var now = DateTimeOffset.UtcNow;

        try
        {
            ticket.LinkAsset(asset.Id, now);
        }
        catch (DomainException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        db.AuditLogs.Add(new AuditLog("Ticket", ticket.Id, "Asset linked to ticket", oldValue, asset.AssetTag, User.GetUserId(), now));
        await db.SaveChangesAsync(cancellationToken);
        await PublishTicketChangedAsync(ticket, "asset_linked", cancellationToken);
        return Ok(await MapDetailsAsync(ticket, cancellationToken));
    }

    [HttpPatch("{id:guid}/tags")]
    [Authorize(Roles = "Technician,Admin")]
    public async Task<ActionResult<TicketDetailsResponse>> UpdateTags(Guid id, UpdateTicketTagsRequest request, CancellationToken cancellationToken)
    {
        if (request.TagIds is null)
        {
            return BadRequest(new { message = "Tag ids are required." });
        }

        var ticket = await LoadTicketAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        var distinctTagIds = request.TagIds.Distinct().ToArray();
        var tags = await db.TicketTags
            .Where(tag => distinctTagIds.Contains(tag.Id) && tag.IsActive)
            .OrderBy(tag => tag.Name)
            .ToListAsync(cancellationToken);
        if (tags.Count != distinctTagIds.Length)
        {
            return BadRequest(new { message = "One or more ticket tags are unknown or inactive." });
        }

        var existingAssignments = await db.TicketTagAssignments
            .Where(assignment => assignment.TicketId == ticket.Id)
            .ToListAsync(cancellationToken);
        var existingTagIds = existingAssignments.Select(assignment => assignment.TagId).ToHashSet();
        var newTagIds = distinctTagIds.ToHashSet();

        db.TicketTagAssignments.RemoveRange(existingAssignments.Where(assignment => !newTagIds.Contains(assignment.TagId)));

        var now = DateTimeOffset.UtcNow;
        foreach (var tagId in distinctTagIds.Where(tagId => !existingTagIds.Contains(tagId)))
        {
            db.TicketTagAssignments.Add(new TicketTagAssignment(ticket.Id, tagId, now));
        }

        var newValue = tags.Count == 0 ? "No tags" : string.Join(", ", tags.Select(tag => tag.Name));
        db.AuditLogs.Add(new AuditLog("Ticket", ticket.Id, "Tags updated", null, newValue, User.GetUserId(), now));
        await db.SaveChangesAsync(cancellationToken);
        await PublishTicketChangedAsync(ticket, "tags_updated", cancellationToken);

        return Ok(await MapDetailsAsync(ticket, cancellationToken));
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<TicketDetailsResponse>> AddComment(Guid id, AddTicketCommentRequest request, CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanAccess(ticket))
        {
            return Forbid();
        }

        if (request.IsInternal && !User.IsTechnicianOrAdmin())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest(new { message = "Comment body is required." });
        }

        var now = DateTimeOffset.UtcNow;
        var comment = new TicketComment(ticket.Id, User.GetUserId(), request.Body, request.IsInternal, now);

        try
        {
            ticket.AddComment(comment, now);
        }
        catch (DomainException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        db.AuditLogs.Add(new AuditLog("Ticket", ticket.Id, "Comment added", null, request.IsInternal ? "Internal comment" : "Public comment", User.GetUserId(), now));
        await db.SaveChangesAsync(cancellationToken);
        await PublishTicketChangedAsync(ticket, "comment_added", cancellationToken);
        return Ok(await MapDetailsAsync(ticket, cancellationToken));
    }

    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(MaxAttachmentSize)]
    public async Task<ActionResult<TicketDetailsResponse>> AddAttachment(Guid id, [FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanAccess(ticket))
        {
            return Forbid();
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Attachment file is required." });
        }

        if (file.Length > MaxAttachmentSize)
        {
            return BadRequest(new { message = "Attachment cannot be larger than 5 MB." });
        }

        var now = DateTimeOffset.UtcNow;
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        var attachment = new TicketAttachment(
            ticket.Id,
            User.GetUserId(),
            Path.GetFileName(file.FileName),
            file.ContentType,
            file.Length,
            stream.ToArray(),
            now);

        db.TicketAttachments.Add(attachment);
        db.AuditLogs.Add(new AuditLog("Ticket", ticket.Id, "Attachment added", null, attachment.FileName, User.GetUserId(), now));
        await db.SaveChangesAsync(cancellationToken);
        await PublishTicketChangedAsync(ticket, "attachment_added", cancellationToken);
        return Ok(await MapDetailsAsync(ticket, cancellationToken));
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanAccess(ticket))
        {
            return Forbid();
        }

        var attachment = await db.TicketAttachments
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == attachmentId && candidate.TicketId == ticket.Id, cancellationToken);
        if (attachment is null)
        {
            return NotFound();
        }

        return File(attachment.Content, attachment.ContentType, attachment.FileName);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyCollection<TicketHistoryResponse>>> History(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanAccess(ticket))
        {
            return Forbid();
        }

        var details = await MapDetailsAsync(ticket, cancellationToken);
        return Ok(details.History);
    }

    private async Task<Ticket?> LoadTicketAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Tickets
            .Include(ticket => ticket.Category)
            .Include(ticket => ticket.Comments)
            .SingleOrDefaultAsync(ticket => ticket.Id == id, cancellationToken);
    }

    private bool CanAccess(Ticket ticket)
    {
        return User.GetUserRole() != UserRole.Employee || ticket.CreatedByUserId == User.GetUserId();
    }

    private bool CanChangeStatus(Ticket ticket, TicketStatus requestedStatus)
    {
        var role = User.GetUserRole();
        if (role is UserRole.Technician or UserRole.Admin)
        {
            return true;
        }

        return ticket.CreatedByUserId == User.GetUserId()
            && ticket.Status == TicketStatus.Resolved
            && requestedStatus == TicketStatus.Closed;
    }

    private async Task<TicketDetailsResponse> MapDetailsAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var showInternal = User.IsTechnicianOrAdmin();
        var createdBy = await db.Users.AsNoTracking().SingleAsync(user => user.Id == ticket.CreatedByUserId, cancellationToken);
        var assignedTo = ticket.AssignedTechnicianId is null
            ? null
            : await db.Users.AsNoTracking().SingleAsync(user => user.Id == ticket.AssignedTechnicianId.Value, cancellationToken);
        var asset = ticket.RelatedAssetId is null
            ? null
            : await db.Assets.AsNoTracking().SingleAsync(candidate => candidate.Id == ticket.RelatedAssetId.Value, cancellationToken);

        var visibleComments = ticket.Comments
            .Where(comment => showInternal || !comment.IsInternal)
            .OrderBy(comment => comment.CreatedAt)
            .ToList();

        var history = await db.AuditLogs
            .AsNoTracking()
            .Where(log => log.EntityType == "Ticket" && log.EntityId == ticket.Id)
            .OrderBy(log => log.CreatedAt)
            .ToListAsync(cancellationToken);
        var attachments = await db.TicketAttachments
            .AsNoTracking()
            .Where(attachment => attachment.TicketId == ticket.Id)
            .OrderBy(attachment => attachment.CreatedAt)
            .ToListAsync(cancellationToken);
        var tagsByTicket = await LoadTagsByTicketAsync([ticket.Id], cancellationToken);

        var actorIds = visibleComments.Select(comment => comment.AuthorId)
            .Concat(attachments.Select(attachment => attachment.UploadedByUserId))
            .Concat(history.Select(log => log.UserId))
            .Distinct()
            .ToArray();
        var actors = await db.Users
            .AsNoTracking()
            .Where(user => actorIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        return new TicketDetailsResponse(
            ticket.Id,
            FormatReference(ticket),
            ticket.Title,
            ticket.Description,
            ticket.Category?.Name ?? "Unknown",
            ticket.Priority,
            ticket.Status,
            new UserSummaryResponse(createdBy.Id, createdBy.FullName, createdBy.Email),
            assignedTo is null ? null : new UserSummaryResponse(assignedTo.Id, assignedTo.FullName, assignedTo.Email),
            asset is null ? null : new AssetSummaryResponse(asset.Id, asset.AssetTag, $"{asset.Brand} {asset.Model}"),
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.ResolvedAt,
            ticket.ClosedAt,
            ticket.SlaDueAt,
            ticket.IsOverdue(now),
            visibleComments.Select(comment => new TicketCommentResponse(
                comment.Id,
                actors[comment.AuthorId].FullName,
                comment.Body,
                comment.IsInternal,
                comment.CreatedAt)).ToList(),
            attachments.Select(attachment => new TicketAttachmentResponse(
                attachment.Id,
                attachment.FileName,
                attachment.ContentType,
                attachment.Size,
                actors[attachment.UploadedByUserId].FullName,
                attachment.CreatedAt)).ToList(),
            tagsByTicket.TryGetValue(ticket.Id, out var tags) ? tags : [],
            history.Select(log => new TicketHistoryResponse(
                log.Id,
                log.Action,
                log.OldValue,
                log.NewValue,
                actors[log.UserId].FullName,
                log.CreatedAt)).ToList());
    }

    private static string FormatReference(Ticket ticket)
    {
        return ticket.Number > 0 ? $"HD-{ticket.Number:00000}" : $"HD-{ticket.Id.ToString()[..8].ToUpperInvariant()}";
    }

    private async Task<Dictionary<Guid, IReadOnlyCollection<TicketTagResponse>>> LoadTagsByTicketAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken)
    {
        if (ticketIds.Count == 0)
        {
            return [];
        }

        var assignments = await db.TicketTagAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Tag)
            .Where(assignment => ticketIds.Contains(assignment.TicketId))
            .OrderBy(assignment => assignment.Tag!.Name)
            .ToListAsync(cancellationToken);

        return assignments
            .Where(assignment => assignment.Tag is not null)
            .GroupBy(assignment => assignment.TicketId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<TicketTagResponse>)group
                    .Select(assignment => new TicketTagResponse(
                        assignment.Tag!.Id,
                        assignment.Tag.Name,
                        assignment.Tag.Color,
                        assignment.Tag.IsActive))
                    .ToList());
    }

    private static bool IsValidTagRequest(string? name, string? color)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(color))
        {
            return false;
        }

        var trimmedColor = color.Trim();
        return trimmedColor.Length == 7
            && trimmedColor[0] == '#'
            && trimmedColor.Skip(1).All(Uri.IsHexDigit);
    }

    private Task PublishTicketChangedAsync(Ticket ticket, string action, CancellationToken cancellationToken)
    {
        var notification = new TicketChangedNotification(ticket.Id, FormatReference(ticket), action, DateTimeOffset.UtcNow);
        return ticketHub.Clients.All.SendAsync("TicketChanged", notification, cancellationToken);
    }

    private sealed record TicketChangedNotification(Guid TicketId, string Reference, string Action, DateTimeOffset ChangedAt);
}
