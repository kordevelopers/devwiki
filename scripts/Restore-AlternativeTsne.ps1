#requires -Version 5.1
<#
.SYNOPSIS
Builds the two GitHub t-SNE dependencies for the .NET Framework 4.5.1 test library.
.DESCRIPTION
Downloads immutable upstream revisions and checksum-pinned NuGet packages into
packages/AlternativeTsne (already ignored by the repository). Requires Windows,
a Visual Studio Roslyn C# compiler and the .NET Framework 4.5.1 targeting pack.
Hybrid source and binaries remain local: its upstream revision has no LICENSE.
No upstream algorithm, learning-rate or random-seed code is patched.
tsne-csharp therefore retains its original learning rate 500 and random seed 1.
The Hybrid DLL is x64, including its original automatic Barnes-Hut/FFT selection.
Use -SkipValidation to omit the small native/managed execution smoke check.
#>
[CmdletBinding()]
param(
    [string]$CscPath,
    [switch]$SkipValidation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$dependencyRoot = Join-Path $repositoryRoot 'packages\AlternativeTsne'
$libRoot = Join-Path $dependencyRoot 'lib'
$nativeRoot = Join-Path $dependencyRoot 'native'
$sourceRoot = Join-Path $dependencyRoot 'source'
$downloadsRoot = Join-Path $dependencyRoot 'downloads'
$licenseRoot = Join-Path $dependencyRoot 'licenses'
$frameworkRoot = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.1'
$hybridCommit = '8a19f4e19ffc0b8e5b142533401e157972e2cdbf'
$csharpCommit = '7739a9ded14ff004b1b213146858ce25f8a97386'

if (-not (Test-Path -LiteralPath (Join-Path $frameworkRoot 'mscorlib.dll'))) {
    throw 'Install the .NET Framework 4.5.1 targeting pack before restoring these dependencies.'
}
if (-not $CscPath) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere) {
        $compilerCandidates = @(& $vswhere -latest -prerelease -products '*' -find 'MSBuild\**\Bin\Roslyn\csc.exe')
        if ($compilerCandidates.Count -gt 0) { $CscPath = $compilerCandidates[0] }
    }
    if (-not $CscPath) {
        $compilerCandidates = @(Get-ChildItem -Path (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\*\*\MSBuild\Current\Bin\Roslyn\csc.exe') -ErrorAction SilentlyContinue)
        if ($compilerCandidates.Count -gt 0) { $CscPath = $compilerCandidates[0].FullName }
    }
}
if (-not $CscPath -or -not (Test-Path -LiteralPath $CscPath)) {
    throw 'Visual Studio Roslyn csc.exe was not found. Supply its full path with -CscPath.'
}

foreach ($directory in @($libRoot, $nativeRoot, $sourceRoot, $downloadsRoot, $licenseRoot)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

function Get-PinnedSource {
    param([string]$Repository, [string]$Commit, [string]$SourcePath, [string]$Destination)
    $url = 'https://raw.githubusercontent.com/' + $Repository + '/' + $Commit + '/' + ($SourcePath -replace ' ', '%20')
    Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $Destination
}

function Get-VerifiedPackage {
    param([string]$Id, [string]$Version, [string]$Sha256)
    $name = $Id.ToLowerInvariant() + '.' + $Version + '.nupkg'
    $path = Join-Path $downloadsRoot $name
    if (-not (Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $Sha256) {
        $url = 'https://api.nuget.org/v3-flatcontainer/' + $Id.ToLowerInvariant() + '/' + $Version + '/' + $name
        Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $path
    }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $Sha256) {
        throw ('The SHA-256 checksum did not match for ' + $name)
    }
    return $path
}

function Export-PackageEntry {
    param([string]$Package, [string]$Entry, [string]$Destination)
    $archive = [IO.Compression.ZipFile]::OpenRead($Package)
    try {
        $item = $archive.GetEntry($Entry)
        if ($null -eq $item) { throw ('Missing package entry: ' + $Entry) }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($item, $Destination, $true)
    }
    finally { $archive.Dispose() }
}

function Invoke-Compiler {
    param([string[]]$Arguments)
    & $CscPath @Arguments
    if ($LASTEXITCODE -ne 0) { throw ('C# compilation failed (exit ' + $LASTEXITCODE + ').') }
}

Write-Host 'Restoring pinned t-SNE sources and MathNet/MKL packages...'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$mathNetPackage = Get-VerifiedPackage 'MathNet.Numerics' '4.7.0' '72738799153317512DE477343E7517AEAE51FFA9881326946C73EA5E31D07F03'
$mklPackage = Get-VerifiedPackage 'MathNet.Numerics.MKL.Win-x64' '2.3.0' 'AC6865FF9C8C90E0DC588BA325639397318F119E6ECF207003E1F40734B01AE9'
Export-PackageEntry $mathNetPackage 'lib/net40/MathNet.Numerics.dll' (Join-Path $libRoot 'MathNet.Numerics.dll')
Export-PackageEntry $mathNetPackage 'lib/net40/MathNet.Numerics.xml' (Join-Path $libRoot 'MathNet.Numerics.xml')
Export-PackageEntry $mathNetPackage 'MathNet.Numerics.nuspec' (Join-Path $licenseRoot 'MathNet.Numerics.nuspec')
Export-PackageEntry $mklPackage 'build/x64/libiomp5md.dll' (Join-Path $nativeRoot 'libiomp5md.dll')
Export-PackageEntry $mklPackage 'build/x64/MathNet.Numerics.MKL.dll' (Join-Path $nativeRoot 'MathNet.Numerics.MKL.dll')
Export-PackageEntry $mklPackage 'license.txt' (Join-Path $licenseRoot 'MathNet.Numerics.MKL.txt')
Export-PackageEntry $mklPackage 'readme.txt' (Join-Path $licenseRoot 'MathNet.Numerics.MKL.README.txt')

$hybridSourceRoot = Join-Path $sourceRoot ('Hybrid-' + $hybridCommit)
$csharpSourceRoot = Join-Path $sourceRoot ('CSharp-' + $csharpCommit)
New-Item -ItemType Directory -Path $hybridSourceRoot, $csharpSourceRoot -Force | Out-Null
$hybridSources = @()
foreach ($name in @('Config.cs', 'FFTRepulsion.cs', 'Gradient.cs', 'HashSpatialTree.cs', 'LSHForest.cs', 'Quicksort.cs', 'Selection.cs', 'tSNE.cs')) {
    $destination = Join-Path $hybridSourceRoot $name
    Get-PinnedSource 'Orlinski/Hybrid_t-SNE' $hybridCommit ('t-SNE/' + $name) $destination
    $hybridSources += $destination
}
Get-PinnedSource 'Orlinski/Hybrid_t-SNE' $hybridCommit 't-SNE/Properties/AssemblyInfo.cs' (Join-Path $hybridSourceRoot 'AssemblyInfo.cs')
$hybridSources += Join-Path $hybridSourceRoot 'AssemblyInfo.cs'
Get-PinnedSource 'Orlinski/Hybrid_t-SNE' $hybridCommit 'README.md' (Join-Path $licenseRoot 'Hybrid_t-SNE.README.md')
Get-PinnedSource 'jdmccaffrey/tsne-csharp' $csharpCommit 'TSNEProgram.cs' (Join-Path $csharpSourceRoot 'TSNEProgram.cs')
Get-PinnedSource 'jdmccaffrey/tsne-csharp' $csharpCommit 'LICENSE' (Join-Path $licenseRoot 'tsne-csharp.LICENSE.txt')

$baseArguments = @('/nologo', '/target:library', '/optimize+', '/deterministic+', '/langversion:7.3', '/nostdlib+')
foreach ($name in @('mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Numerics.dll')) {
    $baseArguments += '/reference:' + (Join-Path $frameworkRoot $name)
}
Write-Host 'Compiling original Hybrid t-SNE (x64) and tsne-csharp source...'
Invoke-Compiler ($baseArguments + @('/platform:x64', ('/out:' + (Join-Path $libRoot 'HybridTsne.dll')), ('/reference:' + (Join-Path $libRoot 'MathNet.Numerics.dll'))) + $hybridSources)
Invoke-Compiler ($baseArguments + @('/platform:anycpu', ('/out:' + (Join-Path $libRoot 'TsneCSharp.dll')), (Join-Path $csharpSourceRoot 'TSNEProgram.cs')))

if (-not $SkipValidation) {
    $smokeSource = @'
using System;
using System.IO;
using Hybrid_tSNE;

internal static class DependencySmoke
{
    private static void Check(double[][] values, string backend)
    {
        if (values == null || values.Length != 40) throw new Exception(backend + " returned the wrong row count.");
        foreach (double[] row in values)
        {
            if (row == null || row.Length != 2) throw new Exception(backend + " returned the wrong dimension.");
            foreach (double value in row)
                if (double.IsNaN(value) || double.IsInfinity(value)) throw new Exception(backend + " returned non-finite coordinates.");
        }
        Console.WriteLine(backend + ": 40 x 2 finite coordinates.");
    }

    private static float[][] Data()
    {
        float[][] data = new float[40][];
        for (int i = 0; i < data.Length; i++)
            data[i] = new float[] { (float)Math.Sin(i * 0.3), (float)Math.Cos(i * 0.7), i / 10f, (i % 7) / 7f };
        return data;
    }

    private static int Main(string[] args)
    {
        try
        {
            if (!Environment.Is64BitProcess) throw new Exception("Hybrid MKL requires an x64 process.");
            MathNet.Numerics.Control.NativeProviderPath = Path.GetFullPath(args[0]);
            foreach (RepulsionMethods method in new[] { RepulsionMethods.auto, RepulsionMethods.barnes_hut })
            {
                tSNE model = new tSNE(Data());
                model.AffinitiesConfig.Perplexity = 5;
                model.LSHFConfig.LSHForestTrees = 32;
                model.LSHFConfig.LSHSeed = 1;
                model.InitializationConfig.InitialSolutionSeed = 1;
                model.GradientConfig.Iterations = 12;
                model.GradientConfig.RepulsionMethod = method;
                Check(model.Reduce(2), "Hybrid " + method);
            }
            float[][] source = Data();
            double[][] input = new double[source.Length][];
            for (int i = 0; i < source.Length; i++)
            {
                input[i] = new double[source[i].Length];
                for (int j = 0; j < source[i].Length; j++) input[i][j] = source[i][j];
            }
            Check(TSNE.TSNE.Reduce(input, 12, 5), "tsne-csharp");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
'@
    $smokeSourcePath = Join-Path $sourceRoot 'DependencySmoke.cs'
    [IO.File]::WriteAllText($smokeSourcePath, $smokeSource, [Text.UTF8Encoding]::new($false))
    $smokeExe = Join-Path $libRoot 'DependencySmoke.exe'
    $smokeArguments = @($baseArguments | Where-Object { $_ -ne '/target:library' })
    $smokeArguments += @('/target:exe', '/platform:x64', ('/out:' + $smokeExe))
    foreach ($name in @('HybridTsne.dll', 'TsneCSharp.dll', 'MathNet.Numerics.dll')) {
        $smokeArguments += '/reference:' + (Join-Path $libRoot $name)
    }
    Invoke-Compiler ($smokeArguments + $smokeSourcePath)
    Write-Host 'Checking native FFT loading, Hybrid auto/Barnes-Hut, and tsne-csharp...'
    & $smokeExe $nativeRoot
    if ($LASTEXITCODE -ne 0) { throw ('Dependency validation failed (exit ' + $LASTEXITCODE + ').') }
}

$manifest = [ordered]@{
    TargetFramework = '.NET Framework 4.5.1'
    Hybrid = [ordered]@{ Repository = 'https://github.com/Orlinski/Hybrid_t-SNE'; Commit = $hybridCommit; Assembly = 'HybridTsne.dll'; License = 'No LICENSE file at this revision; local testing only.' }
    CSharp = [ordered]@{ Repository = 'https://github.com/jdmccaffrey/tsne-csharp'; Commit = $csharpCommit; Assembly = 'TsneCSharp.dll'; License = 'MIT'; LearningRate = 500; RandomSeed = 1; Dimensions = 2 }
    MathNet = '4.7.0 (net40 assembly)'
    MKL = 'MathNet.Numerics.MKL.Win-x64 2.3.0'
    Validated = -not $SkipValidation
}
[IO.File]::WriteAllText((Join-Path $dependencyRoot 'manifest.json'), ($manifest | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
Write-Host ('Managed dependencies: ' + $libRoot)
Write-Host ('Native x64 dependencies: ' + $nativeRoot)
& (Join-Path $PSScriptRoot 'Restore-MulticoreTsne.ps1') -SkipValidation:$SkipValidation
