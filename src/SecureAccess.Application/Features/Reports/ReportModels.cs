using SecureAccess.Domain.Enums;

namespace SecureAccess.Application.Features.Reports;

/// <summary>Common filter for the reporting endpoints.</summary>
public class ReportFilter
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int? UserId { get; set; }
    public int? ClientId { get; set; }
    public int? MachineId { get; set; }
}

public class CredentialAccessRow
{
    public DateTime TimestampUtc { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public string? MachineName { get; set; }
    public string? CredentialType { get; set; }
    public string? Reason { get; set; }
    public string? InternalTicket { get; set; }
    public string? IpAddress { get; set; }
}

public class TechnicianActivityRow
{
    public string UserName { get; set; } = string.Empty;
    public int AccessCount { get; set; }
    public int DistinctClients { get; set; }
    public int DistinctMachines { get; set; }
    public DateTime? LastAccessUtc { get; set; }
}

public class CustomerAccessRow
{
    public string ClientName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string? CredentialType { get; set; }
}

public class MachineAccessRow
{
    public string MachineName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public int Interventions { get; set; }
    public DateTime? LastAccessUtc { get; set; }
    public string? LastTechnician { get; set; }
}

public class UsageRow
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class CredentialUsageReport
{
    public List<UsageRow> MostAccessedCredentialTypes { get; set; } = new();
    public List<UsageRow> MostAccessedCustomers { get; set; } = new();
    public List<UsageRow> MostAccessedMachines { get; set; } = new();
}

public class ReasonAnalysisRow
{
    public AccessReasonCategory Category { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ChangeHistoryRow
{
    public DateTime ModifiedAtUtc { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string? ModifiedBy { get; set; }
    public string? Reason { get; set; }
}

public class ReportListStats
{
    public int TotalLogs { get; set; }
    public int LogsThisMonth { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueMachines { get; set; }
}

public interface IReportService
{
    Task<ReportListStats> GetListStatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CredentialAccessRow>> CredentialAccessAsync(ReportFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<TechnicianActivityRow>> TechnicianActivityAsync(ReportFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerAccessRow>> CustomerAccessAsync(ReportFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<MachineAccessRow>> MachineAccessAsync(ReportFilter filter, CancellationToken ct = default);
    Task<CredentialUsageReport> CredentialUsageAsync(ReportFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<ReasonAnalysisRow>> ReasonAnalysisAsync(ReportFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<ChangeHistoryRow>> ChangeHistoryAsync(ReportFilter filter, CancellationToken ct = default);
}
