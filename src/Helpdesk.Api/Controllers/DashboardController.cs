using Helpdesk.Api.Extensions;
using Helpdesk.Application.Dashboard;
using Helpdesk.Domain.Assets;
using Helpdesk.Domain.Tickets;
using Helpdesk.Domain.Users;
using Helpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(HelpdeskDbContext db) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> Summary(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var oneWeekAgo = now.AddDays(-7);
        var ticketQuery = ScopedTicketQuery();
        var assetQuery = ScopedAssetQuery();

        var tickets = await ticketQuery.ToListAsync(cancellationToken);
        var assets = await assetQuery.ToListAsync(cancellationToken);
        var openStatuses = new[]
        {
            TicketStatus.New,
            TicketStatus.Assigned,
            TicketStatus.InProgress,
            TicketStatus.WaitingForUser
        };

        var resolvedDurations = tickets
            .Where(ticket => ticket.ResolvedAt.HasValue)
            .Select(ticket => ticket.ResolvedAt!.Value - ticket.CreatedAt)
            .ToList();

        var userIds = tickets.Select(ticket => ticket.CreatedByUserId).Distinct().ToArray();
        var users = await db.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        var response = new DashboardSummaryResponse(
            tickets.Count(ticket => openStatuses.Contains(ticket.Status)),
            tickets.Count(ticket => ticket.AssignedTechnicianId is null && ticket.Status == TicketStatus.New),
            tickets.Count(ticket => ticket.IsOverdue(now)),
            tickets.Count(ticket => ticket.ResolvedAt >= oneWeekAgo),
            FormatAverageDuration(resolvedDurations),
            assets.Count(asset => asset.Status == AssetStatus.Available),
            assets.Count(asset => asset.Status == AssetStatus.InRepair),
            Enum.GetValues<TicketPriority>()
                .Select(priority => new CountByTicketPriorityResponse(priority, tickets.Count(ticket => ticket.Priority == priority)))
                .ToList(),
            tickets
                .GroupBy(ticket => ticket.Category?.Name ?? "Unknown")
                .OrderBy(group => group.Key)
                .Select(group => new CountByCategoryResponse(group.Key, group.Count()))
                .ToList(),
            Enum.GetValues<AssetStatus>()
                .Select(status => new CountByAssetStatusResponse(status, assets.Count(asset => asset.Status == status)))
                .ToList(),
            tickets
                .GroupBy(ticket => ticket.CreatedByUserId)
                .OrderByDescending(group => group.Count())
                .Take(5)
                .Select(group => new TopUserTicketsResponse(users[group.Key].FullName, group.Count()))
                .ToList());

        return Ok(response);
    }

    [HttpGet("tickets-by-category")]
    public async Task<ActionResult<IReadOnlyCollection<CountByCategoryResponse>>> TicketsByCategory(CancellationToken cancellationToken)
    {
        var tickets = await ScopedTicketQuery().ToListAsync(cancellationToken);
        var response = tickets
            .GroupBy(ticket => ticket.Category?.Name ?? "Unknown")
            .OrderBy(group => group.Key)
            .Select(group => new CountByCategoryResponse(group.Key, group.Count()))
            .ToList();

        return Ok(response);
    }

    [HttpGet("assets-by-status")]
    public async Task<ActionResult<IReadOnlyCollection<CountByAssetStatusResponse>>> AssetsByStatus(CancellationToken cancellationToken)
    {
        var assets = await ScopedAssetQuery().ToListAsync(cancellationToken);
        var response = Enum.GetValues<AssetStatus>()
            .Select(status => new CountByAssetStatusResponse(status, assets.Count(asset => asset.Status == status)))
            .ToList();

        return Ok(response);
    }

    private IQueryable<Ticket> ScopedTicketQuery()
    {
        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();
        var query = db.Tickets.Include(ticket => ticket.Category).AsNoTracking().AsQueryable();

        return role switch
        {
            UserRole.Employee => query.Where(ticket => ticket.CreatedByUserId == currentUserId),
            UserRole.Technician => query.Where(ticket => ticket.AssignedTechnicianId == currentUserId || ticket.AssignedTechnicianId == null),
            _ => query
        };
    }

    private IQueryable<Asset> ScopedAssetQuery()
    {
        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();
        var query = db.Assets.AsNoTracking().AsQueryable();

        return role == UserRole.Employee
            ? query.Where(asset => asset.AssignedToUserId == currentUserId)
            : query;
    }

    private static string FormatAverageDuration(IReadOnlyCollection<TimeSpan> durations)
    {
        if (durations.Count == 0)
        {
            return "n/a";
        }

        var averageTicks = Convert.ToInt64(durations.Average(duration => duration.Ticks));
        var average = TimeSpan.FromTicks(averageTicks);
        return average.TotalHours >= 1
            ? $"{(int)average.TotalHours}h {average.Minutes}m"
            : $"{average.Minutes}m";
    }
}
