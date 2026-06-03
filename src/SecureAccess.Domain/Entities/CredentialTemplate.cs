using SecureAccess.Domain.Common;

namespace SecureAccess.Domain.Entities;

/// <summary>
/// A reusable set of credential values (for example a standard VPN template)
/// that can be applied to one or more machines or clients in a single action.
/// Secret values are encrypted exactly like <see cref="Credential"/>.
/// </summary>
public class CredentialTemplate : BaseEntity
{
    public int CredentialTypeId { get; set; }
    public CredentialType CredentialType { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string PlainData { get; set; } = "{}";

    public string? SecretData { get; set; }

    public bool IsActive { get; set; } = true;
}
