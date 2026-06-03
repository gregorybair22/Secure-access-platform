using Microsoft.EntityFrameworkCore;
using SecureAccess.Application.Abstractions;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Credentials;
using SecureAccess.Domain.Entities;
using SecureAccess.Domain.Enums;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

public class CredentialService : ICredentialService
{
    private readonly AppDbContext _db;
    private readonly CredentialDataProtector _protector;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _current;

    public CredentialService(AppDbContext db, CredentialDataProtector protector, IAuditService audit, ICurrentUserService current)
    {
        _db = db;
        _protector = protector;
        _audit = audit;
        _current = current;
    }

    public async Task<IReadOnlyList<CredentialDto>> GetForMachineAsync(int machineId, CancellationToken ct = default)
        => await MapList(_db.Credentials.AsNoTracking().Where(c => c.MachineId == machineId), ct);

    public async Task<IReadOnlyList<CredentialDto>> GetForClientAsync(int clientId, CancellationToken ct = default)
        => await MapList(_db.Credentials.AsNoTracking().Where(c => c.Scope == CredentialScope.Client && c.ClientId == clientId), ct);

    public async Task<IReadOnlyList<CredentialDto>> GetGlobalAsync(CancellationToken ct = default)
        => await MapList(_db.Credentials.AsNoTracking().Where(c => c.Scope == CredentialScope.Global), ct);

    public async Task<CredentialDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var c = await _db.Credentials.AsNoTracking()
            .Include(x => x.CredentialType)
            .Include(x => x.Client)
            .Include(x => x.Machine)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? null : Map(c);
    }

    public async Task<Result<int>> CreateAsync(SaveCredentialRequest request, CancellationToken ct = default)
    {
        var type = await _db.CredentialTypes.FindAsync(new object[] { request.CredentialTypeId }, ct);
        if (type is null) return Result<int>.Fail("Credential type not found.");

        var validation = ValidateScope(request);
        if (!validation.Succeeded) return Result<int>.Fail(validation.Error!);

        var fields = CredentialDataProtector.ParseFields(type.FieldsJson);
        var (plain, secret) = _protector.Protect(fields, request.Values);

        var credential = new Credential
        {
            CredentialTypeId = request.CredentialTypeId,
            Scope = request.Scope,
            ClientId = request.Scope == CredentialScope.Global ? null : request.ClientId,
            MachineId = request.Scope == CredentialScope.Machine ? request.MachineId : null,
            Label = string.IsNullOrWhiteSpace(request.Label) ? type.Name : request.Label.Trim(),
            PlainData = plain,
            SecretData = secret,
            Notes = request.Notes,
            IsActive = request.IsActive,
            CreatedByUserId = _current.UserId
        };
        _db.Credentials.Add(credential);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditAction.CredentialCreate,
            CredentialId = credential.Id,
            CredentialType = type.Name,
            ClientId = credential.ClientId,
            MachineId = credential.MachineId,
            Details = $"Created {type.Name} credential '{credential.Label}'"
        }, ct);

        return Result<int>.Success(credential.Id);
    }

    public async Task<Result> UpdateAsync(int id, SaveCredentialRequest request, CancellationToken ct = default)
    {
        var credential = await _db.Credentials.Include(c => c.CredentialType).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (credential is null) return Result.Fail("Credential not found.");

        var fields = CredentialDataProtector.ParseFields(credential.CredentialType.FieldsJson);

        // Change history for non-secret fields (secret changes recorded as masked).
        var oldValues = _protector.Reveal(credential.PlainData, credential.SecretData);
        foreach (var f in fields)
        {
            request.Values.TryGetValue(f.Key, out var newVal);
            oldValues.TryGetValue(f.Key, out var oldVal);
            if ((oldVal ?? "") != (newVal ?? ""))
            {
                await _audit.LogChangeAsync(nameof(Credential), id, f.Label,
                    f.IsSecret ? "********" : oldVal,
                    f.IsSecret ? "********" : newVal,
                    request.ChangeReason, ct);
            }
        }

        var (plain, secret) = _protector.Protect(fields, request.Values);
        credential.Label = string.IsNullOrWhiteSpace(request.Label) ? credential.Label : request.Label.Trim();
        credential.PlainData = plain;
        credential.SecretData = secret;
        credential.Notes = request.Notes;
        credential.IsActive = request.IsActive;
        credential.UpdatedAtUtc = DateTime.UtcNow;
        credential.UpdatedByUserId = _current.UserId;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditAction.CredentialModify,
            CredentialId = credential.Id,
            CredentialType = credential.CredentialType.Name,
            ClientId = credential.ClientId,
            MachineId = credential.MachineId,
            Reason = request.ChangeReason,
            Details = $"Modified credential '{credential.Label}'"
        }, ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var credential = await _db.Credentials.Include(c => c.CredentialType).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (credential is null) return Result.Fail("Credential not found.");

        _db.Credentials.Remove(credential);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditAction.CredentialDelete,
            CredentialId = id,
            CredentialType = credential.CredentialType.Name,
            Details = $"Deleted credential '{credential.Label}'"
        }, ct);
        return Result.Success();
    }

    private static Result ValidateScope(SaveCredentialRequest request)
    {
        switch (request.Scope)
        {
            case CredentialScope.Client when request.ClientId is null:
                return Result.Fail("Client-level credentials require a client.");
            case CredentialScope.Machine when request.MachineId is null:
                return Result.Fail("Machine-level credentials require a machine.");
            default:
                return Result.Success();
        }
    }

    private async Task<IReadOnlyList<CredentialDto>> MapList(IQueryable<Credential> query, CancellationToken ct)
    {
        var list = await query
            .Include(c => c.CredentialType)
            .Include(c => c.Client)
            .Include(c => c.Machine)
            .OrderBy(c => c.Label)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    private CredentialDto Map(Credential c) => new()
    {
        Id = c.Id,
        CredentialTypeId = c.CredentialTypeId,
        CredentialTypeName = c.CredentialType?.Name ?? "",
        Scope = c.Scope,
        ClientId = c.ClientId,
        ClientName = c.Client?.Name,
        MachineId = c.MachineId,
        MachineName = c.Machine?.Name,
        Label = c.Label,
        Notes = c.Notes,
        IsActive = c.IsActive,
        AttachmentCount = c.Attachments?.Count ?? 0,
        PlainValues = _protector.ReadPlain(c.PlainData)
    };
}
