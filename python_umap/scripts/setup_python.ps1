param([switch]$RecreateVenv)
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $root
try {
    if ($RecreateVenv -and (Test-Path .venv)) { Remove-Item .venv -Recurse -Force }
    if (-not (Test-Path .venv\Scripts\python.exe)) { py -3.12 -m venv .venv }
    & .venv\Scripts\python.exe -m pip install -r requirements.txt
    & .venv\Scripts\python.exe -m pip install -e .
    & .venv\Scripts\python.exe -m pip check
} finally { Pop-Location }
