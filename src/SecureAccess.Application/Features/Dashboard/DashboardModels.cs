using SecureAccess.Application.Features.Reports;

namespace SecureAccess.Application.Features.Dashboard;

public class DashboardData
{
    // Activity metrics
    public int AccessesToday { get; set; }
    public int AccessesThisWeek { get; set; }
    public int AccessesThisMonth { get; set; }
    public int ActiveUsers { get; set; }
    public List<UsageRow> MostActiveTechnicians { get; set; } = new();

    // Customer metrics
    public List<UsageRow> MostAccessedCustomers { get; set; } = new();
    public List<UsageRow> MostAccessedMachines { get; set; } = new();

    // Security metrics
    public int FailedLoginAttempts { get; set; }
    public int UnauthorizedAccessAttempts { get; set; }
    public int AccessesOutsideWorkingHours { get; set; }

    // Audit metrics
    public DateTime? LastCredentialViewedUtc { get; set; }
    public string? LastCredentialViewedBy { get; set; }
    public DateTime? LastCredentialModifiedUtc { get; set; }
    public DateTime? LastCredentialDeletedUtc { get; set; }
    public DateTime? LastBulkUpdateUtc { get; set; }

    // Totals
    public int TotalClients { get; set; }
    public int TotalMachines { get; set; }
    public int TotalCredentials { get; set; }
}

public interface IDashboardService
{
    Task<DashboardData> GetAsync(CancellationToken ct = default);
}
