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
using WinFormsControl = System.Windows.Forms.Control;
using Accord.MachineLearning.Clustering;
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
        private readonly double[][] coordinates;

        private TSNEProjectionModel(double[][] coordinates, double effectivePerplexity)
        {
            this.coordinates = CloneMatrix(coordinates);
            EffectivePerplexity = effectivePerplexity;
        }

        public double[][] Coordinates { get { return CloneMatrix(coordinates); } }
        public double EffectivePerplexity { get; private set; }
        public int Iterations { get { return 0; } }
        public double LearningRate { get { return 0d; } }
        public int RandomSeed { get { return 0; } }
        public double KullbackLeiblerDivergence { get { return double.NaN; } }
        public string EngineName { get { return "Accord.NET TSNE (Barnes-Hut)"; } }

        public static TSNEProjectionModel FitTransform(double[][] standardizedMatrix, double perplexity, int iterations, double learningRate, int randomSeed)
        {
            ValidateMatrix(standardizedMatrix);
            int rowCount = standardizedMatrix.Length;
            double effectivePerplexity = Math.Max(1d, Math.Min(perplexity, Math.Max(1d, (rowCount - 1d) / 3d - 1e-6d)));
            var model = new TSNE
            {
                Perplexity = effectivePerplexity,
                Theta = 0.5d,
                NumberOfOutputs = 2
            };
            double[][] transformed = model.Transform(standardizedMatrix, CreateMatrix(rowCount, 2));
            return new TSNEProjectionModel(transformed, effectivePerplexity);
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
