[CmdletBinding()]
param(
    [string]$Revision = 'HEAD',
    [switch]$WorkingTree,
    [string]$BaselineResult,
    [string]$OutputDirectory = (Join-Path ([IO.Path]::GetTempPath()) ('tas-tsne-check-' + [Guid]::NewGuid().ToString('N'))),
    [string]$MsBuildPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$projects = @('SKhynix.TAS.UI.Report.Pccb.ReportMaker', 'SKhynix.TAS.UI.Report.Pccb')
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
if ($outputRoot.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Use an output directory outside the checkout to protect existing bin/obj files.'
}
if (Test-Path -LiteralPath $outputRoot) { throw 'OutputDirectory must be a new directory.' }
New-Item -ItemType Directory -Path $outputRoot | Out-Null
$sourceRoot = Join-Path $outputRoot 'source'
New-Item -ItemType Directory -Path $sourceRoot | Out-Null
if ($WorkingTree) {
    foreach ($projectName in $projects) {
        $projectRoot = Join-Path $repoRoot $projectName
        Get-ChildItem -LiteralPath $projectRoot -Recurse -File | Where-Object {
            $_.Extension -in '.cs', '.csproj', '.resx', '.config' -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
        } | ForEach-Object {
            $relativePath = $_.FullName.Substring($repoRoot.Length + 1)
            $destination = Join-Path $sourceRoot $relativePath
            New-Item -ItemType Directory -Force -Path (Split-Path $destination -Parent) | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination
        }
    }
} else {
    $archivePath = Join-Path $outputRoot 'source.zip'
    & git -C $repoRoot archive --format=zip "--output=$archivePath" $Revision -- @projects
    if ($LASTEXITCODE -ne 0) { throw 'git archive failed.' }
    Expand-Archive -LiteralPath $archivePath -DestinationPath $sourceRoot
}

# Only the temporary project copies are changed. Dependencies remain read-only.
$packageRoot = Join-Path $repoRoot 'packages'
Get-ChildItem -LiteralPath $sourceRoot -Recurse -Filter '*.csproj' | ForEach-Object {
    $projectText = [IO.File]::ReadAllText($_.FullName)
    [IO.File]::WriteAllText($_.FullName, $projectText.Replace('..\packages\', ($packageRoot + '\')))
}
if (-not $MsBuildPath) {
    $msbuildCommand = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
    if ($msbuildCommand) { $MsBuildPath = $msbuildCommand.Source }
    else {
        $MsBuildPath = Get-ChildItem (Join-Path ${env:ProgramFiles} 'Microsoft Visual Studio') -Recurse -Filter MSBuild.exe |
            Where-Object { $_.FullName -match 'MSBuild[\\/]Current[\\/]Bin[\\/]MSBuild.exe$' } |
            Select-Object -First 1 -ExpandProperty FullName
    }
}
if (-not $MsBuildPath) { throw 'Visual Studio MSBuild is required.' }
$hostProject = Join-Path $sourceRoot ($projects[1] + '/' + $projects[1] + '.csproj')
& $MsBuildPath $hostProject /nologo /v:minimal /p:Configuration=Release
if ($LASTEXITCODE -ne 0) { throw 'Isolated project build failed.' }
$binaryRoot = Join-Path $sourceRoot ($projects[0] + '/bin/Release')
$compilerPath = Join-Path (Split-Path $MsBuildPath -Parent) 'Roslyn/csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { $compilerPath = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe' }
$harnessPath = Join-Path $binaryRoot 'TsnePerformanceVerification.exe'
& $compilerPath /nologo /optimize+ /target:exe "/out:$harnessPath" /r:System.Data.dll /r:System.Core.dll `
    "/r:$(Join-Path $binaryRoot 'Newtonsoft.Json.dll')" `
    "/r:$(Join-Path $binaryRoot 'Accord.Math.dll')" `
    "/r:$(Join-Path $binaryRoot ($projects[0] + '.dll'))" `
    (Join-Path $PSScriptRoot 'TsnePerformanceVerification.cs')
if ($LASTEXITCODE -ne 0) { throw 'Harness compilation failed.' }
$resultPath = Join-Path $outputRoot 'results.json'
& $harnessPath $resultPath
if ($LASTEXITCODE -ne 0) { throw 'Verification failed.' }
if ($BaselineResult) {
    & $harnessPath --compare ([IO.Path]::GetFullPath($BaselineResult)) $resultPath
    if ($LASTEXITCODE -ne 0) { throw 'Baseline parity verification failed.' }
}
Write-Output "Results: $resultPath"
