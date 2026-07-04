using Helpdesk.Domain.Users;

namespace Helpdesk.Application.Users;

public sealed record CreateUserRequest(
    string FullName,
    string Email,
    string Password,
    UserRole Role,
    string Department);

public sealed record UpdateUserRoleRequest(UserRole Role);

public sealed record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    string Department,
    bool IsActive,
    DateTimeOffset CreatedAt);
