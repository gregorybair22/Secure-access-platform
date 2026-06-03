using SecureAccess.Application.Common;

namespace SecureAccess.Application.Features.Auth;

public class LoginRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthenticatedUser
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid SessionToken { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public bool MustChangePassword { get; set; }
}

public class LoginResult
{
    public AuthenticatedUser User { get; set; } = new();
    /// <summary>JWT issued for REST API consumption.</summary>
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>Authentication and session lifecycle.</summary>
public interface IAuthService
{
    Task<Result<LoginResult>> LoginAsync(LoginRequest request, string? ip, string? userAgent, CancellationToken ct = default);
    Task LogoutAsync(Guid sessionToken, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default);
    /// <summary>Validates credentials and returns the principal data without issuing a JWT (used by cookie sign-in).</summary>
    Task<Result<AuthenticatedUser>> ValidateAndStartSessionAsync(LoginRequest request, string? ip, string? userAgent, CancellationToken ct = default);
}
