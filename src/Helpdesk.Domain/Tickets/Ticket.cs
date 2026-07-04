using Helpdesk.Domain.Common;

namespace Helpdesk.Domain.Tickets;

public sealed class Ticket
{
    private Ticket()
    {
    }

    private Ticket(
        string title,
        string description,
        Guid categoryId,
        TicketPriority priority,
        Guid createdByUserId,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        Title = title.Trim();
        Description = description.Trim();
        CategoryId = categoryId;
        Priority = priority;
        Status = TicketStatus.New;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        SlaDueAt = SlaPolicy.CalculateDueAt(createdAt, priority);
    }

    public Guid Id { get; private set; }
    public int Number { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public TicketCategory? Category { get; private set; }
    public TicketPriority Priority { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? AssignedTechnicianId { get; private set; }
    public Guid? RelatedAssetId { get; private set; }
    public DateTimeOffset SlaDueAt { get; private set; }
    public List<TicketComment> Comments { get; private set; } = [];

    public static Ticket Create(
        string title,
        string description,
        Guid categoryId,
        TicketPriority priority,
        Guid createdByUserId,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Ticket title is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Ticket description is required.");
        }

        return new Ticket(title, description, categoryId, priority, createdByUserId, createdAt);
    }

    public void AssignTo(Guid technicianId, DateTimeOffset now)
    {
        EnsureOpen();

        AssignedTechnicianId = technicianId;
        UpdatedAt = now;

        if (Status == TicketStatus.New)
        {
            Status = TicketStatus.Assigned;
        }
    }

    public void ChangeStatus(TicketStatus newStatus, DateTimeOffset now)
    {
        if (Status == newStatus)
        {
            return;
        }

        if (!CanTransitionTo(newStatus))
        {
            throw new DomainException($"Ticket cannot move from {Status} to {newStatus}.");
        }

        Status = newStatus;
        UpdatedAt = now;

        if (newStatus == TicketStatus.Resolved)
        {
            ResolvedAt = now;
        }

        if (newStatus == TicketStatus.Closed)
        {
            ResolvedAt ??= now;
            ClosedAt = now;
        }
    }

    public void ChangePriority(TicketPriority priority, DateTimeOffset now)
    {
        EnsureOpen();

        Priority = priority;
        SlaDueAt = SlaPolicy.CalculateDueAt(CreatedAt, priority);
        UpdatedAt = now;
    }

    public void LinkAsset(Guid assetId, DateTimeOffset now)
    {
        EnsureOpen();

        RelatedAssetId = assetId;
        UpdatedAt = now;
    }

    public void AddComment(TicketComment comment, DateTimeOffset now)
    {
        if (Status is TicketStatus.Closed or TicketStatus.Cancelled)
        {
            throw new DomainException("Closed or cancelled ticket cannot be commented.");
        }

        Comments.Add(comment);
        UpdatedAt = now;
    }

    public bool IsOverdue(DateTimeOffset now)
    {
        return Status is not TicketStatus.Resolved and not TicketStatus.Closed and not TicketStatus.Cancelled
            && now > SlaDueAt;
    }

    public bool CanTransitionTo(TicketStatus newStatus)
    {
        return Status switch
        {
            TicketStatus.New => newStatus is TicketStatus.Assigned or TicketStatus.Cancelled,
            TicketStatus.Assigned => newStatus is TicketStatus.InProgress or TicketStatus.WaitingForUser or TicketStatus.Resolved or TicketStatus.Cancelled,
            TicketStatus.InProgress => newStatus is TicketStatus.Assigned or TicketStatus.WaitingForUser or TicketStatus.Resolved or TicketStatus.Cancelled,
            TicketStatus.WaitingForUser => newStatus is TicketStatus.InProgress or TicketStatus.Resolved or TicketStatus.Cancelled,
            TicketStatus.Resolved => newStatus is TicketStatus.Closed or TicketStatus.InProgress,
            TicketStatus.Closed => false,
            TicketStatus.Cancelled => false,
            _ => false
        };
    }

    private void EnsureOpen()
    {
        if (Status is TicketStatus.Closed or TicketStatus.Cancelled)
        {
            throw new DomainException("Closed or cancelled ticket cannot be modified.");
        }
    }
}
