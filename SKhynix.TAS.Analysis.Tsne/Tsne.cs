using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SKhynix.TAS.Analysis.Tsne
{
    /// <summary>
    /// Single-file, Accord-free adapters for Orlinski/Hybrid_t-SNE and
    /// jdmccaffrey/tsne-csharp. Pass the same standardized matrix to compare engines.
    /// Reference the managed assemblies in lib/Tsne; no t-SNE NuGet restore is needed.
    /// </summary>
    public static class Tsne
    {
        public enum Engine { Hybrid, CSharp }

        private static volatile Engine defaultEngine = Engine.Hybrid;

        /// <summary>
        /// Shared default for the standalone library, report pipeline and demo form.
        /// Set once at application startup, before creating forms or analysis options.
        /// Existing options and explicit per-call selections retain their engine.
        /// </summary>
        public static Engine DefaultEngine
        {
            get { return defaultEngine; }
            set
            {
                if (value != Engine.Hybrid && value != Engine.CSharp)
                    throw new ArgumentOutOfRangeException("value", "Unknown t-SNE engine.");
                defaultEngine = value;
            }
        }

        public sealed class Options
        {
            public Options()
            {
                Engine = Tsne.DefaultEngine;
                Perplexity = 30;
                Iterations = 1000;
                LearningRate = 200;
                RandomSeed = 42;
                MaximumCSharpRows = 2000;
            }

            public Engine Engine { get; set; }
            public double Perplexity { get; set; }
            public int Iterations { get; set; }
            /// <summary>Hybrid only. Unmodified tsne-csharp fixes its rate at 500.</summary>
            public double LearningRate { get; set; }
            /// <summary>Hybrid only. Unmodified tsne-csharp fixes its seed at 1.</summary>
            public int RandomSeed { get; set; }
            /// <summary>Bounds the dense C# engine's quadratic allocations; increase explicitly if needed.</summary>
            public int MaximumCSharpRows { get; set; }
        }

        public sealed class Result
        {
            private readonly double[][] coordinates;

            internal Result(double[][] coordinates, Engine engine, int perplexity,
                int iterations, double learningRate, int randomSeed, double elapsed)
            {
                this.coordinates = Clone(coordinates);
                Engine = engine;
                EffectivePerplexity = perplexity;
                Iterations = iterations;
                LearningRate = learningRate;
                RandomSeed = randomSeed;
                ElapsedMilliseconds = elapsed;
            }

            public double[][] Coordinates { get { return Clone(coordinates); } }
            public Engine Engine { get; private set; }
            public string EngineName
            {
                get { return Engine == Tsne.Engine.Hybrid ? "Hybrid_t-SNE (auto: Barnes-Hut / FFT)" : "tsne-csharp (exact)"; }
            }
            public double EffectivePerplexity { get; private set; }
            public int Iterations { get; private set; }
            public double LearningRate { get; private set; }
            public int RandomSeed { get; private set; }
            public double ElapsedMilliseconds { get; private set; }
        }

        // Hybrid initializes MathNet's process-wide native provider. Serialize its
        // calls, without changing Console.Out or any caller-owned input matrix.
        private static readonly object HybridLock = new object();

        public static Result FitTransform(double[][] standardizedMatrix, Options options = null)
        {
            var watch = Stopwatch.StartNew();
            Options settings = options ?? new Options();
            // Snapshot caller settings before dispatching to an upstream engine.
            Engine engine = settings.Engine;
            int iterations = settings.Iterations;
            double requestedPerplexity = settings.Perplexity;
            double learningRate = engine == Engine.CSharp ? 500 : settings.LearningRate;
            int seed = engine == Engine.CSharp ? 1 : settings.RandomSeed;
            ValidateMatrix(standardizedMatrix);
            if (engine != Engine.Hybrid && engine != Engine.CSharp)
                throw new ArgumentOutOfRangeException("options", "Unknown t-SNE engine.");
            if (!Finite(requestedPerplexity) || requestedPerplexity < 1)
                throw new ArgumentOutOfRangeException("options", "Perplexity must be finite and at least 1.");
            if (iterations < 1)
                throw new ArgumentOutOfRangeException("options", "Iterations must be positive.");
            if (!Finite(learningRate) || learningRate <= 0)
                throw new ArgumentOutOfRangeException("options", "Learning rate must be finite and positive.");
            int count = standardizedMatrix.Length;
            if (engine == Engine.Hybrid && count < 4)
                throw new ArgumentException("Hybrid_t-SNE requires at least four rows.", "standardizedMatrix");
            if (engine == Engine.CSharp && settings.MaximumCSharpRows < 3)
                throw new ArgumentOutOfRangeException("options", "MaximumCSharpRows must be at least 3.");
            if (engine == Engine.CSharp && count > settings.MaximumCSharpRows)
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "tsne-csharp input has {0} rows, exceeding MaximumCSharpRows={1}. " +
                    "Its dense matrices require quadratic memory and computation. " +
                    "Set Tsne.DefaultEngine = Tsne.Engine.Hybrid before creating analysis options, " +
                    "and remove any explicit CSharp selection. For a deliberate CSharp test, " +
                    "reduce the sample count or explicitly increase Options.MaximumCSharpRows.",
                    count, settings.MaximumCSharpRows), "standardizedMatrix");

            // An integer perplexity is shared across both engines. Hybrid needs
            // 3*p <= N-1; the C# upstream accepts integers only. Report the cap.
            int perplexity = (int)Math.Min(requestedPerplexity, Math.Max(1, (count - 1) / 3));
            double[][] input = Clone(standardizedMatrix);
            double[][] coordinates;
            if (engine == Engine.Hybrid)
            {
                if (!Environment.Is64BitProcess)
                    throw new PlatformNotSupportedException("Hybrid_t-SNE requires an x64 process for native MKL. Build the host with PlatformTarget=x64.");
                lock (HybridLock)
                    coordinates = RunHybrid(input, perplexity, iterations, learningRate, seed);
            }
            else
            {
                coordinates = RunCSharp(input, iterations, perplexity);
            }
            ValidateCoordinates(coordinates, count);
            return new Result(coordinates, engine, perplexity, iterations, learningRate, seed, watch.Elapsed.TotalMilliseconds);
        }

        // Keep dependency loading local to the chosen engine. CSharp can run
        // independently without loading the Hybrid or MathNet native libraries.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static double[][] RunCSharp(double[][] input, int iterations, int perplexity)
        {
            return global::TSNE.TSNE.Reduce(input, iterations, perplexity);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static double[][] RunHybrid(double[][] input, int perplexity, int iterations, double learningRate, int seed)
        {
            var converted = new float[input.Length][];
            bool varied = false;
            float min = float.MaxValue, max = float.MinValue;
            for (int row = 0; row < input.Length; row++)
            {
                converted[row] = new float[input[row].Length];
                for (int column = 0; column < input[row].Length; column++)
                {
                    float value = (float)input[row][column];
                    if (float.IsNaN(value) || float.IsInfinity(value))
                        throw new ArgumentException("Hybrid input must be representable as finite float values. Standardize the features first.", "input");
                    converted[row][column] = value;
                    min = Math.Min(min, value);
                    max = Math.Max(max, value);
                    if (row > 0 && value != converted[0][column]) varied = true;
                }
            }
            if (!varied || float.IsInfinity(max - min))
                throw new ArgumentException("Hybrid input needs variation between rows and a finite float range. Standardize the features first.", "input");

            var optimizer = new Hybrid_tSNE.tSNE(converted);
            optimizer.AffinitiesConfig.Perplexity = perplexity;
            optimizer.GradientConfig.Iterations = iterations;
            optimizer.GradientConfig.LearningRate = (iteration, total) => learningRate;
            optimizer.GradientConfig.RepulsionMethod = Hybrid_tSNE.RepulsionMethods.auto;
            optimizer.InitializationConfig.InitialSolutionSeed = seed;
            optimizer.LSHFConfig.LSHSeed = seed;
            // Upstream rough selection can return an empty partition below size
            // ten (especially for duplicates). At least seven candidates per
            // tree prevents recursion into that partition, including at p=1.
            // LSHTreeC * neighbours / trees = 4 * (3*p) / trees.
            optimizer.LSHFConfig.LSHForestTrees = (int)Math.Max(1, Math.Min(64, (12L * perplexity) / 7));
            return optimizer.Reduce(2, false);
        }

        private static void ValidateMatrix(double[][] matrix)
        {
            if (matrix == null || matrix.Length < 3 || matrix[0] == null || matrix[0].Length < 2)
                throw new ArgumentException("t-SNE requires at least three rows and two numeric features.", "standardizedMatrix");
            int columns = matrix[0].Length;
            for (int row = 0; row < matrix.Length; row++)
            {
                if (matrix[row] == null || matrix[row].Length != columns)
                    throw new ArgumentException("t-SNE input must be rectangular.", "standardizedMatrix");
                for (int column = 0; column < columns; column++)
                    if (!Finite(matrix[row][column]))
                        throw new ArgumentException("t-SNE input must contain finite values.", "standardizedMatrix");
            }
        }

        private static void ValidateCoordinates(double[][] coordinates, int count)
        {
            if (coordinates == null || coordinates.Length != count)
                throw new InvalidOperationException("The t-SNE engine returned an unexpected row count.");
            foreach (double[] row in coordinates)
                if (row == null || row.Length != 2 || !Finite(row[0]) || !Finite(row[1]))
                    throw new InvalidOperationException("The t-SNE engine returned invalid coordinates. Check the input variation and engine settings.");
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }

        private static double[][] Clone(double[][] matrix)
        {
            var copy = new double[matrix.Length][];
            for (int row = 0; row < matrix.Length; row++) copy[row] = (double[])matrix[row].Clone();
            return copy;
        }
    }
}
