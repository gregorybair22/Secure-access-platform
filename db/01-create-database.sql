/* ============================================================================
   Secure Customer Access and Credential Management Platform
   01 - Create Database (SQL Server 2022)
   ----------------------------------------------------------------------------
   Run this first (as a login with CREATE DATABASE rights), then run
   02-schema-idempotent.sql against the new database to create all tables.
   ============================================================================ */

IF DB_ID(N'SecureAccess') IS NULL
BEGIN
    PRINT 'Creating database [SecureAccess]...';
    CREATE DATABASE [SecureAccess];
END
ELSE
    PRINT 'Database [SecureAccess] already exists.';
GO

ALTER DATABASE [SecureAccess] SET RECOVERY FULL;
GO

/* Recommended: a dedicated least-privilege SQL login for the application.
   Replace the password before running in production.

USE [master];
GO
IF SUSER_ID(N'secureaccess_app') IS NULL
    CREATE LOGIN [secureaccess_app] WITH PASSWORD = N'REPLACE_WITH_STRONG_PASSWORD', CHECK_POLICY = ON;
GO
USE [SecureAccess];
GO
IF USER_ID(N'secureaccess_app') IS NULL
    CREATE USER [secureaccess_app] FOR LOGIN [secureaccess_app];
GO
ALTER ROLE [db_datareader] ADD MEMBER [secureaccess_app];
ALTER ROLE [db_datawriter] ADD MEMBER [secureaccess_app];
GO
*/
