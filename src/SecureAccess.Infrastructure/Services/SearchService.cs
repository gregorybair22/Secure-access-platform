using Microsoft.EntityFrameworkCore;
using SecureAccess.Application.Abstractions;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Search;
using SecureAccess.Domain.Enums;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;

    public SearchService(AppDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, int max = 50, CancellationToken ct = default)
    {
        var hits = new List<SearchHit>();
        if (string.IsNullOrWhiteSpace(query)) return hits;
        var s = query.Trim();
        var seeRestricted = _current.IsInRole(RoleNames.Administrator) || _current.HasPermission(Permissions.AuditView);

        var clients = await _db.Clients.AsNoTracking()
            .Where(c => seeRestricted || !c.IsRestricted)
            .Where(c => c.Name.Contains(s) || c.CustomerCode.Contains(s)
                || (c.Notes != null && c.Notes.Contains(s))
                || (c.ContactPerson != null && c.ContactPerson.Contains(s)))
            .Take(max)
            .Select(c => new SearchHit
            {
                Kind = SearchResultKind.Client,
                Id = c.Id,
                Title = c.Name,
                Subtitle = c.CustomerCode,
                ClientId = c.Id,
                MatchedOn = c.CustomerCode.Contains(s) ? "Customer Code" : "Customer Name"
            }).ToListAsync(ct);
        hits.AddRange(clients);

        var machines = await _db.Machines.AsNoTracking().Include(m => m.Client)
            .Where(m => seeRestricted || !m.Client.IsRestricted)
            .Where(m => m.Name.Contains(s)
                || (m.SerialNumber != null && m.SerialNumber.Contains(s))
                || (m.Model != null && m.Model.Contains(s))
                || (m.Notes != null && m.Notes.Contains(s)))
            .Take(max)
            .Select(m => new SearchHit
            {
                Kind = SearchResultKind.Machine,
                Id = m.Id,
                Title = m.Name,
                Subtitle = m.Client.Name,
                ClientId = m.ClientId,
                MachineId = m.Id,
                MatchedOn = (m.SerialNumber != null && m.SerialNumber.Contains(s)) ? "Serial Number" : "Machine Name"
            }).ToListAsync(ct);
        hits.AddRange(machines);

        // Credentials: only the non-secret PlainData JSON is searched (RustDesk ID,
        // AnyDesk ID, VPN server, username, IP address, notes). Secrets are excluded.
        var creds = await _db.Credentials.AsNoTracking()
            .Include(c => c.CredentialType).Include(c => c.Client).Include(c => c.Machine)
            .Where(c => c.PlainData.Contains(s) || c.Label.Contains(s) || (c.Notes != null && c.Notes.Contains(s)))
            .Take(max)
            .Select(c => new SearchHit
            {
                Kind = SearchResultKind.Credential,
                Id = c.Id,
                Title = c.Label,
                Subtitle = c.CredentialType.Name + (c.Machine != null ? " @ " + c.Machine.Name : c.Client != null ? " @ " + c.Client.Name : " (Global)"),
                ClientId = c.ClientId,
                MachineId = c.MachineId,
                MatchedOn = "Credential Field"
            }).ToListAsync(ct);
        hits.AddRange(creds);

        return hits.Take(max).ToList();
    }
}
