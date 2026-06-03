using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAccess.Application.Abstractions;
using SecureAccess.Application.Features.Auth;

namespace SecureAccess.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _current;

    public AuthController(IAuthService auth, ICurrentUserService current)
    {
        _auth = auth;
        _current = current;
    }

    /// <summary>Authenticate and obtain a JWT plus the user's roles and permissions.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var result = await _auth.LoginAsync(request, ip, ua, ct);
        return result.Succeeded ? Ok(result.Value) : Unauthorized(new { error = result.Error });
    }

    /// <summary>End the current session.</summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (_current.SessionId is { } sid)
            await _auth.LogoutAsync(sid, ct);
        return NoContent();
    }

    /// <summary>Change the authenticated user's password.</summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (_current.UserId is not { } userId) return Unauthorized();
        var result = await _auth.ChangePasswordAsync(userId, request, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }
}
