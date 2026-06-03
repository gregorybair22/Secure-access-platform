using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Credentials;
using SecureAccess.Infrastructure.Security;

namespace SecureAccess.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/credential-types")]
public class CredentialTypesController : ControllerBase
{
    private readonly ICredentialTypeService _types;
    public CredentialTypesController(ICredentialTypeService types) => _types = types;

    [HttpGet]
    [HasPermission(Permissions.CredentialsView)]
    public async Task<IActionResult> Get([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _types.GetTypesAsync(includeInactive, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.CredentialsView)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var type = await _types.GetTypeAsync(id, ct);
        return type is null ? NotFound() : Ok(type);
    }

    [HttpPost]
    [HasPermission(Permissions.CredentialsCreate)]
    public async Task<IActionResult> Create([FromBody] SaveCredentialTypeRequest request, CancellationToken ct)
    {
        var result = await _types.CreateAsync(request, ct);
        return result.Succeeded ? Ok(new { id = result.Value }) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.CredentialsEdit)]
    public async Task<IActionResult> Update(int id, [FromBody] SaveCredentialTypeRequest request, CancellationToken ct)
    {
        var result = await _types.UpdateAsync(id, request, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.CredentialsDelete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _types.DeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }
}
