using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Alternative = SKhynix.TAS.Analysis.Tsne.Tsne;

internal static class AlternativeTsneVerification
{
    private static int assertions;
    public static int Run(string[] args)
    {
        try
        {
            Check(Environment.Is64BitProcess, "x64 harness required");
            Check(!typeof(Alternative).Assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("Accord", StringComparison.Ordinal)), "standalone DLL references Accord");
            if (args.Length > 0 && args[0] == "--settings")
            {
                VerifySettingDiagnostics();
                Console.WriteLine("PASS: exact setting names/values, 5610-row invalid learning rates and Hybrid seed boundaries; " + assertions + " assertions.");
                return 0;
            }
            if (args.Length > 0 && args[0] == "--large")
            {
                var input = Matrix(5610);
                var original = Copy(input);
                var settings = new Alternative.Options();
                Check(settings.Engine == Alternative.Engine.Hybrid, "large population default must be Hybrid");
                Check(settings.MaximumCSharpRows == 2000, "CSharp memory guard was removed");
                var result = Alternative.FitTransform(input, settings);
                Finite2D(result.Coordinates, 5610);
                Equal(input, original, "large default run mutated input");
                Check(result.Engine == Alternative.Engine.Hybrid, "large default run used another engine");
                bool rejected = false;
                try { Alternative.FitTransform(input, new Alternative.Options { Engine = Alternative.Engine.CSharp, FallbackToHybridForLargeInputs = false }); }
                catch (ArgumentException ex)
                {
                    rejected = ex.ParamName == "standardizedMatrix" && ex.Message.Contains("5610")
                        && ex.Message.Contains("MaximumCSharpRows=2000") && ex.Message.Contains("Tsne.Engine.Hybrid");
                }
                Check(rejected, "strict CSharp must report actual row count, limit and remedy");
                var fallbackOptions = new Alternative.Options { Engine = Alternative.Engine.CSharp, LearningRate = 73, RandomSeed = 17 };
                var fallback = Alternative.FitTransform(input, fallbackOptions);
                Finite2D(fallback.Coordinates, 5610);
                Equal(input, original, "fallback mutated input");
                Check(fallback.RequestedEngine == Alternative.Engine.CSharp && fallback.Engine == Alternative.Engine.Hybrid, "CSharp did not switch to Hybrid");
                Check(fallback.EngineSelectionReason.Contains("5610") && fallback.EngineSelectionReason.Contains("2000"), "missing fallback reason");
                Check(fallback.LearningRate == 73 && fallback.RandomSeed == 17 && fallback.Iterations == 1000, "fallback used fixed CSharp parameters");
                Check(fallbackOptions.Engine == Alternative.Engine.CSharp, "fallback mutated caller options");
                Console.WriteLine("PASS: default Hybrid and CSharp-to-Hybrid processed all 5610 rows; strict CSharp retained its guard; " + assertions + " assertions.");
                return 0;
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
        var options = new Alternative.Options { Engine = Alternative.Engine.CSharp, MaximumCSharpRows = count, Iterations = 80, Perplexity = 30, LearningRate = 17, RandomSeed = 97 };
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

    private static void VerifySettingDiagnostics()
    {
        var input = Matrix(5610);
        foreach (var engine in new[] { Alternative.Engine.CSharp, Alternative.Engine.Hybrid })
            foreach (double rate in new[] { 0d, -1d, double.NaN, double.PositiveInfinity })
            {
                var error = ExpectRange("LearningRate", rate,
                    () => Alternative.FitTransform(input, new Alternative.Options { Engine = engine, LearningRate = rate }));
                Check(error.Message.Contains("RequestedEngine=" + engine) && error.Message.Contains("ActualEngine=Hybrid")
                    && error.Message.Contains("Rows=5610") && error.Message.Contains("RandomSeed=42"), "missing execution context");
            }
        ExpectRange("Iterations", 0, () => Alternative.FitTransform(input, new Alternative.Options { Iterations = 0 }));
        ExpectRange("Perplexity", double.NaN, () => Alternative.FitTransform(input, new Alternative.Options { Perplexity = double.NaN }));
        ExpectRange("MaximumCSharpRows", 2, () => Alternative.FitTransform(input, new Alternative.Options { Engine = Alternative.Engine.CSharp, MaximumCSharpRows = 2 }));
        ExpectRange("Engine", (Alternative.Engine)123, () => Alternative.FitTransform(input, new Alternative.Options { Engine = (Alternative.Engine)123 }));
        foreach (int seed in new[] { 0, -1, int.MinValue, int.MaxValue })
        {
            var result = Alternative.FitTransform(Matrix(12), new Alternative.Options { Engine = Alternative.Engine.Hybrid, RandomSeed = seed, Iterations = 80 });
            Finite2D(result.Coordinates, 12);
            Check(result.RandomSeed == seed && result.LearningRate == 200, "valid seed or learning rate changed");
        }
    }

    private static ArgumentOutOfRangeException ExpectRange(string parameter, object value, Action action)
    {
        try { action(); }
        catch (ArgumentOutOfRangeException error)
        {
            Check(error.ParamName == parameter && object.Equals(error.ActualValue, value), "wrong setting diagnostic: " + error);
            return error;
        }
        throw new Exception("Expected range error for " + parameter);
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
        Reject("CSharp size cap", () => Alternative.FitTransform(Matrix(12), new Alternative.Options { Engine = Alternative.Engine.CSharp, MaximumCSharpRows = 11, FallbackToHybridForLargeInputs = false }));
        Reject("invalid CSharp size cap", () => Alternative.FitTransform(Matrix(4), new Alternative.Options { Engine = Alternative.Engine.CSharp, MaximumCSharpRows = 2 }));
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
