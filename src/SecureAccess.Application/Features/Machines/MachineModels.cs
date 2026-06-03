using SecureAccess.Application.Common;
using SecureAccess.Domain.Enums;

namespace SecureAccess.Application.Features.Machines;

public class MachineDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? Location { get; set; }
    public EntityStatus Status { get; set; }
    public DateTime? InstallationDate { get; set; }
    public string? Notes { get; set; }
    public int CredentialCount { get; set; }
}

public class SaveMachineRequest
{
    public int ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? Location { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public DateTime? InstallationDate { get; set; }
    public string? Notes { get; set; }
}

public interface IMachineService
{
    Task<PagedResult<MachineDto>> GetMachinesAsync(int? clientId, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<MachineDto?> GetMachineAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(SaveMachineRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, SaveMachineRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
