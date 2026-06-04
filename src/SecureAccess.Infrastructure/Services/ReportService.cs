using Microsoft.EntityFrameworkCore;
using SecureAccess.Application.Features.Reports;
using SecureAccess.Domain.Enums;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;

    public async Task<ReportListStats> GetListStatsAsync(CancellationToken ct = default)
    {
        var q = _db.AuditLogs.AsNoTracking().Where(a => a.Action == AuditAction.CredentialAccess);
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new ReportListStats
        {
            TotalLogs = await q.CountAsync(ct),
            LogsThisMonth = await q.CountAsync(a => a.TimestampUtc >= monthStart, ct),
            UniqueUsers = await q
                .Where(a => a.UserName != null && a.UserName != "")
                .Select(a => a.UserName!)
                .Distinct()
                .CountAsync(ct),
            UniqueMachines = await q
                .Where(a => a.MachineId != null)
                .Select(a => a.MachineId!.Value)
                .Distinct()
                .CountAsync(ct)
        };
    }

    private IQueryable<Domain.Entities.AuditLog> AccessQuery(ReportFilter f)
    {
        var q = _db.AuditLogs.AsNoTracking().Where(a => a.Action == AuditAction.CredentialAccess);
        if (f.FromUtc.HasValue) q = q.Where(a => a.TimestampUtc >= f.FromUtc.Value);
        if (f.ToUtc.HasValue) q = q.Where(a => a.TimestampUtc <= f.ToUtc.Value);
        if (f.UserId.HasValue) q = q.Where(a => a.UserId == f.UserId.Value);
        if (f.ClientId.HasValue) q = q.Where(a => a.ClientId == f.ClientId.Value);
        if (f.MachineId.HasValue) q = q.Where(a => a.MachineId == f.MachineId.Value);
        return q;
    }

    public async Task<IReadOnlyList<CredentialAccessRow>> CredentialAccessAsync(ReportFilter filter, CancellationToken ct = default)
        => await AccessQuery(filter).OrderByDescending(a => a.TimestampUtc)
            .Select(a => new CredentialAccessRow
            {
                TimestampUtc = a.TimestampUtc,
                UserName = a.UserName ?? "",
                ClientName = a.ClientName,
                MachineName = a.MachineName,
                CredentialType = a.CredentialType,
                Reason = a.Reason,
                InternalTicket = a.InternalTicket,
                IpAddress = a.IpAddress
            }).Take(5000).ToListAsync(ct);

    public async Task<IReadOnlyList<TechnicianActivityRow>> TechnicianActivityAsync(ReportFilter filter, CancellationToken ct = default)
        => await AccessQuery(filter)
            .GroupBy(a => a.UserName)
            .Select(g => new TechnicianActivityRow
            {
                UserName = g.Key ?? "",
                AccessCount = g.Count(),
                DistinctClients = g.Select(x => x.ClientId).Distinct().Count(),
                DistinctMachines = g.Select(x => x.MachineId).Distinct().Count(),
                LastAccessUtc = g.Max(x => x.TimestampUtc)
            })
            .OrderByDescending(r => r.AccessCount).ToListAsync(ct);

    public async Task<IReadOnlyList<CustomerAccessRow>> CustomerAccessAsync(ReportFilter filter, CancellationToken ct = default)
        => await AccessQuery(filter).OrderByDescending(a => a.TimestampUtc)
            .Select(a => new CustomerAccessRow
            {
                ClientName = a.ClientName ?? "",
                UserName = a.UserName ?? "",
                Reason = a.Reason,
                TimestampUtc = a.TimestampUtc,
                CredentialType = a.CredentialType
            }).Take(5000).ToListAsync(ct);

    public async Task<IReadOnlyList<MachineAccessRow>> MachineAccessAsync(ReportFilter filter, CancellationToken ct = default)
    {
        var grouped = await AccessQuery(filter)
            .Where(a => a.MachineId != null)
            .GroupBy(a => new { a.MachineId, a.MachineName, a.ClientName })
            .Select(g => new
            {
                g.Key.MachineName,
                g.Key.ClientName,
                Interventions = g.Count(),
                Last = g.Max(x => x.TimestampUtc)
            })
            .OrderByDescending(x => x.Interventions).ToListAsync(ct);

        var rows = new List<MachineAccessRow>();
        foreach (var g in grouped)
        {
            var lastTech = await AccessQuery(filter)
                .Where(a => a.MachineName == g.MachineName && a.TimestampUtc == g.Last)
                .Select(a => a.UserName).FirstOrDefaultAsync(ct);
            rows.Add(new MachineAccessRow
            {
                MachineName = g.MachineName ?? "",
                ClientName = g.ClientName ?? "",
                Interventions = g.Interventions,
                LastAccessUtc = g.Last,
                LastTechnician = lastTech
            });
        }
        return rows;
    }

    public async Task<CredentialUsageReport> CredentialUsageAsync(ReportFilter filter, CancellationToken ct = default)
    {
        var q = AccessQuery(filter);
        return new CredentialUsageReport
        {
            MostAccessedCredentialTypes = await q.Where(a => a.CredentialType != null)
                .GroupBy(a => a.CredentialType!).Select(g => new UsageRow { Name = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count).Take(20).ToListAsync(ct),
            MostAccessedCustomers = await q.Where(a => a.ClientName != null)
                .GroupBy(a => a.ClientName!).Select(g => new UsageRow { Name = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count).Take(20).ToListAsync(ct),
            MostAccessedMachines = await q.Where(a => a.MachineName != null)
                .GroupBy(a => a.MachineName!).Select(g => new UsageRow { Name = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count).Take(20).ToListAsync(ct)
        };
    }

    public async Task<IReadOnlyList<ReasonAnalysisRow>> ReasonAnalysisAsync(ReportFilter filter, CancellationToken ct = default)
    {
        var q = _db.AccessRequests.AsNoTracking().AsQueryable();
        if (filter.FromUtc.HasValue) q = q.Where(a => a.RequestedAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) q = q.Where(a => a.RequestedAtUtc <= filter.ToUtc.Value);
        if (filter.UserId.HasValue) q = q.Where(a => a.UserId == filter.UserId.Value);

        var grouped = await q.GroupBy(a => a.ReasonCategory)
            .Select(g => new { Category = g.Key, Count = g.Count() }).ToListAsync(ct);

        return grouped.Select(g => new ReasonAnalysisRow
        {
            Category = g.Category,
            CategoryName = g.Category.ToString(),
            Count = g.Count
        }).OrderByDescending(r => r.Count).ToList();
    }

    public async Task<IReadOnlyList<ChangeHistoryRow>> ChangeHistoryAsync(ReportFilter filter, CancellationToken ct = default)
    {
        var q = _db.ChangeLogs.AsNoTracking().AsQueryable();
        if (filter.FromUtc.HasValue) q = q.Where(a => a.ModifiedAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) q = q.Where(a => a.ModifiedAtUtc <= filter.ToUtc.Value);
        if (filter.UserId.HasValue) q = q.Where(a => a.ModifiedByUserId == filter.UserId.Value);

        return await q.OrderByDescending(a => a.ModifiedAtUtc).Take(5000)
            .Select(a => new ChangeHistoryRow
            {
                ModifiedAtUtc = a.ModifiedAtUtc,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                FieldName = a.FieldName,
                PreviousValue = a.PreviousValue,
                NewValue = a.NewValue,
                ModifiedBy = a.ModifiedByUserName,
                Reason = a.Reason
            }).ToListAsync(ct);
    }
}
