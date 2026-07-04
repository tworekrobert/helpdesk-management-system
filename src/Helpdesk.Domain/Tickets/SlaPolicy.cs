namespace Helpdesk.Domain.Tickets;

public static class SlaPolicy
{
    public static DateTimeOffset CalculateDueAt(DateTimeOffset createdAt, TicketPriority priority)
    {
        var hours = priority switch
        {
            TicketPriority.Low => 72,
            TicketPriority.Medium => 48,
            TicketPriority.High => 24,
            TicketPriority.Critical => 4,
            _ => 48
        };

        return createdAt.AddHours(hours);
    }
}
