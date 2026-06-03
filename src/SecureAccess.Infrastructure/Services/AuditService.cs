using SecureAccess.Application.Abstractions;
using SecureAccess.Domain.Entities;
using SecureAccess.Domain.Enums;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

/// <summary>Persists append-only audit, change and login records.</summary>
public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;

    public AuditService(AppDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task LogAsync(AuditEntry entry, CancellationToken ct = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _current.UserId,
            UserName = _current.UserName,
            Action = entry.Action,
            Result = entry.Result,
            TimestampUtc = DateTime.UtcNow,
            IpAddress = _current.IpAddress,
            SessionId = _current.SessionId,
            ClientId = entry.ClientId,
            ClientName = entry.ClientName,
            MachineId = entry.MachineId,
            MachineName = entry.MachineName,
            CredentialId = entry.CredentialId,
            CredentialType = entry.CredentialType,
            Reason = entry.Reason,
            InternalTicket = entry.InternalTicket,
            CustomerTicket = entry.CustomerTicket,
            Details = entry.Details
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task LogChangeAsync(string entityType, int entityId, string field,
        string? previousValue, string? newValue, string? reason = null, CancellationToken ct = default)
    {
        _db.ChangeLogs.Add(new ChangeLog
        {
            EntityType = entityType,
            EntityId = entityId,
            FieldName = field,
            PreviousValue = previousValue,
            NewValue = newValue,
            ModifiedByUserId = _current.UserId,
            ModifiedByUserName = _current.UserName,
            ModifiedAtUtc = DateTime.UtcNow,
            Reason = reason
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task LogLoginAsync(int? userId, string userName, AuditResult result,
        string? ip, string? userAgent, string? failureReason, CancellationToken ct = default)
    {
        _db.LoginLogs.Add(new LoginLog
        {
            UserId = userId,
            UserName = userName,
            Result = result,
            TimestampUtc = DateTime.UtcNow,
            IpAddress = ip,
            UserAgent = userAgent,
            FailureReason = failureReason
        });
        await _db.SaveChangesAsync(ct);
    }
}
