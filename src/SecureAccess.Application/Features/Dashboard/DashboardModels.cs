using SecureAccess.Application.Features.Reports;

namespace SecureAccess.Application.Features.Dashboard;

public class DashboardData
{
    public DateTime RangeFromUtc { get; set; }
    public DateTime RangeToUtc { get; set; }

    // Activity metrics
    public int AccessesToday { get; set; }
    public int AccessesYesterday { get; set; }
    public int AccessesThisWeek { get; set; }
    public int AccessesPreviousWeek { get; set; }
    public int AccessesThisMonth { get; set; }
    public int AccessesPreviousMonth { get; set; }
    public int ActiveSessions { get; set; }
    public int SessionsLoggedInYesterday { get; set; }
    public List<UsageRow> MostActiveTechnicians { get; set; } = new();

    // Customer metrics
    public List<UsageRow> MostAccessedCustomers { get; set; } = new();
    public List<UsageRow> MostAccessedMachines { get; set; } = new();

    // Security metrics
    public int FailedLoginAttempts { get; set; }
    public int FailedLoginAttemptsPrevious30 { get; set; }
    public int UnauthorizedAccessAttempts { get; set; }
    public int UnauthorizedAccessAttemptsPrevious30 { get; set; }
    public int AccessesOutsideWorkingHours { get; set; }
    public int AccessesOutsideWorkingHoursPrevious30 { get; set; }
    public int CredentialsViewedInRange { get; set; }
    public int CredentialsViewedPreviousRange { get; set; }

    // Audit metrics
    public DateTime? LastCredentialViewedUtc { get; set; }
    public string? LastCredentialViewedBy { get; set; }
    public DateTime? LastBulkUpdateUtc { get; set; }

    // Totals
    public int TotalClients { get; set; }
    public int TotalMachines { get; set; }
    public int TotalCredentials { get; set; }
    public int ClientsAddedInRange { get; set; }
    public int ClientsAddedPreviousRange { get; set; }
    public int MachinesAddedInRange { get; set; }
    public int MachinesAddedPreviousRange { get; set; }
    public int CredentialsAddedInRange { get; set; }
    public int CredentialsAddedPreviousRange { get; set; }

    public List<DashboardActivityItem> RecentActivity { get; set; } = new();
}

public class DashboardActivityItem
{
    public DateTime TimestampUtc { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Tone { get; set; } = "blue";
}

public interface IDashboardService
{
    Task<DashboardData> GetAsync(DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default);
}
