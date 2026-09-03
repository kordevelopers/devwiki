using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Accord.MachineLearning.Clustering;
using Accord.Math.Random;
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
    /// <summary>Accord.NET 3.8 Barnes-Hut t-SNE adapter.</summary>
    public sealed class TSNEProjectionModel
    {
        private const double SklearnPerplexityCap = 30d;
        private const double SklearnPerplexityMinimum = 5d;
        private const int AccordDefaultIterations = 1000;
        private const double AccordDefaultLearningRate = 200d;

        // Accord.NET keeps its random generator in process-wide state. Serialize
        // seeded runs so two concurrent chart refreshes cannot change each other.
        private static readonly object AccordRandomSync = new object();
        private readonly double[][] coordinates;

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

        public double[][] Coordinates { get { return CloneMatrix(coordinates); } }
        public double EffectivePerplexity { get; private set; }
        public int Iterations { get; private set; }
        public double LearningRate { get; private set; }
        public int RandomSeed { get; private set; }
        public double KullbackLeiblerDivergence { get { return double.NaN; } }
        public string EngineName { get { return "Accord.NET TSNE (Barnes-Hut)"; } }

        public static TSNEProjectionModel FitTransform(double[][] standardizedMatrix, double perplexity, int iterations, double learningRate, int randomSeed)
        {
            ValidateMatrix(standardizedMatrix);
            int rowCount = standardizedMatrix.Length;
            double effectivePerplexity = ResolveEffectivePerplexity(rowCount, perplexity);
            var model = new TSNE
            {
                Perplexity = effectivePerplexity,
                Theta = 0.5d,
                NumberOfOutputs = 2
            };

            double[][] transformed;
            lock (AccordRandomSync)
            {
                int? previousSeed = Generator.Seed;
                int? previousThreadSeed = Generator.ThreadSeed;
                try
                {
                    if (randomSeed >= 0)
                    {
                        // Apply the requested random_state seed to the Accord engine.
                        Generator.Seed = randomSeed;
                        Generator.ThreadSeed = randomSeed;
                    }

                    // Accord.NET 3.8 internally uses max_iter=1000 and eta=200. It
                    // initializes the low-dimensional map randomly; its public API
                    // has no init='pca' or learning_rate='auto' hook. The engine
                    // normalizes its input in place, so pass a copy and keep the
                    // original StandardScaler output unchanged for Euclidean KNN.
                    transformed = model.Transform(CloneMatrix(standardizedMatrix), CreateMatrix(rowCount, 2));
                    // Align the horizontal orientation with the Python reference
                    // chart. Reflection does not change t-SNE distances or clusters.
                    ReflectXAxis(transformed);
                }
                finally
                {
                    if (randomSeed >= 0)
                    {
                        Generator.ThreadSeed = previousThreadSeed;
                        Generator.Seed = previousSeed;
                    }
                }
            }

            return new TSNEProjectionModel(transformed, effectivePerplexity, randomSeed);
        }

        private static double ResolveEffectivePerplexity(int rowCount, double requestedPerplexity)
        {
            // Match the Python reference expression:
            // min(30, max(5, n_samples - 1) // 3).
            // Math.Floor is the integer (//) operation used by Python.
            double sampleBound = Math.Floor(Math.Max(SklearnPerplexityMinimum, rowCount - 1d) / 3d);
            double accordBound = Math.Floor((rowCount - 1d) / 3d);
            double maximum = Math.Min(SklearnPerplexityCap, Math.Min(sampleBound, accordBound));
            if (maximum < 1d)
            {
                throw new ArgumentException("Accord.NET t-SNE requires at least four samples for the configured perplexity rule.", "standardizedMatrix");
            }

            double requested = double.IsNaN(requestedPerplexity) || double.IsInfinity(requestedPerplexity)
                ? SklearnPerplexityCap
                : requestedPerplexity;
            return Math.Max(1d, Math.Min(requested, maximum));
        }

        private static void ReflectXAxis(double[][] coordinates)
        {
            if (coordinates == null)
            {
                return;
            }

            for (int row = 0; row < coordinates.Length; row++)
            {
                if (coordinates[row] != null && coordinates[row].Length > 0)
                {
                    coordinates[row][0] = -coordinates[row][0];
                }
            }
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
