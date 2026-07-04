using Helpdesk.Domain.Assets;
using Helpdesk.Domain.Tickets;

namespace Helpdesk.Application.Dashboard;

public sealed record DashboardSummaryResponse(
    int OpenTickets,
    int UnassignedTickets,
    int OverdueTickets,
    int ResolvedThisWeek,
    string AverageResolutionTime,
    int AvailableAssets,
    int AssetsInRepair,
    IReadOnlyCollection<CountByTicketPriorityResponse> TicketsByPriority,
    IReadOnlyCollection<CountByCategoryResponse> TicketsByCategory,
    IReadOnlyCollection<CountByAssetStatusResponse> AssetsByStatus,
    IReadOnlyCollection<TopUserTicketsResponse> TopUsers);

public sealed record CountByTicketPriorityResponse(TicketPriority Priority, int Count);

public sealed record CountByCategoryResponse(string Category, int Count);

public sealed record CountByAssetStatusResponse(AssetStatus Status, int Count);

public sealed record TopUserTicketsResponse(string User, int Count);
