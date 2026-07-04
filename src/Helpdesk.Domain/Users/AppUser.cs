namespace Helpdesk.Domain.Users;

public sealed class AppUser
{
    private AppUser()
    {
    }

    public AppUser(string fullName, string email, string passwordHash, UserRole role, string department)
    {
        Id = Guid.NewGuid();
        FullName = fullName.Trim();
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        Role = role;
        Department = department.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public string Department { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void UpdateRole(UserRole role)
    {
        Role = role;
    }

    public void UpdateProfile(string fullName, string department)
    {
        FullName = fullName.Trim();
        Department = department.Trim();
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
