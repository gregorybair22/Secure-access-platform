using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Bulk;
using SecureAccess.Infrastructure.Security;

namespace SecureAccess.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[HasPermission(Permissions.BulkOperations)]
public class BulkController : ControllerBase
{
    private readonly IBulkService _bulk;
    public BulkController(IBulkService bulk) => _bulk = bulk;

    [HttpPost("copy/machine-to-machines")]
    public async Task<IActionResult> CopyMachineToMachines([FromBody] CopyMachineToMachinesRequest request, CancellationToken ct)
    {
        var result = await _bulk.CopyMachineToMachinesAsync(request, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("copy/client-to-clients")]
    public async Task<IActionResult> CopyClientToClients([FromBody] CopyClientToClientsRequest request, CancellationToken ct)
    {
        var result = await _bulk.CopyClientToClientsAsync(request, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("update")]
    public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateRequest request, CancellationToken ct)
    {
        var result = await _bulk.BulkUpdateAsync(request, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
