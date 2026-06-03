IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [AccessRequests] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ClientId] int NULL,
        [MachineId] int NULL,
        [ReasonCategory] int NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [InternalTicket] nvarchar(100) NOT NULL,
        [CustomerTicket] nvarchar(100) NULL,
        [IpAddress] nvarchar(max) NULL,
        [SessionId] uniqueidentifier NULL,
        [RequestedAtUtc] datetime2 NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_AccessRequests] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [Clients] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [CustomerCode] nvarchar(50) NOT NULL,
        [Address] nvarchar(500) NULL,
        [ContactPerson] nvarchar(200) NULL,
        [PhoneNumber] nvarchar(50) NULL,
        [Email] nvarchar(256) NULL,
        [Notes] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [IsRestricted] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_Clients] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [CredentialTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Icon] nvarchar(max) NULL,
        [IsSystem] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [FieldsJson] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_CredentialTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [Permissions] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(100) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Category] nvarchar(100) NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsSystem] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [UserName] nvarchar(100) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [FullName] nvarchar(200) NOT NULL,
        [PasswordHash] nvarchar(256) NOT NULL,
        [Status] int NOT NULL,
        [IsDomainAccount] bit NOT NULL,
        [LastLoginUtc] datetime2 NULL,
        [FailedLoginAttempts] int NOT NULL,
        [LockedOutUntilUtc] datetime2 NULL,
        [MustChangePassword] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [Machines] (
        [Id] int NOT NULL IDENTITY,
        [ClientId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [SerialNumber] nvarchar(100) NULL,
        [Model] nvarchar(150) NULL,
        [Location] nvarchar(200) NULL,
        [Status] int NOT NULL,
        [InstallationDate] datetime2 NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_Machines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Machines_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [CredentialTemplates] (
        [Id] int NOT NULL IDENTITY,
        [CredentialTypeId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NULL,
        [PlainData] nvarchar(max) NOT NULL,
        [SecretData] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_CredentialTemplates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CredentialTemplates_CredentialTypes_CredentialTypeId] FOREIGN KEY ([CredentialTypeId]) REFERENCES [CredentialTypes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [RolePermissions] (
        [RoleId] int NOT NULL,
        [PermissionId] int NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
        CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] int NULL,
        [UserName] nvarchar(100) NULL,
        [Action] int NOT NULL,
        [Result] int NOT NULL,
        [TimestampUtc] datetime2 NOT NULL,
        [IpAddress] nvarchar(64) NULL,
        [SessionId] uniqueidentifier NULL,
        [ClientId] int NULL,
        [ClientName] nvarchar(200) NULL,
        [MachineId] int NULL,
        [MachineName] nvarchar(200) NULL,
        [CredentialId] int NULL,
        [CredentialType] nvarchar(150) NULL,
        [Reason] nvarchar(1000) NULL,
        [InternalTicket] nvarchar(100) NULL,
        [CustomerTicket] nvarchar(100) NULL,
        [Details] nvarchar(max) NULL,
        [IsArchived] bit NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [ChangeLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] int NOT NULL,
        [FieldName] nvarchar(150) NOT NULL,
        [PreviousValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        [ModifiedByUserId] int NULL,
        [ModifiedByUserName] nvarchar(100) NULL,
        [ModifiedAtUtc] datetime2 NOT NULL,
        [Reason] nvarchar(max) NULL,
        [IsArchived] bit NOT NULL,
        CONSTRAINT [PK_ChangeLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChangeLogs_Users_ModifiedByUserId] FOREIGN KEY ([ModifiedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [LoginLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] int NULL,
        [UserName] nvarchar(100) NOT NULL,
        [TimestampUtc] datetime2 NOT NULL,
        [Result] int NOT NULL,
        [IpAddress] nvarchar(64) NULL,
        [UserAgent] nvarchar(500) NULL,
        [FailureReason] nvarchar(300) NULL,
        [IsArchived] bit NOT NULL,
        CONSTRAINT [PK_LoginLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LoginLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [Sessions] (
        [Id] int NOT NULL IDENTITY,
        [SessionToken] uniqueidentifier NOT NULL,
        [UserId] int NOT NULL,
        [LoginUtc] datetime2 NOT NULL,
        [LogoutUtc] datetime2 NULL,
        [IpAddress] nvarchar(64) NULL,
        [UserAgent] nvarchar(500) NULL,
        [MachinesAccessed] int NOT NULL,
        [CustomersAccessed] int NOT NULL,
        [CredentialsViewed] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_Sessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Sessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [Credentials] (
        [Id] int NOT NULL IDENTITY,
        [CredentialTypeId] int NOT NULL,
        [Scope] int NOT NULL,
        [ClientId] int NULL,
        [MachineId] int NULL,
        [Label] nvarchar(200) NOT NULL,
        [PlainData] nvarchar(max) NOT NULL,
        [SecretData] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_Credentials] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Credentials_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Credentials_CredentialTypes_CredentialTypeId] FOREIGN KEY ([CredentialTypeId]) REFERENCES [CredentialTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Credentials_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Machines] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE TABLE [Attachments] (
        [Id] int NOT NULL IDENTITY,
        [CredentialId] int NOT NULL,
        [FileName] nvarchar(260) NOT NULL,
        [ContentType] nvarchar(150) NOT NULL,
        [SizeBytes] bigint NOT NULL,
        [StoragePath] nvarchar(500) NOT NULL,
        [Sha256] nvarchar(100) NULL,
        [IsEncrypted] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] int NULL,
        CONSTRAINT [PK_Attachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Attachments_Credentials_CredentialId] FOREIGN KEY ([CredentialId]) REFERENCES [Credentials] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AccessRequests_ReasonCategory] ON [AccessRequests] ([ReasonCategory]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AccessRequests_RequestedAtUtc] ON [AccessRequests] ([RequestedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AccessRequests_UserId] ON [AccessRequests] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Attachments_CredentialId] ON [Attachments] ([CredentialId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Action] ON [AuditLogs] ([Action]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_TimestampUtc] ON [AuditLogs] ([TimestampUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChangeLogs_EntityType_EntityId] ON [ChangeLogs] ([EntityType], [EntityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChangeLogs_ModifiedAtUtc] ON [ChangeLogs] ([ModifiedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChangeLogs_ModifiedByUserId] ON [ChangeLogs] ([ModifiedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Clients_CustomerCode] ON [Clients] ([CustomerCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Clients_Name] ON [Clients] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Credentials_ClientId] ON [Credentials] ([ClientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Credentials_CredentialTypeId] ON [Credentials] ([CredentialTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Credentials_MachineId] ON [Credentials] ([MachineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Credentials_Scope_ClientId_MachineId] ON [Credentials] ([Scope], [ClientId], [MachineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CredentialTemplates_CredentialTypeId] ON [CredentialTemplates] ([CredentialTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CredentialTypes_Name] ON [CredentialTypes] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LoginLogs_TimestampUtc] ON [LoginLogs] ([TimestampUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LoginLogs_UserId] ON [LoginLogs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Machines_ClientId] ON [Machines] ([ClientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Machines_Name] ON [Machines] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Machines_SerialNumber] ON [Machines] ([SerialNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_Code] ON [Permissions] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Sessions_SessionToken] ON [Sessions] ([SessionToken]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Sessions_UserId] ON [Sessions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_UserName] ON [Users] ([UserName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603064018_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260603064018_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

