using SecureAccess.Application.Common;
using SecureAccess.Domain.Enums;

namespace SecureAccess.Application.Features.Credentials;

public class CredentialFieldDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public CredentialFieldType Type { get; set; } = CredentialFieldType.Text;
    public bool IsSecret { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSearchable { get; set; }
    public int Order { get; set; }
}

public class CredentialTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public List<CredentialFieldDto> Fields { get; set; } = new();
}

public class CredentialTypeListStats
{
    public int TotalTypes { get; set; }
    public int SystemTypes { get; set; }
    public int ActiveTypes { get; set; }
    public int CustomTypes { get; set; }
}

public class SaveCredentialTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CredentialFieldDto> Fields { get; set; } = new();
}

public interface ICredentialTypeService
{
    Task<CredentialTypeListStats> GetListStatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CredentialTypeDto>> GetTypesAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<CredentialTypeDto?> GetTypeAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(SaveCredentialTypeRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, SaveCredentialTypeRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
