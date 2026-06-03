using SecureAccess.Application.Common;
using SecureAccess.Domain.Enums;

namespace SecureAccess.Application.Features.Credentials;

/// <summary>Credential metadata for list/management views. Secret values are NEVER included here.</summary>
public class CredentialDto
{
    public int Id { get; set; }
    public int CredentialTypeId { get; set; }
    public string CredentialTypeName { get; set; } = string.Empty;
    public CredentialScope Scope { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public int? MachineId { get; set; }
    public string? MachineName { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public int AttachmentCount { get; set; }
    /// <summary>Non-secret field values, safe to display in management lists.</summary>
    public Dictionary<string, string?> PlainValues { get; set; } = new();
}

public class SaveCredentialRequest
{
    public int CredentialTypeId { get; set; }
    public CredentialScope Scope { get; set; } = CredentialScope.Machine;
    public int? ClientId { get; set; }
    public int? MachineId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>All field values keyed by field key (both secret and non-secret).</summary>
    public Dictionary<string, string?> Values { get; set; } = new();
    /// <summary>Reason for change, recorded in the change history when editing.</summary>
    public string? ChangeReason { get; set; }
}

/// <summary>A fully resolved credential including decrypted secret values, returned only after an authorized access request.</summary>
public class RevealedCredentialDto
{
    public int Id { get; set; }
    public string CredentialTypeName { get; set; } = string.Empty;
    public CredentialScope Scope { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<RevealedFieldDto> Fields { get; set; } = new();
    public List<CredentialAttachmentDto> Attachments { get; set; } = new();
}

public class RevealedFieldDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsSecret { get; set; }
    public string? Value { get; set; }
}

public class CredentialAttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

public interface ICredentialService
{
    Task<IReadOnlyList<CredentialDto>> GetForMachineAsync(int machineId, CancellationToken ct = default);
    Task<IReadOnlyList<CredentialDto>> GetForClientAsync(int clientId, CancellationToken ct = default);
    Task<IReadOnlyList<CredentialDto>> GetGlobalAsync(CancellationToken ct = default);
    Task<CredentialDto?> GetAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(SaveCredentialRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, SaveCredentialRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
