using SecureAccess.Domain.Common;
using SecureAccess.Domain.Enums;

namespace SecureAccess.Domain.Entities;

/// <summary>
/// A stored set of credential values for a given type and scope.
///
/// Field values are split into two columns: <see cref="PlainData"/> holds the
/// JSON of non-secret (searchable) fields, while <see cref="SecretData"/> holds
/// the AES-256 encrypted JSON of secret fields. Secrets are therefore never
/// persisted in plain text and only decrypted after an authorized access
/// request has been recorded.
/// </summary>
public class Credential : BaseEntity
{
    public int CredentialTypeId { get; set; }
    public CredentialType CredentialType { get; set; } = null!;

    public CredentialScope Scope { get; set; } = CredentialScope.Machine;

    /// <summary>Set for Client and Machine scoped credentials.</summary>
    public int? ClientId { get; set; }
    public Client? Client { get; set; }

    /// <summary>Set for Machine scoped credentials only.</summary>
    public int? MachineId { get; set; }
    public Machine? Machine { get; set; }

    /// <summary>Display label, for example "Primary VPN" or "Local Admin".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>JSON dictionary of non-secret field values (searchable, plain text).</summary>
    public string PlainData { get; set; } = "{}";

    /// <summary>AES-256 encrypted JSON dictionary of secret field values.</summary>
    public string? SecretData { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
