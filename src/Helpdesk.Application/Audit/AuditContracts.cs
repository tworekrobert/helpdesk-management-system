namespace Helpdesk.Application.Audit;

public sealed record AuditLogResponse(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string? OldValue,
    string? NewValue,
    Guid UserId,
    string Actor,
    string ActorEmail,
    DateTimeOffset CreatedAt);
