[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path ([IO.Path]::GetTempPath()) ('tas-tsne-alternatives-' + [Guid]::NewGuid().ToString('N'))),
    [string]$MsBuildPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
if ($outputRoot.TrimEnd('\', '/') -eq $repoRoot.TrimEnd('\', '/') -or
    $outputRoot.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Use a new output directory outside the checkout to protect existing bin/obj files.'
}
if (Test-Path -LiteralPath $outputRoot) { throw 'OutputDirectory must be a new directory.' }
$packageRoot = Join-Path $repoRoot 'packages'
foreach ($dependency in 'lib/HybridTsne.dll', 'lib/TsneCSharp.dll', 'lib/MathNet.Numerics.dll', 'native/MathNet.Numerics.MKL.dll', 'native/libiomp5md.dll', 'native/MulticoreTsne.Native.dll', 'native/vcomp140.dll') {
    if (-not (Test-Path -LiteralPath (Join-Path $packageRoot ('AlternativeTsne/' + $dependency)))) {
        throw 'Restore dependencies first with scripts/Restore-AlternativeTsne.ps1.'
    }
}
if (-not $MsBuildPath) {
    $command = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
    if ($command) { $MsBuildPath = $command.Source }
    else {
        $MsBuildPath = Get-ChildItem (Join-Path ${env:ProgramFiles} 'Microsoft Visual Studio') -Recurse -Filter MSBuild.exe |
            Where-Object { $_.FullName -match 'MSBuild[\\/]Current[\\/]Bin[\\/]MSBuild.exe$' } |
            Select-Object -First 1 -ExpandProperty FullName
    }
}
if (-not $MsBuildPath) { throw 'Visual Studio MSBuild is required.' }
$compilerPath = Join-Path (Split-Path $MsBuildPath -Parent) 'Roslyn/csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { $compilerPath = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe' }

# Compile fresh copies only; the checkout's tracked build outputs are untouched.
$sourceRoot = Join-Path $outputRoot 'source'
New-Item -ItemType Directory -Path $sourceRoot -Force | Out-Null
$projects = @('SKhynix.TAS.Analysis.Tsne', 'SKhynix.TAS.UI.Report.Pccb.ReportMaker', 'SKhynix.TAS.UI.Report.Pccb')
foreach ($projectName in $projects) {
    Get-ChildItem -LiteralPath (Join-Path $repoRoot $projectName) -Recurse -File | Where-Object {
        $_.Extension -in '.cs', '.csproj', '.resx', '.config' -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    } | ForEach-Object {
        $destination = Join-Path $sourceRoot $_.FullName.Substring($repoRoot.Length + 1)
        New-Item -ItemType Directory -Force -Path (Split-Path $destination -Parent) | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $destination
    }
}
New-Item -ItemType Directory -Path (Join-Path $sourceRoot 'scripts') | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'scripts/AlternativeTsne.Dependencies.targets') -Destination (Join-Path $sourceRoot 'scripts')
Get-ChildItem -LiteralPath $sourceRoot -Recurse -Filter '*.csproj' | ForEach-Object {
    $projectText = [IO.File]::ReadAllText($_.FullName)
    [IO.File]::WriteAllText($_.FullName, $projectText.Replace('..\packages\', ($packageRoot + '\')))
}
$standaloneProject = Join-Path $sourceRoot ($projects[0] + '/' + $projects[0] + '.csproj')
[xml]$standaloneXml = Get-Content -LiteralPath $standaloneProject
$compileItems = @($standaloneXml.Project.ItemGroup.Compile | Where-Object { $_ })
if ($compileItems.Count -ne 1 -or $compileItems[0].Include -ne 'Tsne.cs') { throw 'The standalone library must compile exactly one class file: Tsne.cs.' }
$hostProject = Join-Path $sourceRoot ($projects[2] + '/' + $projects[2] + '.csproj')
$dependencyRoot = Join-Path $packageRoot 'AlternativeTsne'
& $MsBuildPath $hostProject /nologo /v:minimal /p:Configuration=Release /p:PlatformTarget=x64 "/p:AlternativeTsnePackageRoot=$dependencyRoot"
if ($LASTEXITCODE -ne 0) { throw 'Isolated host/library build failed.' }
$standaloneRoot = Join-Path $sourceRoot ($projects[0] + '/bin/Release')
$hostRoot = Join-Path $sourceRoot ($projects[2] + '/bin/Release')

# Direct Process ownership keeps ExitCode reliable on Windows PowerShell 5.1,
# where Start-Process -PassThru can lose it after a very fast process exits.
function Invoke-BoundedHarness([string]$Executable, [string]$Argument, [string]$LogName, [int]$Timeout = 60000) {
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = New-Object System.Diagnostics.ProcessStartInfo
    $process.StartInfo.FileName = $Executable
    $process.StartInfo.Arguments = $Argument
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.CreateNoWindow = $true
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    try {
        [void]$process.Start()
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($Timeout)) { $process.Kill(); throw "$LogName exceeded its time limit." }
        $process.WaitForExit()
        [IO.File]::WriteAllText((Join-Path $outputRoot ($LogName + '.log')), $stdout.Result + $stderr.Result)
        Write-Output $stdout.Result
        if ($stderr.Result) { Write-Output $stderr.Result }
        if ($process.ExitCode -ne 0) { throw "$LogName failed with exit code $($process.ExitCode)." }
    }
    finally { $process.Dispose() }
}

# Keep verification C# inside this script, so the experiment has only Tsne.cs.
$standaloneSource = @'
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Alternative = SKhynix.TAS.Analysis.Tsne.Tsne;

internal static class AlternativeTsneVerification
{
    private static int assertions;
    private static int Main(string[] args)
    {
        try
        {
            Check(Environment.Is64BitProcess, "x64 harness required");
            Check(!typeof(Alternative).Assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("Accord", StringComparison.Ordinal)), "standalone DLL references Accord");
            if (args.Length > 0 && args[0] == "--multicore") { VerifyMulticore(); return 0; }
            if (args.Length > 0 && args[0] == "--missing-multicore")
            {
                try { Alternative.FitTransform(Matrix(12), new Alternative.Options { Engine = Alternative.Engine.Multicore }); }
                catch (InvalidOperationException ex) {
                    Check(ex.InnerException is DllNotFoundException && ex.Message.Contains("Restore-MulticoreTsne.ps1"), "Missing native DLL error was not actionable");
                    Console.WriteLine("PASS: missing native DLL gives a restore instruction."); return 0;
                }
                throw new Exception("Missing native DLL unexpectedly succeeded.");
            }
            if (args.Length > 0 && args[0] == "--defaults")
            {
                foreach (Alternative.Engine engine in new[] { Alternative.Engine.CSharp, Alternative.Engine.Hybrid })
                {
                    var input = Matrix(96);
                    var original = Copy(input);
                    var result = Alternative.FitTransform(input, new Alternative.Options { Engine = engine });
                    Finite2D(result.Coordinates, 96);
                    Equal(input, original, "default run mutated input");
                    Check(result.Iterations == 1000 && result.EffectivePerplexity == 30, "default schedule/perplexity mismatch");
                    Check(result.LearningRate == (engine == Alternative.Engine.CSharp ? 500 : 200) && result.RandomSeed == (engine == Alternative.Engine.CSharp ? 1 : 42), "default effective settings mismatch");
                    Console.WriteLine(engine + " default rows=96 iterations=1000 p=30 finite 2D; elapsed ms=" + result.ElapsedMilliseconds.ToString("F1"));
                }
                Console.WriteLine("PASS: production defaults across exaggeration schedules; " + assertions + " assertions.");
                return 0;
            }
            if (args.Length > 0 && args[0] == "--duplicates")
            {
                foreach (int perplexity in new[] { 1, 30 })
                {
                    var repeated = Enumerable.Range(0, 40).Select(i => new[] { (double)(i % 4), (i % 4) * (i % 4) + 0.3 }).ToArray();
                    var original = Copy(repeated);
                    var result = Alternative.FitTransform(repeated, new Alternative.Options { Engine = Alternative.Engine.Hybrid, Perplexity = perplexity, Iterations = 80, LearningRate = 50 });
                    Finite2D(result.Coordinates, 40);
                    Equal(repeated, original, "Hybrid duplicated-input mutation");
                    Console.WriteLine("Hybrid duplicate rows=40 distinct=4 requested p=" + perplexity + " finite 2D");
                }
                Console.WriteLine("PASS: duplicate-row LSH partition termination; " + assertions + " assertions.");
                return 0;
            }
            if (args.Length > 0 && args[0] == "--csharp-only")
            {
                VerifyCSharp(12);
                Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name.StartsWith("Hybrid") || a.GetName().Name.StartsWith("MathNet") || a.GetName().Name.StartsWith("Accord")), "CSharp loaded another engine");
                Console.WriteLine("PASS: CSharp runs with only TsneCSharp and standalone DLLs; " + assertions + " assertions.");
                return 0;
            }
            foreach (int rows in new[] { 3, 4, 12, 40 }) VerifyCSharp(rows);
            foreach (int rows in new[] { 4, 12, 40 }) VerifyHybrid(rows);
            ValidateFailures();
            var native = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().FirstOrDefault(m => string.Equals(m.ModuleName, "MathNet.Numerics.MKL.dll", StringComparison.OrdinalIgnoreCase));
            Check(native != null, "Hybrid did not load native MathNet MKL");
            Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name.StartsWith("Accord", StringComparison.Ordinal)), "standalone engine run loaded Accord");
            Console.WriteLine("Native MKL: " + native.FileName);
            Console.WriteLine("PASS: standalone engines, source parity, input/result isolation and validation; " + assertions + " assertions.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void VerifyCSharp(int count)
    {
        var input = Matrix(count);
        var original = Copy(input);
        var options = new Alternative.Options { Engine = Alternative.Engine.CSharp, Iterations = 80, Perplexity = 30, LearningRate = 17, RandomSeed = 97 };
        var result = Alternative.FitTransform(input, options);
        var again = Alternative.FitTransform(Copy(input), options);
        var direct = TSNE.TSNE.Reduce(Copy(input), options.Iterations, (int)result.EffectivePerplexity);
        Finite2D(result.Coordinates, count);
        Equal(input, original, "CSharp mutated caller input");
        Equal(result.Coordinates, again.Coordinates, "CSharp seed-1 repeat differs");
        Equal(result.Coordinates, direct, "CSharp wrapper differs from pinned upstream Reduce");
        Check(result.Engine == Alternative.Engine.CSharp && result.LearningRate == 500 && result.RandomSeed == 1 && result.Iterations == 80, "CSharp effective metadata mismatch");
        Check(result.EffectivePerplexity == Math.Max(1, (count - 1) / 3), "CSharp perplexity cap mismatch");
        var exposed = result.Coordinates;
        exposed[0][0] = 123456;
        Equal(result.Coordinates, direct, "result Coordinates exposes mutable backing storage");
        Console.WriteLine("CSharp rows=" + count + " p=" + result.EffectivePerplexity + " source parity=exact");
    }

    [DllImport("MulticoreTsne.Native.dll", CallingConvention=CallingConvention.Cdecl)]
    private static extern void tsne_run_double([In,Out] double[] x, int n, int d, [Out] double[] y, int dimensions,
        double perplexity, double theta, int threads, int iterations, int earlyIterations, int seed,
        [MarshalAs(UnmanagedType.I1)] bool initFromY, int verbose, double exaggeration, double rate, out double kl, int distance);

    [DllImport("MulticoreTsne.Native.dll", CallingConvention=CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
    private static extern int tas_tsne_run([In,Out] double[] x, int length, int n, int d, [Out] double[] y,
        int outputLength, double p, double theta, int threads, int iterations, int seed, double rate,
        out double kl, out int actual, StringBuilder error, int capacity);

    private static void VerifyMulticore()
    {
        foreach (int threads in new[] { 1, Math.Min(2, Environment.ProcessorCount), Math.Min(4, Environment.ProcessorCount) }.Distinct())
        {
            var input = Matrix(96); var original = Copy(input);
            var options = new Alternative.Options { Engine=Alternative.Engine.Multicore, NumberOfThreads=threads };
            var result = Alternative.FitTransform(input, options);
            Finite2D(result.Coordinates, 96); Equal(input, original, "Multicore mutated input");
            Check(result.NumberOfThreads==threads && result.Iterations==1000 && result.LearningRate==200 && result.RandomSeed==42 && result.Theta==.5, "Multicore settings lost");
            Check(!double.IsNaN(result.KullbackLeiblerDivergence) && !double.IsInfinity(result.KullbackLeiblerDivergence), "Invalid final KL");
            var again = Alternative.FitTransform(input, options);
            Equal(result.Coordinates, again.Coordinates, "Same Multicore settings changed coordinates");
            double kl; var direct=new double[192];
            tsne_run_double(original.SelectMany(row=>row).ToArray(),96,5,direct,2,30,.5,threads,1000,250,42,false,0,12,200,out kl,1);
            Check(result.Coordinates.SelectMany(row=>row).SequenceEqual(direct), "PInvoke wrapper differs from original native entry point");
            Check(Math.Abs(result.KullbackLeiblerDivergence-kl)<1e-10, "Wrapper KL differs from native");
            Console.WriteLine("Multicore: rows=96, iterations=1000, actual threads="+result.NumberOfThreads+", raw native parity=exact, KL="+kl.ToString("F6")+", ms="+result.ElapsedMilliseconds.ToString("F1"));
        }
        foreach(int count in new[]{4,12}) {
            var result=Alternative.FitTransform(Matrix(count),new Alternative.Options { Engine=Alternative.Engine.Multicore, Iterations=300, NumberOfThreads=1 });
            Finite2D(result.Coordinates,count); Check(result.EffectivePerplexity==(count-1)/3,"Multicore small perplexity cap");
        }
        var repeated=Enumerable.Range(0,40).Select(i=>Matrix(4)[i%4]).ToArray();
        Finite2D(Alternative.FitTransform(repeated,new Alternative.Options { Engine=Alternative.Engine.Multicore, Iterations=300 }).Coordinates,40);
        var lowTheta=new Alternative.Options { Engine=Alternative.Engine.Multicore, Iterations=300, Theta=.2, NumberOfThreads=1 };
        Check(Alternative.FitTransform(Matrix(24),lowTheta).Theta==.2,"Theta ignored");
        var jobs=Enumerable.Range(0,2).Select(i=>Task.Run(()=>Alternative.FitTransform(Matrix(24),new Alternative.Options {Engine=Alternative.Engine.Multicore,Iterations=300,RandomSeed=20+i,NumberOfThreads=1}))).ToArray();
        Task.WaitAll(jobs);
        for(int i=0;i<jobs.Length;i++) Equal(jobs[i].Result.Coordinates,Alternative.FitTransform(Matrix(24),new Alternative.Options {Engine=Alternative.Engine.Multicore,Iterations=300,RandomSeed=20+i,NumberOfThreads=1}).Coordinates,"Concurrent native calls corrupted seed state");
        Reject("Multicore 3 rows",()=>Alternative.FitTransform(Matrix(3),new Alternative.Options {Engine=Alternative.Engine.Multicore}));
        Reject("Multicore zero threads",()=>Alternative.FitTransform(Matrix(12),new Alternative.Options {Engine=Alternative.Engine.Multicore,NumberOfThreads=0}));
        Reject("Multicore too many threads",()=>Alternative.FitTransform(Matrix(12),new Alternative.Options {Engine=Alternative.Engine.Multicore,NumberOfThreads=Environment.ProcessorCount+1}));
        Reject("Multicore theta",()=>Alternative.FitTransform(Matrix(12),new Alternative.Options {Engine=Alternative.Engine.Multicore,Theta=double.NaN}));
        Reject("Multicore seed",()=>Alternative.FitTransform(Matrix(12),new Alternative.Options {Engine=Alternative.Engine.Multicore,RandomSeed=-1}));
        try {
            Alternative.FitTransform(Enumerable.Range(0,12).Select(i=>new[]{1d,2d}).ToArray(),new Alternative.Options {Engine=Alternative.Engine.Multicore});
            throw new Exception("Constant native data accepted");
        } catch(InvalidOperationException ex) { Check(ex.Message.Contains("variation"),"Native error text lost"); }
        var err=new StringBuilder(1024); double invalidKl; int invalidThreads;
        int status=tas_tsne_run(new double[24],-1,12,2,new double[24],24,3,.5,1,300,42,200,out invalidKl,out invalidThreads,err,1024);
        Check(status==1 && err.Length>0,"Native ABI length validation failed");
        Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetName().Name.StartsWith("Accord")||a.GetName().Name.StartsWith("Hybrid")||a.GetName().Name.StartsWith("MathNet")||a.GetName().Name=="TsneCSharp"),"Multicore loaded another engine");
        Check(Process.GetCurrentProcess().Modules.Cast<ProcessModule>().Any(m=>string.Equals(m.ModuleName,"vcomp140.dll",StringComparison.OrdinalIgnoreCase)),"OpenMP runtime not loaded");
        Console.WriteLine("PASS: standalone Multicore native isolation/parity/OpenMP/errors/concurrency; "+assertions+" assertions.");
    }

    private static void VerifyHybrid(int count)
    {
        var input = Matrix(count);
        var original = Copy(input);
        var result = Alternative.FitTransform(input, new Alternative.Options { Engine = Alternative.Engine.Hybrid, Iterations = 80, Perplexity = 30, LearningRate = 50, RandomSeed = 19 });
        Finite2D(result.Coordinates, count);
        Equal(input, original, "Hybrid mutated caller input");
        Check(result.Engine == Alternative.Engine.Hybrid && result.Iterations == 80 && result.LearningRate == 50 && result.RandomSeed == 19, "Hybrid metadata mismatch");
        Check(result.EngineName.Contains("auto") && result.EffectivePerplexity == (count - 1) / 3, "Hybrid configuration mismatch");
        Console.WriteLine("Hybrid auto rows=" + count + " p=" + result.EffectivePerplexity + " finite 2D; elapsed ms=" + result.ElapsedMilliseconds.ToString("F1"));
    }

    private static void ValidateFailures()
    {
        Reject("null", () => Alternative.FitTransform(null));
        Reject("two rows", () => Alternative.FitTransform(Matrix(2)));
        Reject("ragged", () => Alternative.FitTransform(new[] { new[] { 1d, 2d }, new[] { 3d }, new[] { 4d, 5d } }));
        Reject("null row", () => Alternative.FitTransform(new[] { new[] { 1d, 2d }, null, new[] { 4d, 5d } }));
        var invalid = Matrix(4); invalid[1][2] = double.NaN;
        Reject("NaN", () => Alternative.FitTransform(invalid));
        invalid[1][2] = double.PositiveInfinity;
        Reject("infinity", () => Alternative.FitTransform(invalid));
        Reject("unknown engine", () => Alternative.FitTransform(Matrix(4), new Alternative.Options { Engine = (Alternative.Engine)123 }));
        Reject("zero iterations", () => Alternative.FitTransform(Matrix(4), new Alternative.Options { Iterations = 0 }));
        Reject("invalid perplexity", () => Alternative.FitTransform(Matrix(4), new Alternative.Options { Perplexity = double.NaN }));
        Reject("CSharp size cap", () => Alternative.FitTransform(Matrix(12), new Alternative.Options { MaximumCSharpRows = 11 }));
        Reject("Hybrid three rows", () => Alternative.FitTransform(Matrix(3), new Alternative.Options { Engine = Alternative.Engine.Hybrid }));
        Reject("Hybrid zero learning rate", () => Alternative.FitTransform(Matrix(4), new Alternative.Options { Engine = Alternative.Engine.Hybrid, LearningRate = 0 }));
        Reject("Hybrid identical rows", () => Alternative.FitTransform(Enumerable.Range(0, 4).Select(i => new[] { 1d, 2d }).ToArray(), new Alternative.Options { Engine = Alternative.Engine.Hybrid }));
        invalid = Matrix(4); invalid[1][2] = double.MaxValue;
        Reject("Hybrid float overflow", () => Alternative.FitTransform(invalid, new Alternative.Options { Engine = Alternative.Engine.Hybrid }));
    }

    private static double[][] Matrix(int count) { return Enumerable.Range(0, count).Select(i => Enumerable.Range(0, 5).Select(j => Math.Sin((i + 1) * (j + 2) * 0.17) + (i % 3) * 0.71 + i * 0.013).ToArray()).ToArray(); }
    private static double[][] Copy(double[][] input) { return input.Select(row => (double[])row.Clone()).ToArray(); }
    private static void Equal(double[][] a, double[][] b, string message) { Check(a.Length == b.Length && a.Zip(b, (x, y) => x.SequenceEqual(y)).All(equal => equal), message); }
    private static void Finite2D(double[][] values, int count) { Check(values.Length == count && values.All(row => row.Length == 2 && row.All(x => !double.IsNaN(x) && !double.IsInfinity(x))), "invalid 2D output"); }
    private static void Reject(string label, Action action) { try { action(); } catch (ArgumentException) { assertions++; return; } throw new Exception("Expected input rejection: " + label); }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); assertions++; }
}
'@
$standaloneSourcePath = Join-Path $outputRoot 'AlternativeTsneVerification.cs'
[IO.File]::WriteAllText($standaloneSourcePath, $standaloneSource)
$standaloneHarness = Join-Path $standaloneRoot 'AlternativeTsneVerification.exe'
& $compilerPath /nologo /optimize+ /target:exe /platform:x64 "/out:$standaloneHarness" /r:System.Core.dll `
    "/r:$(Join-Path $standaloneRoot 'TsneCSharp.dll')" "/r:$(Join-Path $standaloneRoot ($projects[0] + '.dll'))" $standaloneSourcePath
if ($LASTEXITCODE -ne 0) { throw 'Standalone harness compilation failed.' }
$csharpOnlyRoot = Join-Path $outputRoot 'csharp-only'
New-Item -ItemType Directory -Path $csharpOnlyRoot | Out-Null
foreach ($name in 'AlternativeTsneVerification.exe', 'TsneCSharp.dll', 'SKhynix.TAS.Analysis.Tsne.dll') {
    Copy-Item -LiteralPath (Join-Path $standaloneRoot $name) -Destination $csharpOnlyRoot
}
& (Join-Path $csharpOnlyRoot 'AlternativeTsneVerification.exe') --csharp-only 2>&1 | Tee-Object -FilePath (Join-Path $outputRoot 'csharp-only.log')
if ($LASTEXITCODE -ne 0) { throw 'CSharp dependency isolation verification failed.' }
& $standaloneHarness 2>&1 | Tee-Object -FilePath (Join-Path $outputRoot 'standalone.log')
if ($LASTEXITCODE -ne 0) { throw 'Standalone engine verification failed.' }

# The pinned upstream LSH partitioner can recurse indefinitely on duplicate rows
# without the adapter's minimum per-tree neighbor count. Bound this regression
# case in its own process so a native crash or stack overflow cannot hang checks.
Invoke-BoundedHarness $standaloneHarness '--duplicates' 'duplicates' 30000

# Cross the upstream early-exaggeration transitions with the actual defaults.
Invoke-BoundedHarness $standaloneHarness '--defaults' 'defaults'
$multicoreOnlyRoot = Join-Path $outputRoot 'multicore-only'
New-Item -ItemType Directory -Path $multicoreOnlyRoot | Out-Null
foreach ($name in 'AlternativeTsneVerification.exe', 'SKhynix.TAS.Analysis.Tsne.dll', 'MulticoreTsne.Native.dll', 'vcomp140.dll', 'Multicore-TSNE.LICENSE.txt') {
    Copy-Item -LiteralPath (Join-Path $standaloneRoot $name) -Destination $multicoreOnlyRoot
}
Invoke-BoundedHarness (Join-Path $multicoreOnlyRoot 'AlternativeTsneVerification.exe') '--multicore' 'multicore'
Invoke-BoundedHarness (Join-Path $csharpOnlyRoot 'AlternativeTsneVerification.exe') '--missing-multicore' 'missing-multicore'

$integrationSource = @'
using System;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart;
using Alternative = SKhynix.TAS.Analysis.Tsne.Tsne;

internal static class AlternativeTsneIntegrationVerification
{
    private static int assertions;
    private static int Main()
    {
        try
        {
            var table = new DataTable();
            foreach (string name in new[] { "DRAFT_NO", "PARAM_TYP", "AI_RSLT_VAL", "ENGR_RSLT_VAL", "CONV_EXPER_CTN" }) table.Columns.Add(name);
            for (int i = 0; i < 24; i++)
            {
                if (i == 5) table.Rows.Add("EMPTY", "Response", "", "", "[]");
                table.Rows.Add("D" + i.ToString("D3"), "Response", "AI" + i, i % 2 == 0 ? "Pass" : "Review", JsonConvert.SerializeObject(new {
                    F0 = Math.Sin(i * 0.4) + i * 0.01, F1 = Math.Cos(i * 0.17), F2 = i % 3 + i * 0.07, F3 = (i + 1) * (i + 1) * 0.031, constant = 4, note = "excluded"
                }));
            }
            string originalTable = JsonConvert.SerializeObject(table);
            var service = new TSNEExadataService(table);
            var snapshot = service.SetDataTable(table);
            TSNEAnalysisResult reference = null;
            double[][] firstCSharp = null;
            foreach (Alternative.Engine? engine in new Alternative.Engine?[] { null, Alternative.Engine.CSharp, Alternative.Engine.Hybrid, Alternative.Engine.Multicore, Alternative.Engine.CSharp })
            {
                var options = new TSNEScatterAnalysisOptions { TSNELibraryEngine = engine, TSNEIterations = 80, TSNEPerplexity = 30, TSNELearningRate = 50, TSNERandomSeed = 19, NeighborCount = 7 };
                Check(options.Clone().TSNELibraryEngine == engine, "Clone lost selected engine");
                var result = service.AnalyzeSnapshot(snapshot, TSNEParameterType.Response, options);
                var analysis = result.AnalysisResult;
                var coordinates = analysis.TSNEModel.Coordinates;
                string expectedEngine = !engine.HasValue ? "Accord" : (engine.Value == Alternative.Engine.CSharp ? "tsne-csharp" : engine.Value == Alternative.Engine.Multicore ? "Multicore" : "Hybrid");
                Check(analysis.TSNEModel.EngineName.Contains(expectedEngine), "selected engine ignored: " + expectedEngine);
                if (engine == Alternative.Engine.Multicore) Check(analysis.TSNEModel.NumberOfThreads==options.TSNENumberOfThreads && analysis.TSNEModel.Theta==options.TSNETheta && !double.IsNaN(analysis.TSNEModel.KullbackLeiblerDivergence),"Pipeline lost native settings/diagnostics");
                Check(result.Records.Count == 24 && result.MissingExperimentCount == 1, "population filtering mismatch");
                Check(analysis.Verification.AllScoresFinite && analysis.Verification.SharedScalerInstance && analysis.Verification.KnnResultValid, "pipeline verification failed");
                var exported = result.CreateSurvivingPopulationDataTable();
                var raw = result.CreateRawDataTable();
                for (int i = 0; i < 24; i++)
                {
                    string draft = "D" + i.ToString("D3");
                    Check(result.Records[i].SourceRowIndex == (i < 5 ? i : i + 1), "source index lost after skipped row");
                    Check(result.Records[i].DraftNo == draft && analysis.ScatterData[i].DraftNo == draft && (string)exported.Rows[i]["DRAFT_NO"] == draft && (string)raw.Rows[i]["DRAFT_NO"] == draft, "draft/export row alignment mismatch");
                    Check(result.Records[i].X1 == coordinates[i][0] && result.Records[i].X2 == coordinates[i][1] && (double)exported.Rows[i]["X1"] == coordinates[i][0] && (double)raw.Rows[i]["X2"] == coordinates[i][1], "coordinate/export row alignment mismatch");
                }
                if (reference == null) reference = analysis;
                else
                {
                    Equal(reference.StandardizedMatrix, analysis.StandardizedMatrix, "engine switch changed standardization");
                    Check(reference.FeatureNames.SequenceEqual(analysis.FeatureNames), "engine switch changed features");
                    Check(reference.ExcludedFeatureNames.SequenceEqual(analysis.ExcludedFeatureNames), "engine switch changed exclusions");
                    Check(JsonConvert.SerializeObject(reference.FindNearest("D009", 7)) == JsonConvert.SerializeObject(analysis.FindNearest("D009", 7)), "engine switch changed standardized-space KNN");
                }
                if (engine == Alternative.Engine.CSharp)
                {
                    Check(analysis.TSNEModel.LearningRate == 500 && analysis.TSNEModel.RandomSeed == 1 && analysis.TSNEModel.Iterations == 80, "pipeline CSharp metadata mismatch");
                    if (firstCSharp == null) firstCSharp = coordinates;
                    else Equal(firstCSharp, coordinates, "switching back to CSharp changed its deterministic projection");
                }
                if (engine == Alternative.Engine.Hybrid) Check(!firstCSharp.Zip(coordinates, (a, b) => a.SequenceEqual(b)).All(equal => equal), "Hybrid reused CSharp coordinates");
                Console.WriteLine("Integration " + analysis.TSNEModel.EngineName + ": 24 aligned rows; standardization and KNN preserved.");
            }
            Check(JsonConvert.SerializeObject(table) == originalTable, "analysis mutated source DataTable");
            Console.WriteLine("PASS: Accord/CSharp/Hybrid/Multicore/CSharp DataTable integration; " + assertions + " assertions.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void Equal(double[][] a, double[][] b, string message) { Check(a.Length == b.Length && a.Zip(b, (x, y) => x.SequenceEqual(y)).All(equal => equal), message); }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); assertions++; }
}
'@
$integrationSourcePath = Join-Path $outputRoot 'AlternativeTsneIntegrationVerification.cs'
[IO.File]::WriteAllText($integrationSourcePath, $integrationSource)
$integrationHarness = Join-Path $hostRoot 'AlternativeTsneIntegrationVerification.exe'
& $compilerPath /nologo /optimize+ /target:exe /platform:x64 "/out:$integrationHarness" /r:System.Core.dll /r:System.Data.dll `
    "/r:$(Join-Path $hostRoot 'Newtonsoft.Json.dll')" "/r:$(Join-Path $hostRoot ($projects[0] + '.dll'))" `
    "/r:$(Join-Path $hostRoot ($projects[1] + '.dll'))" $integrationSourcePath
if ($LASTEXITCODE -ne 0) { throw 'Integration harness compilation failed.' }
$hostConfig = Join-Path $hostRoot ($projects[2] + '.exe.config')
if (Test-Path -LiteralPath $hostConfig) { Copy-Item -LiteralPath $hostConfig -Destination ($integrationHarness + '.config') }
& $integrationHarness 2>&1 | Tee-Object -FilePath (Join-Path $outputRoot 'integration.log')
if ($LASTEXITCODE -ne 0) { throw 'DataTable integration verification failed.' }
Write-Output "PASS: isolated x64 host build and all alternative t-SNE checks. Logs and binaries: $outputRoot"
