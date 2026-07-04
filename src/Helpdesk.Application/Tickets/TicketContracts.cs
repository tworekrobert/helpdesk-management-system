using Helpdesk.Domain.Tickets;

namespace Helpdesk.Application.Tickets;

public sealed record CreateTicketRequest(
    string Title,
    string Description,
    Guid CategoryId,
    TicketPriority Priority,
    Guid? RelatedAssetId);

public sealed record ChangeTicketStatusRequest(TicketStatus Status);

public sealed record AssignTicketRequest(Guid? TechnicianId);

public sealed record ChangeTicketPriorityRequest(TicketPriority Priority);

public sealed record LinkTicketAssetRequest(Guid AssetId);

public sealed record AddTicketCommentRequest(string Body, bool IsInternal);

public sealed record CreateTicketCategoryRequest(string Name, string Description);

public sealed record UpdateTicketCategoryRequest(string Name, string Description, bool IsActive);

public sealed record CreateTicketTagRequest(string Name, string Color);

public sealed record UpdateTicketTagRequest(string Name, string Color, bool IsActive);

public sealed record UpdateTicketTagsRequest(IReadOnlyCollection<Guid> TagIds);

public sealed record TicketListItemResponse(
    Guid Id,
    string Reference,
    string Title,
    string Category,
    TicketPriority Priority,
    TicketStatus Status,
    string CreatedBy,
    string? AssignedTo,
    string? RelatedAssetTag,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset SlaDueAt,
    bool IsOverdue,
    IReadOnlyCollection<TicketTagResponse> Tags);

public sealed record TicketDetailsResponse(
    Guid Id,
    string Reference,
    string Title,
    string Description,
    string Category,
    TicketPriority Priority,
    TicketStatus Status,
    UserSummaryResponse CreatedBy,
    UserSummaryResponse? AssignedTo,
    AssetSummaryResponse? RelatedAsset,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset SlaDueAt,
    bool IsOverdue,
    IReadOnlyCollection<TicketCommentResponse> Comments,
    IReadOnlyCollection<TicketAttachmentResponse> Attachments,
    IReadOnlyCollection<TicketTagResponse> Tags,
    IReadOnlyCollection<TicketHistoryResponse> History);

public sealed record TicketCommentResponse(
    Guid Id,
    string Author,
    string Body,
    bool IsInternal,
    DateTimeOffset CreatedAt);

public sealed record TicketHistoryResponse(
    Guid Id,
    string Action,
    string? OldValue,
    string? NewValue,
    string Actor,
    DateTimeOffset CreatedAt);

public sealed record TicketAttachmentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    string UploadedBy,
    DateTimeOffset CreatedAt);

public sealed record TicketCategoryResponse(Guid Id, string Name, string Description, bool IsActive);

public sealed record TicketTagResponse(Guid Id, string Name, string Color, bool IsActive);

public sealed record UserSummaryResponse(Guid Id, string FullName, string Email);

public sealed record AssetSummaryResponse(Guid Id, string AssetTag, string Label);
