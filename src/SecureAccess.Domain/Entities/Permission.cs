using SecureAccess.Domain.Common;

namespace SecureAccess.Domain.Entities;

/// <summary>
/// A granular capability (for example "credentials.view" or "clients.delete")
/// that can be granted to roles. Permission checks back the role-based model.
/// </summary>
public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
