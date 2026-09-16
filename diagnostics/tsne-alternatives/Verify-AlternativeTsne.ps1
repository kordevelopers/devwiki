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
foreach ($dependency in 'lib/HybridTsne.dll', 'lib/TsneCSharp.dll', 'lib/MathNet.Numerics.dll', 'native/MathNet.Numerics.MKL.dll', 'native/libiomp5md.dll') {
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

# Keep verification C# inside this script, so the experiment has only Tsne.cs.
$standaloneSource = @'
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
$duplicateLog = Join-Path $outputRoot 'duplicates.log'
$duplicateErrorLog = Join-Path $outputRoot 'duplicates-errors.log'
$duplicateProcess = Start-Process -FilePath $standaloneHarness -ArgumentList '--duplicates' -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput $duplicateLog -RedirectStandardError $duplicateErrorLog
if (-not $duplicateProcess.WaitForExit(30000)) {
    $duplicateProcess.Kill()
    throw 'Hybrid duplicate-row verification exceeded 30 seconds.'
}
$duplicateProcess.WaitForExit()
Get-Content -LiteralPath $duplicateLog
Get-Content -LiteralPath $duplicateErrorLog
if ($duplicateProcess.ExitCode -ne 0) { throw 'Hybrid duplicate-row verification failed.' }

# Cross the upstream early-exaggeration transitions with the actual defaults.
$defaultLog = Join-Path $outputRoot 'defaults.log'
$defaultErrorLog = Join-Path $outputRoot 'defaults-errors.log'
$defaultProcess = Start-Process -FilePath $standaloneHarness -ArgumentList '--defaults' -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput $defaultLog -RedirectStandardError $defaultErrorLog
if (-not $defaultProcess.WaitForExit(60000)) {
    $defaultProcess.Kill()
    throw 'Default 1000-iteration engine verification exceeded 60 seconds.'
}
$defaultProcess.WaitForExit()
Get-Content -LiteralPath $defaultLog
Get-Content -LiteralPath $defaultErrorLog
if ($defaultProcess.ExitCode -ne 0) { throw 'Default 1000-iteration engine verification failed.' }

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
            foreach (Alternative.Engine? engine in new Alternative.Engine?[] { null, Alternative.Engine.CSharp, Alternative.Engine.Hybrid, Alternative.Engine.CSharp })
            {
                var options = new TSNEScatterAnalysisOptions { TSNELibraryEngine = engine, TSNEIterations = 80, TSNEPerplexity = 30, TSNELearningRate = 50, TSNERandomSeed = 19, NeighborCount = 7 };
                Check(options.Clone().TSNELibraryEngine == engine, "Clone lost selected engine");
                var result = service.AnalyzeSnapshot(snapshot, TSNEParameterType.Response, options);
                var analysis = result.AnalysisResult;
                var coordinates = analysis.TSNEModel.Coordinates;
                string expectedEngine = !engine.HasValue ? "Accord" : (engine.Value == Alternative.Engine.CSharp ? "tsne-csharp" : "Hybrid");
                Check(analysis.TSNEModel.EngineName.Contains(expectedEngine), "selected engine ignored: " + expectedEngine);
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
            Console.WriteLine("PASS: Accord/CSharp/Hybrid/CSharp DataTable integration; " + assertions + " assertions.");
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
