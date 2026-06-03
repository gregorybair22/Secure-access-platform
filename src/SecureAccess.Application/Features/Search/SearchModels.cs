namespace SecureAccess.Application.Features.Search;

public enum SearchResultKind
{
    Client = 1,
    Machine = 2,
    Credential = 3
}

public class SearchHit
{
    public SearchResultKind Kind { get; set; }
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    /// <summary>Which field matched (e.g. "RustDesk ID", "Serial Number").</summary>
    public string? MatchedOn { get; set; }
    public int? ClientId { get; set; }
    public int? MachineId { get; set; }
}

public interface ISearchService
{
    /// <summary>
    /// Global search across customer name/code, machine name/serial, and the
    /// non-secret credential fields (RustDesk ID, AnyDesk ID, VPN server, username,
    /// IP address, notes). Secret values are never searched or returned.
    /// </summary>
    Task<IReadOnlyList<SearchHit>> SearchAsync(string query, int max = 50, CancellationToken ct = default);
}
