param(
    [string]$ExeName = "HynixTasTsne",
    [switch]$Clean,
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Assert-ProjectChildPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $projectRoot $Path))
    $rootPrefix = $projectRoot.TrimEnd('\') + '\'
    if (-not $fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside the project: $fullPath"
    }
    return $fullPath
}

Push-Location $projectRoot
try {
    if (-not (Test-Path -LiteralPath ".\.venv\Scripts\python.exe")) {
        & powershell -ExecutionPolicy Bypass -File .\scripts\setup_python.ps1
    }

    .\.venv\Scripts\python.exe -m pip install -r requirements.txt
    .\.venv\Scripts\python.exe -m pip install -e .

    $buildPath = Assert-ProjectChildPath "build"
    $distProductPath = Assert-ProjectChildPath (Join-Path "dist" $ExeName)
    if ($Clean -and (Test-Path -LiteralPath $buildPath)) {
        Remove-Item -LiteralPath $buildPath -Recurse -Force
    }
    if ($Clean -and (Test-Path -LiteralPath $distProductPath)) {
        Remove-Item -LiteralPath $distProductPath -Recurse -Force
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
        .\tsne_runner_cli.py

    $distDir = Join-Path ".\dist" $ExeName
    if (-not (Test-Path -LiteralPath $distDir)) {
        throw "PyInstaller did not create the expected directory: $distDir"
    }

    Copy-Item -LiteralPath ".\.env.example" -Destination (Join-Path $distDir ".env.example") -Force
    $queryDir = Join-Path $distDir "queries"
    if (-not (Test-Path -LiteralPath $queryDir)) {
        New-Item -ItemType Directory -Path $queryDir | Out-Null
    }
    Copy-Item -Path ".\queries\*" -Destination $queryDir -Recurse -Force

    $readme = @"
Hynix TAS t-SNE Runner
======================

1. Copy .env.example to .env in this folder.
2. Fill the Oracle connection values and TSNE_SQL_FILE.
3. Run $ExeName.exe.

Example:
  Copy-Item .env.example .env
  notepad .env
  .\$ExeName.exe

The executable reads .env and queries\*.sql from this folder.
"@
    $readme | Set-Content -LiteralPath (Join-Path $distDir "RUN_EXE_README.txt") -Encoding UTF8

    if (-not $SkipZip) {
        $zipPath = Assert-ProjectChildPath (Join-Path "dist" "$ExeName.zip")
        if (Test-Path -LiteralPath $zipPath) {
            Remove-Item -LiteralPath $zipPath -Force
        }
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            (Resolve-Path $distDir).Path,
            $zipPath,
            [System.IO.Compression.CompressionLevel]::Optimal,
            $false
        )
        Write-Host "ZIP package:"
        Write-Host $zipPath
    }

    Write-Host ""
    Write-Host "EXE build completed:"
    Write-Host (Resolve-Path $distDir)
    Write-Host "Distribute the entire folder, not only the executable."
}
finally {
    Pop-Location
}
