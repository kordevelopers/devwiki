param([string]$ExeName="HynixTasUmap", [switch]$Clean, [switch]$SkipZip)
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $root
try {
    if (-not (Test-Path .venv\Scripts\python.exe)) { & powershell -ExecutionPolicy Bypass -File .\scripts\setup_python.ps1 }
    & .venv\Scripts\python.exe -m pip install -r requirements.txt
    & .venv\Scripts\python.exe -m pip install -e .
    if ($Clean -and (Test-Path build)) { Remove-Item build -Recurse -Force }
    if ($Clean -and (Test-Path dist)) { Remove-Item dist -Recurse -Force }
    & .venv\Scripts\python.exe -m PyInstaller --noconfirm --clean --onefile --console --name $ExeName --paths .\src --collect-all umap --collect-all matplotlib --collect-all oracledb .\umap_runner_cli.py
    Copy-Item .\.env.example .\dist\$ExeName.env.example -Force
    @"
Hynix TAS UMAP Runner
=====================
Rename $ExeName.env.example to .env, fill the five DB values from DBeaver, and run $ExeName.exe.
The default PCCB SQL is embedded in the executable. No Python or package installation is required on the target PC.
"@ | Set-Content .\dist\RUN_EXE_README.txt -Encoding UTF8
    if (-not $SkipZip) { Compress-Archive -Path .\dist\* -DestinationPath .\dist\$ExeName.zip -Force }
    Write-Host (Resolve-Path .\dist\$ExeName.exe)
} finally { Pop-Location }
