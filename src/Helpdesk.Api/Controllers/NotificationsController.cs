using Helpdesk.Application.Notifications;
using Helpdesk.Domain.Notifications;
using Helpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/notifications")]
public sealed class NotificationsController(HelpdeskDbContext db) : ControllerBase
{
    [HttpGet("email")]
    public async Task<ActionResult<IReadOnlyCollection<EmailNotificationResponse>>> Email(
        [FromQuery] EmailNotificationStatus? status,
        CancellationToken cancellationToken)
    {
        var query = db.EmailNotifications.AsNoTracking().AsQueryable();
        if (status is not null)
        {
            query = query.Where(notification => notification.Status == status);
        }

        var notifications = await query
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(200)
            .Select(notification => new EmailNotificationResponse(
                notification.Id,
                notification.RecipientUserId,
                notification.ToEmail,
                notification.Subject,
                notification.Body,
                notification.Status,
                notification.CreatedAt,
                notification.ProcessedAt,
                notification.Error))
            .ToListAsync(cancellationToken);

        return Ok(notifications);
    }
}
