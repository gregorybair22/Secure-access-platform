using SecureAccess.Application.Common;
using SecureAccess.Domain.Enums;

namespace SecureAccess.Application.Features.Clients;

public class ClientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactPerson { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public EntityStatus Status { get; set; }
    public bool IsRestricted { get; set; }
    public int MachineCount { get; set; }
}

public class ClientListStats
{
    public int TotalClients { get; set; }
    public int ActiveClients { get; set; }
    public int TotalMachines { get; set; }
    public int ActiveCredentials { get; set; }
}

public class SaveClientRequest
{
    public string Name { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactPerson { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public bool IsRestricted { get; set; }
}

public interface IClientService
{
    Task<ClientListStats> GetListStatsAsync(CancellationToken ct = default);
    Task<PagedResult<ClientDto>> GetClientsAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<ClientDto?> GetClientAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(SaveClientRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, SaveClientRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
