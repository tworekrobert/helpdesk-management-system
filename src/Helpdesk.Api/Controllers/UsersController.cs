using Helpdesk.Application.Users;
using Helpdesk.Domain.Users;
using Helpdesk.Infrastructure.Auth;
using Helpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/users")]
public sealed class UsersController(HelpdeskDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> List(CancellationToken cancellationToken)
    {
        var users = await db.Users
            .AsNoTracking()
            .OrderBy(user => user.FullName)
            .Select(user => new UserResponse(
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.Department,
                user.IsActive,
                user.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var email = AppUser.NormalizeEmail(request.Email);
        var exists = await db.Users.AnyAsync(user => user.Email == email, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "User with this email already exists." });
        }

        var user = new AppUser(request.FullName, request.Email, PasswordHasher.Hash(request.Password), request.Role, request.Department);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(List), new UserResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.Department,
            user.IsActive,
            user.CreatedAt));
    }

    [HttpPatch("{id:guid}/role")]
    public async Task<ActionResult<UserResponse>> ChangeRole(Guid id, UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.UpdateRole(request.Role);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new UserResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.Department,
            user.IsActive,
            user.CreatedAt));
    }
}
