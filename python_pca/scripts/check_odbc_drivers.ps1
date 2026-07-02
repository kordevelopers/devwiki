$ErrorActionPreference = "Stop"

Write-Host "Installed ODBC drivers:"
Get-OdbcDriver | Sort-Object Name | Format-Table Name, Platform -AutoSize

Write-Host ""
Write-Host "Oracle-related drivers:"
$oracleDrivers = Get-OdbcDriver | Where-Object { $_.Name -match "Oracle" } | Sort-Object Name
if (-not $oracleDrivers) {
    Write-Warning "No Oracle ODBC driver was found. Install Oracle Instant Client Basic/Basic Lite + ODBC package, then run odbc_install.exe as Administrator."
    exit 1
}

$oracleDrivers | Format-Table Name, Platform -AutoSize
