param(
    [string[]]$Arguments
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $projectRoot
try {
    $venvPython = ".\.venv\Scripts\python.exe"
    if (-not (Test-Path -LiteralPath $venvPython)) {
        throw "Python environment is not installed. Run .\scripts\setup_python.ps1 once."
    }

    # Runtime path: no pip install, pip check, or package comparison.
    & $venvPython -m tsne_runner @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "t-SNE execution failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
