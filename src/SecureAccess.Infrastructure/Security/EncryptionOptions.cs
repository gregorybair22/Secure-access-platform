namespace SecureAccess.Infrastructure.Security;

/// <summary>Configuration for the credential encryption subsystem.</summary>
public class EncryptionOptions
{
    public const string SectionName = "Encryption";

    /// <summary>
    /// Path to the file holding the AES-256 master key, protected at rest with
    /// ASP.NET Core Data Protection (DPAPI on Windows). Created automatically on
    /// first run if it does not exist.
    /// </summary>
    public string KeyFilePath { get; set; } = "App_Data/keys/credential-master.key";

    /// <summary>
    /// Optional base64 32-byte key. Only used to seed the protected key file the
    /// first time; afterwards the protected file is the source of truth. Leave
    /// empty to auto-generate a cryptographically random key.
    /// </summary>
    public string? SeedKeyBase64 { get; set; }

    /// <summary>Directory where the Data Protection key ring is persisted.</summary>
    public string DataProtectionKeysPath { get; set; } = "App_Data/keys/dp";
}
