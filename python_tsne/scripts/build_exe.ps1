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
    $distRootPath = Assert-ProjectChildPath "dist"
    if ($Clean -and (Test-Path -LiteralPath $buildPath)) {
        Remove-Item -LiteralPath $buildPath -Recurse -Force
    }
    if ($Clean -and (Test-Path -LiteralPath $distRootPath)) {
        Remove-Item -LiteralPath $distRootPath -Recurse -Force
    }

    .\.venv\Scripts\python.exe -m PyInstaller `
        --noconfirm `
        --clean `
        --onefile `
        --console `
        --name $ExeName `
        --paths .\src `
        --collect-submodules sqlalchemy.dialects.oracle `
        --collect-all oracledb `
        --collect-all matplotlib `
        .\tsne_runner_cli.py

    $exePath = Join-Path ".\dist" "$ExeName.exe"
    if (-not (Test-Path -LiteralPath $exePath)) {
        throw "PyInstaller did not create the expected executable: $exePath"
    }

    $envExamplePath = Join-Path ".\dist" "$ExeName.env.example"
    Copy-Item -LiteralPath ".\.env.example" -Destination $envExamplePath -Force

    $readme = @"
Hynix TAS t-SNE Runner
======================

1. Rename $ExeName.env.example to .env in this folder.
2. Fill the five DB values from DBeaver JDBC: host, database, port, username, password.
3. Run $ExeName.exe.

Example:
  Copy-Item $ExeName.env.example .env
  notepad .env
  .\$ExeName.exe

The executable reads .env from the same folder. The default PCCB SQL is embedded in the executable.
The optional TSNE_SQL_FILE setting can point to a custom SQL file.
"@
    $readme | Set-Content -LiteralPath (Join-Path ".\dist" "RUN_EXE_README.txt") -Encoding UTF8

    if (-not $SkipZip) {
        $zipPath = Assert-ProjectChildPath (Join-Path "dist" "$ExeName.zip")
        if (Test-Path -LiteralPath $zipPath) {
            Remove-Item -LiteralPath $zipPath -Force
        }
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            (Resolve-Path ".\dist").Path,
            $zipPath,
            [System.IO.Compression.CompressionLevel]::Optimal,
            $false
        )
        Write-Host "ZIP package:"
        Write-Host $zipPath
    }

    Write-Host ""
    Write-Host "EXE build completed:"
    Write-Host (Resolve-Path $exePath)
    Write-Host "Distribute $ExeName.exe and a configured .env file."
}
finally {
    Pop-Location
}
