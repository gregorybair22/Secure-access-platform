# Deployment Guide — IIS on Windows Server 2022

Secure Customer Access and Credential Management Platform

This guide deploys the **REST API** (`SecureAccess.Api`) and the **Blazor Server UI**
(`SecureAccess.Web`) to IIS on Windows Server 2022, backed by SQL Server 2022.

---

## 1. Server prerequisites

1. **IIS** with the *Web Server (IIS)* role, including:
   - Web Server → Common HTTP Features
   - Web Server → Security → Request Filtering, Windows Authentication (optional)
   - Web Server → Application Development → WebSocket Protocol (required for Blazor Server)
2. **.NET 8 ASP.NET Core Hosting Bundle** — install from Microsoft, then `iisreset`.
   (Installs the ASP.NET Core Module v2 used by IIS to host .NET apps.)
3. **SQL Server 2022** reachable from the web server.
4. A **TLS certificate** for the public hostname.

Verify the hosting bundle:

```powershell
dotnet --info        # runtime present
Get-WebGlobalModule | Where-Object Name -like "AspNetCoreModuleV2"
```

---

## 2. Publish the apps

From a build machine (or the server) with the .NET 8 SDK:

```powershell
dotnet publish src/SecureAccess.Api -c Release -o C:\inetpub\SecureAccess.Api
dotnet publish src/SecureAccess.Web -c Release -o C:\inetpub\SecureAccess.Web
```

Copy `scripts/` (backup/restore) somewhere on the server, e.g. `C:\inetpub\SecureAccess\scripts`.

---

## 3. Application pools

Create one **No Managed Code** app pool per app (the ASP.NET Core Module hosts the
app out-of-process or in-process; "No Managed Code" is correct):

```powershell
Import-Module WebAdministration
New-WebAppPool -Name "SecureAccessApi"
Set-ItemProperty IIS:\AppPools\SecureAccessApi managedRuntimeVersion ""
New-WebAppPool -Name "SecureAccessWeb"
Set-ItemProperty IIS:\AppPools\SecureAccessWeb managedRuntimeVersion ""
```

Recommended: run each pool under a dedicated **gMSA** or low-privilege service
account that has:
- `db_datareader` + `db_datawriter` (and `db_ddladmin` only if you let the app run
  migrations at startup) on the `SecureAccess` database, and
- **Modify** NTFS rights on each app's `App_Data` folder.

---

## 4. Sites / applications & bindings

```powershell
New-Website -Name "SecureAccess API" -PhysicalPath "C:\inetpub\SecureAccess.Api" `
    -ApplicationPool "SecureAccessApi" -Port 8443 -Ssl
New-Website -Name "SecureAccess Web" -PhysicalPath "C:\inetpub\SecureAccess.Web" `
    -ApplicationPool "SecureAccessWeb" -HostHeader "access.yourcompany.com" -Port 443 -Ssl
```

Bind the TLS certificate to each HTTPS binding (IIS Manager → Site → Bindings, or
`New-Item IIS:\SslBindings\...`). Force HTTPS (the apps also send HSTS in production).

> **Blazor Server requires WebSockets and sticky sessions.** If load-balanced, enable
> session affinity (ARR affinity) or use a single node; the SignalR circuit is
> stateful per connection.

---

## 5. Production configuration

Place an `appsettings.Production.json` next to each app's DLL **or** set environment
variables on the app pool. Minimum production settings:

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SQL01;Database=SecureAccess;User Id=svc_secureaccess;Password=...;TrustServerCertificate=True;Encrypt=True"
  },
  "Jwt": { "SigningKey": "<64+ char random secret>" },
  "Seed": { "AdminPassword": "<strong one-time password>" },
  "Security": { "AllowedIpRanges": [ "10.0.0.0/8", "192.168.0.0/16" ] },
  "Cors": { "AllowedOrigins": [ "https://access.yourcompany.com" ] }
}
```

Set the environment to Production (per app pool):

```powershell
# environment variable on the app pool
$pool = "IIS:\AppPools\SecureAccessWeb"
Set-WebConfigurationProperty -pspath $pool -filter "add[@name='ASPNETCORE_ENVIRONMENT']" -name "value" -value "Production"
```

(Or add `<environmentVariables>` under `<aspNetCore>` in each app's `web.config`,
which `dotnet publish` generates.)

---

## 6. Data Protection key ring (important)

The apps store the Data Protection key ring under `App_Data/keys/dp`. On a single
server this works out of the box. Ensure:

- The app-pool identity has **Modify** rights to `App_Data`.
- The `App_Data` folder is **included in backups** (see Backup & Restore guide).
- If the API and Web must share encrypted data, point both at the **same**
  `Encryption:DataProtectionKeysPath` and `Encryption:KeyFilePath` on shared,
  ACL-restricted storage.

---

## 7. Database

Apply the schema before first launch (DBA approach):

```powershell
sqlcmd -S SQL01 -i db\01-create-database.sql
sqlcmd -S SQL01 -d SecureAccess -i db\02-schema-idempotent.sql
```

Or let the app apply migrations on startup (requires `db_ddladmin`). The app seeds
roles, permissions, predefined credential types, and the admin account on first run.

---

## 8. Smoke test

1. Browse `https://access.yourcompany.com/` → login page renders over HTTPS.
2. Log in as admin → forced password change → Dashboard loads.
3. Browse the API `https://<api-host>:8443/swagger` → endpoints listed.
4. `GET /health` on both apps returns `Healthy`.
5. Create a client + machine + credential; reveal it via the request workflow.

---

## 9. Scheduled tasks

Register the backup scripts in **Task Scheduler** (see
[06-Backup-and-Restore.md](06-Backup-and-Restore.md)) to run nightly.

---

## 10. Upgrades

1. Stop the site / recycle the app pool.
2. Back up DB + `App_Data`.
3. `dotnet publish` the new build over the folder (keep `App_Data` and
   `appsettings.Production.json`).
4. Apply DB changes with the generated idempotent upgrade script
   (`db/03-upgrade-template.sql` workflow).
5. Start the site; run the smoke test.
