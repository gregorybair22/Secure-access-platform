using SecureAccess.Domain.Common;

namespace SecureAccess.Domain.Entities;

/// <summary>
/// Tracks a user session: login/logout time, duration and the counts of
/// machines, customers and credentials accessed during the session.
/// </summary>
public class Session : BaseEntity
{
    /// <summary>Public session identifier referenced by audit logs and access requests.</summary>
    public Guid SessionToken { get; set; } = Guid.NewGuid();

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime LoginUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LogoutUtc { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public int MachinesAccessed { get; set; }

    public int CustomersAccessed { get; set; }

    public int CredentialsViewed { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Computed session duration; null while the session is still active.</summary>
    public TimeSpan? Duration => LogoutUtc.HasValue ? LogoutUtc.Value - LoginUtc : null;
}
