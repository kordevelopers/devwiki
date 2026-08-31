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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.Common;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public sealed class ConvExperimentQueryOptions
    {
        public ConvExperimentQueryOptions()
        {
            JsonColumnName = "CONV_EXPER_CTN";
            DraftNoColumnName = "DRAFT_NO";
            ParameterTypeColumnName = "PARAM_TYP";
            AiResultColumnName = "AI_RSLT_VAL";
            LabelColumnName = "ENGR_RSLT_VAL";
        }

        public string JsonColumnName { get; set; }
        public string DraftNoColumnName { get; set; }
        public string ParameterTypeColumnName { get; set; }
        public string AiResultColumnName { get; set; }
        public string LabelColumnName { get; set; }

        public static ConvExperimentQueryOptions FromConfiguration()
        {
            return new ConvExperimentQueryOptions();
        }
    }

    public interface ITSNEExadataRowRepository
    {
        IList<TSNEExadataSourceRow> LoadAll();
    }

    public sealed class ConvExperimentRepository : ITSNEExadataRowRepository
    {
        private readonly ConvExperimentQueryOptions options;
        private DataTable sourceTable;

        public ConvExperimentRepository()
            : this(null, ConvExperimentQueryOptions.FromConfiguration())
        {
        }

        public ConvExperimentRepository(DataTable sourceTable)
            : this(sourceTable, ConvExperimentQueryOptions.FromConfiguration())
        {
        }

        public ConvExperimentRepository(ConvExperimentQueryOptions options)
            : this(null, options)
        {
        }

        public ConvExperimentRepository(DataTable sourceTable, ConvExperimentQueryOptions options)
        {
            this.sourceTable = sourceTable;
            this.options = options ?? ConvExperimentQueryOptions.FromConfiguration();
        }

        public void SetSourceTable(DataTable table)
        {
            sourceTable = table;
        }

        public IList<TSNEExadataSourceRow> LoadAll()
        {
            return LoadFromDataTable(sourceTable, options);
        }

        public static IList<TSNEExadataSourceRow> LoadFromDataTable(DataTable table)
        {
            return LoadFromDataTable(table, ConvExperimentQueryOptions.FromConfiguration());
        }

        public static IList<TSNEExadataSourceRow> LoadFromDataTable(DataTable table, ConvExperimentQueryOptions options)
        {
            if (table == null)
            {
                throw new InvalidOperationException(
                    "CONV_EXPER_CTN DataTable is required. Load data through the company service and pass the DataTable.");
            }

            ConvExperimentQueryOptions effectiveOptions = options ?? ConvExperimentQueryOptions.FromConfiguration();
            DataColumn jsonColumn = FindColumn(table, effectiveOptions.JsonColumnName);
            DataColumn draftNoColumn = FindColumn(table, effectiveOptions.DraftNoColumnName);
            DataColumn parameterTypeColumn = FindColumn(table, effectiveOptions.ParameterTypeColumnName);
            DataColumn aiResultColumn = FindOptionalColumn(
                table,
                effectiveOptions.AiResultColumnName,
                "AI_RSLT_VAL",
                "AI_RSLT_Val");
            DataColumn labelColumn = FindColumn(
                table,
                effectiveOptions.LabelColumnName,
                "ENGR_RSLT_VAL",
                "LABEL_Y",
                "AI_RSLT_VAL",
                "AI_RSLT_Val");

            var rows = new List<TSNEExadataSourceRow>();
            for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                DataRow dataRow = table.Rows[rowIndex];
                string draftNo = ReadRequiredText(
                    dataRow,
                    draftNoColumn,
                    rowIndex);
                string parameterTypeText = ReadRequiredText(
                    dataRow,
                    parameterTypeColumn,
                    rowIndex);
                string labelY = ReadOptionalText(
                    dataRow,
                    labelColumn);
                string aiResultValue = aiResultColumn == null
                    ? string.Empty
                    : ReadOptionalText(dataRow, aiResultColumn);

                TSNEParameterType parameterType;
                if (!TSNEParameterTypeParser.TryParse(parameterTypeText, out parameterType))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "PARAM_TYP[{0}] value '{1}' is not supported.",
                            rowIndex,
                            parameterTypeText));
                }

                rows.Add(new TSNEExadataSourceRow(
                    rowIndex,
                    draftNo,
                    parameterType,
                    aiResultValue,
                    labelY,
                    ReadJsonText(dataRow, jsonColumn)));
            }

            if (rows.Count == 0)
            {
                throw new InvalidOperationException(
                    "The CONV_EXPER_CTN DataTable contains no rows for TSNE analysis.");
            }

            return rows;
        }

        private static DataColumn FindColumn(DataTable table, string columnName, params string[] fallbackColumnNames)
        {
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(columnName))
            {
                candidates.Add(columnName.Trim());
            }

            if (fallbackColumnNames != null)
            {
                foreach (string fallback in fallbackColumnNames)
                {
                    if (!string.IsNullOrWhiteSpace(fallback)
                        && !candidates.Exists(candidate => string.Equals(
                            candidate,
                            fallback.Trim(),
                            StringComparison.OrdinalIgnoreCase)))
                    {
                        candidates.Add(fallback.Trim());
                    }
                }
            }

            if (candidates.Count == 0)
            {
                throw new ArgumentException("Column name is required.", "columnName");
            }

            foreach (string candidate in candidates)
            {
                foreach (DataColumn column in table.Columns)
                {
                    if (string.Equals(column.ColumnName, candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        return column;
                    }
                }
            }

            throw new InvalidOperationException(
                "The DataTable does not contain required column '" + string.Join("' or '", candidates.ToArray()) + "'.");
        }

        private static DataColumn FindOptionalColumn(DataTable table, string columnName, params string[] fallbackColumnNames)
        {
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(columnName))
            {
                candidates.Add(columnName.Trim());
            }

            if (fallbackColumnNames != null)
            {
                foreach (string fallback in fallbackColumnNames)
                {
                    if (!string.IsNullOrWhiteSpace(fallback)
                        && !candidates.Exists(candidate => string.Equals(
                            candidate,
                            fallback.Trim(),
                            StringComparison.OrdinalIgnoreCase)))
                    {
                        candidates.Add(fallback.Trim());
                    }
                }
            }

            foreach (string candidate in candidates)
            {
                foreach (DataColumn column in table.Columns)
                {
                    if (string.Equals(column.ColumnName, candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        return column;
                    }
                }
            }

            return null;
        }

        private static string ReadRequiredText(DataRow row, DataColumn column, int rowIndex)
        {
            object value = row[column];
            if (value == null || value == DBNull.Value)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "DataTable column {0}[{1}] is NULL.",
                        column.ColumnName,
                        rowIndex));
            }

            string text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "DataTable column {0}[{1}] is empty.",
                        column.ColumnName,
                        rowIndex));
            }

            return text.Trim();
        }

        private static string ReadOptionalText(DataRow row, DataColumn column)
        {
            object value = row[column];
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            string text = Convert.ToString(value, CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        }

        private static string ReadJsonText(DataRow row, DataColumn column)
        {
            object value = row[column];
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            TextReader textReader = value as TextReader;
            if (textReader != null)
            {
                return textReader.ReadToEnd();
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}






namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public enum TSNEParameterType
    {
        Response,
        Defect,
        Epm,
        Probe
    }

    public enum TSNEExadataRefreshMode
    {
        AlwaysReload,
        PreferMemorySnapshot
    }

    public static class TSNEParameterTypeParser
    {
        public static bool TryParse(string value, out TSNEParameterType parameterType)
        {
            parameterType = TSNEParameterType.Response;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            switch (value.Trim().ToUpperInvariant())
            {
                case "RESPONSE":
                    parameterType = TSNEParameterType.Response;
                    return true;
                case "DEFECT":
                    parameterType = TSNEParameterType.Defect;
                    return true;
                case "EPM":
                    parameterType = TSNEParameterType.Epm;
                    return true;
                case "PROBE":
                    parameterType = TSNEParameterType.Probe;
                    return true;
                default:
                    return false;
            }
        }

        public static string ToDatabaseValue(TSNEParameterType parameterType)
        {
            return parameterType.ToString().ToUpperInvariant();
        }
    }

    public sealed class TSNEExadataSourceRow
    {
        public TSNEExadataSourceRow(int sourceRowIndex, string draftNo, TSNEParameterType parameterType, string labelY, string rawConvExperimentJson)
            : this(sourceRowIndex, draftNo, parameterType, string.Empty, labelY, rawConvExperimentJson)
        {
        }

        public TSNEExadataSourceRow(int sourceRowIndex, string draftNo, TSNEParameterType parameterType, string aiResultValue, string labelY, string rawConvExperimentJson)
        {
            SourceRowIndex = sourceRowIndex;
            DraftNo = (draftNo ?? string.Empty).Trim();
            ParameterType = parameterType;
            AiResultValue = (aiResultValue ?? string.Empty).Trim();
            LabelY = (labelY ?? string.Empty).Trim();
            RawConvExperimentJson = rawConvExperimentJson ?? string.Empty;
        }

        public int SourceRowIndex { get; private set; }
        public string DraftNo { get; private set; }
        public TSNEParameterType ParameterType { get; private set; }
        public string AiResultValue { get; private set; }
        public string LabelY { get; private set; }
        public string RawConvExperimentJson { get; private set; }
    }

    public sealed class TSNEExadataSnapshot
    {
        public TSNEExadataSnapshot(IEnumerable<TSNEExadataSourceRow> rows, DateTime loadedAtUtc)
        {
            Rows = new ReadOnlyCollection<TSNEExadataSourceRow>(
                (rows ?? Enumerable.Empty<TSNEExadataSourceRow>()).ToList());
            LoadedAtUtc = DateTime.SpecifyKind(loadedAtUtc, DateTimeKind.Utc);
        }

        public IList<TSNEExadataSourceRow> Rows { get; private set; }
        public DateTime LoadedAtUtc { get; private set; }
    }

    public sealed class TSNEExperimentRecord
    {
        private readonly double[] standardizedVector;

        internal TSNEExperimentRecord(
            TSNEExadataSourceRow source,
            IDictionary<string, object> flattenedValues,
            IDictionary<string, double> numericFeatures,
            double[] standardizedVector,
            double x1,
            double x2)
        {
            SourceRowIndex = source.SourceRowIndex;
            DraftNo = source.DraftNo;
            ParameterType = source.ParameterType;
            AiResultValue = source.AiResultValue;
            LabelY = source.LabelY;
            RawConvExperimentJson = source.RawConvExperimentJson;
            FlattenedValues = new ReadOnlyDictionary<string, object>(
                new Dictionary<string, object>(
                    flattenedValues ?? new Dictionary<string, object>(),
                    StringComparer.OrdinalIgnoreCase));
            NumericFeatures = new ReadOnlyDictionary<string, double>(
                new Dictionary<string, double>(
                    numericFeatures ?? new Dictionary<string, double>(),
                    StringComparer.OrdinalIgnoreCase));
            this.standardizedVector = standardizedVector == null
                ? new double[0]
                : (double[])standardizedVector.Clone();
            X1 = x1;
            X2 = x2;
        }

        public int SourceRowIndex { get; private set; }
        public string DraftNo { get; private set; }
        public TSNEParameterType ParameterType { get; private set; }
        public string AiResultValue { get; private set; }
        public string LabelY { get; private set; }
        public string RawConvExperimentJson { get; private set; }
        public IReadOnlyDictionary<string, object> FlattenedValues { get; private set; }
        public IReadOnlyDictionary<string, double> NumericFeatures { get; private set; }
        public double[] StandardizedVector
        {
            get { return (double[])standardizedVector.Clone(); }
        }
        public double X1 { get; private set; }
        public double X2 { get; private set; }
    }

    public sealed class TSNEExadataAnalysisResult
    {
        internal TSNEExadataAnalysisResult(
            TSNEExadataSnapshot snapshot,
            TSNEParameterType parameterType,
            TSNEAnalysisResult analysisResult,
            IList<TSNEExperimentRecord> records,
            int missingExperimentCount,
            TSNEFeatureSelectionReport featureSelectionReport)
        {
            Snapshot = snapshot;
            ParameterType = parameterType;
            AnalysisResult = analysisResult;
            Records = new ReadOnlyCollection<TSNEExperimentRecord>(
                (records ?? new List<TSNEExperimentRecord>()).ToList());
            MissingExperimentCount = missingExperimentCount;
            FeatureSelectionReport = featureSelectionReport
                ?? (analysisResult == null ? null : analysisResult.FeatureSelectionReport)
                ?? TSNEFeatureSelectionReport.Empty();
            Diagnostic = TSNEAnalysisDiagnosticReport.Create(
                analysisResult,
                Records.Count,
                MissingExperimentCount);
        }

        public TSNEExadataSnapshot Snapshot { get; private set; }
        public TSNEParameterType ParameterType { get; private set; }
        public TSNEAnalysisResult AnalysisResult { get; private set; }
        public IList<TSNEExperimentRecord> Records { get; private set; }
        public int MissingExperimentCount { get; private set; }
        public TSNEAnalysisDiagnosticReport Diagnostic { get; private set; }
        public TSNEFeatureSelectionReport FeatureSelectionReport { get; private set; }

        public DataTable CreateFeatureSelectionDataTable()
        {
            return (FeatureSelectionReport ?? TSNEFeatureSelectionReport.Empty()).ToDataTable();
        }

        public DataTable CreateSurvivingPopulationDataTable()
        {
            DataTable table = new DataTable("TSNE_SURVIVING_POPULATION");
            table.Columns.Add("DRAFT_NO", typeof(string));
            table.Columns.Add("PARAM_TYP", typeof(string));
            table.Columns.Add("LABEL_Y", typeof(string));
            table.Columns.Add("X1", typeof(double));
            table.Columns.Add("X2", typeof(double));

            string[] featureNames = AnalysisResult == null || AnalysisResult.FeatureNames == null
                ? new string[0]
                : AnalysisResult.FeatureNames;
            var featureColumnNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string featureName in featureNames)
            {
                string columnName = ResolveFeatureColumnName(table, featureName);
                featureColumnNames[featureName] = columnName;
                if (!table.Columns.Contains(columnName))
                {
                    table.Columns.Add(columnName, typeof(double));
                }
            }

            foreach (TSNEExperimentRecord record in Records)
            {
                DataRow row = table.NewRow();
                row["DRAFT_NO"] = record.DraftNo;
                row["PARAM_TYP"] = TSNEParameterTypeParser.ToDatabaseValue(record.ParameterType);
                row["LABEL_Y"] = record.LabelY;
                row["X1"] = record.X1;
                row["X2"] = record.X2;
                foreach (string featureName in featureNames)
                {
                    double value;
                    string columnName = featureColumnNames[featureName];
                    row[columnName] = record.NumericFeatures != null
                        && record.NumericFeatures.TryGetValue(featureName, out value)
                            ? (object)value
                            : DBNull.Value;
                }

                table.Rows.Add(row);
            }

            return table;
        }

        public DataTable CreateRawDataTable()
        {
            DataTable table = new DataTable("RAWDATA");
            table.Columns.Add("DRAFT_NO", typeof(string));
            table.Columns.Add("PARAM_TYP", typeof(string));
            table.Columns.Add("CONV_EXPER_CTN", typeof(string));
            table.Columns.Add("AI_RSLT_VAL", typeof(string));
            table.Columns.Add("ENGR_RSLT_VAL", typeof(string));
            table.Columns.Add("X1", typeof(double));
            table.Columns.Add("X2", typeof(double));

            foreach (TSNEExperimentRecord record in Records)
            {
                DataRow row = table.NewRow();
                row["DRAFT_NO"] = record.DraftNo;
                row["PARAM_TYP"] = TSNEParameterTypeParser.ToDatabaseValue(record.ParameterType);
                row["CONV_EXPER_CTN"] = record.RawConvExperimentJson;
                row["AI_RSLT_VAL"] = record.AiResultValue;
                row["ENGR_RSLT_VAL"] = record.LabelY;
                row["X1"] = record.X1;
                row["X2"] = record.X2;
                table.Rows.Add(row);
            }

            return table;
        }

        public IList<KnnNeighbor> FindNearestByChartDistance(string draftNo, int count)
        {
            return FindNearestByChartDistance(draftNo, count, false);
        }

        public IList<KnnNeighbor> FindNearestByChartDistance(string draftNo, int count, bool labeledOnly)
        {
            if (string.IsNullOrWhiteSpace(draftNo))
            {
                return new List<KnnNeighbor>();
            }

            int targetIndex = -1;
            for (int index = 0; index < Records.Count; index++)
            {
                TSNEExperimentRecord record = Records[index];
                if (record != null && string.Equals(record.DraftNo, draftNo.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    targetIndex = index;
                    break;
                }
            }

            if (targetIndex < 0)
            {
                return new List<KnnNeighbor>();
            }

            TSNEExperimentRecord target = Records[targetIndex];
            int safeCount = Math.Max(1, count);
            List<KnnNeighbor> neighbors = Records
                .Select((record, index) => new { Record = record, Index = index })
                .Where(item => item.Record != null && item.Index != targetIndex)
                .Where(item => !labeledOnly || !string.IsNullOrWhiteSpace(item.Record.LabelY))
                .Select(item => new KnnNeighbor
                {
                    SourceIndex = item.Index,
                    DraftNo = item.Record.DraftNo,
                    Distance = CalculateChartDistance(target, item.Record)
                })
                .OrderBy(item => item.Distance)
                .ThenBy(item => item.DraftNo, StringComparer.OrdinalIgnoreCase)
                .Take(safeCount)
                .ToList();

            for (int index = 0; index < neighbors.Count; index++)
            {
                neighbors[index].Rank = index + 1;
            }

            return neighbors;
        }

        private static double CalculateChartDistance(TSNEExperimentRecord left, TSNEExperimentRecord right)
        {
            double dx = left.X1 - right.X1;
            double dy = left.X2 - right.X2;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static string ResolveFeatureColumnName(DataTable table, string featureName)
        {
            string baseName = string.IsNullOrWhiteSpace(featureName)
                ? "FEATURE"
                : featureName;
            if (!table.Columns.Contains(baseName))
            {
                return baseName;
            }

            string prefixed = "FEATURE_" + baseName;
            if (!table.Columns.Contains(prefixed))
            {
                return prefixed;
            }

            int index = 1;
            string candidate;
            do
            {
                candidate = prefixed + "_" + index.ToString(CultureInfo.InvariantCulture);
                index++;
            }
            while (table.Columns.Contains(candidate));
            return candidate;
        }
    }

    public sealed class TSNEDraftQueryResult
    {
        internal TSNEDraftQueryResult(TSNEExadataAnalysisResult analysis, TSNEExperimentRecord target, IList<KnnNeighbor> neighbors, bool usedMemorySnapshot)
        {
            Analysis = analysis;
            Target = target;
            Neighbors = new ReadOnlyCollection<KnnNeighbor>(
                (neighbors ?? new List<KnnNeighbor>()).ToList());
            UsedMemorySnapshot = usedMemorySnapshot;
        }

        public TSNEExadataAnalysisResult Analysis { get; private set; }
        public TSNEAnalysisResult AnalysisResult
        {
            get { return Analysis == null ? null : Analysis.AnalysisResult; }
        }

        public TSNEExperimentRecord Target { get; private set; }
        public IList<KnnNeighbor> Neighbors { get; private set; }
        public bool UsedMemorySnapshot { get; private set; }
    }

    public sealed class TSNEExperimentDataMissingException : InvalidOperationException
    {
        public TSNEExperimentDataMissingException(string draftNo)
            : base("No experiment data found for DRAFT_NO '" + (draftNo ?? string.Empty) + "'.")
        {
            DraftNo = draftNo ?? string.Empty;
        }

        public string DraftNo { get; private set; }
    }

    internal sealed class ParsedTSNEExperiment
    {
        public TSNEExadataSourceRow Source { get; set; }
        public IDictionary<string, object> FlattenedValues { get; set; }
        public IDictionary<string, double> NumericFeatures { get; set; }
    }

    internal sealed class ConvExperimentRowParser
    {
        /// <summary>
        /// CONV_EXPER_CTN JSON 한 건을 TSNE가 사용할 수 있는 원본값 사전과 수치 feature 사전으로 바꾼다.
        /// </summary>
        public bool TryParse(TSNEExadataSourceRow source, out ParsedTSNEExperiment experiment)
        {
            experiment = null;
            if (source == null || string.IsNullOrWhiteSpace(source.RawConvExperimentJson))
            {
                return false;
            }

            object root;
            try
            {
                // Newtonsoft 기반 유틸로 JSON 문자열을 Dictionary/List 구조로 변환한다.
                root = TSNEJsonUtility.DeserializeObject(
                    source.RawConvExperimentJson.Trim().TrimStart('\uFEFF'));
            }
            catch (Exception ex) when (TSNEJsonUtility.IsJsonException(ex))
            {
                throw new FormatException(
                    string.Format(
                        "CONV_EXPER_CTN[{0}] JSON parsing failed. DRAFT_NO={1}: {2}",
                        source.SourceRowIndex,
                        source.DraftNo,
                        ex.Message),
                    ex);
            }

            IList<object> items = ToObjectList(root);
            if (items.Count == 0)
            {
                return false;
            }

            if (items.Count != 1)
            {
                throw new FormatException(
                    string.Format(
                        "CONV_EXPER_CTN[{0}] must contain exactly one experiment object. DRAFT_NO={1}, Count={2}",
                        source.SourceRowIndex,
                        source.DraftNo,
                        items.Count));
            }

            var dictionary = items[0] as IDictionary<string, object>;
            if (dictionary == null)
            {
                throw new FormatException(
                    string.Format(
                        "CONV_EXPER_CTN[{0}] array element is not a JSON object. DRAFT_NO={1}",
                        source.SourceRowIndex,
                        source.DraftNo));
            }

            var flattened = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            // 중첩 객체와 배열은 "부모.자식", "배열[0]" 형태의 key로 펼쳐 feature 후보를 잃지 않게 한다.
            Flatten(dictionary, flattened, string.Empty, 0);
            var numeric = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, object> pair in flattened)
            {
                double value;
                // 메타데이터와 문자열 설명값은 제외하고, 유한한 숫자로 바뀌는 값만 TSNE feature가 된다.
                if (!IsMetadataFeature(pair.Key)
                    && TryConvertFiniteNumber(pair.Value, out value))
                {
                    numeric[pair.Key] = value;
                }
            }

            if (numeric.Count == 0)
            {
                return false;
            }

            experiment = new ParsedTSNEExperiment
            {
                Source = source,
                FlattenedValues = flattened,
                NumericFeatures = numeric
            };
            return true;
        }

        private static IList<object> ToObjectList(object root)
        {
            var result = new List<object>();
            var enumerable = root as IEnumerable;
            if (enumerable != null && !(root is string) && !(root is IDictionary<string, object>))
            {
                foreach (object item in enumerable)
                {
                    result.Add(item);
                }

                return result;
            }

            if (root is IDictionary<string, object>)
            {
                result.Add(root);
            }

            return result;
        }

        private static void Flatten(IDictionary<string, object> source, IDictionary<string, object> target, string prefix, int depth)
        {
            if (depth > 64)
            {
                throw new FormatException("CONV_EXPER_CTN exceeds the allowed JSON nesting depth.");
            }

            foreach (KeyValuePair<string, object> pair in source)
            {
                string key = string.IsNullOrEmpty(prefix) ? pair.Key : prefix + "." + pair.Key;
                var childDictionary = pair.Value as IDictionary<string, object>;
                if (childDictionary != null)
                {
                    Flatten(childDictionary, target, key, depth + 1);
                    continue;
                }

                var enumerable = pair.Value as IEnumerable;
                if (enumerable != null && !(pair.Value is string))
                {
                    int index = 0;
                    foreach (object item in enumerable)
                    {
                        string itemKey = string.Format("{0}[{1}]", key, index);
                        var itemDictionary = item as IDictionary<string, object>;
                        if (itemDictionary != null)
                        {
                            Flatten(itemDictionary, target, itemKey, depth + 1);
                        }
                        else
                        {
                            target[itemKey] = item;
                        }

                        index++;
                    }

                    continue;
                }

                target[key] = pair.Value;
            }
        }

        private static bool TryConvertFiniteNumber(object value, out double numericValue)
        {
            numericValue = 0d;
            if (value == null || value is bool)
            {
                return false;
            }

            string textValue = value as string;
            if (textValue != null)
            {
                string trimmed = textValue.Trim();
                return trimmed.Length > 0
                    && double.TryParse(
                        trimmed,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out numericValue)
                    && !double.IsNaN(numericValue)
                    && !double.IsInfinity(numericValue);
            }

            TypeCode typeCode = Type.GetTypeCode(value.GetType());
            switch (typeCode)
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    numericValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    return !double.IsNaN(numericValue) && !double.IsInfinity(numericValue);
                default:
                    return false;
            }
        }

        private static bool IsMetadataFeature(string featureName)
        {
            if (string.IsNullOrWhiteSpace(featureName))
            {
                return false;
            }

            string leafName = featureName;
            int dotIndex = leafName.LastIndexOf('.');
            if (dotIndex >= 0 && dotIndex < leafName.Length - 1)
            {
                leafName = leafName.Substring(dotIndex + 1);
            }

            return string.Equals(leafName, "PUB_NO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(leafName, "_VERSION_NM", StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class TSNEExadataService
    {
        private sealed class DelegateRowRepository : ITSNEExadataRowRepository
        {
            private readonly Func<IList<TSNEExadataSourceRow>> loader;

            public DelegateRowRepository(Func<IList<TSNEExadataSourceRow>> loader)
            {
                this.loader = loader;
            }

            public IList<TSNEExadataSourceRow> LoadAll()
            {
                return loader();
            }
        }

        private readonly ITSNEExadataRowRepository repository;
        private readonly object snapshotSync;
        private TSNEExadataSnapshot currentSnapshot;

        public TSNEExadataService()
            : this(new ConvExperimentRepository())
        {
        }

        public TSNEExadataService(DataTable sourceTable)
            : this(new ConvExperimentRepository(sourceTable))
        {
        }

        public TSNEExadataService(DataTable sourceTable, ConvExperimentQueryOptions tableOptions)
            : this(new ConvExperimentRepository(sourceTable, tableOptions))
        {
        }

        public TSNEExadataService(ITSNEExadataRowRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            this.repository = repository;
            snapshotSync = new object();
        }

        public TSNEExadataService(Func<IList<TSNEExadataSourceRow>> rowLoader)
            : this(new DelegateRowRepository(ValidateRowLoader(rowLoader)))
        {
        }

        private static Func<IList<TSNEExadataSourceRow>> ValidateRowLoader(Func<IList<TSNEExadataSourceRow>> rowLoader)
        {
            if (rowLoader == null)
            {
                throw new ArgumentNullException("rowLoader");
            }

            return rowLoader;
        }

        public TSNEExadataSnapshot CurrentSnapshot
        {
            get
            {
                lock (snapshotSync)
                {
                    return currentSnapshot;
                }
            }
        }

        public void SetSnapshot(TSNEExadataSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException("snapshot");
            }

            lock (snapshotSync)
            {
                currentSnapshot = snapshot;
            }
        }

        public TSNEExadataSnapshot SetDataTable(DataTable sourceTable)
        {
            return SetDataTable(sourceTable, ConvExperimentQueryOptions.FromConfiguration());
        }

        public TSNEExadataSnapshot SetDataTable(DataTable sourceTable, ConvExperimentQueryOptions tableOptions)
        {
            IList<TSNEExadataSourceRow> rows = ConvExperimentRepository.LoadFromDataTable(
                sourceTable,
                tableOptions);
            var snapshot = new TSNEExadataSnapshot(rows, DateTime.UtcNow);
            SetSnapshot(snapshot);
            return snapshot;
        }

        public Task<TSNEExadataSnapshot> LoadAllAsync()
        {
            return Task.Run(delegate
            {
                IList<TSNEExadataSourceRow> rows = repository.LoadAll();
                var snapshot = new TSNEExadataSnapshot(rows, DateTime.UtcNow);
                lock (snapshotSync)
                {
                    currentSnapshot = snapshot;
                }

                return snapshot;
            });
        }

        public Task<TSNEExadataSnapshot> LoadFromDataTableAsync(DataTable sourceTable)
        {
            return LoadFromDataTableAsync(
                sourceTable,
                ConvExperimentQueryOptions.FromConfiguration());
        }

        public Task<TSNEExadataSnapshot> LoadFromDataTableAsync(DataTable sourceTable, ConvExperimentQueryOptions tableOptions)
        {
            return Task.Run(delegate
            {
                return SetDataTable(sourceTable, tableOptions);
            });
        }

        public Task<TSNEExadataAnalysisResult> RefreshAndAnalyzeAsync(TSNEParameterType parameterType, TSNEScatterAnalysisOptions analysisOptions)
        {
            return Task.Run(delegate
            {
                IList<TSNEExadataSourceRow> rows = repository.LoadAll();
                var snapshot = new TSNEExadataSnapshot(rows, DateTime.UtcNow);
                TSNEExadataAnalysisResult result = AnalyzePopulation(
                    snapshot,
                    parameterType,
                    FilterPopulation(snapshot, parameterType),
                    analysisOptions,
                    null);
                lock (snapshotSync)
                {
                    currentSnapshot = snapshot;
                }

                return result;
            });
        }

        public Task<TSNEExadataAnalysisResult> AnalyzeDataTableAsync(DataTable sourceTable, TSNEParameterType parameterType, TSNEScatterAnalysisOptions analysisOptions)
        {
            return AnalyzeDataTableAsync(
                sourceTable,
                parameterType,
                analysisOptions,
                ConvExperimentQueryOptions.FromConfiguration());
        }

        public Task<TSNEExadataAnalysisResult> AnalyzeDataTableAsync(
            DataTable sourceTable,
            TSNEParameterType parameterType,
            TSNEScatterAnalysisOptions analysisOptions,
            ConvExperimentQueryOptions tableOptions)
        {
            return Task.Run(delegate
            {
                TSNEExadataSnapshot snapshot = SetDataTable(sourceTable, tableOptions);
                return AnalyzeSnapshot(snapshot, parameterType, analysisOptions);
            });
        }

        public Task<TSNEDraftQueryResult> QueryDraftAsync(string draftNo, TSNEParameterType parameterType, TSNEExadataRefreshMode refreshMode)
        {
            return QueryDraftAsync(
                draftNo,
                parameterType,
                refreshMode,
                new TSNEScatterAnalysisOptions());
        }

        public Task<TSNEDraftQueryResult> QueryDraftAsync(
            string draftNo,
            TSNEParameterType parameterType,
            TSNEExadataRefreshMode refreshMode,
            TSNEScatterAnalysisOptions analysisOptions)
        {
            string resolvedDraftNo = (draftNo ?? string.Empty).Trim();
            if (resolvedDraftNo.Length == 0)
            {
                throw new ArgumentException("A DRAFT_NO is required.", "draftNo");
            }

            return Task.Run(delegate
            {
                bool usedMemorySnapshot;
                TSNEExadataSnapshot snapshot = ResolveSnapshot(refreshMode, out usedMemorySnapshot);
                IList<TSNEExadataSourceRow> population = FilterPopulation(snapshot, parameterType);
                TSNEExadataSourceRow targetSource = population.FirstOrDefault(row =>
                    string.Equals(row.DraftNo, resolvedDraftNo, StringComparison.OrdinalIgnoreCase));
                if (targetSource == null)
                {
                    throw new KeyNotFoundException(
                        string.Format(
                            "Selected PARAM_TYP '{0}' does not contain DRAFT_NO '{1}'.",
                            TSNEParameterTypeParser.ToDatabaseValue(parameterType),
                            resolvedDraftNo));
                }

                TSNEExadataAnalysisResult analysis = AnalyzePopulation(
                    snapshot,
                    parameterType,
                    population,
                    analysisOptions,
                    targetSource.DraftNo);
                TSNEExperimentRecord target = analysis.Records.First(record =>
                    string.Equals(record.DraftNo, resolvedDraftNo, StringComparison.OrdinalIgnoreCase));
                IList<KnnNeighbor> neighbors = analysis.FindNearestByChartDistance(
                    target.DraftNo,
                    Math.Max(1, (analysisOptions ?? new TSNEScatterAnalysisOptions()).NeighborCount),
                    true);
                if (!usedMemorySnapshot)
                {
                    lock (snapshotSync)
                    {
                        currentSnapshot = snapshot;
                    }
                }

                return new TSNEDraftQueryResult(
                    analysis,
                    target,
                    neighbors,
                    usedMemorySnapshot);
            });
        }

        public Task<TSNEDraftQueryResult> QueryDraftFromDataTableAsync(
            string draftNo,
            TSNEParameterType parameterType,
            DataTable sourceTable,
            TSNEScatterAnalysisOptions analysisOptions)
        {
            return QueryDraftFromDataTableAsync(
                draftNo,
                parameterType,
                sourceTable,
                analysisOptions,
                ConvExperimentQueryOptions.FromConfiguration());
        }

        public Task<TSNEDraftQueryResult> QueryDraftFromDataTableAsync(
            string draftNo,
            TSNEParameterType parameterType,
            DataTable sourceTable,
            TSNEScatterAnalysisOptions analysisOptions,
            ConvExperimentQueryOptions tableOptions)
        {
            string resolvedDraftNo = (draftNo ?? string.Empty).Trim();
            if (resolvedDraftNo.Length == 0)
            {
                throw new ArgumentException("A DRAFT_NO is required.", "draftNo");
            }

            return Task.Run(delegate
            {
                TSNEExadataSnapshot snapshot = SetDataTable(sourceTable, tableOptions);
                IList<TSNEExadataSourceRow> population = FilterPopulation(snapshot, parameterType);
                TSNEExadataSourceRow targetSource = population.FirstOrDefault(row =>
                    string.Equals(row.DraftNo, resolvedDraftNo, StringComparison.OrdinalIgnoreCase));
                if (targetSource == null)
                {
                    throw new KeyNotFoundException(
                        string.Format(
                            "Selected PARAM_TYP '{0}' does not contain DRAFT_NO '{1}'.",
                            TSNEParameterTypeParser.ToDatabaseValue(parameterType),
                            resolvedDraftNo));
                }

                TSNEScatterAnalysisOptions effectiveAnalysisOptions =
                    analysisOptions ?? new TSNEScatterAnalysisOptions();
                TSNEExadataAnalysisResult analysis = AnalyzePopulation(
                    snapshot,
                    parameterType,
                    population,
                    effectiveAnalysisOptions,
                    targetSource.DraftNo);
                TSNEExperimentRecord target = analysis.Records.First(record =>
                    string.Equals(record.DraftNo, resolvedDraftNo, StringComparison.OrdinalIgnoreCase));
                IList<KnnNeighbor> neighbors = analysis.FindNearestByChartDistance(
                    target.DraftNo,
                    Math.Max(1, effectiveAnalysisOptions.NeighborCount),
                    true);

                return new TSNEDraftQueryResult(
                    analysis,
                    target,
                    neighbors,
                    false);
            });
        }

        public TSNEExadataAnalysisResult AnalyzeSnapshot(TSNEExadataSnapshot snapshot, TSNEParameterType parameterType, TSNEScatterAnalysisOptions analysisOptions)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException("snapshot");
            }

            return AnalyzePopulation(
                snapshot,
                parameterType,
                FilterPopulation(snapshot, parameterType),
                analysisOptions,
                null);
        }

        public void ClearSnapshot()
        {
            lock (snapshotSync)
            {
                currentSnapshot = null;
            }
        }

        private TSNEExadataSnapshot ResolveSnapshot(TSNEExadataRefreshMode refreshMode, out bool usedMemorySnapshot)
        {
            if (refreshMode == TSNEExadataRefreshMode.PreferMemorySnapshot)
            {
                lock (snapshotSync)
                {
                    if (currentSnapshot != null)
                    {
                        usedMemorySnapshot = true;
                        return currentSnapshot;
                    }
                }
            }

            IList<TSNEExadataSourceRow> rows = repository.LoadAll();
            usedMemorySnapshot = false;
            return new TSNEExadataSnapshot(rows, DateTime.UtcNow);
        }

        private static IList<TSNEExadataSourceRow> FilterPopulation(TSNEExadataSnapshot snapshot, TSNEParameterType parameterType)
        {
            List<TSNEExadataSourceRow> population = snapshot.Rows
                .Where(row => row != null && row.ParameterType == parameterType)
                .ToList();
            if (population.Count == 0)
            {
                throw new InvalidOperationException(
                    "Selected PARAM_TYP '" + TSNEParameterTypeParser.ToDatabaseValue(parameterType)
                    + "' has no t-SNE data.");
            }

            string duplicateDraft = population
                .GroupBy(row => row.DraftNo, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(duplicateDraft))
            {
                throw new InvalidOperationException(
                    "Selected PARAM_TYP contains duplicate DRAFT_NO: " + duplicateDraft);
            }

            return population;
        }

        private static TSNEExadataAnalysisResult AnalyzePopulation(
            TSNEExadataSnapshot snapshot,
            TSNEParameterType parameterType,
            IList<TSNEExadataSourceRow> population,
            TSNEScatterAnalysisOptions analysisOptions,
            string requiredDraftNo)
        {
            // 1단계: DB 행의 CONV_EXPER_CTN JSON을 파싱해 Draft별 실험 객체와 수치 feature 사전을 만든다.
            var parser = new ConvExperimentRowParser();
            var parsed = new List<ParsedTSNEExperiment>();
            int missingCount = 0;
            foreach (TSNEExadataSourceRow source in population)
            {
                ParsedTSNEExperiment experiment;
                if (!parser.TryParse(source, out experiment))
                {
                    missingCount++;
                    if (!string.IsNullOrEmpty(requiredDraftNo)
                        && string.Equals(source.DraftNo, requiredDraftNo, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new TSNEExperimentDataMissingException(source.DraftNo);
                    }

                    continue;
                }

                parsed.Add(experiment);
            }

            if (!string.IsNullOrEmpty(requiredDraftNo)
                && !parsed.Any(item => string.Equals(
                    item.Source.DraftNo,
                    requiredDraftNo,
                    StringComparison.OrdinalIgnoreCase)))
            {
                throw new TSNEExperimentDataMissingException(requiredDraftNo);
            }

            if (parsed.Count < 3)
            {
                throw new InvalidOperationException(
                    "At least three rows with experiment data are required for analysis.");
            }

            // 2단계: 파싱된 수치 feature를 TSNE 파이프라인이 받는 JSON row 형식으로 정규화한다.
            // Draft_NO와 AI_RSLT_Val은 식별/라벨로만 쓰고, 실제 TSNE feature에서는 제외된다.
            IList<string> normalizedRows = parsed.Select(item =>
            {
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Draft_NO", item.Source.DraftNo },
                    { "AI_RSLT_Val", item.Source.LabelY }
                };
                foreach (KeyValuePair<string, double> feature in item.NumericFeatures)
                {
                    row[feature.Key] = feature.Value;
                }

                return TSNEJsonUtility.SerializeObject(row);
            }).ToList();

            TSNEAnalysisOptions pipelineOptions =
                (analysisOptions ?? new TSNEScatterAnalysisOptions()).ToPipelineOptions();
            // 3단계: 수치 feature 선택, 정규화, TSNE 좌표 생성, KNN 인덱스 생성을 한 번에 수행한다.
            TSNEAnalysisResult analysis = new TSNEAnalysisPipeline(pipelineOptions).Analyze(normalizedRows);
            TSNEFeatureSelectionReport featureSelectionReport =
                TSNEFeatureSelectionReport.CreateFromParsedExperiments(
                    parsed,
                    analysis.FeatureNames,
                    pipelineOptions.ConstantVarianceThreshold);
            analysis.FeatureSelectionReport = featureSelectionReport;
            var records = new List<TSNEExperimentRecord>(parsed.Count);
            for (int index = 0; index < parsed.Count; index++)
            {
                TSNEPointData sample = analysis.ScatterData[index];
                ParsedTSNEExperiment source = parsed[index];
                // 4단계: 화면 좌표(X1/X2), 원본 feature, 정규화 벡터를 한 record에 묶어 클릭/로그/그리드에서 재사용한다.
                var record = new TSNEExperimentRecord(
                    source.Source,
                    source.FlattenedValues,
                    source.NumericFeatures,
                    analysis.StandardizedMatrix[index],
                    sample.X1,
                    sample.X2);
                records.Add(record);
                sample.ParameterType = TSNEParameterTypeParser.ToDatabaseValue(parameterType);
                sample.UserData = record;
                sample.TooltipText = string.Format(
                    "DRAFT_NO: {0}\r\nPARAM_TYP: {1}\r\nY: {2}",
                    record.DraftNo,
                    sample.ParameterType,
                    string.IsNullOrWhiteSpace(record.LabelY) ? "-" : record.LabelY);
            }

            return new TSNEExadataAnalysisResult(
                snapshot,
                parameterType,
                analysis,
                records,
                missingCount,
                featureSelectionReport);
        }
    }
}






namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
}
