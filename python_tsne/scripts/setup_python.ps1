$ErrorActionPreference = "Stop"

function Find-Python312 {
    $launcher = Get-Command py -ErrorAction SilentlyContinue
    if ($launcher) {
        & $launcher.Source -3.12 -c "import sys; raise SystemExit(0 if sys.version_info[:2] == (3, 12) else 1)" 2>$null
        if ($LASTEXITCODE -eq 0) {
            return @{ Path = $launcher.Source; Prefix = @("-3.12") }
        }
    }

    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($python) {
        & $python.Source -c "import sys; raise SystemExit(0 if sys.version_info[:2] == (3, 12) else 1)" 2>$null
        if ($LASTEXITCODE -eq 0) {
            return @{ Path = $python.Source; Prefix = @() }
        }
    }

    $candidates = @(
        "$env:LOCALAPPDATA\Programs\Python\Python312\python.exe",
        "$env:ProgramFiles\Python312\python.exe",
        "${env:ProgramFiles(x86)}\Python312\python.exe"
    )
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return @{ Path = $candidate; Prefix = @() }
        }
    }
    return $null
}

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $projectRoot
try {
    $venvPython = ".\.venv\Scripts\python.exe"
    if (Test-Path -LiteralPath $venvPython) {
        & $venvPython -c "import sys; raise SystemExit(0 if sys.version_info[:2] == (3, 12) else 1)"
        if ($LASTEXITCODE -ne 0) {
            throw "The existing .venv does not use Python 3.12. Remove .venv and run this task again."
        }

        Write-Host "Reusing the existing Python 3.12 virtual environment."
    }
    else {
        $pythonCommand = Find-Python312
        if (-not $pythonCommand) {
            $winget = Get-Command winget -ErrorAction SilentlyContinue
            if (-not $winget) {
                throw "Python 3.12 is not installed and winget was not found. Install Python 3.12, then run this task again."
            }

            Write-Host "Python 3.12 was not found. Installing it with winget..."
            & $winget.Source install --id Python.Python.3.12 -e --source winget --accept-source-agreements --accept-package-agreements
            if ($LASTEXITCODE -ne 0) {
                throw "winget failed to install Python 3.12."
            }

            $pythonCommand = Find-Python312
            if (-not $pythonCommand) {
                throw "Python 3.12 was installed, but this terminal cannot find it yet. Restart the terminal and run this task again."
            }
        }

        Write-Host "Using Python 3.12 command: $($pythonCommand.Path)"
        & $pythonCommand.Path @($pythonCommand.Prefix) -m venv .venv
        if ($LASTEXITCODE -ne 0) {
            throw "Python failed to create the virtual environment."
        }

        if (-not (Test-Path -LiteralPath $venvPython)) {
            throw "Virtual environment Python was not created at $venvPython"
        }
    }

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
