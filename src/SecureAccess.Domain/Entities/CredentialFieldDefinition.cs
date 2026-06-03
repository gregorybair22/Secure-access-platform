using SecureAccess.Domain.Enums;

namespace SecureAccess.Domain.Entities;

/// <summary>
/// Describes a single field belonging to a <see cref="CredentialType"/>. A list
/// of these is serialized to JSON and stored on the type, which lets the system
/// support both the predefined types (RustDesk, AnyDesk, Remote Desktop, VPN)
/// and an unlimited number of custom types without schema changes.
/// </summary>
public class CredentialFieldDefinition
{
    /// <summary>Stable machine key, for example "rustdesk_id" or "password".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Human readable label shown in the UI.</summary>
    public string Label { get; set; } = string.Empty;

    public CredentialFieldType Type { get; set; } = CredentialFieldType.Text;

    /// <summary>Secret fields (passwords, keys) are encrypted at rest and masked in reports.</summary>
    public bool IsSecret { get; set; }

    public bool IsRequired { get; set; }

    /// <summary>Whether this (non-secret) field participates in global search.</summary>
    public bool IsSearchable { get; set; }

    public int Order { get; set; }
}
