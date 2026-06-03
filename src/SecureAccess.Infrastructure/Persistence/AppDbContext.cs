using Microsoft.EntityFrameworkCore;
using SecureAccess.Domain.Entities;

namespace SecureAccess.Infrastructure.Persistence;

/// <summary>
/// EF Core context exposing the full schema (the 14+ required tables).
/// Relationships, indexes and delete behaviors are configured here.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<CredentialType> CredentialTypes => Set<CredentialType>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<CredentialTemplate> CredentialTemplates => Set<CredentialTemplate>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ChangeLog> ChangeLogs => Set<ChangeLog>();
    public DbSet<LoginLog> LoginLogs => Set<LoginLog>();
    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Identity ----------------------------------------------------------
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.UserName).IsUnique();
            e.HasIndex(x => x.Email);
            e.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.PasswordHash).HasMaxLength(256);
        });

        b.Entity<Role>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        b.Entity<Permission>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.Category).HasMaxLength(100);
        });

        b.Entity<UserRole>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RolePermission>(e =>
        {
            e.HasKey(x => new { x.RoleId, x.PermissionId });
            e.HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        // Clients & machines ------------------------------------------------
        b.Entity<Client>(e =>
        {
            e.HasIndex(x => x.CustomerCode).IsUnique();
            e.HasIndex(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.CustomerCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.ContactPerson).HasMaxLength(200);
            e.Property(x => x.PhoneNumber).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(256);
        });

        b.Entity<Machine>(e =>
        {
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.SerialNumber);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.SerialNumber).HasMaxLength(100);
            e.Property(x => x.Model).HasMaxLength(150);
            e.Property(x => x.Location).HasMaxLength(200);
            e.HasOne(x => x.Client).WithMany(c => c.Machines).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
        });

        // Credentials -------------------------------------------------------
        b.Entity<CredentialType>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.FieldsJson).IsRequired();
        });

        b.Entity<Credential>(e =>
        {
            e.Property(x => x.Label).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.CredentialType).WithMany(t => t.Credentials).HasForeignKey(x => x.CredentialTypeId).OnDelete(DeleteBehavior.Restrict);
            // Client-level credentials use Restrict to avoid multiple cascade paths
            // (Client -> Machine -> Credential is the cascade path); client-level
            // credentials are removed explicitly in ClientService before client deletion.
            e.HasOne(x => x.Client).WithMany(c => c.Credentials).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Machine).WithMany(m => m.Credentials).HasForeignKey(x => x.MachineId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.Scope, x.ClientId, x.MachineId });
        });

        b.Entity<CredentialTemplate>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.CredentialType).WithMany(t => t.Templates).HasForeignKey(x => x.CredentialTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Attachment>(e =>
        {
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(150);
            e.Property(x => x.StoragePath).HasMaxLength(500);
            e.Property(x => x.Sha256).HasMaxLength(100);
            e.HasOne(x => x.Credential).WithMany(c => c.Attachments).HasForeignKey(x => x.CredentialId).OnDelete(DeleteBehavior.Cascade);
        });

        // Workflow & audit --------------------------------------------------
        b.Entity<AccessRequest>(e =>
        {
            e.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            e.Property(x => x.InternalTicket).HasMaxLength(100).IsRequired();
            e.Property(x => x.CustomerTicket).HasMaxLength(100);
            e.HasIndex(x => x.RequestedAtUtc);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.ReasonCategory);
        });

        b.Entity<AuditLog>(e =>
        {
            e.Property(x => x.UserName).HasMaxLength(100);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.ClientName).HasMaxLength(200);
            e.Property(x => x.MachineName).HasMaxLength(200);
            e.Property(x => x.CredentialType).HasMaxLength(150);
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.Property(x => x.InternalTicket).HasMaxLength(100);
            e.Property(x => x.CustomerTicket).HasMaxLength(100);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.TimestampUtc);
            e.HasIndex(x => x.Action);
        });

        b.Entity<ChangeLog>(e =>
        {
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.FieldName).HasMaxLength(150).IsRequired();
            e.Property(x => x.ModifiedByUserName).HasMaxLength(100);
            e.HasOne(x => x.ModifiedByUser).WithMany().HasForeignKey(x => x.ModifiedByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.ModifiedAtUtc);
        });

        b.Entity<LoginLog>(e =>
        {
            e.Property(x => x.UserName).HasMaxLength(100);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.Property(x => x.FailureReason).HasMaxLength(300);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.TimestampUtc);
        });

        b.Entity<Session>(e =>
        {
            e.HasIndex(x => x.SessionToken).IsUnique();
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.Ignore(x => x.Duration);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
