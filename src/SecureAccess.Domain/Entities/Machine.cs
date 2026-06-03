using SecureAccess.Domain.Common;
using SecureAccess.Domain.Enums;

namespace SecureAccess.Domain.Entities;

/// <summary>
/// A physical or virtual machine belonging to a client. Machine-level
/// credentials attached here override client-level and global credentials.
/// </summary>
public class Machine : BaseEntity
{
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? SerialNumber { get; set; }

    public string? Model { get; set; }

    public string? Location { get; set; }

    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public DateTime? InstallationDate { get; set; }

    public string? Notes { get; set; }

    public ICollection<Credential> Credentials { get; set; } = new List<Credential>();
}
