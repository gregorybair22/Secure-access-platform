using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SecureAccess.Infrastructure.Security;

/// <summary>Authorization requirement for a single permission code.</summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}

/// <summary>Succeeds when the principal carries the matching "perm" claim.</summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim(AppClaimTypes.Permission, requirement.Permission))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Creates authorization policies on demand for names of the form "perm:{code}".
/// This avoids registering a named policy for every permission individually.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    public const string Prefix = "perm:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName[Prefix.Length..]))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        return _fallback.GetPolicyAsync(policyName);
    }
}

/// <summary>Convenience <c>[HasPermission("code")]</c> attribute.</summary>
public class HasPermissionAttribute : Microsoft.AspNetCore.Authorization.AuthorizeAttribute
{
    public HasPermissionAttribute(string permission) => Policy = PermissionPolicyProvider.Prefix + permission;
}
