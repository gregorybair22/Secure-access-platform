using SecureAccess.Domain.Common;
using SecureAccess.Domain.Enums;

namespace SecureAccess.Domain.Entities;

/// <summary>
/// An internal application user (technician, administrator or auditor).
/// Passwords are stored only as BCrypt hashes, never in plain text.
/// </summary>
public class User : BaseEntity
{
    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public EntityStatus Status { get; set; } = EntityStatus.Active;

    /// <summary>Set when the account is provisioned from an external directory (future AD integration).</summary>
    public bool IsDomainAccount { get; set; }

    public DateTime? LastLoginUtc { get; set; }

    public int FailedLoginAttempts { get; set; }

    public DateTime? LockedOutUntilUtc { get; set; }

    public bool MustChangePassword { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
