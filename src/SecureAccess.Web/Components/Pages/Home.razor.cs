using Microsoft.AspNetCore.Components;
using SecureAccess.Application.Features.Dashboard;
using SecureAccess.Application.Features.Reports;

namespace SecureAccess.Web.Components.Pages;

public partial class Home
{
    [Inject] private IDashboardService Dashboard { get; set; } = default!;

    private DashboardData? _data;
    private bool _loading = true;
    private DateTime _rangeFrom = DateTime.Today.AddMonths(-1);
    private DateTime _rangeTo = DateTime.Today;

    private IReadOnlyList<StatCardModel> StatCards => _data is null ? Array.Empty<StatCardModel>() : new StatCardModel[]
    {
        new("Accesses Today", _data.AccessesToday, "purple", Compare(_data.AccessesToday, _data.AccessesYesterday, "vs yesterday")),
        new("This Week", _data.AccessesThisWeek, "green", Compare(_data.AccessesThisWeek, _data.AccessesPreviousWeek, "vs last week")),
        new("This Month", _data.AccessesThisMonth, "orange", Compare(_data.AccessesThisMonth, _data.AccessesPreviousMonth, "vs last month")),
        new("Active Sessions", _data.ActiveSessions, "blue", Compare(_data.ActiveSessions, _data.SessionsLoggedInYesterday, "vs yesterday")),
        new("Clients", _data.TotalClients, "purple", CompareAdded(_data.ClientsAddedInRange, _data.ClientsAddedPreviousRange, "vs previous period")),
        new("Machines", _data.TotalMachines, "green", CompareAdded(_data.MachinesAddedInRange, _data.MachinesAddedPreviousRange, "vs previous period")),
        new("Credentials", _data.TotalCredentials, "orange", CompareAdded(_data.CredentialsAddedInRange, _data.CredentialsAddedPreviousRange, "vs previous period")),
        new("Failed Logins (30d)", _data.FailedLoginAttempts, "red", Compare(_data.FailedLoginAttempts, _data.FailedLoginAttemptsPrevious30, "vs previous 30 days", upIsGood: false), Warn: _data.FailedLoginAttempts > 0)
    };

    private IReadOnlyList<RankingCardModel> RankingCards => _data is null ? Array.Empty<RankingCardModel>() : new RankingCardModel[]
    {
        new("Most Active Technicians", _data.MostActiveTechnicians, "No technician activity found for the selected period."),
        new("Most Accessed Customers", _data.MostAccessedCustomers, "No customer access found for the selected period."),
        new("Most Accessed Machines", _data.MostAccessedMachines, "No machine access found for the selected period.")
    };

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true;
        try
        {
            var from = _rangeFrom.ToUniversalTime();
            var to = _rangeTo.ToUniversalTime();
            if (from > to)
                (from, to) = (to, from);

            _data = await Dashboard.GetAsync(from, to);
        }
        finally
        {
            _loading = false;
        }
    }

    private static string FormatBulkUpdate(DateTime? utc) =>
        utc?.ToLocalTime().ToString("MMM d, yyyy") ?? "—";

    private static DeltaInfo Compare(int current, int previous, string label, bool upIsGood = true)
    {
        if (previous == 0)
        {
            if (current == 0)
                return new DeltaInfo("0% " + label, IsUp: false, IsGood: true, Neutral: true);
            return new DeltaInfo($"↑ 100% {label}", IsUp: true, IsGood: upIsGood);
        }

        var pct = Math.Round((current - previous) * 100m / previous, 0);
        if (pct == 0)
            return new DeltaInfo($"0% {label}", IsUp: false, IsGood: true, Neutral: true);

        var up = pct > 0;
        var arrow = up ? "↑" : "↓";
        var good = up ? upIsGood : !upIsGood;
        return new DeltaInfo($"{arrow} {Math.Abs(pct)}% {label}", IsUp: up, IsGood: good);
    }

    private static DeltaInfo CompareAdded(int current, int previous, string label) =>
        Compare(current, previous, label);

    private static string DeltaClass(DeltaInfo d)
    {
        if (d.Neutral) return "neutral";
        return d.IsGood ? "good" : "bad";
    }

    private record StatCardModel(string Label, int Value, string Tone, DeltaInfo Delta, bool Warn = false);
    private record RankingCardModel(string Title, IReadOnlyList<UsageRow> Items, string EmptyMessage);
    private record DeltaInfo(string Text, bool IsUp, bool IsGood, bool Neutral = false);

    private static RenderFragment StatIcon(string tone) => tone switch
    {
        "green" => builder =>
        {
            builder.AddMarkupContent(0, "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><polyline points=\"23 6 13.5 15.5 8.5 10.5 1 18\"/><polyline points=\"17 6 23 6 23 12\"/></svg>");
        },
        "orange" => builder =>
        {
            builder.AddMarkupContent(0, "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\"/><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/></svg>");
        },
        "blue" => builder =>
        {
            builder.AddMarkupContent(0, "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/></svg>");
        },
        "red" => builder =>
        {
            builder.AddMarkupContent(0, "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z\"/><line x1=\"12\" y1=\"9\" x2=\"12\" y2=\"13\"/><line x1=\"12\" y1=\"17\" x2=\"12.01\" y2=\"17\"/></svg>");
        },
        _ => builder =>
        {
            builder.AddMarkupContent(0, "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><polyline points=\"22 12 18 12 15 21 9 3 6 12 2 12\"/></svg>");
        }
    };
}
