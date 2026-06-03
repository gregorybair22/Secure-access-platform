using Microsoft.EntityFrameworkCore;
using SecureAccess.Application.Abstractions;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Machines;
using SecureAccess.Domain.Entities;
using SecureAccess.Domain.Enums;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

public class MachineService : IMachineService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _current;

    public MachineService(AppDbContext db, IAuditService audit, ICurrentUserService current)
    {
        _db = db;
        _audit = audit;
        _current = current;
    }

    public async Task<PagedResult<MachineDto>> GetMachinesAsync(int? clientId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.Machines.AsNoTracking().Include(m => m.Client).AsQueryable();

        if (!_current.IsInRole(RoleNames.Administrator) && !_current.HasPermission(Permissions.AuditView))
            query = query.Where(m => !m.Client.IsRestricted);

        if (clientId.HasValue) query = query.Where(m => m.ClientId == clientId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(m => m.Name.Contains(s)
                || (m.SerialNumber != null && m.SerialNumber.Contains(s))
                || (m.Model != null && m.Model.Contains(s)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(m => m.Client.Name).ThenBy(m => m.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new MachineDto
            {
                Id = m.Id,
                ClientId = m.ClientId,
                ClientName = m.Client.Name,
                Name = m.Name,
                SerialNumber = m.SerialNumber,
                Model = m.Model,
                Location = m.Location,
                Status = m.Status,
                InstallationDate = m.InstallationDate,
                Notes = m.Notes,
                CredentialCount = m.Credentials.Count
            })
            .ToListAsync(ct);

        return new PagedResult<MachineDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<MachineDto?> GetMachineAsync(int id, CancellationToken ct = default)
    {
        return await _db.Machines.AsNoTracking().Where(m => m.Id == id)
            .Select(m => new MachineDto
            {
                Id = m.Id,
                ClientId = m.ClientId,
                ClientName = m.Client.Name,
                Name = m.Name,
                SerialNumber = m.SerialNumber,
                Model = m.Model,
                Location = m.Location,
                Status = m.Status,
                InstallationDate = m.InstallationDate,
                Notes = m.Notes,
                CredentialCount = m.Credentials.Count
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Result<int>> CreateAsync(SaveMachineRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Result<int>.Fail("Machine name is required.");
        var client = await _db.Clients.FindAsync(new object[] { request.ClientId }, ct);
        if (client is null) return Result<int>.Fail("Client not found.");

        var machine = new Machine
        {
            ClientId = request.ClientId,
            Name = request.Name.Trim(),
            SerialNumber = request.SerialNumber,
            Model = request.Model,
            Location = request.Location,
            Status = request.Status,
            InstallationDate = request.InstallationDate,
            Notes = request.Notes,
            CreatedByUserId = _current.UserId
        };
        _db.Machines.Add(machine);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditAction.MachineCreate,
            ClientId = client.Id,
            ClientName = client.Name,
            MachineId = machine.Id,
            MachineName = machine.Name,
            Details = "Created machine"
        }, ct);
        return Result<int>.Success(machine.Id);
    }

    public async Task<Result> UpdateAsync(int id, SaveMachineRequest request, CancellationToken ct = default)
    {
        var machine = await _db.Machines.Include(m => m.Client).FirstOrDefaultAsync(m => m.Id == id, ct);
        if (machine is null) return Result.Fail("Machine not found.");

        if (machine.Name != request.Name)
            await _audit.LogChangeAsync(nameof(Machine), id, nameof(machine.Name), machine.Name, request.Name, ct: ct);
        if (machine.Status != request.Status)
            await _audit.LogChangeAsync(nameof(Machine), id, nameof(machine.Status), machine.Status.ToString(), request.Status.ToString(), ct: ct);

        machine.ClientId = request.ClientId;
        machine.Name = request.Name.Trim();
        machine.SerialNumber = request.SerialNumber;
        machine.Model = request.Model;
        machine.Location = request.Location;
        machine.Status = request.Status;
        machine.InstallationDate = request.InstallationDate;
        machine.Notes = request.Notes;
        machine.UpdatedAtUtc = DateTime.UtcNow;
        machine.UpdatedByUserId = _current.UserId;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditAction.MachineModify,
            ClientId = machine.ClientId,
            ClientName = machine.Client.Name,
            MachineId = machine.Id,
            MachineName = machine.Name,
            Details = "Updated machine"
        }, ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var machine = await _db.Machines.FindAsync(new object[] { id }, ct);
        if (machine is null) return Result.Fail("Machine not found.");

        _db.Machines.Remove(machine);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditAction.MachineDelete,
            MachineId = id,
            MachineName = machine.Name,
            Details = "Deleted machine"
        }, ct);
        return Result.Success();
    }
}
