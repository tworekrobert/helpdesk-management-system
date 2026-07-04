namespace Helpdesk.Domain.Tickets;

public sealed class TicketComment
{
    private TicketComment()
    {
    }

    public TicketComment(Guid ticketId, Guid authorId, string body, bool isInternal, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        TicketId = ticketId;
        AuthorId = authorId;
        Body = body.Trim();
        IsInternal = isInternal;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public bool IsInternal { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
