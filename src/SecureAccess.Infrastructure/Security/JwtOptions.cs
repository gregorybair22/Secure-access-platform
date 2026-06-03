namespace SecureAccess.Infrastructure.Security;

/// <summary>JWT bearer configuration shared by the API (issue + validate).</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "SecureAccess";
    public string Audience { get; set; } = "SecureAccess";
    /// <summary>Signing key (HMAC-SHA256). Must be at least 32 characters.</summary>
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
}
