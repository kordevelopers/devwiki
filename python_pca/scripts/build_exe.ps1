param(
    [string]$ExeName = "HynixTasPca",
    [switch]$Clean,
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $root
try {
    if (-not (Test-Path -LiteralPath ".\.venv\Scripts\python.exe")) {
        powershell -ExecutionPolicy Bypass -File .\scripts\setup_python.ps1
    }

    .\.venv\Scripts\python.exe -m pip install -r requirements.txt
    .\.venv\Scripts\python.exe -m pip install -e .

    if ($Clean) {
        if (Test-Path -LiteralPath ".\build") {
            Remove-Item -LiteralPath ".\build" -Recurse -Force
        }
        if (Test-Path -LiteralPath ".\dist\$ExeName") {
            Remove-Item -LiteralPath ".\dist\$ExeName" -Recurse -Force
        }
    }

    .\.venv\Scripts\python.exe -m PyInstaller `
        --noconfirm `
        --clean `
        --onedir `
        --name $ExeName `
        --paths .\src `
        --collect-submodules sqlalchemy.dialects.oracle `
        --collect-all oracledb `
        --collect-all matplotlib `
        .\pca_runner_cli.py

    $distDir = Join-Path ".\dist" $ExeName
    if (-not (Test-Path -LiteralPath $distDir)) {
        throw "PyInstaller did not create expected directory: $distDir"
    }

    Copy-Item -LiteralPath ".\.env.example" -Destination (Join-Path $distDir ".env.example") -Force
    if (-not (Test-Path -LiteralPath (Join-Path $distDir "queries"))) {
        New-Item -ItemType Directory -Path (Join-Path $distDir "queries") | Out-Null
    }
    Copy-Item -LiteralPath ".\queries\*" -Destination (Join-Path $distDir "queries") -Recurse -Force

    $readme = @"
Hynix TAS PCA Runner
====================

1. Copy .env.example to .env in this same folder.
2. Fill Oracle connection values and PCA_SQL_FILE.
3. Run $ExeName.exe.

Example:
  Copy-Item .env.example .env
  notepad .env
  .\$ExeName.exe

The executable reads .env and queries\*.sql from this folder.
"@
    $readme | Set-Content -LiteralPath (Join-Path $distDir "RUN_EXE_README.txt") -Encoding UTF8

    if (-not $SkipZip) {
        $zipPath = Join-Path ".\dist" "$ExeName.zip"
        if (Test-Path -LiteralPath $zipPath) {
            Remove-Item -LiteralPath $zipPath -Force
        }
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            (Resolve-Path $distDir).Path,
            (Join-Path (Resolve-Path ".\dist").Path "$ExeName.zip"),
            [System.IO.Compression.CompressionLevel]::Optimal,
            $false
        )
        if (-not (Test-Path -LiteralPath $zipPath)) {
            throw "ZIP package was not created: $zipPath"
        }
        Write-Host "ZIP package:"
        Write-Host (Resolve-Path $zipPath)
    }

    Write-Host ""
    Write-Host "EXE build completed:"
    Write-Host (Resolve-Path $distDir)
    Write-Host ""
    Write-Host "Give this whole folder to another user, not only the .exe file."
}
finally {
    Pop-Location
}
