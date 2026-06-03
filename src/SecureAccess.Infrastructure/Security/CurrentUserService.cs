using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SecureAccess.Application.Abstractions;

namespace SecureAccess.Infrastructure.Security;

/// <summary>Custom claim types issued by both the JWT (API) and cookie (Blazor) sign-in.</summary>
public static class AppClaimTypes
{
    public const string Permission = "perm";
    public const string Session = "sid";
}

/// <summary>Resolves the current user from the authenticated <see cref="ClaimsPrincipal"/>.</summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name);

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public Guid? SessionId
    {
        get
        {
            var raw = Principal?.FindFirstValue(AppClaimTypes.Session);
            return Guid.TryParse(raw, out var g) ? g : null;
        }
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>();

    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll(AppClaimTypes.Permission).Select(c => c.Value).ToArray() ?? Array.Empty<string>();

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

    public bool HasPermission(string permission) =>
        Principal?.HasClaim(AppClaimTypes.Permission, permission) ?? false;
}
