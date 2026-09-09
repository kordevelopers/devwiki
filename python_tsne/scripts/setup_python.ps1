param(
    [string]$PythonExecutable,
    [switch]$RecreateVenv
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot 'python_environment.ps1')

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $projectRoot
try {
    $venvPython = Initialize-Python312Environment -ProjectRoot $projectRoot `
        -PythonExecutable $PythonExecutable -RecreateVenv:$RecreateVenv

    & $venvPython -m pip install --upgrade pip
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to upgrade pip."
    }

    & $venvPython -m pip install -r requirements.txt
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install the required Python packages."
    }

    & $venvPython -m pip install -e .
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install the t-SNE project."
    }

    & $venvPython -m pip check
    if ($LASTEXITCODE -ne 0) {
        throw "The Python environment contains incompatible packages."
    }

    Write-Host "Python t-SNE setup completed."
}
finally {
    Pop-Location
}
