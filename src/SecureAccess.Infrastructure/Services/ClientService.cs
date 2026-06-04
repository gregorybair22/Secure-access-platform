using Microsoft.EntityFrameworkCore;
using SecureAccess.Application.Abstractions;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Clients;
using SecureAccess.Domain.Entities;
using SecureAccess.Domain.Enums;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

public class ClientService : IClientService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _current;

    public ClientService(AppDbContext db, IAuditService audit, ICurrentUserService current)
    {
        _db = db;
        _audit = audit;
        _current = current;
    }

    private IQueryable<Client> VisibleClientsQuery()
    {
        var query = _db.Clients.AsNoTracking();
        if (!_current.IsInRole(RoleNames.Administrator) && !_current.HasPermission(Permissions.AuditView))
            query = query.Where(c => !c.IsRestricted);
        return query;
    }

    public async Task<ClientListStats> GetListStatsAsync(CancellationToken ct = default)
    {
        var clients = VisibleClientsQuery();
        var total = await clients.CountAsync(ct);
        var active = await clients.CountAsync(c => c.Status == EntityStatus.Active, ct);
        var machines = await clients.SelectMany(c => c.Machines).CountAsync(ct);
        var activeCredentials = await (
            from cr in _db.Credentials.AsNoTracking()
            join c in clients on cr.ClientId equals c.Id
            where cr.IsActive
            select cr
        ).CountAsync(ct);

        return new ClientListStats
        {
            TotalClients = total,
            ActiveClients = active,
            TotalMachines = machines,
            ActiveCredentials = activeCredentials
        };
    }

    public async Task<PagedResult<ClientDto>> GetClientsAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = VisibleClientsQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(c => c.Name.Contains(s) || c.CustomerCode.Contains(s)
                || (c.ContactPerson != null && c.ContactPerson.Contains(s)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new ClientDto
            {
                Id = c.Id,
                Name = c.Name,
                CustomerCode = c.CustomerCode,
                Address = c.Address,
                ContactPerson = c.ContactPerson,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                Notes = c.Notes,
                Status = c.Status,
                IsRestricted = c.IsRestricted,
                MachineCount = c.Machines.Count
            })
            .ToListAsync(ct);

        return new PagedResult<ClientDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<ClientDto?> GetClientAsync(int id, CancellationToken ct = default)
    {
        return await _db.Clients.AsNoTracking().Where(c => c.Id == id)
            .Select(c => new ClientDto
            {
                Id = c.Id,
                Name = c.Name,
                CustomerCode = c.CustomerCode,
                Address = c.Address,
                ContactPerson = c.ContactPerson,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                Notes = c.Notes,
                Status = c.Status,
                IsRestricted = c.IsRestricted,
                MachineCount = c.Machines.Count
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Result<int>> CreateAsync(SaveClientRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Result<int>.Fail("Client name is required.");
        if (string.IsNullOrWhiteSpace(request.CustomerCode)) return Result<int>.Fail("Customer code is required.");
        if (await _db.Clients.AnyAsync(c => c.CustomerCode == request.CustomerCode, ct))
            return Result<int>.Fail("A client with this customer code already exists.");

        var client = new Client
        {
            Name = request.Name.Trim(),
            CustomerCode = request.CustomerCode.Trim(),
            Address = request.Address,
            ContactPerson = request.ContactPerson,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Notes = request.Notes,
            Status = request.Status,
            IsRestricted = request.IsRestricted,
            CreatedByUserId = _current.UserId
        };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditAction.ClientCreate,
            ClientId = client.Id,
            ClientName = client.Name,
            Details = $"Created client {client.CustomerCode}"
        }, ct);

        return Result<int>.Success(client.Id);
    }

    public async Task<Result> UpdateAsync(int id, SaveClientRequest request, CancellationToken ct = default)
    {
        var client = await _db.Clients.FindAsync(new object[] { id }, ct);
        if (client is null) return Result.Fail("Client not found.");

        if (client.CustomerCode != request.CustomerCode &&
            await _db.Clients.AnyAsync(c => c.CustomerCode == request.CustomerCode && c.Id != id, ct))
            return Result.Fail("A client with this customer code already exists.");

        // Record field-level change history.
        await TrackChange(id, nameof(client.Name), client.Name, request.Name, ct);
        await TrackChange(id, nameof(client.Status), client.Status.ToString(), request.Status.ToString(), ct);

        client.Name = request.Name.Trim();
        client.CustomerCode = request.CustomerCode.Trim();
        client.Address = request.Address;
        client.ContactPerson = request.ContactPerson;
        client.PhoneNumber = request.PhoneNumber;
        client.Email = request.Email;
        client.Notes = request.Notes;
        client.Status = request.Status;
        client.IsRestricted = request.IsRestricted;
        client.UpdatedAtUtc = DateTime.UtcNow;
        client.UpdatedByUserId = _current.UserId;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditAction.ClientModify,
            ClientId = client.Id,
            ClientName = client.Name,
            Details = "Updated client"
        }, ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var client = await _db.Clients.FindAsync(new object[] { id }, ct);
        if (client is null) return Result.Fail("Client not found.");

        // Client-level credentials use Restrict (to avoid multiple cascade paths),
        // so remove them explicitly before deleting the client. Machine-level
        // credentials cascade automatically via the Machine -> Client cascade.
        var clientCredentials = await _db.Credentials
            .Where(c => c.Scope == CredentialScope.Client && c.ClientId == id)
            .ToListAsync(ct);
        if (clientCredentials.Count > 0)
            _db.Credentials.RemoveRange(clientCredentials);

        _db.Clients.Remove(client);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditAction.ClientDelete,
            ClientId = id,
            ClientName = client.Name,
            Details = $"Deleted client {client.CustomerCode}"
        }, ct);
        return Result.Success();
    }

    private async Task TrackChange(int id, string field, string? oldVal, string? newVal, CancellationToken ct)
    {
        if (oldVal != newVal)
            await _audit.LogChangeAsync(nameof(Client), id, field, oldVal, newVal, ct: ct);
    }
}
