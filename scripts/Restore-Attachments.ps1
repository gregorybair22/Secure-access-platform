<#
.SYNOPSIS
    Restores the SecureAccess App_Data folder (attachments + Data Protection keys)
    from a ZIP archive created by Backup-Attachments.ps1.

.DESCRIPTION
    Extracts the archive over the target App_Data directory. The existing folder is
    moved aside (renamed with a .bak-<timestamp> suffix) before extraction so the
    operation is reversible.

.PARAMETER ArchivePath
    Path to the AppData_*.zip archive to restore.

.PARAMETER AppDataPath
    Target App_Data directory to restore into.

.EXAMPLE
    .\Restore-Attachments.ps1 -ArchivePath "E:\Backups\SecureAccess\files\AppData_20260603_010000.zip" -AppDataPath "C:\inetpub\SecureAccess\App_Data"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ArchivePath,
    [Parameter(Mandatory = $true)][string]$AppDataPath
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ArchivePath)) { throw "Archive not found: $ArchivePath" }

Write-Warning "This will replace the contents of: $AppDataPath"
$confirm = Read-Host "Type YES to continue"
if ($confirm -ne "YES") { Write-Host "Aborted." -ForegroundColor Yellow; return }

if (Test-Path $AppDataPath) {
    $sideFolder = "$AppDataPath.bak-$(Get-Date -Format yyyyMMdd_HHmmss)"
    Write-Host "Moving existing folder aside to: $sideFolder" -ForegroundColor DarkGray
    Move-Item -Path $AppDataPath -Destination $sideFolder
}

New-Item -ItemType Directory -Path $AppDataPath -Force | Out-Null
Write-Host "Extracting archive to: $AppDataPath" -ForegroundColor Cyan
Expand-Archive -Path $ArchivePath -DestinationPath $AppDataPath -Force

Write-Host "Restore completed. Restart the application so the Data Protection key ring is reloaded." -ForegroundColor Green
