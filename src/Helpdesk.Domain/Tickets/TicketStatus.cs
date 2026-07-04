namespace Helpdesk.Domain.Tickets;

public enum TicketStatus
{
    New,
    Assigned,
    InProgress,
    WaitingForUser,
    Resolved,
    Closed,
    Cancelled
}
