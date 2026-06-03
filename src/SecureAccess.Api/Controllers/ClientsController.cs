using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Clients;
using SecureAccess.Infrastructure.Security;

namespace SecureAccess.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clients;
    public ClientsController(IClientService clients) => _clients = clients;

    [HttpGet]
    [HasPermission(Permissions.ClientsView)]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
        => Ok(await _clients.GetClientsAsync(search, page, pageSize, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.ClientsView)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var client = await _clients.GetClientAsync(id, ct);
        return client is null ? NotFound() : Ok(client);
    }

    [HttpPost]
    [HasPermission(Permissions.ClientsCreate)]
    public async Task<IActionResult> Create([FromBody] SaveClientRequest request, CancellationToken ct)
    {
        var result = await _clients.CreateAsync(request, ct);
        return result.Succeeded ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value })
                                : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.ClientsEdit)]
    public async Task<IActionResult> Update(int id, [FromBody] SaveClientRequest request, CancellationToken ct)
    {
        var result = await _clients.UpdateAsync(id, request, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.ClientsDelete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _clients.DeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }
}
