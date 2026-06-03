using SecureAccess.Domain.Enums;

namespace SecureAccess.Application.Abstractions;

/// <summary>Writes append-only audit, change and login records.</summary>
public interface IAuditService
{
    Task LogAsync(AuditEntry entry, CancellationToken ct = default);

    Task LogChangeAsync(string entityType, int entityId, string field,
        string? previousValue, string? newValue, string? reason = null, CancellationToken ct = default);

    Task LogLoginAsync(int? userId, string userName, AuditResult result,
        string? ip, string? userAgent, string? failureReason, CancellationToken ct = default);
}

/// <summary>Parameters for a single audit log entry.</summary>
public class AuditEntry
{
    public AuditAction Action { get; set; }
    public AuditResult Result { get; set; } = AuditResult.Success;
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public int? MachineId { get; set; }
    public string? MachineName { get; set; }
    public int? CredentialId { get; set; }
    public string? CredentialType { get; set; }
    public string? Reason { get; set; }
    public string? InternalTicket { get; set; }
    public string? CustomerTicket { get; set; }
    public string? Details { get; set; }
}
