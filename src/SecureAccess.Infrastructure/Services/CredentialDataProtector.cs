using SecureAccess.Application.Abstractions;
using SecureAccess.Domain.Entities;

namespace SecureAccess.Infrastructure.Services;

/// <summary>
/// Splits credential field values into a plain (searchable) JSON column and an
/// AES-256 encrypted JSON column based on each field's <c>IsSecret</c> flag, and
/// reassembles them on read. Centralizes the rule that secrets never touch the
/// plain column.
/// </summary>
public class CredentialDataProtector
{
    private readonly IEncryptionService _encryption;

    public CredentialDataProtector(IEncryptionService encryption) => _encryption = encryption;

    public static List<CredentialFieldDefinition> ParseFields(string fieldsJson) =>
        Json.Deserialize<List<CredentialFieldDefinition>>(fieldsJson) ?? new();

    /// <summary>Returns (plainDataJson, encryptedSecretData) for storage.</summary>
    public (string PlainData, string? SecretData) Protect(
        IEnumerable<CredentialFieldDefinition> fields, IDictionary<string, string?> values)
    {
        var plain = new Dictionary<string, string?>();
        var secret = new Dictionary<string, string?>();

        foreach (var f in fields)
        {
            values.TryGetValue(f.Key, out var v);
            if (f.IsSecret)
            {
                if (!string.IsNullOrEmpty(v)) secret[f.Key] = v;
            }
            else
            {
                plain[f.Key] = v;
            }
        }

        var plainJson = Json.Serialize(plain);
        string? secretData = secret.Count > 0 ? _encryption.Encrypt(Json.Serialize(secret)) : null;
        return (plainJson, secretData);
    }

    public Dictionary<string, string?> ReadPlain(string? plainData) =>
        Json.Deserialize<Dictionary<string, string?>>(plainData) ?? new();

    /// <summary>Decrypts and merges plain + secret values into a single dictionary.</summary>
    public Dictionary<string, string?> Reveal(string? plainData, string? secretData)
    {
        var result = ReadPlain(plainData);
        if (!string.IsNullOrEmpty(secretData))
        {
            var decrypted = _encryption.Decrypt(secretData);
            var secret = Json.Deserialize<Dictionary<string, string?>>(decrypted) ?? new();
            foreach (var kv in secret) result[kv.Key] = kv.Value;
        }
        return result;
    }
}
