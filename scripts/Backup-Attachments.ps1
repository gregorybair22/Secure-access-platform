<#
.SYNOPSIS
    Backs up the SecureAccess application data folder (encrypted attachments and
    Data Protection key ring) to a timestamped ZIP archive.

.DESCRIPTION
    The platform stores encrypted credential attachments and the Data Protection
    key ring under the application's App_Data folder. Both MUST be backed up
    together with the database: without the Data Protection keys, encrypted
    credential secrets and attachments cannot be decrypted after a restore.

.PARAMETER AppDataPath
    Path to the application's App_Data directory (contains 'attachments' and 'keys').

.PARAMETER BackupRoot
    Directory where the ZIP archives are written.

.PARAMETER RetentionDays
    Delete archives older than this many days (0 = keep everything).

.EXAMPLE
    .\Backup-Attachments.ps1 -AppDataPath "C:\inetpub\SecureAccess\App_Data" -BackupRoot "E:\Backups\SecureAccess\files"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$AppDataPath,
    [Parameter(Mandatory = $true)][string]$BackupRoot,
    [int]$RetentionDays = 30
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $AppDataPath)) { throw "App_Data path not found: $AppDataPath" }
if (-not (Test-Path $BackupRoot))  { New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null }

$timestamp   = Get-Date -Format "yyyyMMdd_HHmmss"
$archivePath = Join-Path $BackupRoot "AppData_$timestamp.zip"

Write-Host "Archiving '$AppDataPath' to:`n  $archivePath" -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $AppDataPath '*') -DestinationPath $archivePath -CompressionLevel Optimal -Force

if (-not (Test-Path $archivePath)) { throw "Archive was not created." }
Write-Host "Attachment/key backup completed: $((Get-Item $archivePath).Length) bytes." -ForegroundColor Green

if ($RetentionDays -gt 0) {
    $cutoff = (Get-Date).AddDays(-$RetentionDays)
    Get-ChildItem -Path $BackupRoot -Filter "AppData_*.zip" |
        Where-Object { $_.LastWriteTime -lt $cutoff } |
        ForEach-Object {
            Write-Host "Pruning old archive: $($_.Name)" -ForegroundColor DarkGray
            Remove-Item $_.FullName -Force
        }
}
