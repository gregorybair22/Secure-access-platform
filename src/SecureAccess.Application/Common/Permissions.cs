namespace SecureAccess.Application.Common;

/// <summary>
/// Canonical permission codes. Centralized so controllers, Blazor components and
/// the seed data all reference the same strings.
/// </summary>
public static class Permissions
{
    public const string ClientsView = "clients.view";
    public const string ClientsCreate = "clients.create";
    public const string ClientsEdit = "clients.edit";
    public const string ClientsDelete = "clients.delete";

    public const string MachinesView = "machines.view";
    public const string MachinesCreate = "machines.create";
    public const string MachinesEdit = "machines.edit";
    public const string MachinesDelete = "machines.delete";

    public const string CredentialsView = "credentials.view";
    public const string CredentialsViewSecret = "credentials.view_secret";
    public const string CredentialsCreate = "credentials.create";
    public const string CredentialsEdit = "credentials.edit";
    public const string CredentialsDelete = "credentials.delete";
    public const string CredentialsRequest = "credentials.request";

    public const string TemplatesManage = "templates.manage";
    public const string BulkOperations = "bulk.operations";

    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";

    public const string ReportsView = "reports.view";
    public const string AuditView = "audit.view";

    public const string ImportData = "data.import";
    public const string ExportData = "data.export";

    public const string DashboardView = "dashboard.view";

    /// <summary>Every permission, used when seeding the Administrator role.</summary>
    public static readonly IReadOnlyDictionary<string, (string Name, string Category)> All =
        new Dictionary<string, (string, string)>
        {
            [ClientsView] = ("View Clients", "Clients"),
            [ClientsCreate] = ("Create Clients", "Clients"),
            [ClientsEdit] = ("Edit Clients", "Clients"),
            [ClientsDelete] = ("Delete Clients", "Clients"),
            [MachinesView] = ("View Machines", "Machines"),
            [MachinesCreate] = ("Create Machines", "Machines"),
            [MachinesEdit] = ("Edit Machines", "Machines"),
            [MachinesDelete] = ("Delete Machines", "Machines"),
            [CredentialsView] = ("View Credentials", "Credentials"),
            [CredentialsViewSecret] = ("View Secret Credential Values", "Credentials"),
            [CredentialsCreate] = ("Create Credentials", "Credentials"),
            [CredentialsEdit] = ("Edit Credentials", "Credentials"),
            [CredentialsDelete] = ("Delete Credentials", "Credentials"),
            [CredentialsRequest] = ("Request Credentials", "Credentials"),
            [TemplatesManage] = ("Manage Templates", "Templates"),
            [BulkOperations] = ("Execute Bulk Operations", "Bulk"),
            [UsersManage] = ("Manage Users", "Administration"),
            [RolesManage] = ("Manage Roles and Permissions", "Administration"),
            [ReportsView] = ("View Reports", "Reporting"),
            [AuditView] = ("View Audit Logs", "Reporting"),
            [ImportData] = ("Import Data", "Data"),
            [ExportData] = ("Export Data", "Data"),
            [DashboardView] = ("View Dashboard", "Dashboard"),
        };
}

/// <summary>Canonical role names matching the specification.</summary>
public static class RoleNames
{
    public const string Administrator = "Administrator";
    public const string Technician = "Technician";
    public const string Auditor = "Auditor";
}
