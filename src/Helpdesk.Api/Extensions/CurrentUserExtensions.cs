using System.Security.Claims;
using Helpdesk.Domain.Users;

namespace Helpdesk.Api.Extensions;

public static class CurrentUserExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return value is null ? throw new InvalidOperationException("Missing user id claim.") : Guid.Parse(value);
    }

    public static UserRole GetUserRole(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<UserRole>(value, out var role)
            ? role
            : throw new InvalidOperationException("Missing role claim.");
    }

    public static bool IsTechnicianOrAdmin(this ClaimsPrincipal user)
    {
        var role = user.GetUserRole();
        return role is UserRole.Technician or UserRole.Admin;
    }
}
