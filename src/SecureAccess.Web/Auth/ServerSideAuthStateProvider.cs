using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace SecureAccess.Web.Auth;

/// <summary>
/// Supplies the authentication state to Blazor components from the cookie-based
/// <see cref="ClaimsPrincipal"/>. The principal is captured when the scoped
/// provider is created (within the authenticated HTTP context that starts the
/// circuit), so it remains stable for the lifetime of the circuit even though
/// the <see cref="HttpContext"/> itself is not available later.
/// </summary>
public class ServerSideAuthStateProvider : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal _principal;

    public ServerSideAuthStateProvider(IHttpContextAccessor accessor)
    {
        _principal = accessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(_principal));
}
