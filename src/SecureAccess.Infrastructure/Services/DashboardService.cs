using Microsoft.EntityFrameworkCore;
using SecureAccess.Application.Features.Dashboard;
using SecureAccess.Application.Features.Reports;
using SecureAccess.Domain.Enums;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardData> GetAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var access = _db.AuditLogs.AsNoTracking().Where(a => a.Action == AuditAction.CredentialAccess);

        var data = new DashboardData
        {
            AccessesToday = await access.CountAsync(a => a.TimestampUtc >= today, ct),
            AccessesThisWeek = await access.CountAsync(a => a.TimestampUtc >= weekStart, ct),
            AccessesThisMonth = await access.CountAsync(a => a.TimestampUtc >= monthStart, ct),
            ActiveUsers = await _db.Sessions.CountAsync(s => s.IsActive, ct),

            MostActiveTechnicians = await access.Where(a => a.UserName != null)
                .GroupBy(a => a.UserName!).Select(g => new UsageRow { Name = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count).Take(5).ToListAsync(ct),

            MostAccessedCustomers = await access.Where(a => a.ClientName != null)
                .GroupBy(a => a.ClientName!).Select(g => new UsageRow { Name = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count).Take(5).ToListAsync(ct),

            MostAccessedMachines = await access.Where(a => a.MachineName != null)
                .GroupBy(a => a.MachineName!).Select(g => new UsageRow { Name = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count).Take(5).ToListAsync(ct),

            FailedLoginAttempts = await _db.LoginLogs.CountAsync(l => l.Result == AuditResult.Failure && l.TimestampUtc >= monthStart, ct),
            UnauthorizedAccessAttempts = await _db.AuditLogs.CountAsync(a => a.Action == AuditAction.UnauthorizedAccess && a.TimestampUtc >= monthStart, ct),

            TotalClients = await _db.Clients.CountAsync(ct),
            TotalMachines = await _db.Machines.CountAsync(ct),
            TotalCredentials = await _db.Credentials.CountAsync(ct)
        };

        // Accesses outside working hours (before 07:00 or after 19:00 UTC) this month.
        data.AccessesOutsideWorkingHours = await access
            .Where(a => a.TimestampUtc >= monthStart && (a.TimestampUtc.Hour < 7 || a.TimestampUtc.Hour >= 19))
            .CountAsync(ct);

        var lastViewed = await access.OrderByDescending(a => a.TimestampUtc).FirstOrDefaultAsync(ct);
        data.LastCredentialViewedUtc = lastViewed?.TimestampUtc;
        data.LastCredentialViewedBy = lastViewed?.UserName;

        data.LastCredentialModifiedUtc = await _db.AuditLogs.Where(a => a.Action == AuditAction.CredentialModify)
            .OrderByDescending(a => a.TimestampUtc).Select(a => (DateTime?)a.TimestampUtc).FirstOrDefaultAsync(ct);
        data.LastCredentialDeletedUtc = await _db.AuditLogs.Where(a => a.Action == AuditAction.CredentialDelete)
            .OrderByDescending(a => a.TimestampUtc).Select(a => (DateTime?)a.TimestampUtc).FirstOrDefaultAsync(ct);
        data.LastBulkUpdateUtc = await _db.AuditLogs.Where(a => a.Action == AuditAction.BulkUpdate)
            .OrderByDescending(a => a.TimestampUtc).Select(a => (DateTime?)a.TimestampUtc).FirstOrDefaultAsync(ct);

        return data;
    }
}
