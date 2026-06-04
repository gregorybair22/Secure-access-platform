using Microsoft.EntityFrameworkCore;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Credentials;
using SecureAccess.Domain.Entities;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

public class CredentialTypeService : ICredentialTypeService
{
    private readonly AppDbContext _db;

    public CredentialTypeService(AppDbContext db) => _db = db;

    public async Task<CredentialTypeListStats> GetListStatsAsync(CancellationToken ct = default)
    {
        var query = _db.CredentialTypes.AsNoTracking();
        var total = await query.CountAsync(ct);
        var system = await query.CountAsync(t => t.IsSystem, ct);
        var active = await query.CountAsync(t => t.IsActive, ct);
        var custom = await query.CountAsync(t => !t.IsSystem, ct);

        return new CredentialTypeListStats
        {
            TotalTypes = total,
            SystemTypes = system,
            ActiveTypes = active,
            CustomTypes = custom
        };
    }

    public async Task<IReadOnlyList<CredentialTypeDto>> GetTypesAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.CredentialTypes.AsNoTracking().AsQueryable();
        if (!includeInactive) query = query.Where(t => t.IsActive);
        var types = await query.OrderBy(t => t.Name).ToListAsync(ct);
        return types.Select(Map).ToList();
    }

    public async Task<CredentialTypeDto?> GetTypeAsync(int id, CancellationToken ct = default)
    {
        var t = await _db.CredentialTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return t is null ? null : Map(t);
    }

    public async Task<Result<int>> CreateAsync(SaveCredentialTypeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Result<int>.Fail("Type name is required.");
        if (await _db.CredentialTypes.AnyAsync(t => t.Name == request.Name, ct))
            return Result<int>.Fail("A credential type with this name already exists.");

        var type = new CredentialType
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Icon = request.Icon,
            IsActive = request.IsActive,
            IsSystem = false,
            FieldsJson = Json.Serialize(ToDefinitions(request.Fields))
        };
        _db.CredentialTypes.Add(type);
        await _db.SaveChangesAsync(ct);
        return Result<int>.Success(type.Id);
    }

    public async Task<Result> UpdateAsync(int id, SaveCredentialTypeRequest request, CancellationToken ct = default)
    {
        var type = await _db.CredentialTypes.FindAsync(new object[] { id }, ct);
        if (type is null) return Result.Fail("Credential type not found.");

        if (type.Name != request.Name && await _db.CredentialTypes.AnyAsync(t => t.Name == request.Name && t.Id != id, ct))
            return Result.Fail("A credential type with this name already exists.");

        type.Name = request.Name.Trim();
        type.Description = request.Description;
        type.Icon = request.Icon;
        type.IsActive = request.IsActive;
        // System types keep their core fields but may be extended; allow editing fields for all.
        type.FieldsJson = Json.Serialize(ToDefinitions(request.Fields));
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var type = await _db.CredentialTypes.FindAsync(new object[] { id }, ct);
        if (type is null) return Result.Fail("Credential type not found.");
        if (type.IsSystem) return Result.Fail("System credential types cannot be deleted.");
        if (await _db.Credentials.AnyAsync(c => c.CredentialTypeId == id, ct))
            return Result.Fail("Cannot delete a type that is in use. Deactivate it instead.");

        _db.CredentialTypes.Remove(type);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static List<CredentialFieldDefinition> ToDefinitions(IEnumerable<CredentialFieldDto> fields) =>
        fields.Select(f => new CredentialFieldDefinition
        {
            Key = f.Key,
            Label = f.Label,
            Type = f.Type,
            IsSecret = f.IsSecret,
            IsRequired = f.IsRequired,
            IsSearchable = f.IsSearchable,
            Order = f.Order
        }).ToList();

    private static CredentialTypeDto Map(CredentialType t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Description = t.Description,
        Icon = t.Icon,
        IsSystem = t.IsSystem,
        IsActive = t.IsActive,
        Fields = CredentialDataProtector.ParseFields(t.FieldsJson)
            .OrderBy(f => f.Order)
            .Select(f => new CredentialFieldDto
            {
                Key = f.Key,
                Label = f.Label,
                Type = f.Type,
                IsSecret = f.IsSecret,
                IsRequired = f.IsRequired,
                IsSearchable = f.IsSearchable,
                Order = f.Order
            }).ToList()
    };
}
