using SecureAccess.Application.Common;
using SecureAccess.Domain.Enums;

namespace SecureAccess.Application.Features.Users;

public class UserDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public EntityStatus Status { get; set; }
    public DateTime? LastLoginUtc { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<int> RoleIds { get; set; } = new();
}

public class UpdateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public EntityStatus Status { get; set; }
    public List<int> RoleIds { get; set; } = new();
    /// <summary>If provided, resets the password.</summary>
    public string? NewPassword { get; set; }
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default);
    Task<UserDto?> GetUserAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<Result> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default);
    Task<Result> UpdateRolePermissionsAsync(int roleId, List<string> permissionCodes, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAllPermissionCodesAsync(CancellationToken ct = default);
}
