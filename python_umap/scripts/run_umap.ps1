param([string[]]$Arguments)
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $root
try { & .venv\Scripts\python.exe -m umap_runner @Arguments; if ($LASTEXITCODE) { throw "UMAP execution failed." } }
finally { Pop-Location }
