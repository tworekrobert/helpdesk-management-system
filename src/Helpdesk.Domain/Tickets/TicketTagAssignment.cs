namespace Helpdesk.Domain.Tickets;

public sealed class TicketTagAssignment
{
    private TicketTagAssignment()
    {
    }

    public TicketTagAssignment(Guid ticketId, Guid tagId, DateTimeOffset assignedAt)
    {
        TicketId = ticketId;
        TagId = tagId;
        AssignedAt = assignedAt;
    }

    public Guid TicketId { get; private set; }
    public Ticket? Ticket { get; private set; }
    public Guid TagId { get; private set; }
    public TicketTag? Tag { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
}
