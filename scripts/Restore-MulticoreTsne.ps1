#requires -Version 5.1
<#
.SYNOPSIS
Build the pinned C++ Multicore-TSNE and the C ABI adapter as an x64 OpenMP DLL.
.DESCRIPTION
Requires Visual Studio C++ desktop build tools and a Windows SDK. No Python,
CMake, Accord or algorithm patches. Download/build outputs stay Git-ignored.
#>
[CmdletBinding()]
param([string]$MsBuildPath, [switch]$SkipValidation)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$packageRoot = Join-Path $repoRoot 'packages/AlternativeTsne'
$commit = 'c1dbf84eb550980876d8ed822af4e9dfd21c5e05'
$sourceRoot = Join-Path $packageRoot ('source/Multicore-' + $commit)
$buildRoot = Join-Path $packageRoot 'build/MulticoreTsne'
$nativeRoot = Join-Path $packageRoot 'native'
$licenseRoot = Join-Path $packageRoot 'licenses'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere)) { throw 'Visual Studio C++ build tools are required.' }
$vsRoot = & $vswhere -latest -prerelease -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $vsRoot) { throw 'Install the Visual Studio Desktop development with C++ workload and Windows SDK.' }
if (-not $MsBuildPath) { $MsBuildPath = Join-Path $vsRoot 'MSBuild/Current/Bin/MSBuild.exe' }
$toolset = Get-ChildItem -Path (Join-Path $vsRoot 'MSBuild/Microsoft/VC/*/Platforms/x64/PlatformToolsets/v*') -Directory |
    Where-Object { $_.Name -match '^v\d+$' } | Sort-Object Name -Descending | Select-Object -First 1
if (-not $toolset) { throw 'No installed MSVC x64 platform toolset was found.' }
foreach ($directory in @($sourceRoot, $buildRoot, $nativeRoot, $licenseRoot)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}
foreach ($file in @('tsne.cpp', 'tsne.h', 'splittree.cpp', 'splittree.h', 'vptree.h')) {
    Invoke-WebRequest -UseBasicParsing -Uri "https://raw.githubusercontent.com/DmitryUlyanov/Multicore-TSNE/$commit/multicore_tsne/$file" -OutFile (Join-Path $sourceRoot $file)
}
Invoke-WebRequest -UseBasicParsing -Uri "https://raw.githubusercontent.com/DmitryUlyanov/Multicore-TSNE/$commit/LICENSE.txt" -OutFile (Join-Path $licenseRoot 'Multicore-TSNE.LICENSE.txt')
Copy-Item -LiteralPath (Join-Path $licenseRoot 'Multicore-TSNE.LICENSE.txt') -Destination (Join-Path $nativeRoot 'Multicore-TSNE.LICENSE.txt')
$interopSource = [Security.SecurityElement]::Escape((Join-Path $repoRoot 'native/MulticoreTsne/MulticoreTsneInterop.cpp'))
$sourceXml = [Security.SecurityElement]::Escape($sourceRoot)
$nativeXml = [Security.SecurityElement]::Escape($nativeRoot)
$project = @"
<Project DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup Label="ProjectConfigurations"><ProjectConfiguration Include="Release|x64"><Configuration>Release</Configuration><Platform>x64</Platform></ProjectConfiguration></ItemGroup>
  <PropertyGroup Label="Globals"><ProjectGuid>{D407E042-1972-46AB-B610-C1A0B8438F23}</ProjectGuid><WindowsTargetPlatformVersion>10.0</WindowsTargetPlatformVersion></PropertyGroup>
  <Import Project="`$(VCTargetsPath)\Microsoft.Cpp.Default.props" />
  <PropertyGroup Label="Configuration"><ConfigurationType>DynamicLibrary</ConfigurationType><PlatformToolset>$($toolset.Name)</PlatformToolset><UseDebugLibraries>false</UseDebugLibraries></PropertyGroup>
  <Import Project="`$(VCTargetsPath)\Microsoft.Cpp.props" />
  <PropertyGroup><OutDir>$nativeXml\</OutDir><IntDir>`$(ProjectDir)obj\</IntDir><TargetName>MulticoreTsne.Native</TargetName></PropertyGroup>
  <ItemDefinitionGroup>
    <ClCompile><Optimization>MaxSpeed</Optimization><RuntimeLibrary>MultiThreaded</RuntimeLibrary><OpenMPSupport>true</OpenMPSupport><ExceptionHandling>Sync</ExceptionHandling><LanguageStandard>stdcpp17</LanguageStandard><FloatingPointModel>Precise</FloatingPointModel><PrecompiledHeader>NotUsing</PrecompiledHeader><WarningLevel>Level3</WarningLevel><PreprocessorDefinitions>_CRT_SECURE_NO_WARNINGS;NOMINMAX;%(PreprocessorDefinitions)</PreprocessorDefinitions></ClCompile>
    <Link><GenerateDebugInformation>false</GenerateDebugInformation></Link>
  </ItemDefinitionGroup>
  <ItemGroup><ClCompile Include="$sourceXml\tsne.cpp" /><ClCompile Include="$sourceXml\splittree.cpp" /><ClCompile Include="$interopSource" /></ItemGroup>
  <Import Project="`$(VCTargetsPath)\Microsoft.Cpp.targets" />
</Project>
"@
$projectPath = Join-Path $buildRoot 'MulticoreTsne.Native.vcxproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
& $MsBuildPath $projectPath /nologo /v:minimal /p:Configuration=Release /p:Platform=x64
if ($LASTEXITCODE -ne 0) { throw 'Multicore-TSNE native build failed.' }
$vcomp = Get-ChildItem -LiteralPath (Join-Path $vsRoot 'VC/Redist/MSVC') -Recurse -Filter vcomp140.dll |
    Where-Object { $_.FullName -match '[\\/]x64[\\/]' -and $_.FullName -notmatch '[\\/]debug_' } |
    Sort-Object FullName -Descending | Select-Object -First 1
if (-not $vcomp) { throw 'The x64 Visual C++ OpenMP redistributable vcomp140.dll was not found.' }
Copy-Item -LiteralPath $vcomp.FullName -Destination (Join-Path $nativeRoot 'vcomp140.dll')

if (-not $SkipValidation) {
    $smoke = @'
using System;
using System.Runtime.InteropServices;
using System.Text;
class MulticoreSmoke {
    [DllImport("MulticoreTsne.Native.dll", CallingConvention=CallingConvention.Cdecl)] static extern int tas_tsne_abi_version();
    [DllImport("MulticoreTsne.Native.dll", CallingConvention=CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
    static extern int tas_tsne_run([In,Out] double[] x, int length, int n, int d, [Out] double[] y, int outputLength, double p, double theta, int threads, int iterations, int seed, double rate, out double kl, out int actual, StringBuilder error, int capacity);
    static int Main() {
        if (tas_tsne_abi_version()!=1) return 1;
        foreach(int threads in new[]{1,Math.Min(4,Environment.ProcessorCount)}) {
            var x=new double[48*5]; var y=new double[48*2];
            for(int i=0;i<x.Length;i++) x[i]=Math.Sin(i*.17)+(i/5%3);
            double kl; int actual; var error=new StringBuilder(1024);
            int code=tas_tsne_run(x,x.Length,48,5,y,y.Length,10,.5,threads,300,42,200,out kl,out actual,error,error.Capacity);
            if(code!=0) { Console.Error.WriteLine(error); return 2; }
            foreach(double value in y) if(double.IsNaN(value)||double.IsInfinity(value)) return 3;
            if(actual!=threads||double.IsNaN(kl)||double.IsInfinity(kl)) return 4;
            Console.WriteLine("Multicore native: 48x2 finite; actual threads="+actual+"; KL="+kl);
        }
        return 0;
    }
}
'@
    $smokePath = Join-Path $buildRoot 'MulticoreSmoke.cs'
    [IO.File]::WriteAllText($smokePath, $smoke)
    $compiler = Join-Path (Split-Path $MsBuildPath -Parent) 'Roslyn/csc.exe'
    $smokeExe = Join-Path $nativeRoot 'MulticoreSmoke.exe'
    & $compiler /nologo /target:exe /platform:x64 "/out:$smokeExe" $smokePath
    if ($LASTEXITCODE -ne 0) { throw 'Native smoke harness compilation failed.' }
    & $smokeExe
    if ($LASTEXITCODE -ne 0) { throw 'Native OpenMP/PInvoke smoke verification failed.' }
}
$manifest = [ordered]@{ Repository='https://github.com/DmitryUlyanov/Multicore-TSNE'; Commit=$commit; Architecture='x64'; OpenMP=$true; AbiVersion=1; Runtime='static MSVC CRT + vcomp140.dll'; AlgorithmPatched=$false; License='LICENSE.txt (four conditions including advertising acknowledgement)'; Validated=(-not $SkipValidation) }
[IO.File]::WriteAllText((Join-Path $packageRoot 'multicore-manifest.json'), ($manifest | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
Write-Output "Multicore-TSNE native DLL and OpenMP runtime: $nativeRoot"
