# Backup & Restore Procedures

Secure Customer Access and Credential Management Platform

This document describes how to back up and restore the platform. There are **two**
assets that must always be backed up **together**:

1. **The SQL Server database** (`SecureAccess`) — all clients, machines, credentials
   (with encrypted secret columns), audit/change/login logs, sessions, users, roles.
2. **The application `App_Data` folder** — the encrypted credential **attachments**
   and the **ASP.NET Core Data Protection key ring** (`App_Data/keys`).

> ⚠️ **Critical:** The secret columns in the database and the attachment files are
> encrypted with AES‑256 using a master key that is itself protected by the Data
> Protection key ring stored in `App_Data/keys`. **If you restore the database
> without the matching key ring, all encrypted secrets become unrecoverable.**
> Always keep the database backup and the `App_Data` backup from the same time
> window, and protect both with the same access controls.

---

## 1. What to back up

| Asset | Location (default) | Script |
|-------|--------------------|--------|
| Database | SQL Server instance, DB `SecureAccess` | `scripts/Backup-Database.ps1` |
| Attachments + DP keys | `<app root>\App_Data` | `scripts/Backup-Attachments.ps1` |
| Configuration | `appsettings.Production.json` | manual / source control (no secrets in plaintext) |

Retention requirement (per specification): audit, change and login data must be
retained for **5 years**. The database retains this data in append‑only tables, so
your backup retention strategy must keep restorable backups (or archived copies)
that can satisfy the 5‑year requirement (e.g. weekly fulls retained long‑term, or a
yearly archived copy stored off‑site).

---

## 2. Scheduled backup (recommended)

Run both scripts nightly via **Windows Task Scheduler** on the database/app host.

**Database** (example, runs 01:00 daily, keep 30 days on disk):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File "C:\inetpub\SecureAccess\scripts\Backup-Database.ps1" `
  -ServerInstance "localhost" -Database "SecureAccess" `
  -BackupRoot "E:\Backups\SecureAccess\db" -RetentionDays 30
```

**Attachments + keys** (example, runs 01:15 daily):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File "C:\inetpub\SecureAccess\scripts\Backup-Attachments.ps1" `
  -AppDataPath "C:\inetpub\SecureAccess\App_Data" `
  -BackupRoot "E:\Backups\SecureAccess\files" -RetentionDays 30
```

Both scripts:
- create timestamped artifacts (`SecureAccess_FULL_<ts>.bak`, `AppData_<ts>.zip`),
- verify them (`RESTORE VERIFYONLY` / archive existence),
- prune artifacts older than `-RetentionDays`.

The database script uses the **SqlServer** PowerShell module if available, otherwise
falls back to `sqlcmd.exe`. Install one of them on the host:

```powershell
Install-Module -Name SqlServer -Scope AllUsers
```

> For long‑term off‑site retention, copy the produced `.bak` and `.zip` files to a
> secured secondary location (UNC share, blob storage, tape) after each run.

---

## 3. Manual backup (T‑SQL)

If you prefer to back up the database directly:

```sql
BACKUP DATABASE [SecureAccess]
TO DISK = N'E:\Backups\SecureAccess\db\SecureAccess_FULL.bak'
WITH FORMAT, INIT, COMPRESSION, CHECKSUM, STATS = 10;

RESTORE VERIFYONLY FROM DISK = N'E:\Backups\SecureAccess\db\SecureAccess_FULL.bak' WITH CHECKSUM;
```

For the `App_Data` folder, simply ZIP `App_Data` (which contains `attachments` and
`keys`).

---

## 4. Restore

Restore order does not matter, but **both** assets must be restored from a matching
time window before the application is started.

### 4.1 Database

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File "C:\inetpub\SecureAccess\scripts\Restore-Database.ps1" `
  -ServerInstance "localhost" -Database "SecureAccess" `
  -BackupFile "E:\Backups\SecureAccess\db\SecureAccess_FULL_20260603_010000.bak"
```

The script sets the database to `SINGLE_USER` to drop connections, restores with
`REPLACE`, then returns it to `MULTI_USER`. It prompts for confirmation because it
overwrites the target database. Use `-DataPath` to relocate data/log files when
restoring onto a different host.

### 4.2 Attachments + Data Protection keys

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File "C:\inetpub\SecureAccess\scripts\Restore-Attachments.ps1" `
  -ArchivePath "E:\Backups\SecureAccess\files\AppData_20260603_010000.zip" `
  -AppDataPath "C:\inetpub\SecureAccess\App_Data"
```

The script moves the existing `App_Data` aside (`.bak-<timestamp>`) before extracting,
so the operation is reversible.

### 4.3 After restore

1. Confirm the IIS app‑pool identity has read/write access to `App_Data`.
2. Restart the application (or recycle the app pool) so the Data Protection key ring
   is reloaded.
3. Log in and spot‑check that an existing credential's secret fields and an existing
   attachment can be revealed/downloaded (this confirms the keys match the data).

---

## 5. Disaster‑recovery test (do this quarterly)

1. Restore the latest DB backup and `App_Data` archive onto a **non‑production**
   host.
2. Point a staging instance at the restored DB and `App_Data`.
3. Log in as an administrator, reveal a credential secret, and download an attachment.
4. Record the restore time (RTO) and the data currency (RPO) in your DR log.

A backup you have never restored is not a backup.
