using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using Accord.MachineLearning.Clustering;
using Accord.Statistics.Analysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.Common;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    /// <summary>
    /// Public JSON conversion entry points used by the t-SNE analysis pipeline.
    /// Keeping this API public allows the Pccb host to prepare and inspect the
    /// same JSON representation without depending on an internal helper type.
    /// </summary>
    public static class TSNEJsonUtility
    {
        private const int DefaultMaxDepth = 256;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Culture = CultureInfo.InvariantCulture,
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Decimal,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Include
        };

        public static object DeserializeObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON text is empty.", "json");
            }

            using (var stringReader = new StringReader(RemoveBom(json.Trim())))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                jsonReader.DateParseHandling = DateParseHandling.None;
                jsonReader.FloatParseHandling = FloatParseHandling.Decimal;
                jsonReader.MaxDepth = DefaultMaxDepth;

                JToken token = JToken.ReadFrom(jsonReader);
                return ConvertToken(token);
            }
        }

        public static string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value, SerializerSettings);
        }

        public static bool IsJsonException(Exception ex)
        {
            return ex is JsonException
                || ex is ArgumentException
                || ex is InvalidOperationException;
        }

        public static string RemoveBom(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.TrimStart('\uFEFF');
        }

        private static object ConvertToken(JToken token)
        {
            if (token == null)
            {
                return null;
            }

            switch (token.Type)
            {
                case JTokenType.Object:
                    var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (JProperty property in token.Children<JProperty>())
                    {
                        dictionary[property.Name] = ConvertToken(property.Value);
                    }

                    return dictionary;

                case JTokenType.Array:
                    var list = new List<object>();
                    foreach (JToken item in token.Children())
                    {
                        list.Add(ConvertToken(item));
                    }

                    return list;

                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;

                default:
                    JValue value = token as JValue;
                    return value == null ? null : value.Value;
            }
        }
    }
}






namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    /// <summary>Projection adapter for the configured t-SNE engine and explicit Accord comparisons.</summary>
    public sealed class TSNEProjectionModel
    {
        private const int OutputDimensionCount = 2;
        private const double BarnesHutTheta = 0.5d;
        private const float SklearnPcaInitialScale = 1e-4f;
        private const double SklearnPerplexityCap = 30d;
        private const double SklearnPerplexityMinimum = 5d;
        private const int AccordDefaultIterations = 1000;
        private const double AccordDefaultLearningRate = 200d;

        // Accord.NET 3.8 exposes random initialization through Transform only.
        // This version-pinned internal overload keeps Accord's optimizer while
        // preserving the PCA coordinates supplied by this adapter.
        // Resolve Accord only when its baseline is actually selected. Alternative
        // engines must not load or call Accord, including through a type initializer.
        private static class AccordMethods
        {
            internal static readonly MethodInfo PcaInitializedRun = ResolveAccordPcaInitializedRunMethod();
        }
        private static readonly object ProjectionCacheLock = new object();
        // Keep only the most recent embedding and its fingerprint, not another
        // potentially large copy of the high-dimensional input matrix.
        private static ProjectionCacheEntry lastProjection;
        private readonly double[][] coordinates;
        private readonly string engineName = "Accord.NET TSNE (Barnes-Hut)";

        private TSNEProjectionModel(double[][] coordinates, double effectivePerplexity, int randomSeed)
        {
            this.coordinates = CloneMatrix(coordinates);
            EffectivePerplexity = effectivePerplexity;
            // Accord.NET 3.8 does not expose these as configurable TSNE properties.
            // These values describe the effective engine settings, not unused caller hints.
            Iterations = AccordDefaultIterations;
            LearningRate = AccordDefaultLearningRate;
            RandomSeed = randomSeed;
        }

        private TSNEProjectionModel(SKhynix.TAS.Analysis.Tsne.Tsne.Result result)
        {
            coordinates = result.Coordinates;
            engineName = result.EngineName;
            EngineSelectionReason = result.EngineSelectionReason;
            EffectivePerplexity = result.EffectivePerplexity;
            Iterations = result.Iterations;
            LearningRate = result.LearningRate;
            RandomSeed = result.RandomSeed;
            ElapsedMilliseconds = result.ElapsedMilliseconds;
            OptimizationMilliseconds = result.ElapsedMilliseconds;
        }

        public double[][] Coordinates { get { return CloneMatrix(coordinates); } }
        public double EffectivePerplexity { get; private set; }
        public int Iterations { get; private set; }
        public double LearningRate { get; private set; }
        public int RandomSeed { get; private set; }
        public bool CacheHit { get; private set; }
        public double PcaInitializationMilliseconds { get; private set; }
        public double OptimizationMilliseconds { get; private set; }
        public double ElapsedMilliseconds { get; private set; }
        public double KullbackLeiblerDivergence { get { return double.NaN; } }
        public string EngineName { get { return engineName; } }
        public string EngineSelectionReason { get; private set; }

        public static TSNEProjectionModel FitTransform(double[][] standardizedMatrix, double perplexity,
            int iterations, double learningRate, int randomSeed,
            SKhynix.TAS.Analysis.Tsne.Tsne.Engine? libraryEngine)
        {
            if (!libraryEngine.HasValue)
                return FitTransformAccord(standardizedMatrix, perplexity, iterations, learningRate, randomSeed);

            // The adapter resolves the CSharp row limit before computing. Always
            // propagate its actual engine and selection reason to the caller.
            var result = SKhynix.TAS.Analysis.Tsne.Tsne.FitTransform(standardizedMatrix,
                new SKhynix.TAS.Analysis.Tsne.Tsne.Options
                {
                    Engine = libraryEngine.Value,
                    Perplexity = perplexity,
                    Iterations = iterations,
                    LearningRate = learningRate,
                    RandomSeed = randomSeed
                });
            return new TSNEProjectionModel(result);
        }

        public static TSNEProjectionModel FitTransform(double[][] standardizedMatrix, double perplexity, int iterations, double learningRate, int randomSeed)
        {
            return FitTransform(standardizedMatrix, perplexity, iterations, learningRate, randomSeed,
                SKhynix.TAS.Analysis.Tsne.Tsne.DefaultEngine);
        }

        private static TSNEProjectionModel FitTransformAccord(double[][] standardizedMatrix, double perplexity, int iterations, double learningRate, int randomSeed)
        {
            Stopwatch elapsed = Stopwatch.StartNew();
            ValidateMatrix(standardizedMatrix);
            int rowCount = standardizedMatrix.Length;
            double effectivePerplexity = ResolveEffectivePerplexity(rowCount, perplexity);
            // The same private copy is fingerprinted, used for PCA initialization,
            // and finally normalized in place by Accord's optimizer.
            double[][] optimizerInput = CloneMatrix(standardizedMatrix);
            string cacheKey = CreateProjectionCacheKey(optimizerInput, effectivePerplexity, randomSeed);
            ProjectionCacheEntry cached;
            lock (ProjectionCacheLock)
            {
                cached = lastProjection;
            }
            if (cached != null && string.Equals(cached.Key, cacheKey, StringComparison.Ordinal))
            {
                var reused = new TSNEProjectionModel(cached.Model.coordinates, effectivePerplexity, randomSeed);
                reused.CacheHit = true;
                reused.ElapsedMilliseconds = elapsed.Elapsed.TotalMilliseconds;
                return reused;
            }

            // Keep iterations and learningRate in the public contract for callers
            // that share chart settings. Accord.NET 3.8 owns the effective values
            // internally (1000 and 200), so differing caller values must not stop
            // an otherwise valid analysis.

            // sklearn init='pca' uses two PCA scores, fixes each component sign
            // from its loadings, casts to float32, and scales both columns so the
            // population standard deviation of PC1 is 1e-4.
            Stopwatch stage = Stopwatch.StartNew();
            double[][] pcaInitialization = CreateSklearnPcaInitialization(optimizerInput);
            double pcaMilliseconds = stage.Elapsed.TotalMilliseconds;
            double[][] orientationReference = CloneMatrix(pcaInitialization);

            // Accord normalizes X in place and writes the optimized embedding into
            // Y. Pass an X copy so the StandardScaler output used by KNN is intact.
            stage.Restart();
            RunAccordWithPcaInitialization(
                optimizerInput,
                pcaInitialization,
                effectivePerplexity);
            double optimizationMilliseconds = stage.Elapsed.TotalMilliseconds;

            // t-SNE distances are invariant under reflection. Resolve that free
            // sign per data set against the canonical PCA initialization instead
            // of applying a hard-coded X-axis reflection.
            AlignAxisSignsToPca(pcaInitialization, orientationReference);

            // Python's reference chart uses the opposite horizontal orientation
            // for this otherwise equivalent t-SNE embedding. Apply the reflection
            // to the shared coordinates so the chart, overlays, and exported model
            // all use the same visual orientation.
            ReflectHorizontalAxis(pcaInitialization);
            ReflectVerticalAxis(pcaInitialization);
            // Apply the requested additional horizontal reflection to the final
            // orientation. This second X reflection cancels the first one.
            ReflectHorizontalAxis(pcaInitialization);

            var model = new TSNEProjectionModel(pcaInitialization, effectivePerplexity, randomSeed);
            model.PcaInitializationMilliseconds = pcaMilliseconds;
            model.OptimizationMilliseconds = optimizationMilliseconds;
            model.ElapsedMilliseconds = elapsed.Elapsed.TotalMilliseconds;
            lock (ProjectionCacheLock)
            {
                lastProjection = new ProjectionCacheEntry(cacheKey, model);
            }
            return model;
        }

        private static string CreateProjectionCacheKey(double[][] matrix, double perplexity, int randomSeed)
        {
            // Include shape, row/feature order, every double's exact binary value,
            // and effective settings. Labels and Draft identifiers are assembled
            // from the current request after projection and are not cached.
            using (SHA256 hash = SHA256.Create())
            {
                byte[] header = new byte[20];
                Buffer.BlockCopy(BitConverter.GetBytes(matrix.Length), 0, header, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(matrix[0].Length), 0, header, 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(perplexity), 0, header, 8, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(randomSeed), 0, header, 16, 4);
                hash.TransformBlock(header, 0, header.Length, header, 0);
                byte[] buffer = new byte[8192];
                for (int row = 0; row < matrix.Length; row++)
                {
                    int valueOffset = 0;
                    while (valueOffset < matrix[row].Length)
                    {
                        int valueCount = Math.Min(buffer.Length / sizeof(double), matrix[row].Length - valueOffset);
                        int byteCount = valueCount * sizeof(double);
                        Buffer.BlockCopy(matrix[row], valueOffset * sizeof(double), buffer, 0, byteCount);
                        hash.TransformBlock(buffer, 0, byteCount, buffer, 0);
                        valueOffset += valueCount;
                    }
                }
                hash.TransformFinalBlock(new byte[0], 0, 0);
                return Convert.ToBase64String(hash.Hash);
            }
        }

        private sealed class ProjectionCacheEntry
        {
            public ProjectionCacheEntry(string key, TSNEProjectionModel model)
            {
                Key = key;
                Model = model;
            }

            public readonly string Key;
            public readonly TSNEProjectionModel Model;
        }

        private static void ReflectHorizontalAxis(double[][] coordinates)
        {
            for (int row = 0; row < coordinates.Length; row++)
            {
                coordinates[row][0] = -coordinates[row][0];
            }
        }

        private static void ReflectVerticalAxis(double[][] coordinates)
        {
            for (int row = 0; row < coordinates.Length; row++)
            {
                coordinates[row][1] = -coordinates[row][1];
            }
        }

        private static double[][] CreateSklearnPcaInitialization(double[][] standardizedMatrix)
        {
            var pca = new PrincipalComponentAnalysis(
                PrincipalComponentMethod.Center,
                false,
                OutputDimensionCount);

            // Learn already creates its centered working matrix when Overwrite
            // is false; an additional input clone duplicates that allocation.
            pca.Overwrite = false;
            pca.Learn(standardizedMatrix, null);
            double[][] scores = pca.Transform(
                standardizedMatrix,
                CreateMatrix(standardizedMatrix.Length, OutputDimensionCount));

            ApplySklearnComponentSigns(scores, pca.ComponentVectors);
            QuantizeToSinglePrecision(scores);

            float firstComponentDeviation = (float)GetPopulationStandardDeviation(scores, 0);
            if (firstComponentDeviation <= 0f
                || float.IsNaN(firstComponentDeviation)
                || float.IsInfinity(firstComponentDeviation))
            {
                throw new InvalidOperationException("PCA initialization requires a non-zero first component variance.");
            }

            float scale = SklearnPcaInitialScale / firstComponentDeviation;
            for (int row = 0; row < scores.Length; row++)
            {
                for (int component = 0; component < OutputDimensionCount; component++)
                {
                    scores[row][component] = (double)((float)scores[row][component] * scale);
                }
            }

            return scores;
        }

        private static void ApplySklearnComponentSigns(double[][] scores, double[][] componentVectors)
        {
            if (componentVectors == null || componentVectors.Length < OutputDimensionCount)
            {
                throw new InvalidOperationException("Accord.NET PCA did not return the two component vectors required by t-SNE.");
            }

            for (int component = 0; component < OutputDimensionCount; component++)
            {
                double[] loadings = componentVectors[component];
                if (loadings == null || loadings.Length == 0)
                {
                    throw new InvalidOperationException("Accord.NET PCA returned an empty component vector.");
                }

                int maximumIndex = 0;
                double maximumMagnitude = Math.Abs(loadings[0]);
                for (int feature = 1; feature < loadings.Length; feature++)
                {
                    double magnitude = Math.Abs(loadings[feature]);
                    if (magnitude > maximumMagnitude)
                    {
                        maximumMagnitude = magnitude;
                        maximumIndex = feature;
                    }
                }

                if (loadings[maximumIndex] < 0d)
                {
                    for (int row = 0; row < scores.Length; row++)
                    {
                        scores[row][component] = -scores[row][component];
                    }
                }
            }
        }

        private static void QuantizeToSinglePrecision(double[][] matrix)
        {
            for (int row = 0; row < matrix.Length; row++)
            {
                for (int column = 0; column < matrix[row].Length; column++)
                {
                    matrix[row][column] = (double)(float)matrix[row][column];
                }
            }
        }

        private static double GetPopulationStandardDeviation(double[][] matrix, int column)
        {
            double mean = 0d;
            for (int row = 0; row < matrix.Length; row++)
            {
                mean += matrix[row][column];
            }

            mean /= matrix.Length;
            double squaredDeviation = 0d;
            for (int row = 0; row < matrix.Length; row++)
            {
                double difference = matrix[row][column] - mean;
                squaredDeviation += difference * difference;
            }

            return Math.Sqrt(squaredDeviation / matrix.Length);
        }

        private static void RunAccordWithPcaInitialization(
            double[][] input,
            double[][] initializedCoordinates,
            double perplexity)
        {
            MethodInfo accordRun = AccordMethods.PcaInitializedRun;
            if (accordRun == null)
            {
                throw new NotSupportedException(
                    "The installed Accord.NET version does not provide the PCA-initialized t-SNE execution path required by this component.");
            }

            try
            {
                accordRun.Invoke(
                    null,
                    new object[] { input, initializedCoordinates, perplexity, BarnesHutTheta, true });
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }

                throw;
            }
        }

        private static MethodInfo ResolveAccordPcaInitializedRunMethod()
        {
            return typeof(TSNE).GetMethod(
                "run",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(double[][]),
                    typeof(double[][]),
                    typeof(double),
                    typeof(double),
                    typeof(bool)
                },
                null);
        }

        private static void AlignAxisSignsToPca(double[][] coordinates, double[][] pcaReference)
        {
            for (int component = 0; component < OutputDimensionCount; component++)
            {
                double coordinateMean = 0d;
                double referenceMean = 0d;
                for (int row = 0; row < coordinates.Length; row++)
                {
                    coordinateMean += coordinates[row][component];
                    referenceMean += pcaReference[row][component];
                }

                coordinateMean /= coordinates.Length;
                referenceMean /= pcaReference.Length;

                double covariance = 0d;
                for (int row = 0; row < coordinates.Length; row++)
                {
                    covariance += (coordinates[row][component] - coordinateMean)
                        * (pcaReference[row][component] - referenceMean);
                }

                if (covariance < 0d)
                {
                    for (int row = 0; row < coordinates.Length; row++)
                    {
                        coordinates[row][component] = -coordinates[row][component];
                    }
                }
            }
        }

        private static double ResolveEffectivePerplexity(int rowCount, double requestedPerplexity)
        {
            // Match the Python reference expression:
            // min(30, max(5, n_samples - 1) // 3).
            // Math.Floor is the integer (//) operation used by Python.
            double sampleBound = Math.Floor(Math.Max(SklearnPerplexityMinimum, rowCount - 1d) / 3d);
            // Accord's neighbor lookup fails when 3 * perplexity is exactly
            // n_samples - 1, so keep its effective value infinitesimally below
            // that boundary while retaining the Python value for normal sizes.
            double accordBound = (rowCount - 1d) / 3d - 1e-6d;
            double maximum = Math.Min(SklearnPerplexityCap, Math.Min(sampleBound, accordBound));
            if (maximum <= 0d)
            {
                throw new ArgumentException("Accord.NET t-SNE could not resolve a positive perplexity for the input size.", "standardizedMatrix");
            }

            double requested = double.IsNaN(requestedPerplexity) || double.IsInfinity(requestedPerplexity)
                ? SklearnPerplexityCap
                : requestedPerplexity;
            return Math.Max(Math.Min(1d, maximum), Math.Min(requested, maximum));
        }

        private static double[][] CreateMatrix(int rowCount, int columnCount)
        {
            var result = new double[rowCount][];
            for (int row = 0; row < rowCount; row++) result[row] = new double[columnCount];
            return result;
        }

        private static double[][] CloneMatrix(double[][] matrix)
        {
            if (matrix == null) return new double[0][];
            var result = new double[matrix.Length][];
            for (int row = 0; row < matrix.Length; row++) result[row] = matrix[row] == null ? new double[0] : (double[])matrix[row].Clone();
            return result;
        }

        private static void ValidateMatrix(double[][] matrix)
        {
            if (matrix == null || matrix.Length < 3 || matrix[0] == null || matrix[0].Length < 2)
                throw new ArgumentException("Accord.NET t-SNE requires at least three rows and two numeric features.", "standardizedMatrix");
            int columnCount = matrix[0].Length;
            for (int row = 0; row < matrix.Length; row++)
            {
                if (matrix[row] == null || matrix[row].Length != columnCount)
                    throw new ArgumentException("t-SNE input must be a rectangular matrix.", "standardizedMatrix");
                for (int column = 0; column < columnCount; column++)
                    if (double.IsNaN(matrix[row][column]) || double.IsInfinity(matrix[row][column]))
                        throw new ArgumentException("t-SNE input must contain finite values.", "standardizedMatrix");
            }
        }
    }
}






namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{

    /// <summary>
    /// TSNE 결과 한 건을 LightningChart와 KNN 결과 그리드에 전달하는 화면 데이터 계약이다.
    /// X1/X2는 임의 좌표가 아니라 TSNEAnalysisPipeline에서 계산한 PC1/PC2 점수다.
    /// </summary>
}
