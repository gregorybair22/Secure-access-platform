using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.AccessRequests;
using SecureAccess.Application.Features.Credentials;
using SecureAccess.Infrastructure.Security;

namespace SecureAccess.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CredentialsController : ControllerBase
{
    private readonly ICredentialService _credentials;
    private readonly IAccessRequestService _accessRequests;

    public CredentialsController(ICredentialService credentials, IAccessRequestService accessRequests)
    {
        _credentials = credentials;
        _accessRequests = accessRequests;
    }

    [HttpGet("machine/{machineId:int}")]
    [HasPermission(Permissions.CredentialsView)]
    public async Task<IActionResult> ForMachine(int machineId, CancellationToken ct)
        => Ok(await _credentials.GetForMachineAsync(machineId, ct));

    [HttpGet("client/{clientId:int}")]
    [HasPermission(Permissions.CredentialsView)]
    public async Task<IActionResult> ForClient(int clientId, CancellationToken ct)
        => Ok(await _credentials.GetForClientAsync(clientId, ct));

    [HttpGet("global")]
    [HasPermission(Permissions.CredentialsView)]
    public async Task<IActionResult> Global(CancellationToken ct)
        => Ok(await _credentials.GetGlobalAsync(ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.CredentialsView)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var c = await _credentials.GetAsync(id, ct);
        return c is null ? NotFound() : Ok(c);
    }

    [HttpPost]
    [HasPermission(Permissions.CredentialsCreate)]
    public async Task<IActionResult> Create([FromBody] SaveCredentialRequest request, CancellationToken ct)
    {
        var result = await _credentials.CreateAsync(request, ct);
        return result.Succeeded ? Ok(new { id = result.Value }) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.CredentialsEdit)]
    public async Task<IActionResult> Update(int id, [FromBody] SaveCredentialRequest request, CancellationToken ct)
    {
        var result = await _credentials.UpdateAsync(id, request, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.CredentialsDelete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _credentials.DeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// The gated credential request workflow. A mandatory reason and internal
    /// ticket must be supplied; only then are the resolved credentials revealed.
    /// </summary>
    [HttpPost("request")]
    [HasPermission(Permissions.CredentialsRequest)]
    public async Task<IActionResult> RequestCredentials([FromBody] CredentialAccessRequest request, CancellationToken ct)
    {
        var result = await _accessRequests.RequestCredentialsAsync(request, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
