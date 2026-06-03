using SecureAccess.Domain.Common;
using SecureAccess.Domain.Enums;

namespace SecureAccess.Domain.Entities;

/// <summary>
/// Records a technician's justified request to view credentials for a machine.
/// The mandatory reason and internal ticket are captured here BEFORE any
/// credential value is disclosed, satisfying the "credentials must never be
/// displayed before the reason is recorded" requirement.
/// </summary>
public class AccessRequest : BaseEntity
{
    // IDs are stored as plain columns (no FK cascade) so the request log survives
    // deletion of the related user/client/machine, matching the retention policy.
    public int UserId { get; set; }

    public int? ClientId { get; set; }

    public int? MachineId { get; set; }

    public AccessReasonCategory ReasonCategory { get; set; } = AccessReasonCategory.Other;

    /// <summary>Free-text reason for access (mandatory).</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Internal support ticket number (mandatory).</summary>
    public string InternalTicket { get; set; } = string.Empty;

    /// <summary>Customer ticket / reference (optional).</summary>
    public string? CustomerTicket { get; set; }

    public string? IpAddress { get; set; }

    public Guid? SessionId { get; set; }

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
}
