namespace SecureAccess.Application.Abstractions;

/// <summary>
/// Provides information about the user making the current request, sourced from
/// the authenticated principal (JWT claims in the API, cookie claims in Blazor).
/// </summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    string? UserName { get; }
    string? IpAddress { get; }
    Guid? SessionId { get; }
    bool IsAuthenticated { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }

    bool IsInRole(string role);
    bool HasPermission(string permission);
}
