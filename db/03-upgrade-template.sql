/* ============================================================================
   Secure Customer Access and Credential Management Platform
   03 - Upgrade / Migration Script Template
   ----------------------------------------------------------------------------
   For schema upgrades between releases, regenerate an idempotent migration
   script that contains ONLY the new migrations using EF Core, for example:

       dotnet ef migrations script <FromMigration> <ToMigration> ^
           --idempotent ^
           --project src/SecureAccess.Infrastructure ^
           -o db/upgrade-<from>-to-<to>.sql

   The idempotent scripts inspect [__EFMigrationsHistory] and apply only the
   migrations that have not yet run, so they are safe to execute repeatedly.

   Audit, change, login and session tables are append-only. Upgrade scripts
   MUST NOT drop or truncate them (5-year minimum retention requirement).
   ============================================================================ */

USE [SecureAccess];
GO

-- Example guard pattern for a manual data fix-up between releases:
-- IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = N'NewColumn' AND object_id = OBJECT_ID(N'dbo.Clients'))
-- BEGIN
--     ALTER TABLE dbo.Clients ADD NewColumn NVARCHAR(100) NULL;
-- END
-- GO
