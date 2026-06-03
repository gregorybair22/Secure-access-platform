using SecureAccess.Domain.Common;

namespace SecureAccess.Domain.Entities;

/// <summary>
/// A named set of permissions. The seed data ships the three roles required by
/// the specification: Administrator, Technician and Auditor (read only).
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>System roles cannot be deleted from the UI.</summary>
    public bool IsSystem { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
