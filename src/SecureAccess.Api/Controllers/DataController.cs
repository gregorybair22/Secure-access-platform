using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.ImportExport;
using SecureAccess.Infrastructure.Security;

namespace SecureAccess.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/data")]
public class DataController : ControllerBase
{
    private readonly IImportService _import;
    private readonly IExportService _export;

    public DataController(IImportService import, IExportService export)
    {
        _import = import;
        _export = export;
    }

    [HttpPost("import/{entity}")]
    [HasPermission(Permissions.ImportData)]
    public async Task<IActionResult> Import(ImportEntity entity, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { error = "No file uploaded." });
        await using var stream = file.OpenReadStream();
        var result = await _import.ImportAsync(entity, stream, file.FileName, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("export/clients")]
    [HasPermission(Permissions.ExportData)]
    public async Task<IActionResult> ExportClients([FromQuery] ExportFormat format = ExportFormat.Xlsx, CancellationToken ct = default)
    {
        var file = await _export.ExportClientsAsync(format, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("export/machines")]
    [HasPermission(Permissions.ExportData)]
    public async Task<IActionResult> ExportMachines([FromQuery] ExportFormat format = ExportFormat.Xlsx, [FromQuery] int? clientId = null, CancellationToken ct = default)
    {
        var file = await _export.ExportMachinesAsync(format, clientId, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
