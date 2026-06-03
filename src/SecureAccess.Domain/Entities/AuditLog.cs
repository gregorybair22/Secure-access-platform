using SecureAccess.Domain.Enums;

namespace SecureAccess.Domain.Entities;

/// <summary>
/// Append-only record of an audited action. Audit logs are never updated or
/// hard-deleted (5 year minimum retention); old rows are flagged
/// <see cref="IsArchived"/> but remain searchable.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public string? UserName { get; set; }

    public AuditAction Action { get; set; }

    public AuditResult Result { get; set; } = AuditResult.Success;

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public string? IpAddress { get; set; }

    public Guid? SessionId { get; set; }

    public int? ClientId { get; set; }
    public string? ClientName { get; set; }

    public int? MachineId { get; set; }
    public string? MachineName { get; set; }

    public int? CredentialId { get; set; }
    public string? CredentialType { get; set; }

    public string? Reason { get; set; }

    public string? InternalTicket { get; set; }

    public string? CustomerTicket { get; set; }

    /// <summary>Optional free-form detail (entity name, summary, etc.).</summary>
    public string? Details { get; set; }

    public bool IsArchived { get; set; }
}
