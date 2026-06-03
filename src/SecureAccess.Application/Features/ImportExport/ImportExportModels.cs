using SecureAccess.Application.Common;

namespace SecureAccess.Application.Features.ImportExport;

public enum ExportFormat
{
    Xlsx = 1,
    Csv = 2,
    Pdf = 3
}

public enum ImportEntity
{
    Clients = 1,
    Machines = 2,
    Credentials = 3,
    Templates = 4
}

public class ExportFile
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

public class ImportResult
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}

public interface IImportService
{
    Task<Result<ImportResult>> ImportAsync(ImportEntity entity, Stream fileStream, string fileName, CancellationToken ct = default);
}

public interface IExportService
{
    Task<ExportFile> ExportClientsAsync(ExportFormat format, CancellationToken ct = default);
    Task<ExportFile> ExportMachinesAsync(ExportFormat format, int? clientId, CancellationToken ct = default);
    /// <summary>Generic tabular export used by reports.</summary>
    ExportFile ExportTable(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string?>> rows, ExportFormat format);
}
