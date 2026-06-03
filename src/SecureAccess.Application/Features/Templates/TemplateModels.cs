using SecureAccess.Application.Common;

namespace SecureAccess.Application.Features.Templates;

public class TemplateDto
{
    public int Id { get; set; }
    public int CredentialTypeId { get; set; }
    public string CredentialTypeName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, string?> PlainValues { get; set; } = new();
}

public class SaveTemplateRequest
{
    public int CredentialTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, string?> Values { get; set; } = new();
}

public class ApplyTemplateRequest
{
    public int TemplateId { get; set; }
    /// <summary>Target machine ids to create credentials on.</summary>
    public List<int> MachineIds { get; set; } = new();
    /// <summary>When true, only fills empty fields on existing matching credentials; otherwise creates/overwrites.</summary>
    public bool FillEmptyOnly { get; set; }
    public bool Overwrite { get; set; }
}

public interface ITemplateService
{
    Task<IReadOnlyList<TemplateDto>> GetTemplatesAsync(CancellationToken ct = default);
    Task<TemplateDto?> GetTemplateAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(SaveTemplateRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, SaveTemplateRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result<int>> ApplyToMachinesAsync(ApplyTemplateRequest request, CancellationToken ct = default);
}
