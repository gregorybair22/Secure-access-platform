# Secure Customer Access and Credential Management Platform

A secure internal platform to centralize, manage, audit, and control all
customer-support credentials and connection methods. Built as a **REST API** plus a
**Blazor Server** web application on a shared application/infrastructure core.

> **Ownership:** 100% of the source code is company-owned. All third-party libraries
> are free/open-source with no ongoing license fees (see [Licensing](#licensing)).

---

## Stack

- **.NET 8** (`net8.0`), ASP.NET Core 8
- **REST API** with **JWT** auth and **Swagger/OpenAPI**
- **Blazor Server** UI with cookie auth and permission-aware navigation
- **Entity Framework Core** + **SQL Server 2022**
- **AES-256-GCM** column-level encryption, keys protected by ASP.NET Core Data Protection
- **Serilog** structured logging
- Deploys to **IIS on Windows Server 2022**

## Architecture

```
Browser ──► Blazor Server (SecureAccess.Web, cookie auth)
Automation ─► REST API (SecureAccess.Api, JWT + Swagger)
                     │
                     ▼
        Application layer (DTOs, service interfaces)
                     │
                     ▼
   Infrastructure (EF Core, encryption, auth, audit, services)
                     │
        ┌────────────┴────────────┐
        ▼                         ▼
 SQL Server 2022           App_Data (attachments + key ring)
```

Projects:
| Project | Responsibility |
|---------|----------------|
| `src/SecureAccess.Domain` | Entities + enums (14 tables). |
| `src/SecureAccess.Application` | DTOs, service interfaces, `Result<T>`. |
| `src/SecureAccess.Infrastructure` | EF Core, encryption, auth, audit, seeding, services. |
| `src/SecureAccess.Api` | REST API, Swagger, JWT, IP allow-list. |
| `src/SecureAccess.Web` | Blazor Server UI. |

## Features

- **Clients & machines** management with status and a restricted-client flag.
- **Credential types**: predefined + **unlimited custom**, with per-field secret/
  searchable/required schema.
- **Credentials** with **Global → Client → Machine** inheritance and override; secret
  fields encrypted at rest, non-secret fields searchable; encrypted **attachments**.
- **Credential request workflow**: mandatory reason + internal ticket (optional
  customer ticket) gate every reveal; persisted as an access request + audit entry.
- **Auditing**: append-only audit logs, change history (previous/new value), login
  logs, and session tracking — 5-year retention.
- **Bulk operations** (copy machine→machines, client→clients; bulk update with
  fill-empty/overwrite) and **templates**.
- **Seven reports** (Credential Access, Technician Activity, Customer Access, Machine
  Access, Credential Usage, Reason Analysis, Change History) + **dashboard**.
- **Global search** across customer, code, machine, serial, RustDesk/AnyDesk ID, VPN
  server, username, IP, and notes (non-secret only).
- **Import/Export**: XLSX/CSV import; XLSX/CSV/PDF export.
- **Security**: roles + granular permissions, IP allow-list, HTTPS/HSTS, secure
  sessions, BCrypt password hashing.

## Quick start (development)

```powershell
# 1) create / update the database (EF migrations)
$env:SECUREACCESS_CONNECTION = "Server=localhost;Database=SecureAccess;Trusted_Connection=True;TrustServerCertificate=True"
dotnet ef database update --project src/SecureAccess.Infrastructure

# 2) run the API (Swagger at /swagger)
dotnet run --project src/SecureAccess.Api

# 3) run the Blazor UI
dotnet run --project src/SecureAccess.Web
```

Log in with the seeded admin (`Seed:AdminUserName` / `Seed:AdminPassword` in
`appsettings`); you'll be asked to change the password on first login.

> The apps also auto-apply migrations and seed roles, permissions, predefined
> credential types, and the admin account on startup.

## Database scripts (`db/`)

| File | Purpose |
|------|---------|
| `01-create-database.sql` | Create the `SecureAccess` database. |
| `02-schema-idempotent.sql` | Create all tables/indexes (idempotent). |
| `03-upgrade-template.sql` | Template/workflow for future migrations. |
| `05-test-data.sql` | Sample clients & machines for UAT. |

## Documentation (`docs/`)

| Doc | Audience |
|-----|----------|
| [01 Installation Guide](docs/01-Installation-Guide.md) | Ops / dev |
| [02 Deployment — IIS](docs/02-Deployment-IIS.md) | Ops |
| [03 Administrator Manual](docs/03-Administrator-Manual.md) | Admins |
| [04 User Manual](docs/04-User-Manual.md) | Technicians |
| [05 REST API Documentation](docs/05-API-Documentation.md) | Integrators |
| [06 Backup & Restore](docs/06-Backup-and-Restore.md) | Ops / DBA |
| [07 Encryption Architecture](docs/07-Encryption-Architecture.md) | Security |
| [08 Security Review Checklist](docs/08-Security-Review-Checklist.md) | Security |

## Backup (`scripts/`)

`Backup-Database.ps1`, `Restore-Database.ps1`, `Backup-Attachments.ps1`,
`Restore-Attachments.ps1`. **Always back up the database and `App_Data` together** —
the Data Protection key ring in `App_Data/keys` is required to decrypt secrets.

## Licensing

| Library | Use | License |
|---------|-----|---------|
| Serilog | Logging | Apache-2.0 |
| BCrypt.Net-Next | Password hashing | MIT |
| ClosedXML | XLSX import/export | MIT |
| CsvHelper | CSV import/export | MS-PL / Apache-2.0 |
| QuestPDF | PDF export | Community (MIT for qualifying use) |
| Microsoft.Data.SqlClient / EF Core | Data access | MIT |

No component requires an ongoing license fee for the intended use.

## Notes for local SQL Server LocalDB

On some Windows machines, the **native** SQL client (SNI) crashes with
`SEHException` when connecting to `(localdb)\MSSQLLocalDB`. The API and Web hosts
call `SqlClientWindowsBootstrap.Initialize()` at startup to enable **managed
networking** on Windows, which avoids that native code path. Development
`appsettings.Development.json` also sets `Encrypt=False` for LocalDB.

If you still have connection issues, ensure LocalDB is running
(`sqllocaldb start MSSQLLocalDB`) or point `ConnectionStrings:DefaultConnection` at
a full SQL Server / Express instance (`Server=localhost;...`).
