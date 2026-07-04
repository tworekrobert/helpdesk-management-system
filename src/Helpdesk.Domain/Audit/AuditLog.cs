namespace Helpdesk.Domain.Audit;

public sealed class AuditLog
{
    private AuditLog()
    {
    }

    public AuditLog(
        string entityType,
        Guid entityId,
        string action,
        string? oldValue,
        string? newValue,
        Guid userId,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        OldValue = oldValue;
        NewValue = newValue;
        UserId = userId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
