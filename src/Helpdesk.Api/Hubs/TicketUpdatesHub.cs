using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Helpdesk.Api.Hubs;

[Authorize]
public sealed class TicketUpdatesHub : Hub
{
}
