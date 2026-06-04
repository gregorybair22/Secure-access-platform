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

    public async Task<DashboardData> GetAsync(DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var to = toUtc?.Date.AddDays(1) ?? today.AddDays(1);
        var from = fromUtc?.Date ?? today.AddMonths(-1);
        var rangeDays = Math.Max(1, (to - from).Days);

        var prevTo = from;
        var prevFrom = from.AddDays(-rangeDays);

        var yesterday = today.AddDays(-1);
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var prevWeekStart = weekStart.AddDays(-7);
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var prevMonthStart = monthStart.AddMonths(-1);

        var access = _db.AuditLogs.AsNoTracking().Where(a => a.Action == AuditAction.CredentialAccess);
        var accessInRange = access.Where(a => a.TimestampUtc >= from && a.TimestampUtc < to);

        var data = new DashboardData
        {
            RangeFromUtc = from,
            RangeToUtc = to.AddDays(-1),

            AccessesToday = await access.CountAsync(a => a.TimestampUtc >= today, ct),
            AccessesYesterday = await access.CountAsync(a => a.TimestampUtc >= yesterday && a.TimestampUtc < today, ct),
            AccessesThisWeek = await access.CountAsync(a => a.TimestampUtc >= weekStart, ct),
            AccessesPreviousWeek = await access.CountAsync(a => a.TimestampUtc >= prevWeekStart && a.TimestampUtc < weekStart, ct),
            AccessesThisMonth = await access.CountAsync(a => a.TimestampUtc >= monthStart, ct),
            AccessesPreviousMonth = await access.CountAsync(a => a.TimestampUtc >= prevMonthStart && a.TimestampUtc < monthStart, ct),

            ActiveSessions = await _db.Sessions.CountAsync(s => s.IsActive, ct),
            SessionsLoggedInYesterday = await _db.Sessions.CountAsync(
                s => s.LoginUtc >= yesterday && s.LoginUtc < today, ct),

            MostActiveTechnicians = await accessInRange.Where(a => a.UserName != null)
                .GroupBy(a => a.UserName!).Select(g => new UsageRow { Name = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count).Take(5).ToListAsync(ct),

            MostAccessedCustomers = await accessInRange.Where(a => a.ClientName != null)
                .GroupBy(a => a.ClientName!).Select(g => new UsageRow { Name = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count).Take(5).ToListAsync(ct),

            MostAccessedMachines = await accessInRange.Where(a => a.MachineName != null)
                .GroupBy(a => a.MachineName!).Select(g => new UsageRow { Name = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count).Take(5).ToListAsync(ct),

            FailedLoginAttempts = await _db.LoginLogs.CountAsync(
                l => l.Result == AuditResult.Failure && l.TimestampUtc >= from && l.TimestampUtc < to, ct),
            FailedLoginAttemptsPrevious30 = await _db.LoginLogs.CountAsync(
                l => l.Result == AuditResult.Failure && l.TimestampUtc >= prevFrom && l.TimestampUtc < prevTo, ct),

            UnauthorizedAccessAttempts = await _db.AuditLogs.CountAsync(
                a => a.Action == AuditAction.UnauthorizedAccess && a.TimestampUtc >= from && a.TimestampUtc < to, ct),
            UnauthorizedAccessAttemptsPrevious30 = await _db.AuditLogs.CountAsync(
                a => a.Action == AuditAction.UnauthorizedAccess && a.TimestampUtc >= prevFrom && a.TimestampUtc < prevTo, ct),

            CredentialsViewedInRange = await accessInRange.CountAsync(ct),
            CredentialsViewedPreviousRange = await access.CountAsync(
                a => a.TimestampUtc >= prevFrom && a.TimestampUtc < prevTo, ct),

            TotalClients = await _db.Clients.CountAsync(ct),
            TotalMachines = await _db.Machines.CountAsync(ct),
            TotalCredentials = await _db.Credentials.CountAsync(ct),

            ClientsAddedInRange = await _db.Clients.CountAsync(c => c.CreatedAtUtc >= from && c.CreatedAtUtc < to, ct),
            ClientsAddedPreviousRange = await _db.Clients.CountAsync(
                c => c.CreatedAtUtc >= prevFrom && c.CreatedAtUtc < prevTo, ct),
            MachinesAddedInRange = await _db.Machines.CountAsync(c => c.CreatedAtUtc >= from && c.CreatedAtUtc < to, ct),
            MachinesAddedPreviousRange = await _db.Machines.CountAsync(
                c => c.CreatedAtUtc >= prevFrom && c.CreatedAtUtc < prevTo, ct),
            CredentialsAddedInRange = await _db.Credentials.CountAsync(c => c.CreatedAtUtc >= from && c.CreatedAtUtc < to, ct),
            CredentialsAddedPreviousRange = await _db.Credentials.CountAsync(
                c => c.CreatedAtUtc >= prevFrom && c.CreatedAtUtc < prevTo, ct)
        };

        data.AccessesOutsideWorkingHours = await accessInRange
            .CountAsync(a => a.TimestampUtc.Hour < 7 || a.TimestampUtc.Hour >= 19, ct);
        data.AccessesOutsideWorkingHoursPrevious30 = await access
            .CountAsync(a => a.TimestampUtc >= prevFrom && a.TimestampUtc < prevTo
                && (a.TimestampUtc.Hour < 7 || a.TimestampUtc.Hour >= 19), ct);

        var lastViewed = await access.OrderByDescending(a => a.TimestampUtc).FirstOrDefaultAsync(ct);
        data.LastCredentialViewedUtc = lastViewed?.TimestampUtc;
        data.LastCredentialViewedBy = lastViewed?.UserName;

        data.LastBulkUpdateUtc = await _db.AuditLogs.Where(a => a.Action == AuditAction.BulkUpdate)
            .OrderByDescending(a => a.TimestampUtc).Select(a => (DateTime?)a.TimestampUtc).FirstOrDefaultAsync(ct);

        var recent = await _db.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.TimestampUtc)
            .Take(6)
            .Select(a => new
            {
                a.TimestampUtc,
                a.Action,
                a.UserName,
                a.ClientName,
                a.MachineName,
                a.Result
            })
            .ToListAsync(ct);

        data.RecentActivity = recent.Select(a => new DashboardActivityItem
        {
            TimestampUtc = a.TimestampUtc,
            Title = FormatActivityTitle(a.Action, a.UserName, a.ClientName, a.MachineName),
            Subtitle = a.TimestampUtc.ToLocalTime().ToString("g"),
            Tone = ActivityTone(a.Action, a.Result)
        }).ToList();

        return data;
    }

    private static string FormatActivityTitle(AuditAction action, string? user, string? client, string? machine)
    {
        var who = string.IsNullOrWhiteSpace(user) ? "System" : user;
        return action switch
        {
            AuditAction.CredentialAccess => $"Credential accessed by {who}" +
                (string.IsNullOrWhiteSpace(machine) ? "" : $" · {machine}"),
            AuditAction.CredentialModify => $"Credential updated by {who}",
            AuditAction.CredentialDelete => $"Credential deleted by {who}",
            AuditAction.BulkUpdate => $"Bulk update by {who}",
            AuditAction.UnauthorizedAccess => "Unauthorized access attempt",
            AuditAction.Login => $"Login: {who}",
            AuditAction.Logout => $"Logout: {who}",
            _ => $"{action}: {who}"
        };
    }

    private static string ActivityTone(AuditAction action, AuditResult result)
    {
        if (action == AuditAction.UnauthorizedAccess || result == AuditResult.Failure)
            return "red";
        if (action == AuditAction.CredentialAccess)
            return "blue";
        if (action == AuditAction.BulkUpdate || action == AuditAction.CredentialModify)
            return "purple";
        if (action == AuditAction.CredentialDelete)
            return "orange";
        return "green";
    }
}
