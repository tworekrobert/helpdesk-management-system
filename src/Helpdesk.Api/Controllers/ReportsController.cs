using System.Globalization;
using System.Text;
using Helpdesk.Application.Reports;
using Helpdesk.Domain.Assets;
using Helpdesk.Domain.Tickets;
using Helpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/reports")]
public sealed class ReportsController(HelpdeskDbContext db) : ControllerBase
{
    [HttpGet("dashboard.csv")]
    public async Task<IActionResult> DashboardCsv(CancellationToken cancellationToken)
    {
        var report = await BuildDashboardReportAsync(cancellationToken);
        var csv = BuildCsv(report);
        var fileName = $"helpdesk-dashboard-{DateTimeOffset.UtcNow:yyyyMMdd-HHmm}.csv";

        return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("dashboard.pdf")]
    public async Task<IActionResult> DashboardPdf(CancellationToken cancellationToken)
    {
        var report = await BuildDashboardReportAsync(cancellationToken);
        var lines = BuildPdfLines(report);
        var fileName = $"helpdesk-dashboard-{DateTimeOffset.UtcNow:yyyyMMdd-HHmm}.pdf";

        return File(SimplePdfReport.Create("IT Helpdesk dashboard report", lines), "application/pdf", fileName);
    }

    private async Task<DashboardReport> BuildDashboardReportAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var oneWeekAgo = now.AddDays(-7);
        var openStatuses = new[]
        {
            TicketStatus.New,
            TicketStatus.Assigned,
            TicketStatus.InProgress,
            TicketStatus.WaitingForUser
        };

        var tickets = await db.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Category)
            .ToListAsync(cancellationToken);
        var assets = await db.Assets
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var resolvedDurations = tickets
            .Where(ticket => ticket.ResolvedAt.HasValue)
            .Select(ticket => ticket.ResolvedAt!.Value - ticket.CreatedAt)
            .ToList();
        var userIds = tickets.Select(ticket => ticket.CreatedByUserId).Distinct().ToArray();
        var users = await db.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        return new DashboardReport(
            now,
            tickets.Count,
            tickets.Count(ticket => openStatuses.Contains(ticket.Status)),
            tickets.Count(ticket => ticket.AssignedTechnicianId is null && ticket.Status == TicketStatus.New),
            tickets.Count(ticket => ticket.IsOverdue(now)),
            tickets.Count(ticket => ticket.ResolvedAt >= oneWeekAgo),
            FormatAverageDuration(resolvedDurations),
            assets.Count,
            assets.Count(asset => asset.Status == AssetStatus.Available),
            assets.Count(asset => asset.Status == AssetStatus.InRepair),
            Enum.GetValues<TicketPriority>()
                .Select(priority => new NamedCount(priority.ToString(), tickets.Count(ticket => ticket.Priority == priority)))
                .ToList(),
            tickets
                .GroupBy(ticket => ticket.Category?.Name ?? "Unknown")
                .OrderBy(group => group.Key)
                .Select(group => new NamedCount(group.Key, group.Count()))
                .ToList(),
            Enum.GetValues<AssetStatus>()
                .Select(status => new NamedCount(status.ToString(), assets.Count(asset => asset.Status == status)))
                .ToList(),
            tickets
                .GroupBy(ticket => ticket.CreatedByUserId)
                .OrderByDescending(group => group.Count())
                .Take(5)
                .Select(group => new NamedCount(users.TryGetValue(group.Key, out var user) ? user.FullName : "Unknown user", group.Count()))
                .ToList());
    }

    private static string BuildCsv(DashboardReport report)
    {
        var builder = new StringBuilder();
        AppendCsvRow(builder, "Section", "Name", "Value");
        AppendCsvRow(builder, "Summary", "Generated at UTC", report.GeneratedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        AppendCsvRow(builder, "Summary", "Total tickets", report.TotalTickets.ToString(CultureInfo.InvariantCulture));
        AppendCsvRow(builder, "Summary", "Open tickets", report.OpenTickets.ToString(CultureInfo.InvariantCulture));
        AppendCsvRow(builder, "Summary", "Unassigned tickets", report.UnassignedTickets.ToString(CultureInfo.InvariantCulture));
        AppendCsvRow(builder, "Summary", "Overdue tickets", report.OverdueTickets.ToString(CultureInfo.InvariantCulture));
        AppendCsvRow(builder, "Summary", "Resolved this week", report.ResolvedThisWeek.ToString(CultureInfo.InvariantCulture));
        AppendCsvRow(builder, "Summary", "Average resolution time", report.AverageResolutionTime);
        AppendCsvRow(builder, "Summary", "Total assets", report.TotalAssets.ToString(CultureInfo.InvariantCulture));
        AppendCsvRow(builder, "Summary", "Available assets", report.AvailableAssets.ToString(CultureInfo.InvariantCulture));
        AppendCsvRow(builder, "Summary", "Assets in repair", report.AssetsInRepair.ToString(CultureInfo.InvariantCulture));

        foreach (var item in report.TicketsByPriority)
        {
            AppendCsvRow(builder, "Tickets by priority", item.Name, item.Count.ToString(CultureInfo.InvariantCulture));
        }

        foreach (var item in report.TicketsByCategory)
        {
            AppendCsvRow(builder, "Tickets by category", item.Name, item.Count.ToString(CultureInfo.InvariantCulture));
        }

        foreach (var item in report.AssetsByStatus)
        {
            AppendCsvRow(builder, "Assets by status", item.Name, item.Count.ToString(CultureInfo.InvariantCulture));
        }

        foreach (var item in report.TopUsers)
        {
            AppendCsvRow(builder, "Top users", item.Name, item.Count.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static IReadOnlyCollection<string> BuildPdfLines(DashboardReport report)
    {
        var lines = new List<string>
        {
            $"Generated at UTC: {report.GeneratedAt.UtcDateTime:yyyy-MM-dd HH:mm}",
            $"Tickets: {report.TotalTickets} total, {report.OpenTickets} open, {report.UnassignedTickets} unassigned, {report.OverdueTickets} overdue",
            $"Resolved this week: {report.ResolvedThisWeek}",
            $"Average resolution time: {report.AverageResolutionTime}",
            $"Assets: {report.TotalAssets} total, {report.AvailableAssets} available, {report.AssetsInRepair} in repair",
            string.Empty,
            "Tickets by priority:"
        };

        lines.AddRange(report.TicketsByPriority.Select(item => $"- {item.Name}: {item.Count}"));
        lines.Add(string.Empty);
        lines.Add("Tickets by category:");
        lines.AddRange(report.TicketsByCategory.Select(item => $"- {item.Name}: {item.Count}"));
        lines.Add(string.Empty);
        lines.Add("Assets by status:");
        lines.AddRange(report.AssetsByStatus.Select(item => $"- {item.Name}: {item.Count}"));
        lines.Add(string.Empty);
        lines.Add("Top requesting users:");
        lines.AddRange(report.TopUsers.Select(item => $"- {item.Name}: {item.Count}"));

        return lines;
    }

    private static void AppendCsvRow(StringBuilder builder, params string[] cells)
    {
        builder.AppendLine(string.Join(',', cells.Select(EscapeCsvCell)));
    }

    private static string EscapeCsvCell(string value)
    {
        return value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
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

    private sealed record DashboardReport(
        DateTimeOffset GeneratedAt,
        int TotalTickets,
        int OpenTickets,
        int UnassignedTickets,
        int OverdueTickets,
        int ResolvedThisWeek,
        string AverageResolutionTime,
        int TotalAssets,
        int AvailableAssets,
        int AssetsInRepair,
        IReadOnlyCollection<NamedCount> TicketsByPriority,
        IReadOnlyCollection<NamedCount> TicketsByCategory,
        IReadOnlyCollection<NamedCount> AssetsByStatus,
        IReadOnlyCollection<NamedCount> TopUsers);

    private sealed record NamedCount(string Name, int Count);
}
