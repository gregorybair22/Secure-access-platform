namespace SecureAccess.Domain.Entities;

/// <summary>Join entity linking a <see cref="Role"/> to a <see cref="Permission"/>.</summary>
public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
