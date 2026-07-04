using Helpdesk.Domain.Users;

namespace Helpdesk.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthenticatedUserResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    CurrentUserResponse User);

public sealed record CurrentUserResponse(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    string Department);
