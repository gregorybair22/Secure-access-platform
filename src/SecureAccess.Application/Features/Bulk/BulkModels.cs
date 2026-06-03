using SecureAccess.Application.Common;

namespace SecureAccess.Application.Features.Bulk;

/// <summary>Copy credentials from one source machine to one or more target machines.</summary>
public class CopyMachineToMachinesRequest
{
    public int SourceMachineId { get; set; }
    public List<int> TargetMachineIds { get; set; } = new();
    public bool FillEmptyOnly { get; set; }
    public bool Overwrite { get; set; }
}

/// <summary>Copy client-level credentials from one source client to one or more target clients.</summary>
public class CopyClientToClientsRequest
{
    public int SourceClientId { get; set; }
    public List<int> TargetClientIds { get; set; } = new();
    public bool FillEmptyOnly { get; set; }
    public bool Overwrite { get; set; }
}

/// <summary>Apply the same field changes to many credentials selected by client/machine scope.</summary>
public class BulkUpdateRequest
{
    public List<int> ClientIds { get; set; } = new();
    public List<int> MachineIds { get; set; } = new();
    public int? CredentialTypeId { get; set; }
    /// <summary>Field values to apply.</summary>
    public Dictionary<string, string?> Values { get; set; } = new();
    public bool FillEmptyOnly { get; set; }
    public bool Overwrite { get; set; }
    public string? Reason { get; set; }
}

public class BulkOperationResult
{
    public int AffectedCredentials { get; set; }
    public int CreatedCredentials { get; set; }
    public List<string> Messages { get; set; } = new();
}

public interface IBulkService
{
    Task<Result<BulkOperationResult>> CopyMachineToMachinesAsync(CopyMachineToMachinesRequest request, CancellationToken ct = default);
    Task<Result<BulkOperationResult>> CopyClientToClientsAsync(CopyClientToClientsRequest request, CancellationToken ct = default);
    Task<Result<BulkOperationResult>> BulkUpdateAsync(BulkUpdateRequest request, CancellationToken ct = default);
}
