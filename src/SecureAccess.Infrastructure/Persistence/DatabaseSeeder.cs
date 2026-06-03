using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureAccess.Application.Common;
using SecureAccess.Domain.Entities;
using SecureAccess.Domain.Enums;
using SecureAccess.Infrastructure.Services;

namespace SecureAccess.Infrastructure.Persistence;

/// <summary>
/// Seeds the permission catalog, the three required roles, the predefined
/// credential types, and an initial administrator account. Idempotent: safe to
/// run on every startup.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config, ILogger logger, CancellationToken ct = default)
    {
        await SeedPermissionsAsync(db, ct);
        await SeedRolesAsync(db, ct);
        await SeedCredentialTypesAsync(db, ct);
        await SeedAdminAsync(db, config, logger, ct);
    }

    private static async Task SeedPermissionsAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = await db.Permissions.Select(p => p.Code).ToListAsync(ct);
        foreach (var (code, meta) in Permissions.All)
        {
            if (existing.Contains(code)) continue;
            db.Permissions.Add(new Permission { Code = code, Name = meta.Name, Category = meta.Category });
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedRolesAsync(AppDbContext db, CancellationToken ct)
    {
        var allPerms = await db.Permissions.ToListAsync(ct);

        // Administrator: everything.
        var adminPerms = allPerms.Select(p => p.Code).ToList();

        // Technician: search, request, view authorized, enter reasons, view reports.
        var techPerms = new[]
        {
            Permissions.ClientsView, Permissions.MachinesView, Permissions.CredentialsView,
            Permissions.CredentialsViewSecret, Permissions.CredentialsRequest,
            Permissions.ReportsView, Permissions.DashboardView
        };

        // Auditor / Read only: view reports, logs, audits; no secret values.
        var auditorPerms = new[]
        {
            Permissions.ClientsView, Permissions.MachinesView, Permissions.CredentialsView,
            Permissions.ReportsView, Permissions.AuditView, Permissions.DashboardView
        };

        await EnsureRole(db, RoleNames.Administrator, "Full system administrator.", adminPerms, allPerms, ct);
        await EnsureRole(db, RoleNames.Technician, "Support technician with credential access.", techPerms, allPerms, ct);
        await EnsureRole(db, RoleNames.Auditor, "Read-only auditor.", auditorPerms, allPerms, ct);
    }

    private static async Task EnsureRole(AppDbContext db, string name, string description,
        IReadOnlyCollection<string> permissionCodes, List<Permission> allPerms, CancellationToken ct)
    {
        var role = await db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Name == name, ct);
        if (role is null)
        {
            role = new Role { Name = name, Description = description, IsSystem = true };
            db.Roles.Add(role);
            await db.SaveChangesAsync(ct);
        }

        var current = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        foreach (var p in allPerms.Where(p => permissionCodes.Contains(p.Code)))
        {
            if (!current.Contains(p.Id))
                role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCredentialTypesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.CredentialTypes.AnyAsync(ct)) return;

        CredentialFieldDefinition F(string key, string label, CredentialFieldType type = CredentialFieldType.Text,
            bool secret = false, bool searchable = false, int order = 0)
            => new() { Key = key, Label = label, Type = type, IsSecret = secret, IsSearchable = searchable, Order = order };

        void Add(string name, string? icon, params CredentialFieldDefinition[] fields)
            => db.CredentialTypes.Add(new CredentialType
            {
                Name = name,
                Icon = icon,
                IsSystem = true,
                IsActive = true,
                FieldsJson = Json.Serialize(fields.ToList())
            });

        Add("RustDesk", "desktop",
            F("rustdesk_id", "RustDesk ID", searchable: true, order: 1),
            F("username", "Username", searchable: true, order: 2),
            F("password", "Password", CredentialFieldType.Password, secret: true, order: 3),
            F("notes", "Notes", CredentialFieldType.MultiLine, order: 4));

        Add("AnyDesk", "desktop",
            F("anydesk_id", "AnyDesk ID", searchable: true, order: 1),
            F("username", "Username", searchable: true, order: 2),
            F("password", "Password", CredentialFieldType.Password, secret: true, order: 3),
            F("notes", "Notes", CredentialFieldType.MultiLine, order: 4));

        Add("Remote Desktop", "remote",
            F("ip_address", "IP Address", searchable: true, order: 1),
            F("hostname", "Hostname", searchable: true, order: 2),
            F("port", "Port", CredentialFieldType.Number, order: 3),
            F("domain", "Domain", searchable: true, order: 4),
            F("username", "Username", searchable: true, order: 5),
            F("password", "Password", CredentialFieldType.Password, secret: true, order: 6),
            F("notes", "Notes", CredentialFieldType.MultiLine, order: 7));

        Add("VPN", "vpn",
            F("vpn_type", "VPN Type", searchable: true, order: 1),
            F("vpn_software", "VPN Software", searchable: true, order: 2),
            F("vpn_server", "VPN Server", searchable: true, order: 3),
            F("username", "Username", searchable: true, order: 4),
            F("password", "Password", CredentialFieldType.Password, secret: true, order: 5),
            F("connection_instructions", "Connection Instructions", CredentialFieldType.MultiLine, order: 6),
            F("notes", "Notes", CredentialFieldType.MultiLine, order: 7));

        // A few of the additional predefined types from the specification.
        foreach (var name in new[] { "Windows Account", "Administrator Account", "Database Account",
            "SQL Server Account", "Router Credentials", "Firewall Credentials", "PLC Access",
            "HMI Access", "SSH Access", "FTP Access", "Web Portal" })
        {
            Add(name, "key",
                F("host", "Host / Address", searchable: true, order: 1),
                F("username", "Username", searchable: true, order: 2),
                F("password", "Password", CredentialFieldType.Password, secret: true, order: 3),
                F("notes", "Notes", CredentialFieldType.MultiLine, order: 4));
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedAdminAsync(AppDbContext db, IConfiguration config, ILogger logger, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct)) return;

        var userName = config["Seed:AdminUserName"] ?? "admin";
        var password = config["Seed:AdminPassword"] ?? "ChangeMe!2026";
        var email = config["Seed:AdminEmail"] ?? "admin@local";

        var adminRole = await db.Roles.FirstAsync(r => r.Name == RoleNames.Administrator, ct);
        var admin = new User
        {
            UserName = userName,
            FullName = "System Administrator",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Status = EntityStatus.Active,
            MustChangePassword = true
        };
        admin.UserRoles.Add(new UserRole { Role = adminRole });
        db.Users.Add(admin);
        await db.SaveChangesAsync(ct);

        logger.LogWarning("Seeded administrator account '{User}'. Default password must be changed on first login.", userName);
    }
}
