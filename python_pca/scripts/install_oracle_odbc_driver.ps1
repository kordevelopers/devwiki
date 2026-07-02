param(
    [Parameter(Mandatory = $true)]
    [string]$InstantClientDir
)

$ErrorActionPreference = "Stop"
$resolved = Resolve-Path -LiteralPath $InstantClientDir
$installer = Join-Path $resolved "odbc_install.exe"

if (-not (Test-Path -LiteralPath $installer)) {
    throw "odbc_install.exe was not found in '$resolved'. Extract Oracle Instant Client Basic/Basic Lite and ODBC ZIP files into the same instantclient folder first."
}

Write-Host "Installing Oracle ODBC driver from $resolved"
Push-Location $resolved
try {
    & $installer
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Current Oracle ODBC drivers:"
Get-OdbcDriver | Where-Object { $_.Name -match "Oracle" } | Format-Table Name, Platform -AutoSize
