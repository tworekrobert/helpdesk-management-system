using Helpdesk.Application.Audit;
using Helpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/audit")]
public sealed class AuditController(HelpdeskDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AuditLogResponse>>> List(
        [FromQuery] string? entityType,
        [FromQuery] string? action,
        [FromQuery] Guid? userId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(take, 1, 250);
        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            var normalizedEntityType = entityType.Trim().ToLowerInvariant();
            query = query.Where(log => log.EntityType.ToLower() == normalizedEntityType);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var normalizedAction = action.Trim().ToLowerInvariant();
            query = query.Where(log => log.Action.ToLower().Contains(normalizedAction));
        }

        if (userId is not null)
        {
            query = query.Where(log => log.UserId == userId);
        }

        if (from is not null)
        {
            query = query.Where(log => log.CreatedAt >= from);
        }

        if (to is not null)
        {
            query = query.Where(log => log.CreatedAt <= to);
        }

        var logs = await query
            .OrderByDescending(log => log.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
        var userIds = logs.Select(log => log.UserId).Distinct().ToArray();
        var users = await db.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        return Ok(logs.Select(log =>
        {
            var actor = users[log.UserId];
            return new AuditLogResponse(
                log.Id,
                log.EntityType,
                log.EntityId,
                log.Action,
                log.OldValue,
                log.NewValue,
                log.UserId,
                actor.FullName,
                actor.Email,
                log.CreatedAt);
        }).ToList());
    }
}
