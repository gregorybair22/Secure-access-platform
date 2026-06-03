using Microsoft.EntityFrameworkCore;
using SecureAccess.Application.Abstractions;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Templates;
using SecureAccess.Domain.Entities;
using SecureAccess.Domain.Enums;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

public class TemplateService : ITemplateService
{
    private readonly AppDbContext _db;
    private readonly CredentialDataProtector _protector;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _current;

    public TemplateService(AppDbContext db, CredentialDataProtector protector, IAuditService audit, ICurrentUserService current)
    {
        _db = db;
        _protector = protector;
        _audit = audit;
        _current = current;
    }

    public async Task<IReadOnlyList<TemplateDto>> GetTemplatesAsync(CancellationToken ct = default)
    {
        var list = await _db.CredentialTemplates.AsNoTracking().Include(t => t.CredentialType).OrderBy(t => t.Name).ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<TemplateDto?> GetTemplateAsync(int id, CancellationToken ct = default)
    {
        var t = await _db.CredentialTemplates.AsNoTracking().Include(x => x.CredentialType).FirstOrDefaultAsync(x => x.Id == id, ct);
        return t is null ? null : Map(t);
    }

    public async Task<Result<int>> CreateAsync(SaveTemplateRequest request, CancellationToken ct = default)
    {
        var type = await _db.CredentialTypes.FindAsync(new object[] { request.CredentialTypeId }, ct);
        if (type is null) return Result<int>.Fail("Credential type not found.");
        if (string.IsNullOrWhiteSpace(request.Name)) return Result<int>.Fail("Template name is required.");

        var fields = CredentialDataProtector.ParseFields(type.FieldsJson);
        var (plain, secret) = _protector.Protect(fields, request.Values);

        var template = new CredentialTemplate
        {
            CredentialTypeId = request.CredentialTypeId,
            Name = request.Name.Trim(),
            Description = request.Description,
            IsActive = request.IsActive,
            PlainData = plain,
            SecretData = secret,
            CreatedByUserId = _current.UserId
        };
        _db.CredentialTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        return Result<int>.Success(template.Id);
    }

    public async Task<Result> UpdateAsync(int id, SaveTemplateRequest request, CancellationToken ct = default)
    {
        var template = await _db.CredentialTemplates.Include(t => t.CredentialType).FirstOrDefaultAsync(t => t.Id == id, ct);
        if (template is null) return Result.Fail("Template not found.");

        var fields = CredentialDataProtector.ParseFields(template.CredentialType.FieldsJson);
        var (plain, secret) = _protector.Protect(fields, request.Values);
        template.Name = request.Name.Trim();
        template.Description = request.Description;
        template.IsActive = request.IsActive;
        template.PlainData = plain;
        template.SecretData = secret;
        template.UpdatedAtUtc = DateTime.UtcNow;
        template.UpdatedByUserId = _current.UserId;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var template = await _db.CredentialTemplates.FindAsync(new object[] { id }, ct);
        if (template is null) return Result.Fail("Template not found.");
        _db.CredentialTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<int>> ApplyToMachinesAsync(ApplyTemplateRequest request, CancellationToken ct = default)
    {
        var template = await _db.CredentialTemplates.Include(t => t.CredentialType).FirstOrDefaultAsync(t => t.Id == request.TemplateId, ct);
        if (template is null) return Result<int>.Fail("Template not found.");
        if (request.MachineIds.Count == 0) return Result<int>.Fail("Select at least one target machine.");

        var fields = CredentialDataProtector.ParseFields(template.CredentialType.FieldsJson);
        var templateValues = _protector.Reveal(template.PlainData, template.SecretData);

        int affected = 0;
        foreach (var machineId in request.MachineIds.Distinct())
        {
            var existing = await _db.Credentials.FirstOrDefaultAsync(
                c => c.Scope == CredentialScope.Machine && c.MachineId == machineId && c.CredentialTypeId == template.CredentialTypeId, ct);

            if (existing is null)
            {
                var (plain, secret) = _protector.Protect(fields, templateValues);
                _db.Credentials.Add(new Credential
                {
                    CredentialTypeId = template.CredentialTypeId,
                    Scope = CredentialScope.Machine,
                    MachineId = machineId,
                    Label = template.Name,
                    PlainData = plain,
                    SecretData = secret,
                    IsActive = true,
                    CreatedByUserId = _current.UserId
                });
                affected++;
            }
            else if (request.Overwrite || request.FillEmptyOnly)
            {
                var current = _protector.Reveal(existing.PlainData, existing.SecretData);
                foreach (var f in fields)
                {
                    templateValues.TryGetValue(f.Key, out var tVal);
                    if (string.IsNullOrEmpty(tVal)) continue;
                    var hasValue = current.TryGetValue(f.Key, out var cVal) && !string.IsNullOrEmpty(cVal);
                    if (request.FillEmptyOnly && hasValue) continue;
                    current[f.Key] = tVal;
                }
                var (plain, secret) = _protector.Protect(fields, current);
                existing.PlainData = plain;
                existing.SecretData = secret;
                existing.UpdatedAtUtc = DateTime.UtcNow;
                existing.UpdatedByUserId = _current.UserId;
                affected++;
            }
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditAction.CopyOperation,
            Details = $"Applied template '{template.Name}' to {affected} machine(s)"
        }, ct);
        return Result<int>.Success(affected);
    }

    private TemplateDto Map(CredentialTemplate t) => new()
    {
        Id = t.Id,
        CredentialTypeId = t.CredentialTypeId,
        CredentialTypeName = t.CredentialType?.Name ?? "",
        Name = t.Name,
        Description = t.Description,
        IsActive = t.IsActive,
        PlainValues = _protector.ReadPlain(t.PlainData)
    };
}
