# Installation Guide

Secure Customer Access and Credential Management Platform

This guide covers installing the platform for **development** and for a **production
single-server** deployment. For the IIS-specific steps see
[02-Deployment-IIS.md](02-Deployment-IIS.md).

---

## 1. Prerequisites

| Component | Version | Notes |
|-----------|---------|-------|
| Windows | Server 2022 (prod) / Windows 10+ (dev) | |
| .NET SDK | **8.0** | Build/dev. `dotnet --list-sdks` should list an 8.0.x SDK. |
| .NET Hosting Bundle | **8.0** (ASP.NET Core) | Production IIS hosting. |
| SQL Server | **2022** (Express/Standard/Enterprise) | LocalDB acceptable for dev. |
| Browser | Any modern (Edge/Chrome/Firefox) | For the Blazor UI. |

Optional tooling:
- `dotnet-ef` global tool for migrations: `dotnet tool install --global dotnet-ef`
- `SqlServer` PowerShell module (for backup scripts): `Install-Module SqlServer`

---

## 2. Solution layout

```
SecureAccess.sln
 ├─ src/SecureAccess.Domain          # Entities + enums (no dependencies)
 ├─ src/SecureAccess.Application     # DTOs, service interfaces, Result<T>
 ├─ src/SecureAccess.Infrastructure  # EF Core, encryption, auth, services, seed
 ├─ src/SecureAccess.Api             # REST API + Swagger + JWT
 └─ src/SecureAccess.Web             # Blazor Server UI (cookie auth)
db/      # SQL scripts (create, schema, upgrade, test data)
docs/    # Manuals (this folder)
scripts/ # Backup / restore PowerShell scripts
```

The API and Web apps share the **same** Application/Infrastructure core, so business
rules and the data model are defined exactly once.

---

## 3. Configuration

Settings live in `appsettings.json` (committed defaults) and are overridden by
`appsettings.Development.json` (dev) or `appsettings.Production.json` /
environment variables (prod). **Never commit production secrets.**

Key sections (see `src/SecureAccess.Api/appsettings.json`):

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string. |
| `Jwt:SigningKey` | HMAC signing key for API tokens (**≥ 32 random chars**). |
| `Jwt:Issuer` / `Jwt:Audience` / `Jwt:AccessTokenMinutes` | Token settings. |
| `Encryption:KeyFilePath` | Path to the AES master key (under `App_Data`). |
| `Encryption:DataProtectionKeysPath` | Data Protection key-ring folder. |
| `Seed:AdminUserName` / `AdminPassword` / `AdminEmail` | Initial admin account. |
| `Security:AllowedIpRanges` | IP allow-list (CIDR). Empty = allow all. |
| `Cors:AllowedOrigins` | Allowed CORS origins for the API. |
| `Session:TimeoutMinutes` (Web) | Cookie session idle timeout. |

Override via environment variables using the `__` separator, e.g.:

```
ConnectionStrings__DefaultConnection=Server=SQL01;Database=SecureAccess;...
Jwt__SigningKey=<64-char-random>
Seed__AdminPassword=<strong-password>
```

---

## 4. Create the database

Two supported approaches:

### 4a. EF Core migrations (recommended)

```powershell
# from the repo root
$env:SECUREACCESS_CONNECTION = "Server=localhost;Database=SecureAccess;Trusted_Connection=True;TrustServerCertificate=True"
dotnet ef database update --project src/SecureAccess.Infrastructure
```

The apps also auto-apply migrations and seed on startup (admin user, roles,
permissions, predefined credential types).

### 4b. SQL scripts (DBA-controlled)

Run, in order, against your SQL Server 2022 instance:

1. `db/01-create-database.sql` — creates the `SecureAccess` database.
2. `db/02-schema-idempotent.sql` — creates all tables/indexes (idempotent).
3. *(optional)* `db/05-test-data.sql` — sample clients & machines.

Future schema changes: generate an idempotent upgrade script (see
`db/03-upgrade-template.sql`).

---

## 5. Run (development)

```powershell
# REST API + Swagger
dotnet run --project src/SecureAccess.Api
#   → https://localhost:xxxx/swagger

# Blazor Server UI
dotnet run --project src/SecureAccess.Web
#   → https://localhost:xxxx/
```

Log in with the seeded administrator account (`Seed:AdminUserName` /
`Seed:AdminPassword`). You will be prompted to change the password on first login.

---

## 6. First-run checklist

- [ ] `Jwt:SigningKey` replaced with a long random value.
- [ ] `Seed:AdminPassword` changed from the default before first launch.
- [ ] HTTPS enforced (dev certs via `dotnet dev-certs https --trust`).
- [ ] `App_Data` folder writable by the app (keys + attachments).
- [ ] Database reachable; migrations applied.

> **Backup note:** Once the app has started once, the `App_Data/keys` folder
> contains the Data Protection key ring that protects all encrypted secrets.
> Back it up together with the database from then on (see
> [06-Backup-and-Restore.md](06-Backup-and-Restore.md)).
