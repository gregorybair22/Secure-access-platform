namespace SecureAccess.Application.Abstractions;

/// <summary>
/// Symmetric encryption for credential secrets. Implemented with AES-256-GCM;
/// the data key is protected at rest via ASP.NET Core Data Protection (DPAPI on
/// Windows). See docs/encryption-architecture.md.
/// </summary>
public interface IEncryptionService
{
    /// <summary>Encrypts plaintext, returning a self-describing base64 token (null/empty passes through).</summary>
    string? Encrypt(string? plainText);

    /// <summary>Decrypts a token produced by <see cref="Encrypt"/>.</summary>
    string? Decrypt(string? cipherText);

    byte[] EncryptBytes(byte[] data);

    byte[] DecryptBytes(byte[] data);
}
