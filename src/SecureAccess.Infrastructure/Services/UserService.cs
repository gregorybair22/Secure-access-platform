using Microsoft.EntityFrameworkCore;
using SecureAccess.Application.Abstractions;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.Users;
using SecureAccess.Domain.Entities;
using SecureAccess.Domain.Enums;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public UserService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default)
    {
        return await _db.Users.AsNoTracking()
            .OrderBy(u => u.UserName)
            .Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                FullName = u.FullName,
                Email = u.Email,
                Status = u.Status,
                LastLoginUtc = u.LastLoginUtc,
                Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
            })
            .ToListAsync(ct);
    }

    public async Task<UserDto?> GetUserAsync(int id, CancellationToken ct = default)
    {
        return await _db.Users.AsNoTracking().Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                FullName = u.FullName,
                Email = u.Email,
                Status = u.Status,
                LastLoginUtc = u.LastLoginUtc,
                Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Result<int>> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName)) return Result<int>.Fail("Username is required.");
        if (request.Password.Length < 8) return Result<int>.Fail("Password must be at least 8 characters.");
        if (await _db.Users.AnyAsync(u => u.UserName == request.UserName, ct))
            return Result<int>.Fail("Username already exists.");

        var user = new User
        {
            UserName = request.UserName.Trim(),
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = EntityStatus.Active
        };
        foreach (var roleId in request.RoleIds.Distinct())
            user.UserRoles.Add(new UserRole { RoleId = roleId });

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditEntry { Action = AuditAction.UserManagement, Details = $"Created user {user.UserName}" }, ct);
        return Result<int>.Success(user.Id);
    }

    public async Task<Result> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return Result.Fail("User not found.");

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (request.NewPassword.Length < 8) return Result.Fail("Password must be at least 8 characters.");
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MustChangePassword = true;
        }

        user.UserRoles.Clear();
        foreach (var roleId in request.RoleIds.Distinct())
            user.UserRoles.Add(new UserRole { RoleId = roleId, UserId = user.Id });

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditEntry { Action = AuditAction.UserManagement, Details = $"Updated user {user.UserName}" }, ct);
        return Result.Success();
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default)
    {
        return await _db.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystem = r.IsSystem,
                Permissions = r.RolePermissions.Select(rp => rp.Permission.Code).ToList()
            })
            .ToListAsync(ct);
    }

    public async Task<Result> UpdateRolePermissionsAsync(int roleId, List<string> permissionCodes, CancellationToken ct = default)
    {
        var role = await _db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Id == roleId, ct);
        if (role is null) return Result.Fail("Role not found.");

        var permissions = await _db.Permissions.Where(p => permissionCodes.Contains(p.Code)).ToListAsync(ct);
        role.RolePermissions.Clear();
        foreach (var p in permissions)
            role.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = p.Id });

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditEntry { Action = AuditAction.UserManagement, Details = $"Updated permissions for role {role.Name}" }, ct);
        return Result.Success();
    }

    public async Task<IReadOnlyList<string>> GetAllPermissionCodesAsync(CancellationToken ct = default)
        => await _db.Permissions.AsNoTracking().OrderBy(p => p.Category).ThenBy(p => p.Code).Select(p => p.Code).ToListAsync(ct);
}
