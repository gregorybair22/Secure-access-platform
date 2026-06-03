using Microsoft.EntityFrameworkCore;
using SecureAccess.Application.Abstractions;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.AccessRequests;
using SecureAccess.Application.Features.Credentials;
using SecureAccess.Domain.Entities;
using SecureAccess.Domain.Enums;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

public class AccessRequestService : IAccessRequestService
{
    private readonly AppDbContext _db;
    private readonly CredentialDataProtector _protector;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _current;

    public AccessRequestService(AppDbContext db, CredentialDataProtector protector, IAuditService audit, ICurrentUserService current)
    {
        _db = db;
        _protector = protector;
        _audit = audit;
        _current = current;
    }

    public async Task<Result<AccessRequestResult>> RequestCredentialsAsync(CredentialAccessRequest request, CancellationToken ct = default)
    {
        // Mandatory gating: reason and internal ticket must be present BEFORE any disclosure.
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result<AccessRequestResult>.Fail("A reason for access is mandatory.");
        if (string.IsNullOrWhiteSpace(request.InternalTicket))
            return Result<AccessRequestResult>.Fail("An internal support ticket number is mandatory.");

        var machine = await _db.Machines.Include(m => m.Client).FirstOrDefaultAsync(m => m.Id == request.MachineId, ct);
        if (machine is null) return Result<AccessRequestResult>.Fail("Machine not found.");

        // Technicians may not access restricted clients.
        if (machine.Client.IsRestricted && !_current.IsInRole(RoleNames.Administrator))
        {
            await _audit.LogAsync(new AuditEntry
            {
                Action = AuditAction.UnauthorizedAccess,
                Result = AuditResult.Denied,
                ClientId = machine.ClientId,
                ClientName = machine.Client.Name,
                MachineId = machine.Id,
                MachineName = machine.Name,
                Reason = request.Reason,
                InternalTicket = request.InternalTicket,
                Details = "Attempted access to restricted client"
            }, ct);
            return Result<AccessRequestResult>.Fail("You are not authorized to access this client.");
        }

        // Persist the justified request first.
        var accessRequest = new AccessRequest
        {
            UserId = _current.UserId ?? 0,
            ClientId = machine.ClientId,
            MachineId = machine.Id,
            ReasonCategory = request.ReasonCategory,
            Reason = request.Reason.Trim(),
            InternalTicket = request.InternalTicket.Trim(),
            CustomerTicket = string.IsNullOrWhiteSpace(request.CustomerTicket) ? null : request.CustomerTicket.Trim(),
            IpAddress = _current.IpAddress,
            SessionId = _current.SessionId,
            RequestedAtUtc = DateTime.UtcNow
        };
        _db.AccessRequests.Add(accessRequest);
        await _db.SaveChangesAsync(ct);

        // Resolve applicable credentials with machine > client > global override by type.
        var resolved = await ResolveCredentialsAsync(machine, request.CredentialId, ct);

        var revealed = new List<RevealedCredentialDto>();
        foreach (var c in resolved)
        {
            var fields = CredentialDataProtector.ParseFields(c.CredentialType.FieldsJson);
            var values = _protector.Reveal(c.PlainData, c.SecretData);

            revealed.Add(new RevealedCredentialDto
            {
                Id = c.Id,
                CredentialTypeName = c.CredentialType.Name,
                Scope = c.Scope,
                Label = c.Label,
                Notes = c.Notes,
                Fields = fields.OrderBy(f => f.Order).Select(f => new RevealedFieldDto
                {
                    Key = f.Key,
                    Label = f.Label,
                    IsSecret = f.IsSecret,
                    Value = values.TryGetValue(f.Key, out var v) ? v : null
                }).ToList(),
                Attachments = c.Attachments.Select(a => new CredentialAttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    SizeBytes = a.SizeBytes
                }).ToList()
            });

            await _audit.LogAsync(new AuditEntry
            {
                Action = AuditAction.CredentialAccess,
                CredentialId = c.Id,
                CredentialType = c.CredentialType.Name,
                ClientId = machine.ClientId,
                ClientName = machine.Client.Name,
                MachineId = machine.Id,
                MachineName = machine.Name,
                Reason = accessRequest.Reason,
                InternalTicket = accessRequest.InternalTicket,
                CustomerTicket = accessRequest.CustomerTicket,
                Details = $"Viewed credential '{c.Label}'"
            }, ct);
        }

        await UpdateSessionCountersAsync(machine, revealed.Count, ct);

        return Result<AccessRequestResult>.Success(new AccessRequestResult
        {
            AccessRequestId = accessRequest.Id,
            MachineName = machine.Name,
            ClientName = machine.Client.Name,
            Credentials = revealed
        });
    }

    /// <summary>
    /// Applies the inheritance rule: start from machine-level credentials, then
    /// add client-level credentials for types not already present at machine level,
    /// then add global credentials for types not present at machine or client level.
    /// </summary>
    private async Task<List<Credential>> ResolveCredentialsAsync(Machine machine, int? singleCredentialId, CancellationToken ct)
    {
        if (singleCredentialId.HasValue)
        {
            var single = await _db.Credentials
                .Include(c => c.CredentialType).Include(c => c.Attachments)
                .FirstOrDefaultAsync(c => c.Id == singleCredentialId.Value && c.IsActive, ct);
            return single is null ? new() : new() { single };
        }

        var machineCreds = await _db.Credentials
            .Include(c => c.CredentialType).Include(c => c.Attachments)
            .Where(c => c.IsActive && c.Scope == CredentialScope.Machine && c.MachineId == machine.Id)
            .ToListAsync(ct);

        var clientCreds = await _db.Credentials
            .Include(c => c.CredentialType).Include(c => c.Attachments)
            .Where(c => c.IsActive && c.Scope == CredentialScope.Client && c.ClientId == machine.ClientId)
            .ToListAsync(ct);

        var globalCreds = await _db.Credentials
            .Include(c => c.CredentialType).Include(c => c.Attachments)
            .Where(c => c.IsActive && c.Scope == CredentialScope.Global)
            .ToListAsync(ct);

        var result = new List<Credential>(machineCreds);
        var typesCovered = new HashSet<int>(machineCreds.Select(c => c.CredentialTypeId));

        foreach (var c in clientCreds)
            if (!typesCovered.Contains(c.CredentialTypeId)) result.Add(c);
        var clientTypes = new HashSet<int>(clientCreds.Select(c => c.CredentialTypeId));
        typesCovered.UnionWith(clientTypes);

        foreach (var c in globalCreds)
            if (!typesCovered.Contains(c.CredentialTypeId)) result.Add(c);

        return result;
    }

    private async Task UpdateSessionCountersAsync(Machine machine, int credentialsViewed, CancellationToken ct)
    {
        var sessionToken = _current.SessionId;
        if (sessionToken is null) return;
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.SessionToken == sessionToken.Value, ct);
        if (session is null) return;

        session.MachinesAccessed++;
        session.CustomersAccessed++;
        session.CredentialsViewed += credentialsViewed;
        await _db.SaveChangesAsync(ct);
    }
}
