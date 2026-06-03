using Microsoft.EntityFrameworkCore;
using SecureAccess.Application.Abstractions;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Bulk;
using SecureAccess.Domain.Entities;
using SecureAccess.Domain.Enums;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

public class BulkService : IBulkService
{
    private readonly AppDbContext _db;
    private readonly CredentialDataProtector _protector;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _current;

    public BulkService(AppDbContext db, CredentialDataProtector protector, IAuditService audit, ICurrentUserService current)
    {
        _db = db;
        _protector = protector;
        _audit = audit;
        _current = current;
    }

    public async Task<Result<BulkOperationResult>> CopyMachineToMachinesAsync(CopyMachineToMachinesRequest request, CancellationToken ct = default)
    {
        var sourceCreds = await _db.Credentials.Include(c => c.CredentialType)
            .Where(c => c.Scope == CredentialScope.Machine && c.MachineId == request.SourceMachineId).ToListAsync(ct);
        if (sourceCreds.Count == 0) return Result<BulkOperationResult>.Fail("Source machine has no machine-level credentials.");

        var result = new BulkOperationResult();
        foreach (var targetId in request.TargetMachineIds.Distinct().Where(id => id != request.SourceMachineId))
        {
            foreach (var src in sourceCreds)
            {
                var fields = CredentialDataProtector.ParseFields(src.CredentialType.FieldsJson);
                var values = _protector.Reveal(src.PlainData, src.SecretData);
                await UpsertMachineCredential(targetId, src.CredentialTypeId, src.Label, fields, values, request.FillEmptyOnly, request.Overwrite, result, ct);
            }
        }
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditEntry { Action = AuditAction.CopyOperation, MachineId = request.SourceMachineId, Details = $"Copied credentials to {request.TargetMachineIds.Count} machine(s)" }, ct);
        return Result<BulkOperationResult>.Success(result);
    }

    public async Task<Result<BulkOperationResult>> CopyClientToClientsAsync(CopyClientToClientsRequest request, CancellationToken ct = default)
    {
        var sourceCreds = await _db.Credentials.Include(c => c.CredentialType)
            .Where(c => c.Scope == CredentialScope.Client && c.ClientId == request.SourceClientId).ToListAsync(ct);
        if (sourceCreds.Count == 0) return Result<BulkOperationResult>.Fail("Source client has no client-level credentials.");

        var result = new BulkOperationResult();
        foreach (var targetId in request.TargetClientIds.Distinct().Where(id => id != request.SourceClientId))
        {
            foreach (var src in sourceCreds)
            {
                var fields = CredentialDataProtector.ParseFields(src.CredentialType.FieldsJson);
                var values = _protector.Reveal(src.PlainData, src.SecretData);
                await UpsertClientCredential(targetId, src.CredentialTypeId, src.Label, fields, values, request.FillEmptyOnly, request.Overwrite, result, ct);
            }
        }
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditEntry { Action = AuditAction.CopyOperation, ClientId = request.SourceClientId, Details = $"Copied credentials to {request.TargetClientIds.Count} client(s)" }, ct);
        return Result<BulkOperationResult>.Success(result);
    }

    public async Task<Result<BulkOperationResult>> BulkUpdateAsync(BulkUpdateRequest request, CancellationToken ct = default)
    {
        if (request.CredentialTypeId is null) return Result<BulkOperationResult>.Fail("A credential type is required for bulk update.");
        var type = await _db.CredentialTypes.FindAsync(new object[] { request.CredentialTypeId.Value }, ct);
        if (type is null) return Result<BulkOperationResult>.Fail("Credential type not found.");

        var fields = CredentialDataProtector.ParseFields(type.FieldsJson);

        var query = _db.Credentials.Where(c => c.CredentialTypeId == request.CredentialTypeId.Value);
        if (request.MachineIds.Count > 0)
            query = query.Where(c => c.MachineId != null && request.MachineIds.Contains(c.MachineId.Value));
        else if (request.ClientIds.Count > 0)
            query = query.Where(c => c.ClientId != null && request.ClientIds.Contains(c.ClientId.Value));
        else
            return Result<BulkOperationResult>.Fail("Select at least one client or machine.");

        var targets = await query.ToListAsync(ct);
        var result = new BulkOperationResult();

        foreach (var cred in targets)
        {
            var current = _protector.Reveal(cred.PlainData, cred.SecretData);
            foreach (var f in fields)
            {
                if (!request.Values.TryGetValue(f.Key, out var newVal)) continue;
                if (string.IsNullOrEmpty(newVal) && !request.Overwrite) continue;
                var hasValue = current.TryGetValue(f.Key, out var cVal) && !string.IsNullOrEmpty(cVal);
                if (request.FillEmptyOnly && hasValue) continue;
                if (!request.FillEmptyOnly && !request.Overwrite && hasValue) continue;
                current[f.Key] = newVal;
            }
            var (plain, secret) = _protector.Protect(fields, current);
            cred.PlainData = plain;
            cred.SecretData = secret;
            cred.UpdatedAtUtc = DateTime.UtcNow;
            cred.UpdatedByUserId = _current.UserId;
            result.AffectedCredentials++;
            await _audit.LogChangeAsync(nameof(Credential), cred.Id, "BulkUpdate", null, "Updated via bulk operation", request.Reason, ct);
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditEntry { Action = AuditAction.BulkUpdate, Reason = request.Reason, Details = $"Bulk updated {result.AffectedCredentials} credential(s)" }, ct);
        return Result<BulkOperationResult>.Success(result);
    }

    private async Task UpsertMachineCredential(int machineId, int typeId, string label,
        List<CredentialFieldDefinition> fields, Dictionary<string, string?> values,
        bool fillEmptyOnly, bool overwrite, BulkOperationResult result, CancellationToken ct)
    {
        var existing = await _db.Credentials.FirstOrDefaultAsync(
            c => c.Scope == CredentialScope.Machine && c.MachineId == machineId && c.CredentialTypeId == typeId, ct);
        Upsert(existing, () => new Credential { Scope = CredentialScope.Machine, MachineId = machineId, CredentialTypeId = typeId, Label = label, IsActive = true, CreatedByUserId = _current.UserId },
            fields, values, fillEmptyOnly, overwrite, result);
    }

    private async Task UpsertClientCredential(int clientId, int typeId, string label,
        List<CredentialFieldDefinition> fields, Dictionary<string, string?> values,
        bool fillEmptyOnly, bool overwrite, BulkOperationResult result, CancellationToken ct)
    {
        var existing = await _db.Credentials.FirstOrDefaultAsync(
            c => c.Scope == CredentialScope.Client && c.ClientId == clientId && c.CredentialTypeId == typeId, ct);
        Upsert(existing, () => new Credential { Scope = CredentialScope.Client, ClientId = clientId, CredentialTypeId = typeId, Label = label, IsActive = true, CreatedByUserId = _current.UserId },
            fields, values, fillEmptyOnly, overwrite, result);
    }

    private void Upsert(Credential? existing, Func<Credential> factory,
        List<CredentialFieldDefinition> fields, Dictionary<string, string?> sourceValues,
        bool fillEmptyOnly, bool overwrite, BulkOperationResult result)
    {
        if (existing is null)
        {
            var (plain, secret) = _protector.Protect(fields, sourceValues);
            var created = factory();
            created.PlainData = plain;
            created.SecretData = secret;
            _db.Credentials.Add(created);
            result.CreatedCredentials++;
            return;
        }

        if (!overwrite && !fillEmptyOnly) { result.Messages.Add($"Skipped existing credential {existing.Id} (no overwrite)."); return; }

        var current = _protector.Reveal(existing.PlainData, existing.SecretData);
        foreach (var f in fields)
        {
            sourceValues.TryGetValue(f.Key, out var sVal);
            if (string.IsNullOrEmpty(sVal)) continue;
            var hasValue = current.TryGetValue(f.Key, out var cVal) && !string.IsNullOrEmpty(cVal);
            if (fillEmptyOnly && hasValue) continue;
            current[f.Key] = sVal;
        }
        var (p, s) = _protector.Protect(fields, current);
        existing.PlainData = p;
        existing.SecretData = s;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        existing.UpdatedByUserId = _current.UserId;
        result.AffectedCredentials++;
    }
}
