using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SecureAccess.Application.Features.Reports;

namespace SecureAccess.Web.Components.Pages;

public partial class Reports
{
    [Inject] private IReportService ReportSvc { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private ReportListStats _stats = new();
    private string _report = "credential-access";
    private DateTime? _from = DateTime.Today.AddMonths(-1);
    private DateTime? _to = DateTime.Today;
    private List<string>? _headers;
    private List<List<string>> _rows = new();
    private List<List<string>> _sortedRows = new();
    private string _search = "";
    private int _page = 1;
    private int _pageSize = 10;
    private int? _sortColumn;
    private bool _sortAsc = true;
    private bool _hasRun;
    private bool _busy;
    private string? _error;

    private string MonthLabel => DateTime.Today.ToString("MMMM yyyy");
    private bool CanExport => _hasRun && _headers is not null && GetFilteredRows().Any();
    private string FooterSummary => _hasRun && FilteredCount > 0
        ? $"Showing {RangeStart} to {RangeEnd} of {FilteredCount} results"
        : "Showing 0 results";

    private IEnumerable<List<string>> GetFilteredRows()
    {
        var rows = _sortedRows.AsEnumerable();
        if (string.IsNullOrWhiteSpace(_search))
            return rows;

        var term = _search.Trim();
        return rows.Where(r => r.Any(c => c.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    private int FilteredCount => GetFilteredRows().Count();
    private int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)_pageSize));
    private List<List<string>> PagedRows =>
        GetFilteredRows().Skip((_page - 1) * _pageSize).Take(_pageSize).ToList();
    private int RangeStart => FilteredCount == 0 ? 0 : (_page - 1) * _pageSize + 1;
    private int RangeEnd => Math.Min(_page * _pageSize, FilteredCount);

    protected override async Task OnInitializedAsync()
    {
        _stats = await ReportSvc.GetListStatsAsync();
    }

    private ReportFilter BuildFilter()
    {
        return new ReportFilter
        {
            FromUtc = _from?.ToUniversalTime(),
            ToUtc = _to?.AddDays(1).ToUniversalTime()
        };
    }

    private async Task Run()
    {
        _error = null;
        _busy = true;
        _hasRun = true;
        _page = 1;
        _sortColumn = null;
        _sortAsc = true;

        try
        {
            _rows = new List<List<string>>();
            var f = BuildFilter();

            switch (_report)
            {
                case "credential-access":
                    _headers = new List<string> { "Date", "User", "Client", "Machine", "Type", "Reason", "Ticket", "IP" };
                    foreach (var r in await ReportSvc.CredentialAccessAsync(f))
                        _rows.Add(new List<string> { L(r.TimestampUtc), r.UserName, r.ClientName ?? "", r.MachineName ?? "", r.CredentialType ?? "", r.Reason ?? "", r.InternalTicket ?? "", r.IpAddress ?? "" });
                    break;
                case "technician-activity":
                    _headers = new List<string> { "Technician", "Accesses", "Clients", "Machines", "Last Access" };
                    foreach (var r in await ReportSvc.TechnicianActivityAsync(f))
                        _rows.Add(new List<string> { r.UserName, r.AccessCount.ToString(), r.DistinctClients.ToString(), r.DistinctMachines.ToString(), L(r.LastAccessUtc) });
                    break;
                case "customer-access":
                    _headers = new List<string> { "Client", "User", "Reason", "Date", "Type" };
                    foreach (var r in await ReportSvc.CustomerAccessAsync(f))
                        _rows.Add(new List<string> { r.ClientName, r.UserName, r.Reason ?? "", L(r.TimestampUtc), r.CredentialType ?? "" });
                    break;
                case "machine-access":
                    _headers = new List<string> { "Machine", "Client", "Interventions", "Last Access", "Last Technician" };
                    foreach (var r in await ReportSvc.MachineAccessAsync(f))
                        _rows.Add(new List<string> { r.MachineName, r.ClientName, r.Interventions.ToString(), L(r.LastAccessUtc), r.LastTechnician ?? "" });
                    break;
                case "credential-usage":
                    _headers = new List<string> { "Category", "Name", "Count" };
                    var usage = await ReportSvc.CredentialUsageAsync(f);
                    foreach (var r in usage.MostAccessedCredentialTypes) _rows.Add(new List<string> { "Credential Type", r.Name, r.Count.ToString() });
                    foreach (var r in usage.MostAccessedCustomers) _rows.Add(new List<string> { "Customer", r.Name, r.Count.ToString() });
                    foreach (var r in usage.MostAccessedMachines) _rows.Add(new List<string> { "Machine", r.Name, r.Count.ToString() });
                    break;
                case "reason-analysis":
                    _headers = new List<string> { "Reason Category", "Count" };
                    foreach (var r in await ReportSvc.ReasonAnalysisAsync(f))
                        _rows.Add(new List<string> { r.CategoryName, r.Count.ToString() });
                    break;
                case "change-history":
                    _headers = new List<string> { "Date", "Entity", "Field", "Previous", "New", "By", "Reason" };
                    foreach (var r in await ReportSvc.ChangeHistoryAsync(f))
                        _rows.Add(new List<string> { L(r.ModifiedAtUtc), $"{r.EntityType}#{r.EntityId}", r.FieldName, r.PreviousValue ?? "", r.NewValue ?? "", r.ModifiedBy ?? "", r.Reason ?? "" });
                    break;
            }

            _sortedRows = _rows.ToList();
            _stats = await ReportSvc.GetListStatsAsync();
        }
        catch (Exception ex)
        {
            _error = $"Failed to run report: {ex.Message}";
            _headers = null;
            _sortedRows = new List<List<string>>();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ExportCsv()
    {
        if (_headers is null || !GetFilteredRows().Any())
            return;

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", _headers.Select(CsvEscape)));
        foreach (var row in GetFilteredRows())
            sb.AppendLine(string.Join(",", row.Select(CsvEscape)));

        var name = $"{_report}-{DateTime.Today:yyyyMMdd}.csv";
        await Js.InvokeVoidAsync("saDownload.textFile", name, sb.ToString());
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private void SortBy(int column)
    {
        if (_sortColumn == column)
            _sortAsc = !_sortAsc;
        else
        {
            _sortColumn = column;
            _sortAsc = true;
        }

        _sortedRows = _sortAsc
            ? _rows.OrderBy(r => Cell(r, column), StringComparer.OrdinalIgnoreCase).ToList()
            : _rows.OrderByDescending(r => Cell(r, column), StringComparer.OrdinalIgnoreCase).ToList();
        _page = 1;
    }

    private static string Cell(List<string> row, int column) =>
        column < row.Count ? row[column] : "";

    private string SortClass(int column) => _sortColumn == column ? "active" : "";

    private void ResetPage() => _page = 1;

    private void OnPageSizeChanged()
    {
        _page = 1;
        if (_page > TotalPages)
            _page = TotalPages;
    }

    private void PrevPage()
    {
        if (_page > 1) _page--;
    }

    private void NextPage()
    {
        if (_page >= TotalPages) return;
        _page++;
    }

    private static string L(DateTime? d) => d?.ToLocalTime().ToString("g") ?? "";
}
