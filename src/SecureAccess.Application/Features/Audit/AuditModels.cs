using SecureAccess.Application.Common;
using SecureAccess.Domain.Enums;

namespace SecureAccess.Application.Features.Audit;

public class AuditLogDto
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string? UserName { get; set; }
    public AuditAction Action { get; set; }
    public AuditResult Result { get; set; }
    public string? IpAddress { get; set; }
    public string? ClientName { get; set; }
    public string? MachineName { get; set; }
    public string? CredentialType { get; set; }
    public string? Reason { get; set; }
    public string? InternalTicket { get; set; }
    public string? Details { get; set; }
}

public class AuditQueryFilter
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int? UserId { get; set; }
    public AuditAction? Action { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class SessionDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime LoginUtc { get; set; }
    public DateTime? LogoutUtc { get; set; }
    public string? Duration { get; set; }
    public string? IpAddress { get; set; }
    public int MachinesAccessed { get; set; }
    public int CustomersAccessed { get; set; }
    public int CredentialsViewed { get; set; }
    public bool IsActive { get; set; }
}

public interface IAuditQueryService
{
    Task<PagedResult<AuditLogDto>> QueryAsync(AuditQueryFilter filter, CancellationToken ct = default);
    Task<PagedResult<SessionDto>> GetSessionsAsync(int page, int pageSize, CancellationToken ct = default);
}
