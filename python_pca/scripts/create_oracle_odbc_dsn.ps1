param(
    [Parameter(Mandatory = $true)]
    [string]$DsnName,

    [Parameter(Mandatory = $true)]
    [string]$DriverName,

    [Parameter(Mandatory = $true)]
    [string]$Host,

    [int]$Port = 1521,

    [string]$ServiceName = "",

    [string]$Sid = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ServiceName) -and [string]::IsNullOrWhiteSpace($Sid)) {
    throw "ServiceName or Sid is required for an Oracle ODBC DSN."
}

$driver = Get-OdbcDriver | Where-Object { $_.Name -eq $DriverName } | Select-Object -First 1
if (-not $driver) {
    Write-Host "Installed Oracle ODBC drivers:"
    Get-OdbcDriver | Where-Object { $_.Name -match "Oracle" } | Format-Table Name, Platform -AutoSize
    throw "ODBC driver '$DriverName' was not found. Run install_oracle_odbc_driver.ps1 first or adjust PCA_ODBC_DRIVER."
}

if (-not [string]::IsNullOrWhiteSpace($ServiceName)) {
    $dbq = "//$Host`:$Port/$ServiceName"
}
else {
    $dbq = "$Host`:$Port`:$Sid"
}

$existing = Get-OdbcDsn -Name $DsnName -DsnType User -ErrorAction SilentlyContinue
if ($existing) {
    Remove-OdbcDsn -Name $DsnName -DsnType User
}

Add-OdbcDsn `
    -Name $DsnName `
    -DriverName $DriverName `
    -DsnType User `
    -SetPropertyValue @("ServerName=$dbq")

Write-Host "Created User DSN '$DsnName' using driver '$DriverName' and DBQ '$dbq'."
