<#
.SYNOPSIS
    Creates a full backup of the SecureAccess SQL Server database.

.DESCRIPTION
    Performs a native SQL Server full backup (.bak) with compression and checksum.
    Old backups beyond the retention window are pruned. Intended to be run as a
    scheduled task (e.g. Windows Task Scheduler) on the database host.

.PARAMETER ServerInstance
    SQL Server instance, e.g. "localhost", "SQLSERVER01\PROD" or "(localdb)\MSSQLLocalDB".

.PARAMETER Database
    Database name. Defaults to "SecureAccess".

.PARAMETER BackupRoot
    Directory where .bak files are written. Created if it does not exist.

.PARAMETER RetentionDays
    Delete .bak files older than this many days (0 = keep everything).

.EXAMPLE
    .\Backup-Database.ps1 -ServerInstance "localhost" -BackupRoot "E:\Backups\SecureAccess"
#>
[CmdletBinding()]
param(
    [string]$ServerInstance = "localhost",
    [string]$Database       = "SecureAccess",
    [Parameter(Mandatory = $true)][string]$BackupRoot,
    [int]$RetentionDays     = 30
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BackupRoot)) {
    New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null
}

$timestamp  = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupRoot "$($Database)_FULL_$timestamp.bak"

$sql = @"
BACKUP DATABASE [$Database]
TO DISK = N'$backupFile'
WITH FORMAT, INIT, COMPRESSION, CHECKSUM, STATS = 10,
     NAME = N'$Database-Full-$timestamp';
"@

Write-Host "Backing up [$Database] on [$ServerInstance] to:`n  $backupFile" -ForegroundColor Cyan

# Prefer the SqlServer module if present; fall back to sqlcmd.
if (Get-Module -ListAvailable -Name SqlServer) {
    Import-Module SqlServer -ErrorAction Stop
    Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $sql -QueryTimeout 0
} elseif (Get-Command sqlcmd -ErrorAction SilentlyContinue) {
    & sqlcmd -S $ServerInstance -b -Q $sql
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd returned exit code $LASTEXITCODE" }
} else {
    throw "Neither the SqlServer PowerShell module nor sqlcmd.exe is available. Install one of them."
}

# Verify the backup is readable/restorable (header + checksum).
$verify = "RESTORE VERIFYONLY FROM DISK = N'$backupFile' WITH CHECKSUM;"
if (Get-Module -Name SqlServer) {
    Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $verify -QueryTimeout 0
} else {
    & sqlcmd -S $ServerInstance -b -Q $verify
    if ($LASTEXITCODE -ne 0) { throw "Backup verification failed." }
}

Write-Host "Backup completed and verified." -ForegroundColor Green

if ($RetentionDays -gt 0) {
    $cutoff = (Get-Date).AddDays(-$RetentionDays)
    Get-ChildItem -Path $BackupRoot -Filter "$($Database)_FULL_*.bak" |
        Where-Object { $_.LastWriteTime -lt $cutoff } |
        ForEach-Object {
            Write-Host "Pruning old backup: $($_.Name)" -ForegroundColor DarkGray
            Remove-Item $_.FullName -Force
        }
}
