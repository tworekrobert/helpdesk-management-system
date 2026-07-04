using Helpdesk.Domain.Notifications;
using Helpdesk.Domain.Tickets;
using Helpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Services;

public sealed class TicketEmailNotificationService(HelpdeskDbContext db)
{
    public async Task QueueStatusChangedAsync(
        Ticket ticket,
        string reference,
        TicketStatus oldStatus,
        TicketStatus newStatus,
        CancellationToken cancellationToken)
    {
        var recipient = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == ticket.CreatedByUserId && user.IsActive, cancellationToken);
        if (recipient is null)
        {
            return;
        }

        var subject = $"Ticket {reference} status changed to {newStatus}";
        var body = string.Join(
            Environment.NewLine,
            $"Ticket: {reference}",
            $"Title: {ticket.Title}",
            $"Previous status: {oldStatus}",
            $"Current status: {newStatus}",
            $"Changed at UTC: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm}");

        db.EmailNotifications.Add(new EmailNotification(recipient.Id, recipient.Email, subject, body, DateTimeOffset.UtcNow));
    }
}
