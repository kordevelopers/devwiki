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

// Consolidated t-SNE chart engine and data contracts.
namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    #region ACT_DATA Dict/List JSON Expansion

    /// <summary>
    /// DB의 ACT_DATA 문자열을 Dictionary/List 구조로 파싱하고 실험 JSON 객체 목록으로 정규화한다.
    /// 단일 객체, 최상위 배열, wrapper 객체의 items/data 목록, 이중 인코딩 JSON 문자열을 처리한다.
    /// </summary>
    public sealed class ActDataJsonParser
    {
        private static readonly string[] DraftNoAliases = { "Draft_NO", "Draft_No", "draft_No" };

        public IList<string> ExpandDocuments(IEnumerable<string> actDataDocuments)
        {
            return ExpandDocuments(actDataDocuments, "ACT_DATA");
        }

        public IList<string> ExpandDocuments(IEnumerable<string> jsonDocuments, string sourceName)
        {
            string resolvedSourceName = string.IsNullOrWhiteSpace(sourceName)
                ? "JSON_DATA"
                : sourceName.Trim();
            string[] source = jsonDocuments == null
                ? new string[0]
                : jsonDocuments.ToArray();
            if (source.Length == 0)
            {
                throw new ArgumentException(resolvedSourceName + " JSON document is empty.", "jsonDocuments");
            }

            var normalizedRows = new List<string>();
            for (int documentIndex = 0; documentIndex < source.Length; documentIndex++)
            {
                if (string.IsNullOrWhiteSpace(source[documentIndex]))
                {
                    throw new FormatException(string.Format("{0}[{1}] JSON string is empty.", resolvedSourceName, documentIndex));
                }

                object root;
                try
                {
                    root = TSNEJsonUtility.DeserializeObject(TSNEJsonUtility.RemoveBom(source[documentIndex].Trim()));
                }
                catch (Exception ex) when (TSNEJsonUtility.IsJsonException(ex))
                {
                    throw new FormatException(string.Format("{0}[{1}] JSON parsing failed: {2}",
                        resolvedSourceName, documentIndex, ex.Message), ex);
                }

                int beforeCount = normalizedRows.Count;
                CollectExperimentRows(root, normalizedRows, resolvedSourceName + "[" + documentIndex + "]", 0);
                if (normalizedRows.Count == beforeCount)
                {
                    throw new FormatException(string.Format("{0}[{1}] does not contain an experiment object with Draft_NO.",
                        resolvedSourceName, documentIndex));
                }
            }

            return normalizedRows;
        }

        private void CollectExperimentRows(object node, ICollection<string> rows, string path, int depth)
        {
            if (node == null)
            {
                return;
            }

            if (depth > 64)
            {
                throw new FormatException(path + " exceeds the allowed JSON nesting depth.");
            }

            var dictionary = node as IDictionary<string, object>;
            if (dictionary != null)
            {
                if (ContainsDraftNo(dictionary))
                {
                    // 중첩 객체와 숫자 배열은 점 표기와 [index] 표기로 평탄화한다.
                    // 이후 TSNE 파이프라인은 평탄화된 사전의 수치 leaf 값만 특징으로 사용한다.
                    var flattened = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    FlattenDictionary(dictionary, flattened, string.Empty, 0);
                    rows.Add(TSNEJsonUtility.SerializeObject(flattened));
                    return;
                }

                foreach (KeyValuePair<string, object> pair in dictionary)
                {
                    CollectExperimentRows(pair.Value, rows, path + "." + pair.Key, depth + 1);
                }

                return;
            }

            string nestedJson = node as string;
            if (nestedJson != null && LooksLikeJson(nestedJson))
            {
                try
                {
                    CollectExperimentRows(
                        TSNEJsonUtility.DeserializeObject(TSNEJsonUtility.RemoveBom(nestedJson.Trim())),
                        rows,
                        path + "(json-string)",
                        depth + 1);
                }
                catch (Exception ex) when (TSNEJsonUtility.IsJsonException(ex))
                {
                    throw new FormatException(path + " failed to parse nested JSON string.", ex);
                }

                return;
            }

            var enumerable = node as IEnumerable;
            if (enumerable == null || node is string)
            {
                return;
            }

            int itemIndex = 0;
            foreach (object item in enumerable)
            {
                CollectExperimentRows(item, rows, path + "[" + itemIndex + "]", depth + 1);
                itemIndex++;
            }
        }

        private static bool ContainsDraftNo(IDictionary<string, object> dictionary)
        {
            return dictionary.Keys.Any(key => DraftNoAliases.Any(alias =>
                string.Equals(key, alias, StringComparison.OrdinalIgnoreCase)));
        }

        private static void FlattenDictionary(IDictionary<string, object> source, IDictionary<string, object> target, string prefix, int depth)
        {
            if (depth > 64)
            {
                throw new FormatException("Experiment JSON nesting depth exceeds the allowed limit.");
            }

            foreach (KeyValuePair<string, object> pair in source)
            {
                string key = string.IsNullOrEmpty(prefix) ? pair.Key : prefix + "." + pair.Key;
                var childDictionary = pair.Value as IDictionary<string, object>;
                if (childDictionary != null)
                {
                    FlattenDictionary(childDictionary, target, key, depth + 1);
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
                            FlattenDictionary(itemDictionary, target, itemKey, depth + 1);
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

        private static bool LooksLikeJson(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string trimmed = RemoveBom(value.Trim());
            return (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
                || (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal));
        }

        private static string RemoveBom(string value)
        {
            return TSNEJsonUtility.RemoveBom(value);
        }
    }

    #endregion
}






namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    #region ACT_DATA DataTable Options

    public sealed class ActDataQueryOptions
    {
        public ActDataQueryOptions()
        {
            ActDataColumnName = "ACT_DATA";
        }

        public string ActDataColumnName { get; set; }

        public static ActDataQueryOptions FromConfiguration()
        {
            return new ActDataQueryOptions();
        }
    }

    #endregion

    #region ACT_DATA DataTable Repository

    /// <summary>
    /// Converts caller-supplied ACT_DATA service results into JSON documents.
    /// DB access is intentionally outside this class.
    /// </summary>
    public sealed class ActDataRepository
    {
        private readonly ActDataQueryOptions options;
        private DataTable sourceTable;

        public ActDataRepository()
            : this(null, ActDataQueryOptions.FromConfiguration())
        {
        }

        public ActDataRepository(DataTable sourceTable)
            : this(sourceTable, ActDataQueryOptions.FromConfiguration())
        {
        }

        public ActDataRepository(ActDataQueryOptions options)
            : this(null, options)
        {
        }

        public ActDataRepository(DataTable sourceTable, ActDataQueryOptions options)
        {
            this.sourceTable = sourceTable;
            this.options = options ?? ActDataQueryOptions.FromConfiguration();
        }

        public void SetSourceTable(DataTable table)
        {
            sourceTable = table;
        }

        public IList<string> LoadActData()
        {
            return LoadFromDataTable(sourceTable, options);
        }

        public static IList<string> LoadFromDataTable(DataTable table)
        {
            return LoadFromDataTable(table, ActDataQueryOptions.FromConfiguration());
        }

        public static IList<string> LoadFromDataTable(DataTable table, ActDataQueryOptions options)
        {
            if (table == null)
            {
                throw new InvalidOperationException(
                    "ACT_DATA DataTable is required. Load data in the UI/service layer and pass the DataTable.");
            }

            ActDataQueryOptions effectiveOptions = options ?? ActDataQueryOptions.FromConfiguration();
            DataColumn actDataColumn = FindColumn(table, effectiveOptions.ActDataColumnName);
            var documents = new List<string>();
            for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                object value = table.Rows[rowIndex][actDataColumn];
                if (value == null || value == DBNull.Value)
                {
                    continue;
                }

                string json = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    documents.Add(json.Trim());
                }
            }

            if (documents.Count == 0)
            {
                throw new InvalidOperationException(
                    "The ACT_DATA DataTable contains no JSON data.");
            }

            return documents;
        }

        private static DataColumn FindColumn(DataTable table, string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                throw new ArgumentException("Column name is required.", "columnName");
            }

            foreach (DataColumn column in table.Columns)
            {
                if (string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return column;
                }
            }

            throw new InvalidOperationException(
                "The DataTable does not contain required column '" + columnName + "'.");
        }
    }

    #endregion
}






namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public enum DimensionalityReductionMethod
    {
        TSNE
    }

    #region Analysis Result Models

    public enum KnnSearchAlgorithm
    {
        Auto,
        BruteForce,
        KdTree,
        BallTree
    }

    public sealed class TSNEAnalysisOptions
    {
        public TSNEAnalysisOptions()
        {
            ConstantVarianceThreshold = 1e-10d;
            MinimumNumericFeatureCoverageRatio = 0.90d;
            MeanImputationEnabled = true;
            ComponentCount = 2;
            MaxIterations = 2000;
            ConvergenceTolerance = 1e-10d;
            NeighborCount = 3;
            KnnSearchAlgorithm = KnnSearchAlgorithm.Auto;
            ProjectionMethod = DimensionalityReductionMethod.TSNE;
            TSNEPerplexity = 30d;
            TSNEIterations = 750;
            TSNELearningRate = 200d;
            TSNERandomSeed = 20260831;
        }

        public double ConstantVarianceThreshold { get; set; }
        public double MinimumNumericFeatureCoverageRatio { get; set; }
        public bool MeanImputationEnabled { get; set; }
        public int ComponentCount { get; set; }
        public int MaxIterations { get; set; }
        public double ConvergenceTolerance { get; set; }
        public int NeighborCount { get; set; }
        public KnnSearchAlgorithm KnnSearchAlgorithm { get; set; }
        public DimensionalityReductionMethod ProjectionMethod { get; set; }
        public double TSNEPerplexity { get; set; }
        public int TSNEIterations { get; set; }
        public double TSNELearningRate { get; set; }
        public int TSNERandomSeed { get; set; }
    }

    public sealed class KnnNeighbor
    {
        public int Rank { get; set; }
        public int SourceIndex { get; set; }
        public string DraftNo { get; set; }
        public double Distance { get; set; }
    }

    public sealed class TSNEVerificationReport
    {
        public bool IsValid { get; set; }
        public double MaximumAbsoluteStandardizedMean { get; set; }
        public double MaximumStandardDeviationError { get; set; }
        public double ComponentDotProduct { get; set; }
        public bool EigenValuesDescending { get; set; }
        public bool AllScoresFinite { get; set; }
        public bool KnnResultValid { get; set; }
        public bool SharedScalerInstance { get; set; }
        public string Message { get; set; }
    }

    public sealed class TSNEAnalysisDiagnosticReport
    {
        private TSNEAnalysisDiagnosticReport()
        {
        }

        public int RowCount { get; private set; }
        public int FeatureCount { get; private set; }
        public int ExcludedFeatureCount { get; private set; }
        public int MissingExperimentCount { get; private set; }
        public double Pc1Percent { get; private set; }
        public double Pc2Percent { get; private set; }
        public double Pc1Pc2Percent { get; private set; }
        public string ShapeCode { get; private set; }
        public KnnSearchAlgorithm KnnAlgorithm { get; private set; }
        public string KnnAlgorithmReason { get; private set; }
        public string CompactText { get; private set; }

        public static TSNEAnalysisDiagnosticReport Create(TSNEAnalysisResult analysisResult, int rowCount, int missingExperimentCount)
        {
            int featureCount = analysisResult == null || analysisResult.FeatureNames == null
                ? 0
                : analysisResult.FeatureNames.Length;
            int excludedCount = ResolveExcludedFeatureCount(analysisResult);
            double pc1 = GetExplainedVariancePercent(analysisResult, 0);
            double pc2 = GetExplainedVariancePercent(analysisResult, 1);
            KnnSearchAlgorithm knnAlgorithm = analysisResult == null || analysisResult.Knn == null
                ? KnnSearchAlgorithm.Auto
                : analysisResult.Knn.ActualAlgorithm;
            string knnReason = analysisResult == null || analysisResult.Knn == null
                ? string.Empty
                : analysisResult.Knn.SelectionReason;
            bool isTSNE = analysisResult != null
                && analysisResult.ProjectionMethod == DimensionalityReductionMethod.TSNE;
            string shapeCode = isTSNE
                ? ResolveTSNEShapeCode(rowCount, featureCount)
                : ResolveShapeCode(rowCount, featureCount, pc1, pc2);
            string compactText = isTSNE
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "DIAG R={0} F={1} X={2} M={3} TSNE PERP={4:0.##} ENGINE=ACCORD SHAPE={5} KNN={6}",
                    rowCount,
                    featureCount,
                    excludedCount,
                    missingExperimentCount,
                    analysisResult.TSNEModel == null ? 0d : analysisResult.TSNEModel.EffectivePerplexity,
                    shapeCode,
                    knnAlgorithm)
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "DIAG R={0} F={1} X={2} M={3} PC1={4:0.0} PC2={5:0.0} SUM={6:0.0} SHAPE={7} KNN={8}",
                    rowCount,
                    featureCount,
                    excludedCount,
                    missingExperimentCount,
                    pc1,
                    pc2,
                    pc1 + pc2,
                    shapeCode,
                    knnAlgorithm);

            return new TSNEAnalysisDiagnosticReport
            {
                RowCount = rowCount,
                FeatureCount = featureCount,
                ExcludedFeatureCount = excludedCount,
                MissingExperimentCount = missingExperimentCount,
                Pc1Percent = pc1,
                Pc2Percent = pc2,
                Pc1Pc2Percent = pc1 + pc2,
                ShapeCode = shapeCode,
                KnnAlgorithm = knnAlgorithm,
                KnnAlgorithmReason = knnReason,
                CompactText = compactText
            };
        }

        private static int ResolveExcludedFeatureCount(TSNEAnalysisResult analysisResult)
        {
            if (analysisResult == null)
            {
                return 0;
            }

            if (analysisResult.FeatureSelectionReport != null)
            {
                return analysisResult.FeatureSelectionReport.ExcludedFeatureCount;
            }

            return analysisResult.ExcludedFeatureNames == null
                ? 0
                : analysisResult.ExcludedFeatureNames.Length;
        }

        private static double GetExplainedVariancePercent(TSNEAnalysisResult analysisResult, int index)
        {
            // t-SNE does not expose PCA explained-variance ratios.
            return 0d;
        }

        private static string ResolveShapeCode(int rowCount, int featureCount, double pc1Percent, double pc2Percent)
        {
            if (rowCount < 3)
            {
                return "ROWS_LT3";
            }

            if (rowCount < 30)
            {
                return "ROWS_LOW";
            }

            if (featureCount < 2)
            {
                return "FEATURE_LT2";
            }

            if (featureCount <= 5)
            {
                return "FEATURE_LOW";
            }

            if (pc1Percent >= 95d && pc2Percent <= 5d)
            {
                return "LINE_PC1_HIGH";
            }

            if (pc1Percent >= 85d && pc2Percent <= 10d)
            {
                return "LINE_LIKELY";
            }

            if (pc1Percent + pc2Percent < 50d)
            {
                return "TSNE2_LOW";
            }

            return "OK";
        }

        private static string ResolveTSNEShapeCode(int rowCount, int featureCount)
        {
            if (rowCount < 3)
            {
                return "ROWS_LT3";
            }

            if (rowCount < 30)
            {
                return "ROWS_LOW";
            }

            return featureCount < 2 ? "FEATURE_LT2" : "OK";
        }
    }

    public enum TSNEFeatureSelectionReason
    {
        Included,
        Metadata,
        MissingInRows,
        NonNumeric,
        ConstantOrLowVariance
    }

    public sealed class TSNEFeatureSelectionDetail
    {
        internal TSNEFeatureSelectionDetail()
        {
        }

        public string FeatureName { get; internal set; }
        public bool Included { get; internal set; }
        public TSNEFeatureSelectionReason Reason { get; internal set; }
        public int RowCount { get; internal set; }
        public int PresentCount { get; internal set; }
        public int NumericCount { get; internal set; }
        public int MissingCount { get; internal set; }
        public int NonNumericCount { get; internal set; }
        public bool HasStatistics { get; internal set; }
        public double Mean { get; internal set; }
        public double Variance { get; internal set; }
        public double StandardDeviation { get; internal set; }
        public double Minimum { get; internal set; }
        public double Maximum { get; internal set; }
        public string SampleDraftNo { get; internal set; }

        public string ReasonText
        {
            get { return Reason.ToString(); }
        }
    }

    public sealed class TSNEFeatureSelectionReport
    {
        private static readonly string[] KnownMetadataNames =
        {
            "Draft_NO",
            "Draft_No",
            "draft_No",
            "AI_RSLT_Val",
            "AI_RSLT_VAL",
            "ENGR_RSLT_VAL",
            "AiResultValue",
            "PUB_NO",
            "_VERSION_NM"
        };

        private readonly ReadOnlyCollection<TSNEFeatureSelectionDetail> details;
        private readonly ReadOnlyCollection<string> includedFeatureNames;
        private readonly ReadOnlyCollection<string> excludedFeatureNames;

        private TSNEFeatureSelectionReport(int rowCount, IEnumerable<TSNEFeatureSelectionDetail> detailItems)
        {
            RowCount = rowCount;
            details = new ReadOnlyCollection<TSNEFeatureSelectionDetail>(
                (detailItems ?? Enumerable.Empty<TSNEFeatureSelectionDetail>()).ToList());
            includedFeatureNames = new ReadOnlyCollection<string>(
                details.Where(item => item.Included).Select(item => item.FeatureName).ToList());
            excludedFeatureNames = new ReadOnlyCollection<string>(
                details.Where(item => !item.Included).Select(item => item.FeatureName).ToList());
        }

        public int RowCount { get; private set; }
        public IList<TSNEFeatureSelectionDetail> Details
        {
            get { return details; }
        }

        public IList<string> IncludedFeatureNames
        {
            get { return includedFeatureNames; }
        }

        public IList<string> ExcludedFeatureNames
        {
            get { return excludedFeatureNames; }
        }

        public int IncludedFeatureCount
        {
            get { return includedFeatureNames.Count; }
        }

        public int ExcludedFeatureCount
        {
            get { return excludedFeatureNames.Count; }
        }

        public static TSNEFeatureSelectionReport Empty()
        {
            return new TSNEFeatureSelectionReport(0, new TSNEFeatureSelectionDetail[0]);
        }

        public string ToSummaryText()
        {
            string reasonSummary = string.Join(
                ",",
                details
                    .Where(item => !item.Included)
                    .GroupBy(item => item.Reason)
                    .OrderByDescending(group => group.Count())
                    .Select(group => group.Key + ":" + group.Count())
                    .ToArray());
            if (string.IsNullOrEmpty(reasonSummary))
            {
                reasonSummary = "None";
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "FEATURE_AUDIT ROWS={0} INCLUDED={1} EXCLUDED={2} REASONS={3}",
                RowCount,
                IncludedFeatureCount,
                ExcludedFeatureCount,
                reasonSummary);
        }

        public DataTable ToDataTable()
        {
            DataTable table = new DataTable("TSNE_FEATURE_SELECTION");
            table.Columns.Add("FeatureName", typeof(string));
            table.Columns.Add("Included", typeof(bool));
            table.Columns.Add("Reason", typeof(string));
            table.Columns.Add("RowCount", typeof(int));
            table.Columns.Add("PresentCount", typeof(int));
            table.Columns.Add("NumericCount", typeof(int));
            table.Columns.Add("MissingCount", typeof(int));
            table.Columns.Add("NonNumericCount", typeof(int));
            table.Columns.Add("Mean", typeof(double));
            table.Columns.Add("Variance", typeof(double));
            table.Columns.Add("StdDev", typeof(double));
            table.Columns.Add("Min", typeof(double));
            table.Columns.Add("Max", typeof(double));
            table.Columns.Add("SampleDraftNo", typeof(string));

            foreach (TSNEFeatureSelectionDetail detail in details)
            {
                DataRow row = table.NewRow();
                row["FeatureName"] = detail.FeatureName;
                row["Included"] = detail.Included;
                row["Reason"] = detail.ReasonText;
                row["RowCount"] = detail.RowCount;
                row["PresentCount"] = detail.PresentCount;
                row["NumericCount"] = detail.NumericCount;
                row["MissingCount"] = detail.MissingCount;
                row["NonNumericCount"] = detail.NonNumericCount;
                row["SampleDraftNo"] = detail.SampleDraftNo ?? string.Empty;
                if (detail.HasStatistics)
                {
                    row["Mean"] = detail.Mean;
                    row["Variance"] = detail.Variance;
                    row["StdDev"] = detail.StandardDeviation;
                    row["Min"] = detail.Minimum;
                    row["Max"] = detail.Maximum;
                }
                else
                {
                    row["Mean"] = DBNull.Value;
                    row["Variance"] = DBNull.Value;
                    row["StdDev"] = DBNull.Value;
                    row["Min"] = DBNull.Value;
                    row["Max"] = DBNull.Value;
                }

                table.Rows.Add(row);
            }

            return table;
        }

        internal static TSNEFeatureSelectionReport CreateFromSourceRows(IList<TSNESourceRow> rows, IEnumerable<string> includedFeatureNames, double varianceThreshold)
        {
            IEnumerable<FeatureSelectionAuditRow> auditRows = (rows ?? new List<TSNESourceRow>())
                .Select(row => new FeatureSelectionAuditRow
                {
                    DraftNo = row.DraftNo,
                    FieldNames = row.DataFieldNames,
                    NumericValues = row.NumericValues
                });
            return CreateFromAuditRows(auditRows, includedFeatureNames, varianceThreshold);
        }

        internal static TSNEFeatureSelectionReport CreateFromParsedExperiments(
            IList<ParsedTSNEExperiment> experiments,
            IEnumerable<string> includedFeatureNames,
            double varianceThreshold)
        {
            IEnumerable<FeatureSelectionAuditRow> auditRows =
                (experiments ?? new List<ParsedTSNEExperiment>())
                    .Select(item => new FeatureSelectionAuditRow
                    {
                        DraftNo = item.Source == null ? string.Empty : item.Source.DraftNo,
                        FieldNames = item.FlattenedValues == null
                            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            : new HashSet<string>(item.FlattenedValues.Keys, StringComparer.OrdinalIgnoreCase),
                        NumericValues = item.NumericFeatures
                    });
            return CreateFromAuditRows(auditRows, includedFeatureNames, varianceThreshold);
        }

        internal static bool IsKnownMetadataFeature(string featureName)
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

            return KnownMetadataNames.Any(name =>
                string.Equals(name, leafName, StringComparison.OrdinalIgnoreCase));
        }

        private static TSNEFeatureSelectionReport CreateFromAuditRows(
            IEnumerable<FeatureSelectionAuditRow> sourceRows,
            IEnumerable<string> includedFeatureNames,
            double varianceThreshold)
        {
            List<FeatureSelectionAuditRow> rows =
                (sourceRows ?? Enumerable.Empty<FeatureSelectionAuditRow>()).ToList();
            var includedSet = new HashSet<string>(
                includedFeatureNames ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            string[] allFeatureNames = rows
                .SelectMany(row => row.FieldNames ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase))
                .Concat(rows.SelectMany(row => row.NumericValues == null
                    ? Enumerable.Empty<string>()
                    : row.NumericValues.Keys))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var details = new List<TSNEFeatureSelectionDetail>(allFeatureNames.Length);
            foreach (string featureName in allFeatureNames)
            {
                details.Add(CreateDetail(rows, featureName, includedSet, varianceThreshold));
            }

            return new TSNEFeatureSelectionReport(rows.Count, details);
        }

        private static TSNEFeatureSelectionDetail CreateDetail(IList<FeatureSelectionAuditRow> rows, string featureName, ISet<string> includedFeatureNames, double varianceThreshold)
        {
            int rowCount = rows.Count;
            int presentCount = 0;
            int numericCount = 0;
            string sampleDraftNo = string.Empty;
            var numericValues = new List<double>();

            foreach (FeatureSelectionAuditRow row in rows)
            {
                bool present = row.FieldNames != null && row.FieldNames.Contains(featureName);
                double numericValue = 0d;
                bool numeric = row.NumericValues != null
                    && row.NumericValues.TryGetValue(featureName, out numericValue);
                if (present || numeric)
                {
                    presentCount++;
                    if (string.IsNullOrEmpty(sampleDraftNo))
                    {
                        sampleDraftNo = row.DraftNo;
                    }
                }

                if (numeric)
                {
                    numericCount++;
                    numericValues.Add(numericValue);
                }
            }

            bool included = includedFeatureNames != null && includedFeatureNames.Contains(featureName);
            TSNEFeatureSelectionReason reason = ResolveReason(
                featureName,
                included,
                rowCount,
                presentCount,
                numericCount,
                numericValues,
                varianceThreshold);

            var detail = new TSNEFeatureSelectionDetail
            {
                FeatureName = featureName,
                Included = included,
                Reason = reason,
                RowCount = rowCount,
                PresentCount = presentCount,
                NumericCount = numericCount,
                MissingCount = Math.Max(0, rowCount - presentCount),
                NonNumericCount = Math.Max(0, presentCount - numericCount),
                SampleDraftNo = sampleDraftNo
            };
            ApplyStatistics(detail, numericValues);
            return detail;
        }

        private static TSNEFeatureSelectionReason ResolveReason(
            string featureName,
            bool included,
            int rowCount,
            int presentCount,
            int numericCount,
            IList<double> numericValues,
            double varianceThreshold)
        {
            if (included)
            {
                return TSNEFeatureSelectionReason.Included;
            }

            if (IsKnownMetadataFeature(featureName))
            {
                return TSNEFeatureSelectionReason.Metadata;
            }

            if (presentCount < rowCount || numericCount < rowCount)
            {
                return presentCount < rowCount
                    ? TSNEFeatureSelectionReason.MissingInRows
                    : TSNEFeatureSelectionReason.NonNumeric;
            }

            if (numericValues == null || numericValues.Count == 0)
            {
                return TSNEFeatureSelectionReason.NonNumeric;
            }

            return TSNEFeatureSelectionReason.ConstantOrLowVariance;
        }

        private static void ApplyStatistics(TSNEFeatureSelectionDetail detail, IList<double> numericValues)
        {
            if (numericValues == null || numericValues.Count == 0)
            {
                detail.HasStatistics = false;
                return;
            }

            double mean = numericValues.Average();
            double variance = numericValues.Average(value =>
            {
                double diff = value - mean;
                return diff * diff;
            });
            detail.HasStatistics = true;
            detail.Mean = mean;
            detail.Variance = variance;
            detail.StandardDeviation = Math.Sqrt(variance);
            detail.Minimum = numericValues.Min();
            detail.Maximum = numericValues.Max();
        }

        private sealed class FeatureSelectionAuditRow
        {
            public string DraftNo { get; set; }
            public ISet<string> FieldNames { get; set; }
            public IDictionary<string, double> NumericValues { get; set; }
        }
    }

    public sealed class TSNEAnalysisResult
    {
        internal TSNEAnalysisResult()
        {
            ScatterData = new List<ScatterSampleData>();
            FeatureNames = new string[0];
            ExcludedFeatureNames = new string[0];
            StandardizedMatrix = new double[0][];
            FeatureSelectionReport = TSNEFeatureSelectionReport.Empty();
        }

        public IList<ScatterSampleData> ScatterData { get; internal set; }
        public string[] FeatureNames { get; internal set; }
        public string[] ExcludedFeatureNames { get; internal set; }
        public double[][] StandardizedMatrix { get; internal set; }
        public StandardScalerModel Scaler { get; internal set; }
        public TSNEProjectionModel TSNEModel { get; internal set; }
        public DimensionalityReductionMethod ProjectionMethod { get; internal set; }
        public KnnSimilarityService Knn { get; internal set; }
        public TSNEVerificationReport Verification { get; internal set; }
        public TSNEAnalysisDiagnosticReport Diagnostic { get; internal set; }
        public TSNEFeatureSelectionReport FeatureSelectionReport { get; internal set; }

        public IList<KnnNeighbor> FindNearest(string draftNo, int count)
        {
            return Knn.FindNearest(draftNo, count);
        }
    }

    internal sealed class TSNESourceRow
    {
        public string DraftNo { get; set; }
        public string AiResultValue { get; set; }
        public IDictionary<string, double> NumericValues { get; set; }
        public ISet<string> DataFieldNames { get; set; }
    }

    internal sealed class FeatureMatrixResult
    {
        public string[] FeatureNames { get; set; }
        public string[] ExcludedFeatureNames { get; set; }
        public double[][] Matrix { get; set; }
        public TSNEFeatureSelectionReport FeatureSelectionReport { get; set; }
    }

    #endregion

    #region JSON Parsing and Numeric Feature Selection

    /// <summary>
    /// JSON 실험 데이터에서 메타데이터와 수치 feature를 분리하고 TSNE 분석 행렬을 만든다.
    /// </summary>
    public sealed class TSNEAnalysisPipeline
    {
        private static readonly string[] DraftNoAliases = { "Draft_NO", "Draft_No", "draft_No" };
        private static readonly string[] AiResultAliases = { "AI_RSLT_Val", "AI_RSLT_VAL", "ENGR_RSLT_VAL", "AiResultValue" };
        private static readonly HashSet<string> MetadataNames = new HashSet<string>(
            DraftNoAliases.Concat(AiResultAliases),
            StringComparer.OrdinalIgnoreCase);

        private readonly TSNEAnalysisOptions options;

        public TSNEAnalysisPipeline()
            : this(new TSNEAnalysisOptions())
        {
        }

        public TSNEAnalysisPipeline(TSNEAnalysisOptions options)
        {
            this.options = options ?? new TSNEAnalysisOptions();
        }

        /// <summary>
        /// DB ACT_DATA 컬럼에서 읽은 JSON 문서를 Dict/List 구조로 파싱하고,
        /// 내부 실험 객체를 개별 JSON 행으로 펼친 뒤 전체 분석을 수행한다.
        /// </summary>
        public TSNEAnalysisResult AnalyzeActDataDocuments(IEnumerable<string> actDataDocuments)
        {
            var parser = new ActDataJsonParser();
            IList<string> experimentRows = parser.ExpandDocuments(actDataDocuments);
            return Analyze(experimentRows);
        }

        /// <summary>
        /// Service DataTable의 CONV_EXPER_CTN JSON 배열을 개별 실험 행으로 펼쳐 분석한다.
        /// 전체 데이터가 하나의 모집단으로 표준화되며 TSNE와 KNN은 같은 결과를 공유한다.
        /// </summary>
        public TSNEAnalysisResult AnalyzeConvExperimentDocuments(IEnumerable<string> convExperimentDocuments)
        {
            var parser = new ActDataJsonParser();
            IList<string> experimentRows = parser.ExpandDocuments(convExperimentDocuments, "CONV_EXPER_CTN");
            return Analyze(experimentRows);
        }

        /// <summary>
        /// 전체 분석 순서를 한 곳에서 보장한다.
        /// JSON 파싱 -> 수치 feature 행렬 생성 -> 정규화 -> TSNE 2차원 좌표 생성 -> KNN 거리 인덱스 생성 -> 검증 순서다.
        /// TSNE와 KNN은 같은 StandardizedMatrix를 공유하므로 특징 좌표계가 달라지지 않는다.
        /// </summary>
        public TSNEAnalysisResult Analyze(IEnumerable<string> jsonSamples)
        {
            // rows: Draft별 원본 JSON에서 식별자/라벨과 수치 후보를 분리한 중간 데이터다.
            List<TSNESourceRow> rows = ParseRows(jsonSamples);
            // features.Matrix: 행은 Draft, 열은 살아남은 수치 feature인 TSNE 입력 수치행렬이다.
            FeatureMatrixResult features = BuildFeatureMatrix(rows, options);
            StandardScalerModel scaler = StandardScalerModel.Fit(features.Matrix, features.FeatureNames);
            // standardized: 각 feature별 평균을 빼고 표준편차로 나눈 정규화 행렬이다.
            double[][] standardized = scaler.Transform(features.Matrix);
            TSNEProjectionModel tsne = null;
            double[][] scores;
            tsne = TSNEProjectionModel.FitTransform(
                standardized,
                options.TSNEPerplexity,
                options.TSNEIterations,
                options.TSNELearningRate,
                options.TSNERandomSeed);
            scores = tsne.Coordinates;

            var scatterData = new List<ScatterSampleData>(rows.Count);
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                scatterData.Add(new ScatterSampleData
                {
                    SourceIndex = rowIndex,
                    DraftNo = rows[rowIndex].DraftNo,
                    AiResultValue = rows[rowIndex].AiResultValue,
                    X1 = scores[rowIndex][0],
                    X2 = scores[rowIndex][1],
                    Distance = null
                });
            }

            var knn = new KnnSimilarityService(
                rows.Select(row => row.DraftNo).ToArray(),
                standardized,
                scaler,
                options.KnnSearchAlgorithm);
            TSNEVerificationReport verification = TSNEAlgorithmVerifier.VerifyTSNE(
                standardized,
                scores,
                scaler,
                knn,
                rows[0].DraftNo,
                options.NeighborCount);

            if (!verification.IsValid)
            {
                throw new InvalidOperationException("Projection/KNN verification failed: " + verification.Message);
            }

            var result = new TSNEAnalysisResult
            {
                ScatterData = scatterData,
                FeatureNames = features.FeatureNames,
                ExcludedFeatureNames = features.ExcludedFeatureNames,
                StandardizedMatrix = standardized,
                Scaler = scaler,
                TSNEModel = tsne,
                ProjectionMethod = options.ProjectionMethod,
                Knn = knn,
                Verification = verification,
                FeatureSelectionReport = features.FeatureSelectionReport
            };
            result.Diagnostic = TSNEAnalysisDiagnosticReport.Create(result, rows.Count, 0);
            return result;
        }

        private static List<TSNESourceRow> ParseRows(IEnumerable<string> jsonSamples)
        {
            string[] source = jsonSamples == null
                ? new string[0]
                : jsonSamples.Where(json => !string.IsNullOrWhiteSpace(json)).ToArray();
            if (source.Length < 3)
            {
                throw new ArgumentException("TSNE requires at least three JSON samples.", "jsonSamples");
            }

            var rows = new List<TSNESourceRow>(source.Length);
            var draftNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < source.Length; index++)
            {
                object deserialized = TSNEJsonUtility.DeserializeObject(source[index]);
                var dictionary = deserialized as IDictionary<string, object>;
                if (dictionary == null)
                {
                    throw new FormatException(string.Format("JSON row {0} is not an object.", index));
                }

                string draftNo = GetRequiredText(dictionary, DraftNoAliases, "Draft_NO", index);
                string aiResult = GetOptionalText(dictionary, AiResultAliases);
                if (!draftNos.Add(draftNo))
                {
                    throw new InvalidOperationException("Duplicate Draft_NO: " + draftNo);
                }

                var numericValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, object> pair in dictionary)
                {
                    // Draft_NO와 AI_RSLT_Val은 검색/라벨용 데이터라 TSNE 계산 feature에서는 제외한다.
                    if (MetadataNames.Contains(pair.Key))
                    {
                        continue;
                    }

                    fieldNames.Add(pair.Key);
                    double numericValue;
                    if (TryConvertFiniteDouble(pair.Value, out numericValue))
                    {
                        numericValues[pair.Key] = numericValue;
                    }
                }

                rows.Add(new TSNESourceRow
                {
                    DraftNo = draftNo,
                    AiResultValue = aiResult,
                    NumericValues = numericValues,
                    DataFieldNames = fieldNames
                });
            }

            return rows;
        }

        private static string GetRequiredText(IDictionary<string, object> dictionary, IEnumerable<string> aliases, string displayName, int rowIndex)
        {
            foreach (string alias in aliases)
            {
                KeyValuePair<string, object> match = dictionary.FirstOrDefault(pair =>
                    string.Equals(pair.Key, alias, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match.Key) && match.Value != null)
                {
                    string text = Convert.ToString(match.Value, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text.Trim();
                    }
                }
            }

            throw new FormatException(string.Format("JSON row {0} does not contain {1}.", rowIndex, displayName));
        }

        private static string GetOptionalText(IDictionary<string, object> dictionary, IEnumerable<string> aliases)
        {
            foreach (string alias in aliases)
            {
                KeyValuePair<string, object> match = dictionary.FirstOrDefault(pair =>
                    string.Equals(pair.Key, alias, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match.Key) && match.Value != null)
                {
                    string text = Convert.ToString(match.Value, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text.Trim();
                    }
                }
            }

            return string.Empty;
        }

        private static bool TryConvertFiniteDouble(object value, out double numericValue)
        {
            numericValue = 0d;
            if (value == null || value is bool)
            {
                return false;
            }

            string text = value as string;
            if (text != null)
            {
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out numericValue))
                {
                    return false;
                }
            }
            else
            {
                try
                {
                    numericValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                }
                catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
                {
                    return false;
                }
            }

            return !double.IsNaN(numericValue) && !double.IsInfinity(numericValue);
        }

        /// <summary>
        /// 모든 Draft에서 사용할 수 있는 수치 feature만 골라 TSNE 입력 행렬을 만든다.
        /// 누락이 있더라도 옵션 기준을 통과하면 feature 평균값으로 보정한다.
        /// </summary>
        private static FeatureMatrixResult BuildFeatureMatrix(IList<TSNESourceRow> rows, TSNEAnalysisOptions analysisOptions)
        {
            TSNEAnalysisOptions effectiveOptions = analysisOptions ?? new TSNEAnalysisOptions();
            double varianceThreshold = Math.Max(0d, effectiveOptions.ConstantVarianceThreshold);
            double minimumNumericCoverageRatio = NormalizeCoverageRatio(
                effectiveOptions.MinimumNumericFeatureCoverageRatio);
            string[] allFields = rows
                .SelectMany(row => row.DataFieldNames)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var included = new List<string>();
            var excluded = new List<string>();
            var imputationMeans = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (string fieldName in allFields)
            {
                double[] numericValues = rows
                    .Where(row => row.NumericValues.ContainsKey(fieldName))
                    .Select(row => row.NumericValues[fieldName])
                    .ToArray();
                double numericCoverageRatio = rows.Count == 0
                    ? 0d
                    : numericValues.Length / (double)rows.Count;
                bool numericInEveryRow = numericValues.Length == rows.Count;
                bool coverageAccepted = numericValues.Length > 0
                    && (numericInEveryRow
                    || (effectiveOptions.MeanImputationEnabled
                        && numericCoverageRatio >= minimumNumericCoverageRatio));
                if (!coverageAccepted)
                {
                    excluded.Add(fieldName);
                    continue;
                }

                double mean = numericValues.Average();
                double variance = numericValues.Average(value =>
                {
                    double difference = value - mean;
                    return difference * difference;
                });

                // 분산이 기준값 이하인 컬럼은 정보량이 거의 없고 표준화 시 0으로 나누게 되므로 제거한다.
                if (variance <= varianceThreshold)
                {
                    excluded.Add(fieldName);
                    continue;
                }

                included.Add(fieldName);
                imputationMeans[fieldName] = mean;
            }

            if (included.Count < 2)
            {
                throw new InvalidOperationException("At least two numeric non-constant features are required.");
            }

            double[][] matrix = rows
                .Select(row => included.Select(feature =>
                {
                    double value;
                    // 누락된 값은 feature 평균으로 채워 행렬의 모든 row가 같은 열 구조를 갖게 한다.
                    return row.NumericValues.TryGetValue(feature, out value)
                        ? value
                        : imputationMeans[feature];
                }).ToArray())
                .ToArray();
            return new FeatureMatrixResult
            {
                FeatureNames = included.ToArray(),
                ExcludedFeatureNames = excluded.ToArray(),
                Matrix = matrix,
                FeatureSelectionReport = TSNEFeatureSelectionReport.CreateFromSourceRows(
                    rows,
                    included,
                    varianceThreshold)
            };
        }

        private static double NormalizeCoverageRatio(double ratio)
        {
            if (double.IsNaN(ratio) || double.IsInfinity(ratio))
            {
                return 1d;
            }

            if (ratio < 0d)
            {
                return 0d;
            }

            if (ratio > 1d)
            {
                return 1d;
            }

            return ratio;
        }
    }

    #endregion

    #region StandardScaler - Mean 0 and Standard Deviation 1

    public sealed class StandardScalerModel
    {
        private StandardScalerModel(string[] featureNames, double[] means, double[] standardDeviations)
        {
            FeatureNames = featureNames;
            Means = means;
            StandardDeviations = standardDeviations;
        }

        public string[] FeatureNames { get; private set; }
        public double[] Means { get; private set; }
        public double[] StandardDeviations { get; private set; }

        public static StandardScalerModel Fit(double[][] matrix, string[] featureNames)
        {
            ValidateMatrix(matrix);
            int columnCount = matrix[0].Length;
            if (featureNames == null || featureNames.Length != columnCount)
            {
                throw new ArgumentException("Feature name count must match matrix column count.", "featureNames");
            }

            var means = new double[columnCount];
            var standardDeviations = new double[columnCount];
            for (int column = 0; column < columnCount; column++)
            {
                // feature별 평균과 표준편차는 전체 모집단 기준으로 한 번만 계산한다.
                means[column] = matrix.Average(row => row[column]);
                double variance = matrix.Average(row =>
                {
                    double difference = row[column] - means[column];
                    return difference * difference;
                });
                standardDeviations[column] = Math.Sqrt(variance);
                if (standardDeviations[column] <= 0d)
                {
                    throw new InvalidOperationException("Cannot standardize a constant feature: " + featureNames[column]);
                }
            }

            return new StandardScalerModel(
                (string[])featureNames.Clone(),
                means,
                standardDeviations);
        }

        public double[][] Transform(double[][] matrix)
        {
            ValidateMatrix(matrix);
            if (matrix[0].Length != Means.Length)
            {
                throw new ArgumentException("Matrix column count does not match the fitted scaler.", "matrix");
            }

            var transformed = new double[matrix.Length][];
            for (int row = 0; row < matrix.Length; row++)
            {
                transformed[row] = new double[Means.Length];
                for (int column = 0; column < Means.Length; column++)
                {
                    // 정규화 공식: 현재 값에서 모집단 평균을 빼고 모집단 표준편차로 나눈다.
                    transformed[row][column] = (matrix[row][column] - Means[column])
                        / StandardDeviations[column];
                }
            }

            return transformed;
        }

        private static void ValidateMatrix(double[][] matrix)
        {
            if (matrix == null || matrix.Length == 0 || matrix[0] == null || matrix[0].Length == 0)
            {
                throw new ArgumentException("Matrix must contain rows and columns.", "matrix");
            }

            int columnCount = matrix[0].Length;
            if (matrix.Any(row => row == null || row.Length != columnCount))
            {
                throw new ArgumentException("All matrix rows must have the same column count.", "matrix");
            }
        }
    }

    #endregion

    public sealed class KnnSimilarityService
    {
        private readonly string[] draftNos;
        private readonly double[][] standardizedMatrix;
        private readonly Dictionary<string, int> indexByDraftNo;
        private readonly IKnnSearchIndex searchIndex;

        public KnnSimilarityService(string[] draftNos, double[][] standardizedMatrix)
            : this(draftNos, standardizedMatrix, null, KnnSearchAlgorithm.Auto)
        {
        }

        public KnnSimilarityService(string[] draftNos, double[][] standardizedMatrix, StandardScalerModel scaler)
            : this(draftNos, standardizedMatrix, scaler, KnnSearchAlgorithm.Auto)
        {
        }

        public KnnSimilarityService(string[] draftNos, double[][] standardizedMatrix, StandardScalerModel scaler, KnnSearchAlgorithm requestedAlgorithm)
        {
            if (draftNos == null || standardizedMatrix == null || draftNos.Length != standardizedMatrix.Length)
            {
                throw new ArgumentException("Draft numbers and standardized matrix must have the same row count.");
            }

            this.draftNos = (string[])draftNos.Clone();
            this.standardizedMatrix = standardizedMatrix.Select(row => (double[])row.Clone()).ToArray();
            ValidateMatrix(this.standardizedMatrix);
            Scaler = scaler;
            RequestedAlgorithm = requestedAlgorithm;
            ActualAlgorithm = ResolveAlgorithm(
                requestedAlgorithm,
                this.standardizedMatrix.Length,
                this.standardizedMatrix[0].Length,
                out string selectionReason);
            SelectionReason = selectionReason;
            searchIndex = CreateSearchIndex(ActualAlgorithm, this.standardizedMatrix, this.draftNos);
            indexByDraftNo = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < this.draftNos.Length; index++)
            {
                if (!indexByDraftNo.ContainsKey(this.draftNos[index]))
                {
                    indexByDraftNo.Add(this.draftNos[index], index);
                }
            }
        }

        public StandardScalerModel Scaler { get; private set; }
        public KnnSearchAlgorithm RequestedAlgorithm { get; private set; }
        public KnnSearchAlgorithm ActualAlgorithm { get; private set; }
        public string SelectionReason { get; private set; }

        public IList<KnnNeighbor> FindNearest(string draftNo, int count)
        {
            int targetIndex;
            if (string.IsNullOrWhiteSpace(draftNo) || !indexByDraftNo.TryGetValue(draftNo.Trim(), out targetIndex))
            {
                throw new KeyNotFoundException("존재하지 않는 Draft_NO입니다. " + (draftNo ?? string.Empty));
            }

            int safeCount = Math.Max(0, count);
            if (safeCount == 0)
            {
                return new List<KnnNeighbor>();
            }

            IList<NeighborCandidate> candidates = searchIndex.FindNearest(targetIndex, safeCount);
            var neighbors = new List<KnnNeighbor>(candidates.Count);
            for (int index = 0; index < candidates.Count; index++)
            {
                NeighborCandidate candidate = candidates[index];
                neighbors.Add(new KnnNeighbor
                {
                    Rank = index + 1,
                    SourceIndex = candidate.SourceIndex,
                    DraftNo = draftNos[candidate.SourceIndex],
                    Distance = Math.Sqrt(candidate.DistanceSquared)
                });
            }

            return neighbors;
        }

        private static void ValidateMatrix(double[][] matrix)
        {
            if (matrix == null || matrix.Length == 0 || matrix[0] == null || matrix[0].Length == 0)
            {
                throw new ArgumentException("KNN matrix must contain rows and columns.", "standardizedMatrix");
            }

            int dimensionCount = matrix[0].Length;
            if (matrix.Any(row => row == null || row.Length != dimensionCount))
            {
                throw new ArgumentException("All KNN matrix rows must have the same dimension count.", "standardizedMatrix");
            }
        }

        private static KnnSearchAlgorithm ResolveAlgorithm(KnnSearchAlgorithm requestedAlgorithm, int rowCount, int dimensionCount, out string reason)
        {
            if (requestedAlgorithm != KnnSearchAlgorithm.Auto)
            {
                reason = string.Format(
                    CultureInfo.InvariantCulture,
                    "ManualSelection Rows={0} Dimensions={1}",
                    rowCount,
                    dimensionCount);
                return requestedAlgorithm;
            }

            if (rowCount <= 10000)
            {
                reason = string.Format(
                    CultureInfo.InvariantCulture,
                    "Auto:RowsNotLarge Rows={0} Dimensions={1}",
                    rowCount,
                    dimensionCount);
                return KnnSearchAlgorithm.BruteForce;
            }

            if (dimensionCount <= 10)
            {
                reason = string.Format(
                    CultureInfo.InvariantCulture,
                    "Auto:LowDimension Rows={0} Dimensions={1}",
                    rowCount,
                    dimensionCount);
                return KnnSearchAlgorithm.KdTree;
            }

            if (dimensionCount <= 30)
            {
                reason = string.Format(
                    CultureInfo.InvariantCulture,
                    "Auto:MediumDimension Rows={0} Dimensions={1}",
                    rowCount,
                    dimensionCount);
                return KnnSearchAlgorithm.BallTree;
            }

            reason = string.Format(
                CultureInfo.InvariantCulture,
                "Auto:HighDimension Rows={0} Dimensions={1}",
                rowCount,
                dimensionCount);
            return KnnSearchAlgorithm.BruteForce;
        }

        private static IKnnSearchIndex CreateSearchIndex(KnnSearchAlgorithm algorithm, double[][] matrix, string[] draftNos)
        {
            switch (algorithm)
            {
                case KnnSearchAlgorithm.KdTree:
                    return new KdTreeSearchIndex(matrix, draftNos);
                case KnnSearchAlgorithm.BallTree:
                    return new BallTreeSearchIndex(matrix, draftNos);
                case KnnSearchAlgorithm.BruteForce:
                case KnnSearchAlgorithm.Auto:
                default:
                    return new BruteForceSearchIndex(matrix, draftNos);
            }
        }

        private static double CalculateSquaredDistance(double[] left, double[] right)
        {
            double squaredDistance = 0d;
            for (int index = 0; index < left.Length; index++)
            {
                // KNN 거리는 정규화된 전체 feature 벡터의 차이를 제곱합으로 누적한다.
                double difference = left[index] - right[index];
                squaredDistance += difference * difference;
            }

            return squaredDistance;
        }

        private static int CompareCandidate(NeighborCandidate left, NeighborCandidate right, string[] draftNumbers)
        {
            int distanceCompare = left.DistanceSquared.CompareTo(right.DistanceSquared);
            if (distanceCompare != 0)
            {
                return distanceCompare;
            }

            return string.Compare(
                draftNumbers[left.SourceIndex],
                draftNumbers[right.SourceIndex],
                StringComparison.OrdinalIgnoreCase);
        }

        private interface IKnnSearchIndex
        {
            IList<NeighborCandidate> FindNearest(int targetIndex, int count);
        }

        private sealed class NeighborCandidate
        {
            public int SourceIndex { get; set; }
            public double DistanceSquared { get; set; }
        }

        private sealed class NeighborCandidateQueue
        {
            private readonly string[] draftNumbers;
            private readonly int capacity;
            private readonly List<NeighborCandidate> candidates;

            public NeighborCandidateQueue(int capacity, string[] draftNumbers)
            {
                this.capacity = Math.Max(0, capacity);
                this.draftNumbers = draftNumbers;
                candidates = new List<NeighborCandidate>(this.capacity);
            }

            public int Count
            {
                get { return candidates.Count; }
            }

            public double WorstDistanceSquared
            {
                get
                {
                    return candidates.Count < capacity || candidates.Count == 0
                        ? double.PositiveInfinity
                        : candidates[candidates.Count - 1].DistanceSquared;
                }
            }

            public bool IsFull
            {
                get { return capacity > 0 && candidates.Count >= capacity; }
            }

            public void Add(int sourceIndex, double distanceSquared)
            {
                if (capacity <= 0)
                {
                    return;
                }

                var candidate = new NeighborCandidate
                {
                    SourceIndex = sourceIndex,
                    DistanceSquared = distanceSquared
                };
                int insertIndex = candidates.FindIndex(item =>
                    CompareCandidate(candidate, item, draftNumbers) < 0);
                if (insertIndex < 0)
                {
                    candidates.Add(candidate);
                }
                else
                {
                    candidates.Insert(insertIndex, candidate);
                }

                if (candidates.Count > capacity)
                {
                    candidates.RemoveAt(candidates.Count - 1);
                }
            }

            public IList<NeighborCandidate> ToList()
            {
                return candidates.ToList();
            }
        }

        private sealed class BruteForceSearchIndex : IKnnSearchIndex
        {
            private readonly double[][] matrix;
            private readonly string[] draftNumbers;

            public BruteForceSearchIndex(double[][] matrix, string[] draftNumbers)
            {
                this.matrix = matrix;
                this.draftNumbers = draftNumbers;
            }

            public IList<NeighborCandidate> FindNearest(int targetIndex, int count)
            {
                var queue = new NeighborCandidateQueue(count, draftNumbers);
                double[] target = matrix[targetIndex];
                for (int sourceIndex = 0; sourceIndex < matrix.Length; sourceIndex++)
                {
                    if (sourceIndex == targetIndex)
                    {
                        continue;
                    }

                    queue.Add(sourceIndex, CalculateSquaredDistance(target, matrix[sourceIndex]));
                }

                return queue.ToList();
            }
        }

        private sealed class KdTreeSearchIndex : IKnnSearchIndex
        {
            private readonly double[][] matrix;
            private readonly string[] draftNumbers;
            private readonly KdNode root;
            private readonly int dimensionCount;

            public KdTreeSearchIndex(double[][] matrix, string[] draftNumbers)
            {
                this.matrix = matrix;
                this.draftNumbers = draftNumbers;
                dimensionCount = matrix[0].Length;
                root = Build(Enumerable.Range(0, matrix.Length).ToArray(), 0);
            }

            public IList<NeighborCandidate> FindNearest(int targetIndex, int count)
            {
                var queue = new NeighborCandidateQueue(count, draftNumbers);
                Search(root, targetIndex, matrix[targetIndex], queue);
                return queue.ToList();
            }

            private KdNode Build(int[] indices, int depth)
            {
                if (indices == null || indices.Length == 0)
                {
                    return null;
                }

                int axis = SelectSplitAxis(indices, depth);
                Array.Sort(indices, (left, right) => matrix[left][axis].CompareTo(matrix[right][axis]));
                int median = indices.Length / 2;
                return new KdNode
                {
                    SourceIndex = indices[median],
                    Axis = axis,
                    Left = Build(indices.Take(median).ToArray(), depth + 1),
                    Right = Build(indices.Skip(median + 1).ToArray(), depth + 1)
                };
            }

            private int SelectSplitAxis(int[] indices, int depth)
            {
                if (indices.Length < 8)
                {
                    return depth % dimensionCount;
                }

                int bestAxis = 0;
                double bestVariance = double.NegativeInfinity;
                for (int axis = 0; axis < dimensionCount; axis++)
                {
                    double mean = indices.Average(index => matrix[index][axis]);
                    double variance = indices.Average(index =>
                    {
                        double diff = matrix[index][axis] - mean;
                        return diff * diff;
                    });
                    if (variance > bestVariance)
                    {
                        bestVariance = variance;
                        bestAxis = axis;
                    }
                }

                return bestAxis;
            }

            private void Search(KdNode node, int targetIndex, double[] target, NeighborCandidateQueue queue)
            {
                if (node == null)
                {
                    return;
                }

                double axisDifference = target[node.Axis] - matrix[node.SourceIndex][node.Axis];
                KdNode first = axisDifference <= 0d ? node.Left : node.Right;
                KdNode second = axisDifference <= 0d ? node.Right : node.Left;
                Search(first, targetIndex, target, queue);

                if (node.SourceIndex != targetIndex)
                {
                    queue.Add(
                        node.SourceIndex,
                        CalculateSquaredDistance(target, matrix[node.SourceIndex]));
                }

                if (!queue.IsFull
                    || (axisDifference * axisDifference) <= queue.WorstDistanceSquared)
                {
                    Search(second, targetIndex, target, queue);
                }
            }

            private sealed class KdNode
            {
                public int SourceIndex { get; set; }
                public int Axis { get; set; }
                public KdNode Left { get; set; }
                public KdNode Right { get; set; }
            }
        }

        private sealed class BallTreeSearchIndex : IKnnSearchIndex
        {
            private const int LeafSize = 32;
            private readonly double[][] matrix;
            private readonly string[] draftNumbers;
            private readonly int dimensionCount;
            private readonly BallNode root;

            public BallTreeSearchIndex(double[][] matrix, string[] draftNumbers)
            {
                this.matrix = matrix;
                this.draftNumbers = draftNumbers;
                dimensionCount = matrix[0].Length;
                root = Build(Enumerable.Range(0, matrix.Length).ToArray());
            }

            public IList<NeighborCandidate> FindNearest(int targetIndex, int count)
            {
                var queue = new NeighborCandidateQueue(count, draftNumbers);
                Search(root, targetIndex, matrix[targetIndex], queue);
                return queue.ToList();
            }

            private BallNode Build(int[] indices)
            {
                if (indices == null || indices.Length == 0)
                {
                    return null;
                }

                double[] center = CalculateCenter(indices);
                double radius = indices.Max(index => Math.Sqrt(CalculateSquaredDistance(center, matrix[index])));
                if (indices.Length <= LeafSize)
                {
                    return new BallNode
                    {
                        Center = center,
                        Radius = radius,
                        Indices = indices
                    };
                }

                int axis = SelectSplitAxis(indices);
                Array.Sort(indices, (left, right) => matrix[left][axis].CompareTo(matrix[right][axis]));
                int middle = indices.Length / 2;
                return new BallNode
                {
                    Center = center,
                    Radius = radius,
                    Left = Build(indices.Take(middle).ToArray()),
                    Right = Build(indices.Skip(middle).ToArray())
                };
            }

            private double[] CalculateCenter(int[] indices)
            {
                var center = new double[dimensionCount];
                for (int axis = 0; axis < dimensionCount; axis++)
                {
                    center[axis] = indices.Average(index => matrix[index][axis]);
                }

                return center;
            }

            private int SelectSplitAxis(int[] indices)
            {
                int bestAxis = 0;
                double bestVariance = double.NegativeInfinity;
                for (int axis = 0; axis < dimensionCount; axis++)
                {
                    double mean = indices.Average(index => matrix[index][axis]);
                    double variance = indices.Average(index =>
                    {
                        double diff = matrix[index][axis] - mean;
                        return diff * diff;
                    });
                    if (variance > bestVariance)
                    {
                        bestVariance = variance;
                        bestAxis = axis;
                    }
                }

                return bestAxis;
            }

            private void Search(BallNode node, int targetIndex, double[] target, NeighborCandidateQueue queue)
            {
                if (node == null)
                {
                    return;
                }

                double lowerBound = CalculateLowerBoundSquared(target, node);
                if (queue.IsFull && lowerBound > queue.WorstDistanceSquared)
                {
                    return;
                }

                if (node.Indices != null)
                {
                    foreach (int sourceIndex in node.Indices)
                    {
                        if (sourceIndex == targetIndex)
                        {
                            continue;
                        }

                        queue.Add(sourceIndex, CalculateSquaredDistance(target, matrix[sourceIndex]));
                    }

                    return;
                }

                double leftBound = CalculateLowerBoundSquared(target, node.Left);
                double rightBound = CalculateLowerBoundSquared(target, node.Right);
                if (leftBound <= rightBound)
                {
                    Search(node.Left, targetIndex, target, queue);
                    Search(node.Right, targetIndex, target, queue);
                }
                else
                {
                    Search(node.Right, targetIndex, target, queue);
                    Search(node.Left, targetIndex, target, queue);
                }
            }

            private static double CalculateLowerBoundSquared(double[] target, BallNode node)
            {
                if (node == null)
                {
                    return double.PositiveInfinity;
                }

                double centerDistance = Math.Sqrt(CalculateSquaredDistance(target, node.Center));
                double lowerBound = Math.Max(0d, centerDistance - node.Radius);
                return lowerBound * lowerBound;
            }

            private sealed class BallNode
            {
                public double[] Center { get; set; }
                public double Radius { get; set; }
                public int[] Indices { get; set; }
                public BallNode Left { get; set; }
                public BallNode Right { get; set; }
            }
        }
    }

    #region Algorithm Self Verification


    internal static class TSNEAlgorithmVerifier
    {
        public static TSNEVerificationReport VerifyTSNE(
            double[][] standardized,
            double[][] coordinates,
            StandardScalerModel scaler,
            KnnSimilarityService knn,
            string firstDraftNo,
            int neighborCount)
        {
            int rowCount = standardized.Length;
            int columnCount = standardized[0].Length;
            double maxMean = 0d;
            double maxStandardDeviationError = 0d;
            for (int column = 0; column < columnCount; column++)
            {
                double mean = standardized.Average(row => row[column]);
                double variance = standardized.Average(row =>
                {
                    double difference = row[column] - mean;
                    return difference * difference;
                });
                maxMean = Math.Max(maxMean, Math.Abs(mean));
                maxStandardDeviationError = Math.Max(maxStandardDeviationError, Math.Abs(Math.Sqrt(variance) - 1d));
            }

            bool finiteCoordinates = coordinates != null
                && coordinates.Length == rowCount
                && coordinates.All(row => row != null
                    && row.Length == 2
                    && row.All(value => !double.IsNaN(value) && !double.IsInfinity(value)));
            IList<KnnNeighbor> neighbors = knn.FindNearest(firstDraftNo, neighborCount);
            bool knnValid = neighbors.Count == Math.Min(Math.Max(0, neighborCount), rowCount - 1)
                && neighbors.All(item => !string.Equals(item.DraftNo, firstDraftNo, StringComparison.OrdinalIgnoreCase))
                && neighbors.Select(item => item.Distance).SequenceEqual(neighbors.Select(item => item.Distance).OrderBy(value => value));
            bool sharedScaler = scaler != null
                && object.ReferenceEquals(scaler, knn.Scaler)
                && scaler.FeatureNames != null
                && scaler.FeatureNames.Length == columnCount;
            bool valid = finiteCoordinates && sharedScaler;

            return new TSNEVerificationReport
            {
                IsValid = valid,
                MaximumAbsoluteStandardizedMean = maxMean,
                MaximumStandardDeviationError = maxStandardDeviationError,
                ComponentDotProduct = 0d,
                EigenValuesDescending = true,
                AllScoresFinite = finiteCoordinates,
                KnnResultValid = knnValid,
                SharedScalerInstance = sharedScaler,
                Message = valid
                    ? "Accord.NET t-SNE returned finite coordinates and shares the StandardScaler with KNN."
                    : string.Format(
                        CultureInfo.InvariantCulture,
                        "Accord.NET t-SNE validation failed (finite={0}, sharedScaler={1}, knn={2}, mean={3:0.####}, stdError={4:0.####}).",
                        finiteCoordinates,
                        sharedScaler,
                        knnValid,
                        maxMean,
                        maxStandardDeviationError)
            };
        }

    }

    #endregion
}












namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public sealed class TSNESampleClickedEventArgs : EventArgs
    {
        public TSNESampleClickedEventArgs(ScatterSampleData sample, IList<KnnNeighbor> neighbors, LightningScatterPointClickEventArgs sourceEventArgs)
        {
            Sample = sample;
            Neighbors = neighbors == null ? new List<KnnNeighbor>() : neighbors.ToList();
            SourceEventArgs = sourceEventArgs;
        }

        public ScatterSampleData Sample { get; private set; }
        public IList<KnnNeighbor> Neighbors { get; private set; }
        public LightningScatterPointClickEventArgs SourceEventArgs { get; private set; }
    }

    public sealed class TSNEAnalysisCompletedEventArgs : EventArgs
    {
        public TSNEAnalysisCompletedEventArgs(TSNEAnalysisResult analysisResult)
        {
            AnalysisResult = analysisResult;
        }

        public TSNEAnalysisResult AnalysisResult { get; private set; }
    }

    public sealed class TSNEAnalysisFailedEventArgs : EventArgs
    {
        public TSNEAnalysisFailedEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }
    }

    public sealed class TSNEChart : IDisposable
    {
        private readonly WinFormsControl parent;
        private readonly TSNEScatterSeriesBuilder seriesBuilder;
        private LightningScatter scatterChart;
        private TSNEScatterOptions options;
        private TSNEAnalysisResult analysisResult;
        private DataTable rawData;
        private bool disposed;

        private TSNEChart(WinFormsControl parent, TSNEScatterOptions options)
        {
            this.parent = parent;
            this.options = options == null ? TSNEScatterOptions.CreateDefault() : options.Clone();
            seriesBuilder = new TSNEScatterSeriesBuilder();
            scatterChart = LightningScatter.Create(parent, Enumerable.Empty<LightningScatterSeries>(), this.options.ToScatterOptions(null));
            scatterChart.PointClicked += ScatterChart_PointClicked;
            scatterChart.Clear();
        }

        public event EventHandler<TSNESampleClickedEventArgs> SampleClicked;
        public event EventHandler<TSNEAnalysisCompletedEventArgs> AnalysisCompleted;
        public event EventHandler<TSNEAnalysisFailedEventArgs> AnalysisFailed;

        public LightningScatter ScatterControl
        {
            get { return scatterChart; }
        }

        public TSNEScatterOptions Options
        {
            get { return options.Clone(); }
        }

        public TSNEAnalysisResult AnalysisResult
        {
            get { return analysisResult; }
        }

        public DataTable RawData
        {
            get { return rawData == null ? null : rawData.Copy(); }
        }

        public string LastSavedImagePath
        {
            get { return scatterChart.LastSavedImagePath; }
        }

        public Image LastSavedImage
        {
            get { return scatterChart.LastSavedImage; }
        }

        public static TSNEChart Create(WinFormsControl parent)
        {
            return Create(parent, TSNEScatterOptions.CreateDefault());
        }

        public static TSNEChart Create(WinFormsControl parent, TSNEScatterOptions options)
        {
            return new TSNEChart(parent, options);
        }

        public static TSNEChart Create(WinFormsControl parent, TSNEScatterDataSource dataSource)
        {
            return Create(parent, dataSource, TSNEScatterOptions.CreateDefault());
        }

        public static TSNEChart Create(WinFormsControl parent, TSNEScatterDataSource dataSource, TSNEScatterOptions options)
        {
            TSNEChart chart = Create(parent, options);
            chart.Bind(dataSource, options);
            return chart;
        }

        public void Bind(TSNEScatterDataSource dataSource)
        {
            Bind(dataSource, options);
        }

        public void Bind(TSNEScatterDataSource dataSource, TSNEScatterOptions newOptions)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException("dataSource");
            }

            TSNEScatterOptions nextOptions = newOptions == null ? TSNEScatterOptions.CreateDefault() : newOptions.Clone();
            try
            {
                TSNEAnalysisResult result = dataSource.Analyze(nextOptions.Analysis);
                Bind(result, nextOptions);
            }
            catch (Exception ex)
            {
                OnAnalysisFailed(new TSNEAnalysisFailedEventArgs(ex));
                throw;
            }
        }

        public void Bind(TSNEAnalysisResult result)
        {
            Bind(result, options);
        }

        public void Bind(TSNEAnalysisResult result, TSNEScatterOptions newOptions)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            options = newOptions == null ? TSNEScatterOptions.CreateDefault() : newOptions.Clone();
            analysisResult = result;
            rawData = null;
            IEnumerable<LightningScatterSeries> series = seriesBuilder.Build(analysisResult, options.Series);
            scatterChart.UpdateData(series, options.ToScatterOptions(analysisResult));
            OnAnalysisCompleted(new TSNEAnalysisCompletedEventArgs(analysisResult));
        }

        public void Bind(TSNEExadataAnalysisResult result, TSNEScatterOptions newOptions)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            Bind(result.AnalysisResult, newOptions);
            rawData = result.CreateRawDataTable();
        }

        public void BindFromDatabase(TSNEScatterDatabaseOptions databaseOptions)
        {
            BindFromDatabase(databaseOptions, options);
        }

        public void BindFromDatabase(DataTable sourceTable)
        {
            BindFromDatabase(sourceTable, TSNEScatterDatabaseOptions.CreateDefault(), options);
        }

        public void BindFromDatabase(DataTable sourceTable, TSNEScatterDatabaseOptions databaseOptions)
        {
            BindFromDatabase(sourceTable, databaseOptions, options);
        }

        public void BindFromDatabase(DataTable sourceTable, TSNEScatterDatabaseOptions databaseOptions, TSNEScatterOptions newOptions)
        {
            TSNEScatterDatabaseOptions effectiveDatabaseOptions = databaseOptions ?? TSNEScatterDatabaseOptions.CreateDefault();
            effectiveDatabaseOptions.SourceTable = sourceTable;
            BindFromDatabase(effectiveDatabaseOptions, newOptions);
        }

        public void BindFromDatabase(TSNEScatterDatabaseOptions databaseOptions, TSNEScatterOptions newOptions)
        {
            TSNEScatterDatabaseOptions effectiveDatabaseOptions = databaseOptions ?? TSNEScatterDatabaseOptions.CreateDefault();
            TSNEScatterOptions nextOptions = newOptions == null ? TSNEScatterOptions.CreateDefault() : newOptions.Clone();

            try
            {
                if (effectiveDatabaseOptions.SourceTable == null)
                {
                    throw new InvalidOperationException(
                        "BindFromDatabase requires a DataTable. Load data in the UI/service layer and pass it to the chart.");
                }

                ActDataRepository repository = new ActDataRepository(effectiveDatabaseOptions.SourceTable, effectiveDatabaseOptions.ToActDataQueryOptions());
                IList<string> actDataDocuments = repository.LoadActData();
                TSNEAnalysisResult result = TSNEScatterDataSource
                    .FromActDataJson(actDataDocuments)
                    .Analyze(nextOptions.Analysis);
                Bind(result, nextOptions);
            }
            catch (Exception ex)
            {
                OnAnalysisFailed(new TSNEAnalysisFailedEventArgs(ex));
                throw;
            }
        }

        public void BindFromExadata(TSNEScatterExadataOptions exadataOptions)
        {
            BindFromExadata(exadataOptions, options);
        }

        public void BindFromExadata(DataTable sourceTable)
        {
            BindFromExadata(sourceTable, TSNEScatterExadataOptions.CreateDefault(), options);
        }

        public void BindFromExadata(DataTable sourceTable, TSNEScatterExadataOptions exadataOptions)
        {
            BindFromExadata(sourceTable, exadataOptions, options);
        }

        public void BindFromExadata(DataTable sourceTable, TSNEScatterExadataOptions exadataOptions, TSNEScatterOptions newOptions)
        {
            TSNEScatterExadataOptions effectiveOptions = exadataOptions ?? TSNEScatterExadataOptions.CreateDefault();
            effectiveOptions.SourceTable = sourceTable;
            BindFromExadata(effectiveOptions, newOptions);
        }

        public void BindFromExadata(TSNEScatterExadataOptions exadataOptions, TSNEScatterOptions newOptions)
        {
            TSNEScatterExadataOptions effectiveOptions = exadataOptions ?? TSNEScatterExadataOptions.CreateDefault();
            TSNEScatterOptions nextOptions = newOptions == null ? TSNEScatterOptions.CreateDefault() : newOptions.Clone();

            try
            {
                if (effectiveOptions.SourceTable == null)
                {
                    throw new InvalidOperationException(
                        "BindFromExadata requires a DataTable. Load data in the UI/service layer and pass it to the chart.");
                }

                var repository = new ConvExperimentRepository(effectiveOptions.SourceTable, effectiveOptions.ToQueryOptions());
                IList<TSNEExadataSourceRow> rows = repository.LoadAll();
                var snapshot = new TSNEExadataSnapshot(rows, DateTime.UtcNow);
                var service = new TSNEExadataService(repository);
                TSNEExadataAnalysisResult result = service.AnalyzeSnapshot(snapshot, effectiveOptions.ParameterType, nextOptions.Analysis);
                Bind(result, nextOptions);
            }
            catch (Exception ex)
            {
                OnAnalysisFailed(new TSNEAnalysisFailedEventArgs(ex));
                throw;
            }
        }

        public IList<KnnNeighbor> FindNearest(string draftNo)
        {
            return FindNearest(draftNo, options.Analysis.NeighborCount);
        }

        public IList<KnnNeighbor> FindNearest(string draftNo, int count)
        {
            if (analysisResult == null)
            {
                return new List<KnnNeighbor>();
            }

            return analysisResult.FindNearest(draftNo, Math.Max(1, count));
        }

        public void HighlightSelectedDraft(string draftNo)
        {
            if (analysisResult == null)
            {
                return;
            }

            TSNEScatterOptions nextOptions = options == null ? TSNEScatterOptions.CreateDefault() : options.Clone();
            if (nextOptions.Series == null)
            {
                nextOptions.Series = new TSNEScatterSeriesOptions();
            }

            nextOptions.Series.SelectedDraftNo = string.IsNullOrWhiteSpace(draftNo) ? string.Empty : draftNo.Trim();
            RefreshSeries(nextOptions);
        }

        public void HighlightDraft(string draftNo)
        {
            if (analysisResult == null)
            {
                return;
            }

            TSNEScatterOptions nextOptions = options == null ? TSNEScatterOptions.CreateDefault() : options.Clone();
            if (nextOptions.Series == null)
            {
                nextOptions.Series = new TSNEScatterSeriesOptions();
            }

            nextOptions.Series.HighlightDraftNo = string.IsNullOrWhiteSpace(draftNo) ? string.Empty : draftNo.Trim();
            nextOptions.Series.SelectedDraftNo = string.Empty;
            RefreshSeries(nextOptions);
        }

        public void ClearDraftHighlight()
        {
            HighlightDraft(string.Empty);
        }

        public void ClearSelectedDraftHighlight()
        {
            HighlightSelectedDraft(string.Empty);
        }

        public void Clear()
        {
            analysisResult = null;
            rawData = null;
            scatterChart.Clear();
        }

        public string SaveImage()
        {
            return scatterChart.SaveImage();
        }

        public string SaveImage(LightningScatterImageOptions imageOptions)
        {
            return scatterChart.SaveImage(imageOptions);
        }

        public Image LoadLastSavedImage()
        {
            return scatterChart.LoadLastSavedImage();
        }

        public Image GetLastSavedImage()
        {
            return scatterChart.GetLastSavedImage();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ScatterChart_PointClicked(object sender, LightningScatterPointClickEventArgs e)
        {
            ScatterSampleData sample = e == null || e.Point == null ? null : e.Point.Tag as ScatterSampleData;
            if (sample == null)
            {
                return;
            }

            IList<KnnNeighbor> neighbors = FindNearest(sample.DraftNo, options.Analysis.NeighborCount);
            OnSampleClicked(new TSNESampleClickedEventArgs(sample, neighbors, e));
        }

        private void RefreshSeries(TSNEScatterOptions nextOptions)
        {
            options = nextOptions == null ? TSNEScatterOptions.CreateDefault() : nextOptions.Clone();
            IEnumerable<LightningScatterSeries> series = seriesBuilder.Build(analysisResult, options.Series);
            scatterChart.UpdateData(series, options.ToScatterOptions(analysisResult), true);
        }

        private void OnSampleClicked(TSNESampleClickedEventArgs e)
        {
            EventHandler<TSNESampleClickedEventArgs> handler = SampleClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnAnalysisCompleted(TSNEAnalysisCompletedEventArgs e)
        {
            EventHandler<TSNEAnalysisCompletedEventArgs> handler = AnalysisCompleted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnAnalysisFailed(TSNEAnalysisFailedEventArgs e)
        {
            EventHandler<TSNEAnalysisFailedEventArgs> handler = AnalysisFailed;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing && scatterChart != null)
            {
                scatterChart.PointClicked -= ScatterChart_PointClicked;
                if (parent != null && parent.Controls.Contains(scatterChart))
                {
                    parent.Controls.Remove(scatterChart);
                }

                scatterChart.Dispose();
                scatterChart = null;
            }

            disposed = true;
        }
    }
}









namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    /// <summary>
    /// Converts caller-supplied CONV_EXPER_CTN service results into TSNE source rows.
    /// DB access is intentionally outside this class. The UI or service layer should
    /// call the company data service and pass the completed DataTable here.
    /// </summary>
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
                ScatterSampleData sample = analysis.ScatterData[index];
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
    internal static class TSNEJsonUtility
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
    #region TSNE Scatter Output Contract

    /// <summary>
    /// TSNE 결과 한 건을 LightningChart와 KNN 결과 그리드에 전달하는 화면 데이터 계약이다.
    /// X1/X2는 임의 좌표가 아니라 TSNEAnalysisPipeline에서 계산한 PC1/PC2 점수다.
    /// </summary>
    public sealed class ScatterSampleData
    {
        public int SourceIndex { get; set; }
        public string DraftNo { get; set; }
        public double X1 { get; set; }
        public double X2 { get; set; }
        public string AiResultValue { get; set; }
        public double? Distance { get; set; }
        public string ParameterType { get; set; }
        public string TooltipText { get; set; }
        public object UserData { get; set; }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(TooltipText)
                ? DraftNo ?? string.Empty
                : TooltipText;
        }
    }

    #endregion
}






namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public enum TSNEScatterDataSourceKind
    {
        JsonSamples,
        ActDataJsonDocuments,
        ConvExperimentJsonDocuments,
        AnalysisResult
    }

    public sealed class TSNEScatterDataSource
    {
        private readonly IList<string> documents;
        private readonly TSNEAnalysisResult analysisResult;

        private TSNEScatterDataSource(TSNEScatterDataSourceKind kind, IEnumerable<string> documents, TSNEAnalysisResult analysisResult)
        {
            Kind = kind;
            this.documents = documents == null
                ? new List<string>()
                : documents.ToList();
            this.analysisResult = analysisResult;
        }

        public TSNEScatterDataSourceKind Kind { get; private set; }

        public static TSNEScatterDataSource FromJsonSamples(IEnumerable<string> jsonSamples)
        {
            return new TSNEScatterDataSource(TSNEScatterDataSourceKind.JsonSamples, jsonSamples, null);
        }

        public static TSNEScatterDataSource FromActDataJson(IEnumerable<string> actDataDocuments)
        {
            return new TSNEScatterDataSource(TSNEScatterDataSourceKind.ActDataJsonDocuments, actDataDocuments, null);
        }

        public static TSNEScatterDataSource FromActDataJson(string actDataDocument)
        {
            return FromActDataJson(new[] { actDataDocument });
        }

        public static TSNEScatterDataSource FromConvExperimentJson(IEnumerable<string> convExperimentDocuments)
        {
            return new TSNEScatterDataSource(
                TSNEScatterDataSourceKind.ConvExperimentJsonDocuments,
                convExperimentDocuments,
                null);
        }

        public static TSNEScatterDataSource FromAnalysisResult(TSNEAnalysisResult analysisResult)
        {
            if (analysisResult == null)
            {
                throw new ArgumentNullException("analysisResult");
            }

            return new TSNEScatterDataSource(TSNEScatterDataSourceKind.AnalysisResult, null, analysisResult);
        }

        public TSNEAnalysisResult Analyze(TSNEScatterAnalysisOptions analysisOptions)
        {
            if (Kind == TSNEScatterDataSourceKind.AnalysisResult)
            {
                return analysisResult;
            }

            TSNEAnalysisOptions pipelineOptions = (analysisOptions ?? new TSNEScatterAnalysisOptions()).ToPipelineOptions();
            // This assembly is intentionally t-SNE-only; callers cannot switch the projection back to TSNE.
            pipelineOptions.ProjectionMethod = DimensionalityReductionMethod.TSNE;
            TSNEAnalysisPipeline pipeline = new TSNEAnalysisPipeline(pipelineOptions);
            if (Kind == TSNEScatterDataSourceKind.ActDataJsonDocuments)
            {
                return pipeline.AnalyzeActDataDocuments(documents);
            }

            if (Kind == TSNEScatterDataSourceKind.ConvExperimentJsonDocuments)
            {
                return pipeline.AnalyzeConvExperimentDocuments(documents);
            }

            return pipeline.Analyze(documents);
        }
    }

    public sealed class TSNEScatterExadataOptions
    {
        public TSNEScatterExadataOptions()
        {
            JsonColumnName = "CONV_EXPER_CTN";
            DraftNoColumnName = "DRAFT_NO";
            ParameterTypeColumnName = "PARAM_TYP";
            AiResultColumnName = "AI_RSLT_VAL";
            LabelColumnName = "ENGR_RSLT_VAL";
            ParameterType = TSNEParameterType.Response;
        }

        public DataTable SourceTable { get; set; }
        public string JsonColumnName { get; set; }
        public string DraftNoColumnName { get; set; }
        public string ParameterTypeColumnName { get; set; }
        public string AiResultColumnName { get; set; }
        public string LabelColumnName { get; set; }
        public TSNEParameterType ParameterType { get; set; }

        public static TSNEScatterExadataOptions CreateDefault()
        {
            ConvExperimentQueryOptions configured = ConvExperimentQueryOptions.FromConfiguration();
            return new TSNEScatterExadataOptions
            {
                JsonColumnName = configured.JsonColumnName,
                DraftNoColumnName = configured.DraftNoColumnName,
                ParameterTypeColumnName = configured.ParameterTypeColumnName,
                AiResultColumnName = configured.AiResultColumnName,
                LabelColumnName = configured.LabelColumnName,
                ParameterType = TSNEParameterType.Response
            };
        }

        public static TSNEScatterExadataOptions FromDataTable(DataTable sourceTable)
        {
            return new TSNEScatterExadataOptions
            {
                SourceTable = sourceTable
            };
        }

        public ConvExperimentQueryOptions ToQueryOptions()
        {
            return new ConvExperimentQueryOptions
            {
                JsonColumnName = string.IsNullOrWhiteSpace(JsonColumnName)
                    ? "CONV_EXPER_CTN"
                    : JsonColumnName.Trim(),
                DraftNoColumnName = string.IsNullOrWhiteSpace(DraftNoColumnName)
                    ? "DRAFT_NO"
                    : DraftNoColumnName.Trim(),
                ParameterTypeColumnName = string.IsNullOrWhiteSpace(ParameterTypeColumnName)
                    ? "PARAM_TYP"
                    : ParameterTypeColumnName.Trim(),
                AiResultColumnName = string.IsNullOrWhiteSpace(AiResultColumnName)
                    ? "AI_RSLT_VAL"
                    : AiResultColumnName.Trim(),
                LabelColumnName = string.IsNullOrWhiteSpace(LabelColumnName)
                    ? "ENGR_RSLT_VAL"
                    : LabelColumnName.Trim()
            };
        }
    }

    public sealed class TSNEScatterDatabaseOptions
    {
        public TSNEScatterDatabaseOptions()
        {
            ActDataColumnName = "ACT_DATA";
        }

        public DataTable SourceTable { get; set; }
        public string ActDataColumnName { get; set; }

        public static TSNEScatterDatabaseOptions CreateDefault()
        {
            return new TSNEScatterDatabaseOptions();
        }

        public static TSNEScatterDatabaseOptions FromDataTable(DataTable sourceTable)
        {
            return new TSNEScatterDatabaseOptions
            {
                SourceTable = sourceTable
            };
        }

        internal ActDataQueryOptions ToActDataQueryOptions()
        {
            return new ActDataQueryOptions
            {
                ActDataColumnName = string.IsNullOrWhiteSpace(ActDataColumnName)
                    ? "ACT_DATA"
                    : ActDataColumnName.Trim()
            };
        }
    }
}






namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public sealed class TSNEScatterAnalysisOptions
    {
        public TSNEScatterAnalysisOptions()
        {
            ConstantVarianceThreshold = 1e-10d;
            MinimumNumericFeatureCoverageRatio = 0.90d;
            MeanImputationEnabled = true;
            ComponentCount = 2;
            MaxIterations = 2000;
            ConvergenceTolerance = 1e-10d;
            NeighborCount = 3;
            KnnSearchAlgorithm = KnnSearchAlgorithm.Auto;
            ProjectionMethod = DimensionalityReductionMethod.TSNE;
            TSNEPerplexity = 30d;
            TSNEIterations = 750;
            TSNELearningRate = 200d;
            TSNERandomSeed = 20260831;
        }

        public double ConstantVarianceThreshold { get; set; }
        public double MinimumNumericFeatureCoverageRatio { get; set; }
        public bool MeanImputationEnabled { get; set; }
        public int ComponentCount { get; set; }
        public int MaxIterations { get; set; }
        public double ConvergenceTolerance { get; set; }
        public int NeighborCount { get; set; }
        public KnnSearchAlgorithm KnnSearchAlgorithm { get; set; }
        public DimensionalityReductionMethod ProjectionMethod { get; set; }
        public double TSNEPerplexity { get; set; }
        public int TSNEIterations { get; set; }
        public double TSNELearningRate { get; set; }
        public int TSNERandomSeed { get; set; }

        internal TSNEAnalysisOptions ToPipelineOptions()
        {
            return new TSNEAnalysisOptions
            {
                ConstantVarianceThreshold = ConstantVarianceThreshold,
                MinimumNumericFeatureCoverageRatio = MinimumNumericFeatureCoverageRatio,
                MeanImputationEnabled = MeanImputationEnabled,
                ComponentCount = ComponentCount,
                MaxIterations = MaxIterations,
                ConvergenceTolerance = ConvergenceTolerance,
                NeighborCount = NeighborCount,
                KnnSearchAlgorithm = KnnSearchAlgorithm,
                ProjectionMethod = ProjectionMethod,
                TSNEPerplexity = TSNEPerplexity,
                TSNEIterations = TSNEIterations,
                TSNELearningRate = TSNELearningRate,
                TSNERandomSeed = TSNERandomSeed
            };
        }

        public TSNEScatterAnalysisOptions Clone()
        {
            return (TSNEScatterAnalysisOptions)MemberwiseClone();
        }
    }

    public sealed class TSNEScatterSeriesOptions
    {
        public TSNEScatterSeriesOptions()
        {
            PointSize = 7f;
            PointShape = LightningScatterPointShape.RoundedRectangle;
            ShowLine = false;
            ShowPoints = true;
            UsePaletteColors = true;
            RequireSeriesLabel = true;
            ApplyColorAlpha = true;
            ColorTransparencyPercent = 20f;
            ColorAlpha = ResolveAlphaFromTransparencyPercent(ColorTransparencyPercent, 190);
            ApplyBorderTransparency = false;
            BorderTransparencyPercent = 0f;
            NaSeriesName = string.Empty;
            NaSeriesColor = Color.Empty;
            PassResultName = "Pass";
            ReviewResultName = "Review";
            PassColor = Color.Red;
            ReviewColor = Color.Green;
            DefaultColor = Color.Red;
            HighlightColor = Color.Yellow;
            HighlightPointBorderColor = Color.Yellow;
            HighlightPointBorderWidth = 1f;
            HighlightPointSize = 0f;
            SelectedDraftNo = string.Empty;
            SelectedPointColor = Color.Empty;
            SelectedPointBorderColor = Color.Lime;
            SelectedPointBorderWidth = 2.2f;
            SelectedPointSize = 0f;
            SeriesOrder = new[] { PassResultName, ReviewResultName, "FAIL" };
            SeriesColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            PastelPalette = CreateCompanySeriesPalette();
            BorderPalette = CreateCompanySeriesBorderPalette();
        }

        public float PointSize { get; set; }
        public LightningScatterPointShape PointShape { get; set; }
        public bool ShowLine { get; set; }
        public bool ShowPoints { get; set; }
        public bool UsePaletteColors { get; set; }
        public bool RequireSeriesLabel { get; set; }
        public bool ApplyColorAlpha { get; set; }
        public int ColorAlpha { get; set; }
        public float ColorTransparencyPercent { get; set; }
        public bool ApplyBorderTransparency { get; set; }
        public float BorderTransparencyPercent { get; set; }
        public string NaSeriesName { get; set; }
        public Color NaSeriesColor { get; set; }
        public string PassResultName { get; set; }
        public string ReviewResultName { get; set; }
        public Color PassColor { get; set; }
        public Color ReviewColor { get; set; }
        public Color DefaultColor { get; set; }
        public string HighlightDraftNo { get; set; }
        public Color HighlightColor { get; set; }
        public Color HighlightPointBorderColor { get; set; }
        public float HighlightPointBorderWidth { get; set; }
        public float HighlightPointSize { get; set; }
        public string SelectedDraftNo { get; set; }
        public Color SelectedPointColor { get; set; }
        public Color SelectedPointBorderColor { get; set; }
        public float SelectedPointBorderWidth { get; set; }
        public float SelectedPointSize { get; set; }
        public string[] SeriesOrder { get; set; }
        public IDictionary<string, Color> SeriesColors { get; set; }
        public Color[] PastelPalette { get; set; }
        public Color[] BorderPalette { get; set; }
        public Func<ScatterSampleData, string> SeriesNameSelector { get; set; }
        public Func<string, string> LegendLabelFormatter { get; set; }

        public TSNEScatterSeriesOptions Clone()
        {
            return new TSNEScatterSeriesOptions
            {
                PointSize = PointSize,
                PointShape = PointShape,
                ShowLine = ShowLine,
                ShowPoints = ShowPoints,
                UsePaletteColors = UsePaletteColors,
                RequireSeriesLabel = RequireSeriesLabel,
                ApplyColorAlpha = ApplyColorAlpha,
                ColorAlpha = ColorAlpha,
                ColorTransparencyPercent = ColorTransparencyPercent,
                ApplyBorderTransparency = ApplyBorderTransparency,
                BorderTransparencyPercent = BorderTransparencyPercent,
                NaSeriesName = NaSeriesName,
                NaSeriesColor = NaSeriesColor,
                PassResultName = PassResultName,
                ReviewResultName = ReviewResultName,
                PassColor = PassColor,
                ReviewColor = ReviewColor,
                DefaultColor = DefaultColor,
                HighlightDraftNo = HighlightDraftNo,
                HighlightColor = HighlightColor,
                HighlightPointBorderColor = HighlightPointBorderColor,
                HighlightPointBorderWidth = HighlightPointBorderWidth,
                HighlightPointSize = HighlightPointSize,
                SelectedDraftNo = SelectedDraftNo,
                SelectedPointColor = SelectedPointColor,
                SelectedPointBorderColor = SelectedPointBorderColor,
                SelectedPointBorderWidth = SelectedPointBorderWidth,
                SelectedPointSize = SelectedPointSize,
                SeriesOrder = SeriesOrder == null ? new string[0] : SeriesOrder.ToArray(),
                SeriesColors = SeriesColors == null
                    ? new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, Color>(SeriesColors, StringComparer.OrdinalIgnoreCase),
                PastelPalette = PastelPalette == null ? CreateCompanySeriesPalette() : (Color[])PastelPalette.Clone(),
                BorderPalette = BorderPalette == null ? CreateCompanySeriesBorderPalette() : (Color[])BorderPalette.Clone(),
                SeriesNameSelector = SeriesNameSelector,
                LegendLabelFormatter = LegendLabelFormatter
            };
        }

        public static Color[] CreateCompanySeriesPalette()
        {
            return new[]
            {
                Color.DarkBlue,
                Color.Red,
                Color.Green,
                Color.Black,
                Color.Yellow,
                Color.Navy,
                Color.Orange,
                Color.OliveDrab,
                Color.Purple,
                Color.Lime,
                Color.Pink,
                Color.MistyRose,
                Color.LightCyan
            };
        }

        public static Color[] CreateCompanySeriesBorderPalette()
        {
            return new[]
            {
                Color.FromArgb(255, 0, 0, 96),
                Color.FromArgb(255, 160, 0, 0),
                Color.FromArgb(255, 0, 110, 0),
                Color.FromArgb(255, 0, 0, 0),
                Color.FromArgb(255, 180, 150, 0),
                Color.FromArgb(255, 0, 0, 100),
                Color.FromArgb(255, 190, 90, 0),
                Color.FromArgb(255, 75, 110, 25),
                Color.FromArgb(255, 90, 0, 120),
                Color.FromArgb(255, 0, 150, 0),
                Color.FromArgb(255, 190, 80, 120),
                Color.FromArgb(255, 190, 140, 140),
                Color.FromArgb(255, 120, 190, 200)
            };
        }

        public static int ResolveAlphaFromTransparencyPercent(float transparencyPercent, int fallbackAlpha)
        {
            if (float.IsNaN(transparencyPercent) || float.IsInfinity(transparencyPercent))
            {
                return Math.Max(0, Math.Min(255, fallbackAlpha));
            }

            float transparency = Math.Max(0f, Math.Min(100f, transparencyPercent));
            int alpha = (int)Math.Round(255f * ((100f - transparency) / 100f));
            return Math.Max(0, Math.Min(255, alpha));
        }
    }

    public sealed class TSNEScatterDisplayOptions
    {
        public TSNEScatterDisplayOptions()
        {
            FontName = "Segoe UI";
            ShowTitle = true;
            Title = "Distribution Chart";
            TitleColor = Color.Black;
            BackgroundColor = Color.White;
            GraphBackgroundColor = Color.FromArgb(230, 230, 230);
            ThemeMode = LightningScatterThemeMode.LightGray;
            XAxisTitle = string.Empty;
            YAxisTitle = string.Empty;
            AutoCalculateAxisRange = true;
            IncludeZeroInAxisRange = true;
            AxisPaddingRatio = 0.08d;
            MinimumAxisPadding = 0.2d;
            MajorDivCount = 8;
            AxisLabelFormat = "0.##";
            GridLinesVisible = false;
            MinorGridLinesVisible = false;
            GridColor = Color.FromArgb(232, 234, 238);
        }

        public string FontName { get; set; }
        public bool ShowTitle { get; set; }
        public string Title { get; set; }
        public Color TitleColor { get; set; }
        public Color BackgroundColor { get; set; }
        public Color GraphBackgroundColor { get; set; }
        public LightningScatterThemeMode ThemeMode { get; set; }
        public string XAxisTitle { get; set; }
        public string YAxisTitle { get; set; }
        public bool AutoCalculateAxisRange { get; set; }
        public bool IncludeZeroInAxisRange { get; set; }
        public double AxisPaddingRatio { get; set; }
        public double MinimumAxisPadding { get; set; }
        public int MajorDivCount { get; set; }
        public string AxisLabelFormat { get; set; }
        public bool GridLinesVisible { get; set; }
        public bool MinorGridLinesVisible { get; set; }
        public Color GridColor { get; set; }

        public TSNEScatterDisplayOptions Clone()
        {
            return (TSNEScatterDisplayOptions)MemberwiseClone();
        }
    }

    public sealed class TSNEScatterOptions
    {
        public TSNEScatterOptions()
        {
            Analysis = new TSNEScatterAnalysisOptions();
            Series = new TSNEScatterSeriesOptions();
            Display = new TSNEScatterDisplayOptions();
            Legend = new LightningScatterLegendOptions
            {
                Position = LightningScatterLegendPosition.BottomCenter,
                OffsetY = 0,
                ShowCheckboxes = false,
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent,
                TransparentBackground = true
            };
            Tooltip = new LightningScatterTooltipOptions
            {
                Enabled = true,
                HitPixelTolerance = 14,
                Format = "{5}\r\nX1:{1:0.###}, X2:{2:0.###}\r\nAI_RSLT_Val:{0}"
            };
            NoData = new LightningScatterNoDataOptions
            {
                Text = "t-SNE Scatter No data available.",
                ShowWhenDataMissing = true,
                ShowWhenAllValuesZero = false,
                FontSize = 11f,
                TextAlignment = LightningScatterTextAlignment.Center,
                BadgeSingleLine = true,
                BadgeHorizontalPadding = 10f,
                BadgeVerticalPadding = 4f
            };
            Image = new LightningScatterImageOptions
            {
                Width = 600,
                Height = 400,
                SubDirectoryName = "TSNEScatterImages"
            };
            Interaction = new LightningScatterInteractionOptions
            {
                ZoomEnabled = true,
                PanEnabled = true,
                MouseWheelZoomEnabled = true,
                AllowInternalMouseCursorChange = true,
                OpenPropertyEditorOnRightClick = true
            };
        }

        public TSNEScatterAnalysisOptions Analysis { get; set; }
        public TSNEScatterSeriesOptions Series { get; set; }
        public TSNEScatterDisplayOptions Display { get; set; }
        public LightningScatterLegendOptions Legend { get; set; }
        public LightningScatterTooltipOptions Tooltip { get; set; }
        public LightningScatterNoDataOptions NoData { get; set; }
        public LightningScatterImageOptions Image { get; set; }
        public LightningScatterInteractionOptions Interaction { get; set; }
        public Action<LightningScatterOptions> CustomizeScatterOptions { get; set; }

        public static TSNEScatterOptions CreateDefault()
        {
            return new TSNEScatterOptions();
        }

        public static TSNEScatterOptions CreateDefault600x400()
        {
            return new TSNEScatterOptions
            {
                Image = new LightningScatterImageOptions
                {
                    Width = 600,
                    Height = 400,
                    SubDirectoryName = "TSNEScatterImages"
                }
            };
        }

        public static TSNEScatterOptions CreateExcelImageOptimized()
        {
            TSNEScatterOptions options = CreateDefault600x400();
            options.Image.Width = 900;
            options.Image.Height = 600;
            options.Image.SubDirectoryName = "TSNEScatterExcelImages";
            options.Display.AxisPaddingRatio = 0.05d;
            options.Display.MinimumAxisPadding = 0.1d;
            options.Legend.FontSize = 8f;
            options.Series.PointSize = 17f;
            return options;
        }

        public TSNEScatterOptions Clone()
        {
            return new TSNEScatterOptions
            {
                Analysis = Analysis == null ? new TSNEScatterAnalysisOptions() : Analysis.Clone(),
                Series = Series == null ? new TSNEScatterSeriesOptions() : Series.Clone(),
                Display = Display == null ? new TSNEScatterDisplayOptions() : Display.Clone(),
                Legend = Legend == null ? new LightningScatterLegendOptions() : Legend.Clone(),
                Tooltip = Tooltip == null ? new LightningScatterTooltipOptions() : Tooltip.Clone(),
                NoData = NoData == null ? new LightningScatterNoDataOptions() : NoData.Clone(),
                Image = Image == null ? new LightningScatterImageOptions() : Image.Clone(),
                Interaction = Interaction == null ? new LightningScatterInteractionOptions() : Interaction.Clone(),
                CustomizeScatterOptions = CustomizeScatterOptions
            };
        }

        internal LightningScatterOptions ToScatterOptions(TSNEAnalysisResult analysisResult)
        {
            TSNEScatterOptions snapshot = Clone();
            LightningScatterOptions scatterOptions = LightningScatterOptions.CreateDefaultBubble();
            TSNEScatterDisplayOptions display = snapshot.Display ?? new TSNEScatterDisplayOptions();

            scatterOptions.FontName = string.IsNullOrWhiteSpace(display.FontName) ? "Segoe UI" : display.FontName.Trim();
            scatterOptions.ShowTitle = display.ShowTitle;
            scatterOptions.Title = display.Title ?? string.Empty;
            scatterOptions.TitleColor = display.TitleColor.IsEmpty ? Color.Black : display.TitleColor;
            scatterOptions.BackgroundColor = display.BackgroundColor;
            scatterOptions.GraphBackgroundColor = display.GraphBackgroundColor;
            scatterOptions.ThemeMode = display.ThemeMode;
            scatterOptions.Legend = snapshot.Legend ?? new LightningScatterLegendOptions();
            scatterOptions.Tooltip = snapshot.Tooltip ?? new LightningScatterTooltipOptions();
            scatterOptions.NoData = snapshot.NoData ?? new LightningScatterNoDataOptions();
            scatterOptions.Image = snapshot.Image ?? new LightningScatterImageOptions();
            scatterOptions.Interaction = snapshot.Interaction ?? new LightningScatterInteractionOptions();
            scatterOptions.Style.UsePastelPalette = false;
            scatterOptions.Style.ForceBubbleStyle = true;
            TSNEScatterSeriesOptions series = snapshot.Series ?? new TSNEScatterSeriesOptions();
            scatterOptions.Style.BubbleSize = Math.Max(1f, series.PointSize);
            scatterOptions.Style.PointShape = series.PointShape;
            scatterOptions.Style.ApplyColorAlpha = series.ApplyColorAlpha;
            scatterOptions.Style.ColorTransparencyPercent = series.ColorTransparencyPercent;
            scatterOptions.Style.ColorAlpha = TSNEScatterSeriesOptions.ResolveAlphaFromTransparencyPercent(series.ColorTransparencyPercent, series.ColorAlpha);
            scatterOptions.Style.ApplyColorTransparencyBlend = true;
            scatterOptions.Style.ColorBlendBackground = display.GraphBackgroundColor.IsEmpty ? display.BackgroundColor : display.GraphBackgroundColor;
            scatterOptions.Style.ApplyBorderTransparency = series.ApplyBorderTransparency;
            scatterOptions.Style.BorderTransparencyPercent = series.BorderTransparencyPercent;
            scatterOptions.Style.BubbleBorderWidth = 1f;
            scatterOptions.Style.PointBodyThickness = 1f;

            ApplyAxisOptions(scatterOptions, analysisResult, display, series);

            if (snapshot.CustomizeScatterOptions != null)
            {
                snapshot.CustomizeScatterOptions(scatterOptions);
            }

            return scatterOptions;
        }

        private static void ApplyAxisOptions(LightningScatterOptions scatterOptions, TSNEAnalysisResult analysisResult, TSNEScatterDisplayOptions display, TSNEScatterSeriesOptions series)
        {
            IList<ScatterSampleData> axisSamples = ResolveAxisSamples(analysisResult, series);
            AxisRange xRange = CalculateRange(
                axisSamples.Select(item => item.X1),
                display);
            AxisRange yRange = CalculateRange(
                axisSamples.Select(item => item.X2),
                display);

            scatterOptions.XAxis.Title = display.XAxisTitle ?? string.Empty;
            scatterOptions.XAxis.AutoFit = false;
            scatterOptions.XAxis.Minimum = xRange.Minimum;
            scatterOptions.XAxis.Maximum = xRange.Maximum;
            scatterOptions.XAxis.MajorDivCount = Math.Max(1, display.MajorDivCount);
            scatterOptions.XAxis.LabelFormat = string.IsNullOrWhiteSpace(display.AxisLabelFormat) ? "0.##" : display.AxisLabelFormat;
            scatterOptions.XAxis.GridLinesVisible = display.GridLinesVisible;
            scatterOptions.XAxis.MinorGridLinesVisible = display.MinorGridLinesVisible;
            scatterOptions.XAxis.GridColor = display.GridColor;

            scatterOptions.YAxis.Title = display.YAxisTitle ?? string.Empty;
            scatterOptions.YAxis.AutoFit = false;
            scatterOptions.YAxis.Minimum = yRange.Minimum;
            scatterOptions.YAxis.Maximum = yRange.Maximum;
            scatterOptions.YAxis.MajorDivCount = Math.Max(1, display.MajorDivCount);
            scatterOptions.YAxis.LabelFormat = string.IsNullOrWhiteSpace(display.AxisLabelFormat) ? "0.##" : display.AxisLabelFormat;
            scatterOptions.YAxis.GridLinesVisible = display.GridLinesVisible;
            scatterOptions.YAxis.MinorGridLinesVisible = display.MinorGridLinesVisible;
            scatterOptions.YAxis.GridColor = display.GridColor;
        }

        private static IList<ScatterSampleData> ResolveAxisSamples(TSNEAnalysisResult analysisResult, TSNEScatterSeriesOptions series)
        {
            IList<ScatterSampleData> samples = analysisResult == null || analysisResult.ScatterData == null
                ? new List<ScatterSampleData>()
                : analysisResult.ScatterData.Where(item => item != null).ToList();
            if (series == null || !series.RequireSeriesLabel)
            {
                return samples;
            }

            string highlightedDraftNo = (series.HighlightDraftNo ?? string.Empty).Trim();
            string selectedDraftNo = (series.SelectedDraftNo ?? string.Empty).Trim();
            return samples.Where(sample =>
                HasSeriesLabel(sample, series)
                || IsSameDraftNo(sample.DraftNo, highlightedDraftNo)
                || IsSameDraftNo(sample.DraftNo, selectedDraftNo))
                .ToList();
        }

        private static bool HasSeriesLabel(ScatterSampleData sample, TSNEScatterSeriesOptions series)
        {
            if (sample == null)
            {
                return false;
            }

            string label = series != null && series.SeriesNameSelector != null
                ? series.SeriesNameSelector(sample)
                : sample.AiResultValue;
            return !string.IsNullOrWhiteSpace(label);
        }

        private static bool IsSameDraftNo(string left, string right)
        {
            return !string.IsNullOrWhiteSpace(left)
                && !string.IsNullOrWhiteSpace(right)
                && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static AxisRange CalculateRange(IEnumerable<double> values, TSNEScatterDisplayOptions display)
        {
            List<double> cleanValues = values == null
                ? new List<double>()
                : values.Where(value => !double.IsNaN(value) && !double.IsInfinity(value)).ToList();

            if (!display.AutoCalculateAxisRange || cleanValues.Count == 0)
            {
                return new AxisRange(-1d, 1d);
            }

            double minimum = cleanValues.Min();
            double maximum = cleanValues.Max();
            if (display.IncludeZeroInAxisRange)
            {
                minimum = Math.Min(0d, minimum);
                maximum = Math.Max(0d, maximum);
            }

            if (Math.Abs(maximum - minimum) < 0.000001d)
            {
                minimum -= 1d;
                maximum += 1d;
            }

            double padding = Math.Max(Math.Max(0d, display.MinimumAxisPadding), (maximum - minimum) * Math.Max(0d, display.AxisPaddingRatio));
            return new AxisRange(minimum - padding, maximum + padding);
        }

        private sealed class AxisRange
        {
            public AxisRange(double minimum, double maximum)
            {
                Minimum = minimum;
                Maximum = maximum;
            }

            public double Minimum { get; private set; }
            public double Maximum { get; private set; }
        }
    }
}







namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public interface ITSNEScatterPopupDataProvider
    {
        string SourceDescription { get; }
        Task<DataTable> LoadAllAsync();
    }
}






namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public sealed class TSNEScatterSeriesBuilder
    {
        public IEnumerable<LightningScatterSeries> Build(TSNEAnalysisResult analysisResult, TSNEScatterSeriesOptions seriesOptions)
        {
            TSNEScatterSeriesOptions options = seriesOptions == null
                ? new TSNEScatterSeriesOptions()
                : seriesOptions.Clone();
            IList<ScatterSampleData> samples = analysisResult == null || analysisResult.ScatterData == null
                ? new List<ScatterSampleData>()
                : analysisResult.ScatterData.Where(item => item != null).ToList();
            ScatterSampleData highlightedSample = ResolveHighlightedSample(samples, options);
            ScatterSampleData selectedSample = ResolveSelectedSample(samples, options);
            IList<ScatterSampleData> regularSamples = samples
                .Where(item => ShouldIncludeInRegularSeries(item, options))
                .ToList();
            if (highlightedSample != null)
            {
                regularSamples = regularSamples
                    .Where(item => !object.ReferenceEquals(item, highlightedSample))
                    .ToList();
            }

            if (selectedSample != null)
            {
                regularSamples = regularSamples
                    .Where(item => !object.ReferenceEquals(item, selectedSample))
                    .ToList();
            }

            Dictionary<string, List<ScatterSampleData>> allGroups = regularSamples
                .GroupBy(item => ResolveSeriesName(item, options), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<ScatterSampleData>> groups = regularSamples
                .GroupBy(item => ResolveSeriesName(item, options), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            List<string> orderedNames = ResolveSeriesOrder(allGroups.Keys, options);
            Dictionary<string, Color> seriesColors = ResolveSeriesColors(orderedNames, options);
            Dictionary<string, Color> seriesBorderColors = ResolveSeriesBorderColors(orderedNames, options);
            var result = new List<LightningScatterSeries>();
            for (int index = 0; index < orderedNames.Count; index++)
            {
                string seriesName = orderedNames[index];
                if (!groups.ContainsKey(seriesName) || groups[seriesName].Count == 0)
                {
                    continue;
                }

                Color seriesColor = seriesColors[seriesName];
                Color seriesBorderColor = seriesBorderColors.ContainsKey(seriesName)
                    ? seriesBorderColors[seriesName]
                    : seriesColor;
                result.Add(new LightningScatterSeries
                {
                    Name = seriesName,
                    LegendLabel = ResolveLegendLabel(seriesName, options),
                    LineColor = seriesColor,
                    PointColor = seriesColor,
                    PointBorderColor = seriesBorderColor,
                    PointSize = Math.Max(1f, options.PointSize),
                    PointShape = options.PointShape,
                    ShowLine = options.ShowLine,
                    ShowPoints = options.ShowPoints,
                    Points = groups[seriesName]
                        .Select(item => new LightningScatterPoint(item.X1, item.X2, item))
                        .ToList()
                });
            }

            if (highlightedSample != null)
            {
                result.Add(CreateSinglePointSeries(highlightedSample, highlightedSample.DraftNo.Trim(), options.HighlightColor, options.HighlightPointBorderColor, options.PointShape, ResolveHighlightedPointSize(options), Math.Max(0f, options.HighlightPointBorderWidth), true));
            }

            if (selectedSample != null && !object.ReferenceEquals(selectedSample, highlightedSample))
            {
                string selectedSeriesName = ResolveSeriesName(selectedSample, options);
                Color selectedPointColor = ResolveSelectedPointColor(selectedSeriesName, seriesColors, options);
                result.Add(CreateSinglePointSeries(selectedSample, selectedSample.DraftNo.Trim(), selectedPointColor, options.SelectedPointBorderColor, options.PointShape, ResolveSelectedPointSize(options), Math.Max(0f, options.SelectedPointBorderWidth), false));
            }

            return result;
        }

        private static ScatterSampleData ResolveHighlightedSample(IEnumerable<ScatterSampleData> samples, TSNEScatterSeriesOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.HighlightDraftNo))
            {
                return null;
            }

            string draftNo = options.HighlightDraftNo.Trim();
            return (samples ?? Enumerable.Empty<ScatterSampleData>()).FirstOrDefault(item =>
                item != null
                && string.Equals(item.DraftNo, draftNo, StringComparison.OrdinalIgnoreCase));
        }

        private static ScatterSampleData ResolveSelectedSample(IEnumerable<ScatterSampleData> samples, TSNEScatterSeriesOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.SelectedDraftNo))
            {
                return null;
            }

            string draftNo = options.SelectedDraftNo.Trim();
            return (samples ?? Enumerable.Empty<ScatterSampleData>()).FirstOrDefault(item =>
                item != null
                && string.Equals(item.DraftNo, draftNo, StringComparison.OrdinalIgnoreCase));
        }

        private static LightningScatterSeries CreateSinglePointSeries(
            ScatterSampleData sample, string seriesName, Color fillColor, Color borderColor,
            LightningScatterPointShape pointShape, float pointSize, float borderWidth, bool showInLegend)
        {
            return new LightningScatterSeries
            {
                Name = seriesName,
                LegendLabel = seriesName,
                LineColor = borderColor,
                PointColor = fillColor,
                PointBorderColor = borderColor,
                PointBorderWidth = borderWidth,
                PointSize = pointSize,
                PointShape = pointShape,
                ShowLine = false,
                ShowPoints = true,
                ShowInLegend = showInLegend,
                Points = new List<LightningScatterPoint>
                {
                    new LightningScatterPoint(sample.X1, sample.X2, sample)
                }
            };
        }

        private static string ResolveSeriesName(ScatterSampleData sample, TSNEScatterSeriesOptions options)
        {
            string seriesName = ResolveRawSeriesName(sample, options);
            return string.IsNullOrWhiteSpace(seriesName) ? "Unknown" : seriesName.Trim();
        }

        private static string ResolveRawSeriesName(ScatterSampleData sample, TSNEScatterSeriesOptions options)
        {
            if (sample == null)
            {
                return string.Empty;
            }

            return options != null && options.SeriesNameSelector != null
                ? options.SeriesNameSelector(sample)
                : sample.AiResultValue;
        }

        private static bool ShouldIncludeInRegularSeries(ScatterSampleData sample, TSNEScatterSeriesOptions options)
        {
            if (sample == null)
            {
                return false;
            }

            if (options == null || !options.RequireSeriesLabel)
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(ResolveRawSeriesName(sample, options));
        }

        private static string ResolveLegendLabel(string seriesName, TSNEScatterSeriesOptions options)
        {
            if (options.LegendLabelFormatter == null)
            {
                return seriesName ?? string.Empty;
            }

            string formatted = options.LegendLabelFormatter(seriesName);
            return string.IsNullOrWhiteSpace(formatted) ? seriesName ?? string.Empty : formatted.Trim();
        }

        private static Color ResolveSeriesColor(string seriesName, int seriesIndex, TSNEScatterSeriesOptions options)
        {
            if (IsNaSeriesName(seriesName, options))
            {
                return ApplyColorAlpha(options.NaSeriesColor, options);
            }

            Color configuredColor;
            if (options.SeriesColors != null && options.SeriesColors.TryGetValue(seriesName, out configuredColor))
            {
                return ApplyColorAlpha(configuredColor, options);
            }

            Color[] palette = options.PastelPalette == null || options.PastelPalette.Length == 0
                ? TSNEScatterSeriesOptions.CreateCompanySeriesPalette()
                : options.PastelPalette;
            if (options.UsePaletteColors && palette.Length > 0)
            {
                return ApplyColorAlpha(palette[Math.Abs(seriesIndex) % palette.Length], options);
            }

            if (string.Equals(seriesName, options.PassResultName, StringComparison.OrdinalIgnoreCase))
            {
                return ApplyColorAlpha(options.PassColor, options);
            }

            if (string.Equals(seriesName, options.ReviewResultName, StringComparison.OrdinalIgnoreCase))
            {
                return ApplyColorAlpha(options.ReviewColor, options);
            }

            if (seriesIndex >= 0 && seriesIndex < palette.Length)
            {
                return ApplyColorAlpha(palette[seriesIndex], options);
            }

            return ApplyColorAlpha(options.DefaultColor, options);
        }

        private static Dictionary<string, Color> ResolveSeriesColors(IEnumerable<string> orderedNames, TSNEScatterSeriesOptions options)
        {
            var colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            int companyPaletteIndex = 0;
            foreach (string seriesName in orderedNames ?? Enumerable.Empty<string>())
            {
                bool isNaSeries = IsNaSeriesName(seriesName, options);
                int colorIndex = isNaSeries ? 0 : companyPaletteIndex++;
                colors[seriesName] = ResolveSeriesColor(seriesName, colorIndex, options);
            }

            return colors;
        }

        private static Dictionary<string, Color> ResolveSeriesBorderColors(IEnumerable<string> orderedNames, TSNEScatterSeriesOptions options)
        {
            var colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            Color[] palette = options == null || options.BorderPalette == null || options.BorderPalette.Length == 0
                ? TSNEScatterSeriesOptions.CreateCompanySeriesBorderPalette()
                : options.BorderPalette;
            int companyPaletteIndex = 0;
            foreach (string seriesName in orderedNames ?? Enumerable.Empty<string>())
            {
                bool isNaSeries = IsNaSeriesName(seriesName, options);
                int colorIndex = isNaSeries ? 0 : companyPaletteIndex++;
                colors[seriesName] = palette.Length == 0
                    ? ResolveSeriesColor(seriesName, colorIndex, options)
                    : palette[Math.Abs(colorIndex) % palette.Length];
            }

            return colors;
        }

        private static Color ResolveSelectedPointColor(string selectedSeriesName, IDictionary<string, Color> seriesColors, TSNEScatterSeriesOptions options)
        {
            if (options != null && !options.SelectedPointColor.IsEmpty)
            {
                return ApplyColorAlpha(options.SelectedPointColor, options);
            }

            Color seriesColor;
            return seriesColors != null && seriesColors.TryGetValue(selectedSeriesName, out seriesColor)
                ? seriesColor
                : ResolveSeriesColor(selectedSeriesName, 0, options);
        }

        private static float ResolveSelectedPointSize(TSNEScatterSeriesOptions options)
        {
            float basePointSize = options == null ? 7f : Math.Max(1f, options.PointSize);
            return options != null && options.SelectedPointSize > 0f
                ? Math.Max(1f, options.SelectedPointSize)
                : Math.Max(1f, basePointSize * 1.1f);
        }

        private static float ResolveHighlightedPointSize(TSNEScatterSeriesOptions options)
        {
            float basePointSize = options == null ? 7f : Math.Max(1f, options.PointSize);
            return options != null && options.HighlightPointSize > 0f
                ? Math.Max(1f, options.HighlightPointSize)
                : Math.Max(1f, basePointSize * 1.1f);
        }

        private static Color ApplyColorAlpha(Color color, TSNEScatterSeriesOptions options)
        {
            return color;
        }

        private static bool IsNaSeriesName(string seriesName, TSNEScatterSeriesOptions options)
        {
            if (options == null || string.IsNullOrWhiteSpace(options.NaSeriesName))
            {
                return false;
            }

            string naSeriesName = options.NaSeriesName.Trim();
            return string.Equals(seriesName, naSeriesName, StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> ResolveSeriesOrder(IEnumerable<string> groupNames, TSNEScatterSeriesOptions options)
        {
            HashSet<string> remaining = new HashSet<string>(
                groupNames ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            var ordered = new List<string>();

            if (options.SeriesOrder != null)
            {
                foreach (string preferredName in options.SeriesOrder)
                {
                    if (string.IsNullOrWhiteSpace(preferredName) || !remaining.Contains(preferredName))
                    {
                        continue;
                    }

                    ordered.Add(preferredName);
                    remaining.Remove(preferredName);
                }
            }

            ordered.AddRange(remaining.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
            return ordered;
        }
    }
}








