namespace SecureAccess.Domain.Enums;

/// <summary>Lifecycle status shared by users, clients and machines.</summary>
public enum EntityStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    Archived = 4
}

/// <summary>
/// Credential inheritance level. Machine overrides Client overrides Global
/// when credentials are resolved for a given machine.
/// </summary>
public enum CredentialScope
{
    Global = 1,
    Client = 2,
    Machine = 3
}

/// <summary>Data type of a single field inside a credential type schema.</summary>
public enum CredentialFieldType
{
    Text = 1,
    Password = 2,
    MultiLine = 3,
    Number = 4,
    Url = 5,
    File = 6
}

/// <summary>Outcome recorded on an audit log entry.</summary>
public enum AuditResult
{
    Success = 1,
    Denied = 2,
    Failure = 3
}

/// <summary>High level category of an audited action.</summary>
public enum AuditAction
{
    Login = 1,
    Logout = 2,
    LoginFailed = 3,
    CredentialAccess = 4,
    CredentialCreate = 5,
    CredentialModify = 6,
    CredentialDelete = 7,
    BulkUpdate = 8,
    CopyOperation = 9,
    UserManagement = 10,
    ClientCreate = 11,
    ClientModify = 12,
    ClientDelete = 13,
    MachineCreate = 14,
    MachineModify = 15,
    MachineDelete = 16,
    Import = 17,
    Export = 18,
    UnauthorizedAccess = 19
}

/// <summary>Standard reasons offered for a credential access request (spec Reason Analysis report).</summary>
public enum AccessReasonCategory
{
    SoftwareUpdate = 1,
    PreventiveMaintenance = 2,
    CorrectiveMaintenance = 3,
    Installation = 4,
    Training = 5,
    Diagnostics = 6,
    EmergencySupport = 7,
    Other = 8
}
