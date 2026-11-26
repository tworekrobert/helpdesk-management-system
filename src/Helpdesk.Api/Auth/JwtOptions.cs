namespace Helpdesk.Api.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "it-helpdesk";
    public string Audience { get; init; } = "it-helpdesk-client";
    public string SigningKey { get; init; } = string.Empty;
    public int ExpiresMinutes { get; init; } = 120;
}
