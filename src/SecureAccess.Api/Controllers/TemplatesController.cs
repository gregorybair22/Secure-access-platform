using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Templates;
using SecureAccess.Infrastructure.Security;

namespace SecureAccess.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templates;
    public TemplatesController(ITemplateService templates) => _templates = templates;

    [HttpGet]
    [HasPermission(Permissions.CredentialsView)]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await _templates.GetTemplatesAsync(ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.CredentialsView)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var t = await _templates.GetTemplateAsync(id, ct);
        return t is null ? NotFound() : Ok(t);
    }

    [HttpPost]
    [HasPermission(Permissions.TemplatesManage)]
    public async Task<IActionResult> Create([FromBody] SaveTemplateRequest request, CancellationToken ct)
    {
        var result = await _templates.CreateAsync(request, ct);
        return result.Succeeded ? Ok(new { id = result.Value }) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.TemplatesManage)]
    public async Task<IActionResult> Update(int id, [FromBody] SaveTemplateRequest request, CancellationToken ct)
    {
        var result = await _templates.UpdateAsync(id, request, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.TemplatesManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _templates.DeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPost("apply")]
    [HasPermission(Permissions.BulkOperations)]
    public async Task<IActionResult> Apply([FromBody] ApplyTemplateRequest request, CancellationToken ct)
    {
        var result = await _templates.ApplyToMachinesAsync(request, ct);
        return result.Succeeded ? Ok(new { affected = result.Value }) : BadRequest(new { error = result.Error });
    }
}
