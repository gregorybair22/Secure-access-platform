using SecureAccess.Domain.Common;

namespace SecureAccess.Domain.Entities;

/// <summary>
/// A file attached to a credential (for example a VPN configuration file).
/// The binary content is stored on disk under the configured attachment root;
/// only metadata and the relative storage path live in the database, which
/// keeps the attachment backup/restore procedure independent of the DB backup.
/// </summary>
public class Attachment : BaseEntity
{
    public int CredentialId { get; set; }
    public Credential Credential { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    public long SizeBytes { get; set; }

    /// <summary>Relative path under the configured attachment storage root.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>SHA-256 of the stored (encrypted) file for integrity verification.</summary>
    public string? Sha256 { get; set; }

    public bool IsEncrypted { get; set; } = true;
}
