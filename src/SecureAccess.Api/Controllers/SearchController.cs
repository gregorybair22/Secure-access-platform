using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAccess.Application.Features.Search;

namespace SecureAccess.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _search;
    public SearchController(ISearchService search) => _search = search;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int max = 50, CancellationToken ct = default)
        => Ok(await _search.SearchAsync(q, max, ct));
}
