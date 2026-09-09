function Get-PythonRuntime {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string[]]$Prefix = @()
    )

    # A copied venv can exist on disk but fail to launch its original base Python.
    # Treat native stderr/launch failures the same in Windows PowerShell 5.1 and 7.
    try {
        $probe = & $Path @Prefix -c 'import json, sys; print(json.dumps(dict(major=sys.version_info.major, minor=sys.version_info.minor, version=sys.version.split()[0], executable=sys.executable, base_executable=sys._base_executable)))' 2>$null
        if ($LASTEXITCODE -eq 0) {
            return ($probe | ConvertFrom-Json -ErrorAction Stop)
        }
    }
    catch {
        return $null
    }
    return $null
}

function Get-Python312Executable {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string[]]$Prefix = @()
    )

    $runtime = Get-PythonRuntime -Path $Path -Prefix $Prefix
    if ($runtime -and $runtime.major -eq 3 -and $runtime.minor -eq 12) {
        # The selected command may itself be inside the venv we are about to move.
        $baseRuntime = Get-PythonRuntime -Path $runtime.base_executable
        if ($baseRuntime -and $baseRuntime.major -eq 3 -and $baseRuntime.minor -eq 12) {
            return $baseRuntime.executable
        }
    }
    return $null
}

function Find-Python312 {
    $launcher = Get-Command py -ErrorAction SilentlyContinue
    if ($launcher) {
        $executable = Get-Python312Executable -Path $launcher.Source -Prefix @('-3.12')
        if ($executable) { return $executable }
    }

    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($python) {
        $executable = Get-Python312Executable -Path $python.Source
        if ($executable) { return $executable }
    }

    $candidates = @(
        "$env:LOCALAPPDATA\Programs\Python\Python312\python.exe",
        "$env:ProgramFiles\Python312\python.exe",
        "${env:ProgramFiles(x86)}\Python312\python.exe",
        "$env:SystemDrive\Python312\python.exe"
    )
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $executable = Get-Python312Executable -Path $candidate
            if ($executable) { return $executable }
        }
    }
    return $null
}

function Initialize-Python312Environment {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [string]$PythonExecutable,
        [switch]$RecreateVenv
    )

    $resolvedRoot = (Resolve-Path -LiteralPath $ProjectRoot -ErrorAction Stop).Path
    $venvPath = Join-Path $resolvedRoot '.venv'
    $venvPython = Join-Path $venvPath 'Scripts\python.exe'
    if (Test-Path -LiteralPath $venvPath) {
        $venvItem = Get-Item -LiteralPath $venvPath -Force
        if (-not $venvItem.PSIsContainer -or
            ($venvItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            throw "Expected a regular .venv directory inside the project: $venvPath"
        }
    }

    $runtime = Get-PythonRuntime -Path $venvPython
    if ($runtime -and $runtime.major -eq 3 -and $runtime.minor -eq 12 -and -not $RecreateVenv) {
        Write-Host "Reusing Python $($runtime.version): $venvPython"
        return $venvPython
    }

    # Find a working replacement before touching the existing environment.
    if ($PythonExecutable) {
        $basePython = Get-Python312Executable -Path $PythonExecutable
        if (-not $basePython) {
            throw "The specified interpreter is not a working Python 3.12 installation: $PythonExecutable"
        }
    }
    else {
        $basePython = Find-Python312
        if (-not $basePython) {
            $winget = Get-Command winget -ErrorAction SilentlyContinue
            if (-not $winget) {
                throw 'Python 3.12 was not found. Install it or pass -PythonExecutable with its full python.exe path. The existing .venv has not been changed.'
            }
            Write-Host 'Python 3.12 was not found. Installing it with winget...'
            & $winget.Source install --id Python.Python.3.12 -e --source winget --accept-source-agreements --accept-package-agreements | Out-Host
            if ($LASTEXITCODE -ne 0) { throw 'winget failed to install Python 3.12.' }
            $basePython = Find-Python312
            if (-not $basePython) {
                throw 'Python 3.12 is still unavailable. Restart VS Code or pass -PythonExecutable with its full python.exe path. The existing .venv has not been changed.'
            }
        }
    }

    if (Test-Path -LiteralPath $venvPath) {
        $backupPath = Join-Path $resolvedRoot ('.venv.backup-' + [guid]::NewGuid().ToString('N'))
        # Both exact paths must be direct children of this project before any move.
        foreach ($targetPath in @($venvPath, $backupPath)) {
            $parentPath = Split-Path -Parent ([System.IO.Path]::GetFullPath($targetPath))
            if (-not $parentPath.Equals($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "Refusing to move an environment outside the project: $targetPath"
            }
        }
        if ($runtime) {
            Write-Host "Replacing the existing Python $($runtime.version) environment."
        }
        else {
            Write-Host 'The existing .venv is incomplete or cannot start its base Python.'
        }
        try {
            Move-Item -LiteralPath $venvPath -Destination $backupPath -ErrorAction Stop
        }
        catch {
            throw "Could not back up $venvPath. Stop Python debugging and close terminals using this environment, then retry. $($_.Exception.Message)"
        }
        Write-Host "Previous environment preserved at: $backupPath"
    }

    Write-Host "Creating .venv with: $basePython"
    & $basePython -m venv $venvPath | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw 'Python failed to create .venv. Any previous environment remains in the printed backup directory.'
    }
    $createdRuntime = Get-PythonRuntime -Path $venvPython
    if (-not $createdRuntime -or $createdRuntime.major -ne 3 -or $createdRuntime.minor -ne 12) {
        throw "A working Python 3.12 environment was not created at: $venvPython"
    }
    return $venvPython
}
