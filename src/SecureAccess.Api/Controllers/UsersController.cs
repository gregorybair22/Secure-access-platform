using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Users;
using SecureAccess.Infrastructure.Security;

namespace SecureAccess.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    public UsersController(IUserService users) => _users = users;

    [HttpGet]
    [HasPermission(Permissions.UsersManage)]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await _users.GetUsersAsync(ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.UsersManage)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var u = await _users.GetUserAsync(id, ct);
        return u is null ? NotFound() : Ok(u);
    }

    [HttpPost]
    [HasPermission(Permissions.UsersManage)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await _users.CreateUserAsync(request, ct);
        return result.Succeeded ? Ok(new { id = result.Value }) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.UsersManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await _users.UpdateUserAsync(id, request, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpGet("roles")]
    [HasPermission(Permissions.RolesManage)]
    public async Task<IActionResult> Roles(CancellationToken ct) => Ok(await _users.GetRolesAsync(ct));

    [HttpPut("roles/{roleId:int}/permissions")]
    [HasPermission(Permissions.RolesManage)]
    public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] List<string> permissionCodes, CancellationToken ct)
    {
        var result = await _users.UpdateRolePermissionsAsync(roleId, permissionCodes, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }
}
