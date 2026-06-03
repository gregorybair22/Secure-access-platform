using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SecureAccess.Application.Abstractions;
using SecureAccess.Application.Features.Auth;
using SecureAccess.Infrastructure.Security;

namespace SecureAccess.Web.Auth;

/// <summary>
/// Minimal endpoints that perform cookie sign-in/sign-out. Blazor interactive
/// components cannot set cookies directly (the response has already started), so
/// the login form posts here via a normal HTTP request.
/// </summary>
public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/account/login", async (HttpContext http, IAuthService auth) =>
        {
            var form = await http.Request.ReadFormAsync();
            var userName = form["userName"].ToString();
            var password = form["password"].ToString();
            var returnUrl = form["returnUrl"].ToString();
            var rememberMe = form["rememberMe"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);

            var ip = http.Connection.RemoteIpAddress?.ToString();
            var ua = http.Request.Headers.UserAgent.ToString();

            var result = await auth.ValidateAndStartSessionAsync(new LoginRequest { UserName = userName, Password = password }, ip, ua);
            if (!result.Succeeded)
                return Results.Redirect($"/login?error={Uri.EscapeDataString(result.Error ?? "Login failed")}");

            var u = result.Value!;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, u.UserId.ToString()),
                new(ClaimTypes.Name, u.UserName),
                new(AppClaimTypes.Session, u.SessionToken.ToString())
            };
            claims.AddRange(u.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
            claims.AddRange(u.Permissions.Select(p => new Claim(AppClaimTypes.Permission, p)));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = rememberMe });

            if (u.MustChangePassword) return Results.Redirect("/change-password");
            return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
        });

        app.MapPost("/account/logout", async (HttpContext http, IAuthService auth, ICurrentUserService current) =>
        {
            if (current.SessionId is { } sid) await auth.LogoutAsync(sid);
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        });
    }
}
