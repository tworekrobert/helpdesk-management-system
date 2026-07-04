using Helpdesk.Api.Auth;
using Helpdesk.Api.Extensions;
using Helpdesk.Application.Auth;
using Helpdesk.Domain.Users;
using Helpdesk.Infrastructure.Auth;
using Helpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(HelpdeskDbContext db, JwtTokenFactory jwtTokenFactory) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticatedUserResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = AppUser.NormalizeEmail(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null || !user.IsActive || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(jwtTokenFactory.Create(user));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse(user.Id, user.FullName, user.Email, user.Role, user.Department));
    }
}
