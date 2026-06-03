using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Machines;
using SecureAccess.Infrastructure.Security;

namespace SecureAccess.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class MachinesController : ControllerBase
{
    private readonly IMachineService _machines;
    public MachinesController(IMachineService machines) => _machines = machines;

    [HttpGet]
    [HasPermission(Permissions.MachinesView)]
    public async Task<IActionResult> Get([FromQuery] int? clientId, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
        => Ok(await _machines.GetMachinesAsync(clientId, search, page, pageSize, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.MachinesView)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var machine = await _machines.GetMachineAsync(id, ct);
        return machine is null ? NotFound() : Ok(machine);
    }

    [HttpPost]
    [HasPermission(Permissions.MachinesCreate)]
    public async Task<IActionResult> Create([FromBody] SaveMachineRequest request, CancellationToken ct)
    {
        var result = await _machines.CreateAsync(request, ct);
        return result.Succeeded ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value })
                                : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.MachinesEdit)]
    public async Task<IActionResult> Update(int id, [FromBody] SaveMachineRequest request, CancellationToken ct)
    {
        var result = await _machines.UpdateAsync(id, request, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.MachinesDelete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _machines.DeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }
}
