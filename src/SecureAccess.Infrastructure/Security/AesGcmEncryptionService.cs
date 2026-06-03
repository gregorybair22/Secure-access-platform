using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureAccess.Application.Abstractions;

namespace SecureAccess.Infrastructure.Security;

/// <summary>
/// AES-256-GCM authenticated encryption for credential secrets. The 32-byte
/// master key is generated on first run and stored protected by ASP.NET Core
/// Data Protection (DPAPI-backed on Windows), so the key is never persisted in
/// clear text. Output token format: "v1:" + base64(nonce(12) | tag(16) | cipher).
/// </summary>
public class AesGcmEncryptionService : IEncryptionService
{
    private const string Prefix = "v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public AesGcmEncryptionService(
        IOptions<EncryptionOptions> options,
        IDataProtectionProvider dataProtection,
        ILogger<AesGcmEncryptionService> logger)
    {
        var opts = options.Value;
        _key = LoadOrCreateKey(opts, dataProtection.CreateProtector("SecureAccess.CredentialMasterKey"), logger);
    }

    private static byte[] LoadOrCreateKey(EncryptionOptions opts, IDataProtector protector, ILogger logger)
    {
        var path = Path.GetFullPath(opts.KeyFilePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (File.Exists(path))
        {
            var protectedBytes = File.ReadAllBytes(path);
            var key = protector.Unprotect(protectedBytes);
            if (key.Length != 32)
                throw new InvalidOperationException("Stored credential master key is not 256-bit.");
            return key;
        }

        byte[] newKey;
        if (!string.IsNullOrWhiteSpace(opts.SeedKeyBase64))
        {
            newKey = Convert.FromBase64String(opts.SeedKeyBase64);
            if (newKey.Length != 32)
                throw new InvalidOperationException("Encryption:SeedKeyBase64 must decode to exactly 32 bytes.");
        }
        else
        {
            newKey = RandomNumberGenerator.GetBytes(32);
            logger.LogWarning("Generated a new random AES-256 credential master key at {Path}. Back up this file.", path);
        }

        File.WriteAllBytes(path, protector.Protect(newKey));
        return newKey;
    }

    public string? Encrypt(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        var plain = Encoding.UTF8.GetBytes(plainText);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        var combined = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, combined, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, combined, NonceSize + TagSize, cipher.Length);
        return Prefix + Convert.ToBase64String(combined);
    }

    public string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;
        if (!cipherText.StartsWith(Prefix, StringComparison.Ordinal))
            return cipherText; // not encrypted by us; pass through

        var combined = Convert.FromBase64String(cipherText[Prefix.Length..]);
        var nonce = combined.AsSpan(0, NonceSize);
        var tag = combined.AsSpan(NonceSize, TagSize);
        var cipher = combined.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    public byte[] EncryptBytes(byte[] data)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[data.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, data, cipher, tag);

        var combined = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, combined, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, combined, NonceSize + TagSize, cipher.Length);
        return combined;
    }

    public byte[] DecryptBytes(byte[] data)
    {
        var nonce = data.AsSpan(0, NonceSize);
        var tag = data.AsSpan(NonceSize, TagSize);
        var cipher = data.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return plain;
    }
}
