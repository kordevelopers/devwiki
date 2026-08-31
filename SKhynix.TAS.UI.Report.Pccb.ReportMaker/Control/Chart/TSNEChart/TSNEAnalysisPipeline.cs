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
    public enum DimensionalityReductionMethod
    {
        TSNE
    }


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
            ScatterData = new List<TSNEPointData>();
            FeatureNames = new string[0];
            ExcludedFeatureNames = new string[0];
            StandardizedMatrix = new double[0][];
            FeatureSelectionReport = TSNEFeatureSelectionReport.Empty();
        }

        public IList<TSNEPointData> ScatterData { get; internal set; }
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

            var scatterData = new List<TSNEPointData>(rows.Count);
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                scatterData.Add(new TSNEPointData
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

}












namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
}
