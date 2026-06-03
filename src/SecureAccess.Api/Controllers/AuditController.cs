using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Audit;
using SecureAccess.Infrastructure.Security;

namespace SecureAccess.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[HasPermission(Permissions.AuditView)]
public class AuditController : ControllerBase
{
    private readonly IAuditQueryService _audit;
    public AuditController(IAuditQueryService audit) => _audit = audit;

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] AuditQueryFilter filter, CancellationToken ct)
        => Ok(await _audit.QueryAsync(filter, ct));

    [HttpGet("sessions")]
    public async Task<IActionResult> Sessions([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await _audit.GetSessionsAsync(page, pageSize, ct));
}
