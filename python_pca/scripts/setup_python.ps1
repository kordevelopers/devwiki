$ErrorActionPreference = "Stop"

function Find-Python {
    $pyLauncher = Get-Command py -ErrorAction SilentlyContinue
    if ($pyLauncher) {
        return @{ Kind = "py"; Path = $pyLauncher.Source }
    }

    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($python) {
        return @{ Kind = "python"; Path = $python.Source }
    }

    $candidatePaths = @(
        "$env:LOCALAPPDATA\Programs\Python\Python312\python.exe",
        "$env:ProgramFiles\Python312\python.exe",
        "${env:ProgramFiles(x86)}\Python312\python.exe"
    )
    foreach ($candidate in $candidatePaths) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return @{ Kind = "path"; Path = $candidate }
        }
    }

    return $null
}

function Invoke-Python {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$PythonCommand,
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    if ($PythonCommand["Kind"] -eq "py") {
        & $PythonCommand["Path"] -3 @Arguments
    }
    else {
        & $PythonCommand["Path"] @Arguments
    }
}

$pythonCommand = Find-Python
if (-not $pythonCommand) {
    $winget = Get-Command winget -ErrorAction SilentlyContinue
    if (-not $winget) {
        throw "Python is not installed and winget was not found. Install Python 3.12 or newer, then run this task again."
    }

    Write-Host "Python was not found. Installing Python 3.12 with winget..."
    & $winget.Source install --id Python.Python.3.12 -e --source winget --accept-source-agreements --accept-package-agreements
    $pythonCommand = Find-Python
    if (-not $pythonCommand) {
        throw "Python was installed, but this terminal cannot find it yet. Restart VS Code and run the task again."
    }
}

Write-Host "Using Python command: $($pythonCommand["Path"])"
Invoke-Python -PythonCommand $pythonCommand -Arguments @("-m", "venv", ".venv")

$venvPython = ".\.venv\Scripts\python.exe"
if (-not (Test-Path -LiteralPath $venvPython)) {
    throw "Virtual environment Python was not created at $venvPython"
}

& $venvPython -m pip install --upgrade pip
& $venvPython -m pip install -r requirements.txt
& $venvPython -m pip install -e .

Write-Host "Python setup completed. Run the 'Run PCA sample' task or the 'Run PCA sample' debug configuration."
