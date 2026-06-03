using Microsoft.EntityFrameworkCore;
using SecureAccess.Application.Abstractions;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Auth;
using SecureAccess.Domain.Entities;
using SecureAccess.Domain.Enums;
using SecureAccess.Infrastructure.Persistence;
using SecureAccess.Infrastructure.Security;

namespace SecureAccess.Infrastructure.Services;

public class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly AppDbContext _db;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IAuditService _audit;

    public AuthService(AppDbContext db, IJwtTokenGenerator jwt, IAuditService audit)
    {
        _db = db;
        _jwt = jwt;
        _audit = audit;
    }

    public async Task<Result<LoginResult>> LoginAsync(LoginRequest request, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var validated = await ValidateAndStartSessionAsync(request, ip, userAgent, ct);
        if (!validated.Succeeded) return Result<LoginResult>.Fail(validated.Error!);

        var (token, expires) = _jwt.Create(validated.Value!);
        return Result<LoginResult>.Success(new LoginResult
        {
            User = validated.Value!,
            Token = token,
            ExpiresAtUtc = expires
        });
    }

    public async Task<Result<AuthenticatedUser>> ValidateAndStartSessionAsync(LoginRequest request, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.UserName == request.UserName, ct);

        if (user is null)
        {
            await _audit.LogLoginAsync(null, request.UserName, AuditResult.Failure, ip, userAgent, "Unknown user", ct);
            return Result<AuthenticatedUser>.Fail("Invalid username or password.");
        }

        if (user.Status != EntityStatus.Active)
        {
            await _audit.LogLoginAsync(user.Id, user.UserName, AuditResult.Denied, ip, userAgent, "Account not active", ct);
            return Result<AuthenticatedUser>.Fail("Account is not active.");
        }

        if (user.LockedOutUntilUtc.HasValue && user.LockedOutUntilUtc > DateTime.UtcNow)
        {
            await _audit.LogLoginAsync(user.Id, user.UserName, AuditResult.Denied, ip, userAgent, "Locked out", ct);
            return Result<AuthenticatedUser>.Fail("Account is temporarily locked. Try again later.");
        }

        var ok = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!ok)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
                user.LockedOutUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
            await _db.SaveChangesAsync(ct);
            await _audit.LogLoginAsync(user.Id, user.UserName, AuditResult.Failure, ip, userAgent, "Bad password", ct);
            return Result<AuthenticatedUser>.Fail("Invalid username or password.");
        }

        // Successful authentication: reset counters and open a session.
        user.FailedLoginAttempts = 0;
        user.LockedOutUntilUtc = null;
        user.LastLoginUtc = DateTime.UtcNow;

        var session = new Session
        {
            UserId = user.Id,
            LoginUtc = DateTime.UtcNow,
            IpAddress = ip,
            UserAgent = userAgent,
            IsActive = true
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);

        await _audit.LogLoginAsync(user.Id, user.UserName, AuditResult.Success, ip, userAgent, null, ct);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        return Result<AuthenticatedUser>.Success(new AuthenticatedUser
        {
            UserId = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            SessionToken = session.SessionToken,
            Roles = roles,
            Permissions = permissions,
            MustChangePassword = user.MustChangePassword
        });
    }

    public async Task LogoutAsync(Guid sessionToken, CancellationToken ct = default)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive, ct);
        if (session is null) return;

        session.LogoutUtc = DateTime.UtcNow;
        session.IsActive = false;
        await _db.SaveChangesAsync(ct);

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = session.UserId,
            Action = AuditAction.Logout,
            Result = AuditResult.Success,
            TimestampUtc = DateTime.UtcNow,
            SessionId = sessionToken
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user is null) return Result.Fail("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Fail("Current password is incorrect.");

        if (request.NewPassword.Length < 8)
            return Result.Fail("New password must be at least 8 characters.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
