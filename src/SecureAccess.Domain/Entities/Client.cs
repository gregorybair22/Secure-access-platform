using SecureAccess.Domain.Common;
using SecureAccess.Domain.Enums;

namespace SecureAccess.Domain.Entities;

/// <summary>
/// A customer whose machines are supported by technicians. Holds the contact
/// information required by the specification plus an optional restriction flag.
/// </summary>
public class Client : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string CustomerCode { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? ContactPerson { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Notes { get; set; }

    public EntityStatus Status { get; set; } = EntityStatus.Active;

    /// <summary>Restricted clients are hidden from technicians without explicit authorization.</summary>
    public bool IsRestricted { get; set; }

    public ICollection<Machine> Machines { get; set; } = new List<Machine>();

    public ICollection<Credential> Credentials { get; set; } = new List<Credential>();
}
