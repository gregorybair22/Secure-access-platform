<#
.SYNOPSIS
    Restores the SecureAccess database from a .bak file.

.DESCRIPTION
    Restores a full backup, overwriting the target database. The database is set
    to SINGLE_USER during the restore to drop active connections, then returned to
    MULTI_USER. USE WITH CARE: this overwrites the target database.

.PARAMETER ServerInstance
    SQL Server instance.

.PARAMETER Database
    Target database name to restore into. Defaults to "SecureAccess".

.PARAMETER BackupFile
    Full path to the .bak file to restore.

.PARAMETER DataPath
    Optional folder for relocated data/log files (MOVE). If omitted, the server's
    default data directory is used (requires matching logical file names).

.EXAMPLE
    .\Restore-Database.ps1 -ServerInstance "localhost" -BackupFile "E:\Backups\SecureAccess\SecureAccess_FULL_20260603_010000.bak"
#>
[CmdletBinding()]
param(
    [string]$ServerInstance = "localhost",
    [string]$Database       = "SecureAccess",
    [Parameter(Mandatory = $true)][string]$BackupFile,
    [string]$DataPath
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BackupFile)) { throw "Backup file not found: $BackupFile" }

function Invoke-Sql([string]$query) {
    if (Get-Module -ListAvailable -Name SqlServer) {
        Import-Module SqlServer -ErrorAction Stop
        return Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $query -QueryTimeout 0
    } elseif (Get-Command sqlcmd -ErrorAction SilentlyContinue) {
        $out = & sqlcmd -S $ServerInstance -b -Q $query
        if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed (exit $LASTEXITCODE) for query." }
        return $out
    } else {
        throw "Neither the SqlServer module nor sqlcmd.exe is available."
    }
}

Write-Warning "This will OVERWRITE database [$Database] on [$ServerInstance]."
$confirm = Read-Host "Type the database name to confirm"
if ($confirm -ne $Database) { Write-Host "Aborted." -ForegroundColor Yellow; return }

$move = ""
if ($DataPath) {
    if (-not (Test-Path $DataPath)) { New-Item -ItemType Directory -Path $DataPath -Force | Out-Null }
    # Discover logical file names from the backup.
    $files = Invoke-Sql "RESTORE FILELISTONLY FROM DISK = N'$BackupFile';"
    foreach ($f in $files) {
        $logical = $f.LogicalName
        $ext = if ($f.Type -eq 'L') { 'ldf' } else { 'mdf' }
        $target = Join-Path $DataPath "$logical.$ext"
        $move += "    MOVE N'$logical' TO N'$target',`n"
    }
}

$restore = @"
IF DB_ID(N'$Database') IS NOT NULL
BEGIN
    ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END
RESTORE DATABASE [$Database]
FROM DISK = N'$BackupFile'
WITH REPLACE, CHECKSUM, STATS = 10,
$move    RECOVERY;
ALTER DATABASE [$Database] SET MULTI_USER;
"@

Write-Host "Restoring [$Database] from $BackupFile ..." -ForegroundColor Cyan
Invoke-Sql $restore
Write-Host "Restore completed." -ForegroundColor Green
