using SecureAccess.Domain.Enums;

namespace SecureAccess.Domain.Entities;

/// <summary>
/// Record of an authentication attempt (successful or failed). Failed attempts
/// and out-of-hours logins feed the security metrics on the dashboard.
/// </summary>
public class LoginLog
{
    public long Id { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public string UserName { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public AuditResult Result { get; set; } = AuditResult.Success;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? FailureReason { get; set; }

    public bool IsArchived { get; set; }
}
