using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Credentials;
using SecureAccess.Domain.Enums;

namespace SecureAccess.Application.Features.AccessRequests;

/// <summary>
/// A technician's request to view the credentials for a machine. The reason and
/// internal ticket are mandatory and are persisted before any secret is revealed.
/// </summary>
public class CredentialAccessRequest
{
    public int MachineId { get; set; }
    public AccessReasonCategory ReasonCategory { get; set; } = AccessReasonCategory.Other;
    public string Reason { get; set; } = string.Empty;
    public string InternalTicket { get; set; } = string.Empty;
    public string? CustomerTicket { get; set; }
    /// <summary>Optional: limit disclosure to a single credential; otherwise all resolved credentials are returned.</summary>
    public int? CredentialId { get; set; }
}

public class AccessRequestResult
{
    public int AccessRequestId { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public List<RevealedCredentialDto> Credentials { get; set; } = new();
}

public interface IAccessRequestService
{
    /// <summary>
    /// Validates the mandatory reason/ticket, records the access request plus audit
    /// entries, then resolves and decrypts the credentials applicable to the machine
    /// (machine-level overriding client-level overriding global).
    /// </summary>
    Task<Result<AccessRequestResult>> RequestCredentialsAsync(CredentialAccessRequest request, CancellationToken ct = default);
}
