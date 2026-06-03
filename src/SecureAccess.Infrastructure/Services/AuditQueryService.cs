using Microsoft.EntityFrameworkCore;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Audit;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

public class AuditQueryService : IAuditQueryService
{
    private readonly AppDbContext _db;

    public AuditQueryService(AppDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogDto>> QueryAsync(AuditQueryFilter filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);

        var q = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (filter.FromUtc.HasValue) q = q.Where(a => a.TimestampUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) q = q.Where(a => a.TimestampUtc <= filter.ToUtc.Value);
        if (filter.UserId.HasValue) q = q.Where(a => a.UserId == filter.UserId.Value);
        if (filter.Action.HasValue) q = q.Where(a => a.Action == filter.Action.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.TimestampUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                TimestampUtc = a.TimestampUtc,
                UserName = a.UserName,
                Action = a.Action,
                Result = a.Result,
                IpAddress = a.IpAddress,
                ClientName = a.ClientName,
                MachineName = a.MachineName,
                CredentialType = a.CredentialType,
                Reason = a.Reason,
                InternalTicket = a.InternalTicket,
                Details = a.Details
            }).ToListAsync(ct);

        return new PagedResult<AuditLogDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResult<SessionDto>> GetSessionsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.Sessions.AsNoTracking().Include(s => s.User);
        var total = await q.CountAsync(ct);
        var raw = await q.OrderByDescending(s => s.LoginUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var items = raw.Select(s => new SessionDto
        {
            Id = s.Id,
            UserName = s.User.UserName,
            LoginUtc = s.LoginUtc,
            LogoutUtc = s.LogoutUtc,
            Duration = s.Duration?.ToString(@"hh\:mm\:ss"),
            IpAddress = s.IpAddress,
            MachinesAccessed = s.MachinesAccessed,
            CustomersAccessed = s.CustomersAccessed,
            CredentialsViewed = s.CredentialsViewed,
            IsActive = s.IsActive
        }).ToList();

        return new PagedResult<SessionDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }
}
