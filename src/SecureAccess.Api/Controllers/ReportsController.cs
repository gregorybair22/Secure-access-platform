using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Reports;
using SecureAccess.Infrastructure.Security;

namespace SecureAccess.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[HasPermission(Permissions.ReportsView)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;
    public ReportsController(IReportService reports) => _reports = reports;

    [HttpPost("credential-access")]
    public async Task<IActionResult> CredentialAccess([FromBody] ReportFilter filter, CancellationToken ct)
        => Ok(await _reports.CredentialAccessAsync(filter, ct));

    [HttpPost("technician-activity")]
    public async Task<IActionResult> TechnicianActivity([FromBody] ReportFilter filter, CancellationToken ct)
        => Ok(await _reports.TechnicianActivityAsync(filter, ct));

    [HttpPost("customer-access")]
    public async Task<IActionResult> CustomerAccess([FromBody] ReportFilter filter, CancellationToken ct)
        => Ok(await _reports.CustomerAccessAsync(filter, ct));

    [HttpPost("machine-access")]
    public async Task<IActionResult> MachineAccess([FromBody] ReportFilter filter, CancellationToken ct)
        => Ok(await _reports.MachineAccessAsync(filter, ct));

    [HttpPost("credential-usage")]
    public async Task<IActionResult> CredentialUsage([FromBody] ReportFilter filter, CancellationToken ct)
        => Ok(await _reports.CredentialUsageAsync(filter, ct));

    [HttpPost("reason-analysis")]
    public async Task<IActionResult> ReasonAnalysis([FromBody] ReportFilter filter, CancellationToken ct)
        => Ok(await _reports.ReasonAnalysisAsync(filter, ct));

    [HttpPost("change-history")]
    public async Task<IActionResult> ChangeHistory([FromBody] ReportFilter filter, CancellationToken ct)
        => Ok(await _reports.ChangeHistoryAsync(filter, ct));
}
