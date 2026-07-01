using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.AccordPcaScatter
{
    public enum PcaParameterType
    {
        Response,
        Defect,
        Epm,
        Probe
    }

    public enum PcaExadataRefreshMode
    {
        AlwaysReload,
        PreferMemorySnapshot
    }

    public static class PcaParameterTypeParser
    {
        public static bool TryParse(string value, out PcaParameterType parameterType)
        {
            parameterType = PcaParameterType.Response;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            switch (value.Trim().ToUpperInvariant())
            {
                case "RESPONSE":
                    parameterType = PcaParameterType.Response;
                    return true;
                case "DEFECT":
                    parameterType = PcaParameterType.Defect;
                    return true;
                case "EPM":
                    parameterType = PcaParameterType.Epm;
                    return true;
                case "PROBE":
                    parameterType = PcaParameterType.Probe;
                    return true;
                default:
                    return false;
            }
        }

        public static string ToDatabaseValue(PcaParameterType parameterType)
        {
            return parameterType.ToString().ToUpperInvariant();
        }
    }

    public sealed class PcaExadataSourceRow
    {
        public PcaExadataSourceRow(int sourceRowIndex, string draftNo, PcaParameterType parameterType, string labelY, string rawConvExperimentJson)
        {
            SourceRowIndex = sourceRowIndex;
            DraftNo = (draftNo ?? string.Empty).Trim();
            ParameterType = parameterType;
            LabelY = (labelY ?? string.Empty).Trim();
            RawConvExperimentJson = rawConvExperimentJson ?? string.Empty;
        }

        public int SourceRowIndex { get; private set; }
        public string DraftNo { get; private set; }
        public PcaParameterType ParameterType { get; private set; }
        public string LabelY { get; private set; }
        public string RawConvExperimentJson { get; private set; }
    }

    public sealed class PcaExadataSnapshot
    {
        public PcaExadataSnapshot(IEnumerable<PcaExadataSourceRow> rows, DateTime loadedAtUtc)
        {
            Rows = new ReadOnlyCollection<PcaExadataSourceRow>(
                (rows ?? Enumerable.Empty<PcaExadataSourceRow>()).ToList());
            LoadedAtUtc = DateTime.SpecifyKind(loadedAtUtc, DateTimeKind.Utc);
        }

        public IList<PcaExadataSourceRow> Rows { get; private set; }
        public DateTime LoadedAtUtc { get; private set; }
    }

    public sealed class PcaExperimentRecord
    {
        private readonly double[] standardizedVector;

        internal PcaExperimentRecord(PcaExadataSourceRow source, IDictionary<string, object> flattenedValues, IDictionary<string, double> numericFeatures,
            double[] standardizedVector, double x1, double x2)
        {
            SourceRowIndex = source.SourceRowIndex;
            DraftNo = source.DraftNo;
            ParameterType = source.ParameterType;
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
        public PcaParameterType ParameterType { get; private set; }
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

    public sealed class PcaExadataAnalysisResult
    {
        internal PcaExadataAnalysisResult(PcaExadataSnapshot snapshot, PcaParameterType parameterType, PcaAnalysisResult analysisResult,
            IList<PcaExperimentRecord> records, int missingExperimentCount, PcaFeatureSelectionReport featureSelectionReport)
        {
            Snapshot = snapshot;
            ParameterType = parameterType;
            AnalysisResult = analysisResult;
            Records = new ReadOnlyCollection<PcaExperimentRecord>(
                (records ?? new List<PcaExperimentRecord>()).ToList());
            MissingExperimentCount = missingExperimentCount;
            FeatureSelectionReport = featureSelectionReport
                ?? (analysisResult == null ? null : analysisResult.FeatureSelectionReport)
                ?? PcaFeatureSelectionReport.Empty();
            Diagnostic = PcaAnalysisDiagnosticReport.Create(
                analysisResult,
                Records.Count,
                MissingExperimentCount);
        }

        public PcaExadataSnapshot Snapshot { get; private set; }
        public PcaParameterType ParameterType { get; private set; }
        public PcaAnalysisResult AnalysisResult { get; private set; }
        public IList<PcaExperimentRecord> Records { get; private set; }
        public int MissingExperimentCount { get; private set; }
        public PcaAnalysisDiagnosticReport Diagnostic { get; private set; }
        public PcaFeatureSelectionReport FeatureSelectionReport { get; private set; }

        public DataTable CreateFeatureSelectionDataTable()
        {
            return (FeatureSelectionReport ?? PcaFeatureSelectionReport.Empty()).ToDataTable();
        }

        public DataTable CreateSurvivingPopulationDataTable()
        {
            DataTable table = new DataTable("PCA_SURVIVING_POPULATION");
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

            foreach (PcaExperimentRecord record in Records)
            {
                DataRow row = table.NewRow();
                row["DRAFT_NO"] = record.DraftNo;
                row["PARAM_TYP"] = PcaParameterTypeParser.ToDatabaseValue(record.ParameterType);
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

    public sealed class PcaDraftQueryResult
    {
        internal PcaDraftQueryResult(PcaExadataAnalysisResult analysis, PcaExperimentRecord target, IList<KnnNeighbor> neighbors, bool usedMemorySnapshot)
        {
            Analysis = analysis;
            Target = target;
            Neighbors = new ReadOnlyCollection<KnnNeighbor>(
                (neighbors ?? new List<KnnNeighbor>()).ToList());
            UsedMemorySnapshot = usedMemorySnapshot;
        }

        public PcaExadataAnalysisResult Analysis { get; private set; }
        public PcaAnalysisResult AnalysisResult
        {
            get { return Analysis == null ? null : Analysis.AnalysisResult; }
        }

        public PcaExperimentRecord Target { get; private set; }
        public IList<KnnNeighbor> Neighbors { get; private set; }
        public bool UsedMemorySnapshot { get; private set; }
    }

    public sealed class PcaExperimentDataMissingException : InvalidOperationException
    {
        public PcaExperimentDataMissingException(string draftNo)
            : base("DRAFT_NO '" + (draftNo ?? string.Empty) + "' does not contain experiment data.")
        {
            DraftNo = draftNo ?? string.Empty;
        }

        public string DraftNo { get; private set; }
    }

    internal sealed class ParsedPcaExperiment
    {
        public PcaExadataSourceRow Source { get; set; }
        public IDictionary<string, object> FlattenedValues { get; set; }
        public IDictionary<string, double> NumericFeatures { get; set; }
    }

    internal sealed class ConvExperimentRowParser
    {
        public bool TryParse(PcaExadataSourceRow source, out ParsedPcaExperiment experiment)
        {
            experiment = null;
            if (source == null || string.IsNullOrWhiteSpace(source.RawConvExperimentJson))
            {
                return false;
            }

            object root;
            try
            {
                root = PcaJsonUtility.DeserializeObject(
                    source.RawConvExperimentJson.Trim().TrimStart('\uFEFF'));
            }
            catch (Exception ex) when (PcaJsonUtility.IsJsonException(ex))
            {
                throw new FormatException(string.Format("CONV_EXPER_CTN[{0}] JSON parsing failed. DRAFT_NO={1}: {2}",
                    source.SourceRowIndex, source.DraftNo, ex.Message), ex);
            }

            IList<object> items = ToObjectList(root);
            if (items.Count == 0)
            {
                return false;
            }

            if (items.Count != 1)
            {
                throw new FormatException(string.Format("CONV_EXPER_CTN[{0}] must contain exactly one experiment object. DRAFT_NO={1}, Count={2}",
                    source.SourceRowIndex, source.DraftNo, items.Count));
            }

            var dictionary = items[0] as IDictionary<string, object>;
            if (dictionary == null)
            {
                throw new FormatException(string.Format("CONV_EXPER_CTN[{0}] array item is not a JSON object. DRAFT_NO={1}",
                    source.SourceRowIndex, source.DraftNo));
            }

            var flattened = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            Flatten(dictionary, flattened, string.Empty, 0);
            var numeric = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, object> pair in flattened)
            {
                double value;
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

            experiment = new ParsedPcaExperiment
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
                throw new FormatException("CONV_EXPER_CTN JSON nesting depth exceeded the supported limit.");
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


}
