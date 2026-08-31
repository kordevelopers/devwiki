using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public enum DimensionalityReductionMethod
    {
        Pca,
        Tsne
    }

    #region Analysis Result Models

    public enum KnnSearchAlgorithm
    {
        Auto,
        BruteForce,
        KdTree,
        BallTree
    }

    public sealed class PcaAnalysisOptions
    {
        public PcaAnalysisOptions()
        {
            ConstantVarianceThreshold = 1e-10d;
            MinimumNumericFeatureCoverageRatio = 0.90d;
            MeanImputationEnabled = true;
            ComponentCount = 2;
            MaxIterations = 2000;
            ConvergenceTolerance = 1e-10d;
            NeighborCount = 3;
            KnnSearchAlgorithm = KnnSearchAlgorithm.Auto;
            ProjectionMethod = DimensionalityReductionMethod.Tsne;
            TsnePerplexity = 30d;
            TsneIterations = 750;
            TsneLearningRate = 200d;
            TsneRandomSeed = 20260831;
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
        public double TsnePerplexity { get; set; }
        public int TsneIterations { get; set; }
        public double TsneLearningRate { get; set; }
        public int TsneRandomSeed { get; set; }
    }

    public sealed class KnnNeighbor
    {
        public int Rank { get; set; }
        public int SourceIndex { get; set; }
        public string DraftNo { get; set; }
        public double Distance { get; set; }
    }

    public sealed class PcaVerificationReport
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

    public sealed class PcaAnalysisDiagnosticReport
    {
        private PcaAnalysisDiagnosticReport()
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

        public static PcaAnalysisDiagnosticReport Create(PcaAnalysisResult analysisResult, int rowCount, int missingExperimentCount)
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
            bool isTsne = analysisResult != null
                && analysisResult.ProjectionMethod == DimensionalityReductionMethod.Tsne;
            string shapeCode = isTsne
                ? ResolveTsneShapeCode(rowCount, featureCount)
                : ResolveShapeCode(rowCount, featureCount, pc1, pc2);
            string compactText = isTsne
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "DIAG R={0} F={1} X={2} M={3} TSNE PERP={4:0.##} ENGINE=ACCORD SHAPE={5} KNN={6}",
                    rowCount,
                    featureCount,
                    excludedCount,
                    missingExperimentCount,
                    analysisResult.TsneModel == null ? 0d : analysisResult.TsneModel.EffectivePerplexity,
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

            return new PcaAnalysisDiagnosticReport
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

        private static int ResolveExcludedFeatureCount(PcaAnalysisResult analysisResult)
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

        private static double GetExplainedVariancePercent(PcaAnalysisResult analysisResult, int index)
        {
            if (analysisResult == null
                || analysisResult.PcaModel == null
                || analysisResult.PcaModel.ExplainedVarianceRatios == null
                || analysisResult.PcaModel.ExplainedVarianceRatios.Length <= index)
            {
                return 0d;
            }

            double ratio = analysisResult.PcaModel.ExplainedVarianceRatios[index];
            if (double.IsNaN(ratio) || double.IsInfinity(ratio))
            {
                return 0d;
            }

            return ratio * 100d;
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
                return "PCA2_LOW";
            }

            return "OK";
        }

        private static string ResolveTsneShapeCode(int rowCount, int featureCount)
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

    public enum PcaFeatureSelectionReason
    {
        Included,
        Metadata,
        MissingInRows,
        NonNumeric,
        ConstantOrLowVariance
    }

    public sealed class PcaFeatureSelectionDetail
    {
        internal PcaFeatureSelectionDetail()
        {
        }

        public string FeatureName { get; internal set; }
        public bool Included { get; internal set; }
        public PcaFeatureSelectionReason Reason { get; internal set; }
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

    public sealed class PcaFeatureSelectionReport
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

        private readonly ReadOnlyCollection<PcaFeatureSelectionDetail> details;
        private readonly ReadOnlyCollection<string> includedFeatureNames;
        private readonly ReadOnlyCollection<string> excludedFeatureNames;

        private PcaFeatureSelectionReport(int rowCount, IEnumerable<PcaFeatureSelectionDetail> detailItems)
        {
            RowCount = rowCount;
            details = new ReadOnlyCollection<PcaFeatureSelectionDetail>(
                (detailItems ?? Enumerable.Empty<PcaFeatureSelectionDetail>()).ToList());
            includedFeatureNames = new ReadOnlyCollection<string>(
                details.Where(item => item.Included).Select(item => item.FeatureName).ToList());
            excludedFeatureNames = new ReadOnlyCollection<string>(
                details.Where(item => !item.Included).Select(item => item.FeatureName).ToList());
        }

        public int RowCount { get; private set; }
        public IList<PcaFeatureSelectionDetail> Details
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

        public static PcaFeatureSelectionReport Empty()
        {
            return new PcaFeatureSelectionReport(0, new PcaFeatureSelectionDetail[0]);
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
            DataTable table = new DataTable("PCA_FEATURE_SELECTION");
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

            foreach (PcaFeatureSelectionDetail detail in details)
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

        internal static PcaFeatureSelectionReport CreateFromSourceRows(IList<PcaSourceRow> rows, IEnumerable<string> includedFeatureNames, double varianceThreshold)
        {
            IEnumerable<FeatureSelectionAuditRow> auditRows = (rows ?? new List<PcaSourceRow>())
                .Select(row => new FeatureSelectionAuditRow
                {
                    DraftNo = row.DraftNo,
                    FieldNames = row.DataFieldNames,
                    NumericValues = row.NumericValues
                });
            return CreateFromAuditRows(auditRows, includedFeatureNames, varianceThreshold);
        }

        internal static PcaFeatureSelectionReport CreateFromParsedExperiments(
            IList<ParsedPcaExperiment> experiments,
            IEnumerable<string> includedFeatureNames,
            double varianceThreshold)
        {
            IEnumerable<FeatureSelectionAuditRow> auditRows =
                (experiments ?? new List<ParsedPcaExperiment>())
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

        private static PcaFeatureSelectionReport CreateFromAuditRows(
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

            var details = new List<PcaFeatureSelectionDetail>(allFeatureNames.Length);
            foreach (string featureName in allFeatureNames)
            {
                details.Add(CreateDetail(rows, featureName, includedSet, varianceThreshold));
            }

            return new PcaFeatureSelectionReport(rows.Count, details);
        }

        private static PcaFeatureSelectionDetail CreateDetail(IList<FeatureSelectionAuditRow> rows, string featureName, ISet<string> includedFeatureNames, double varianceThreshold)
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
            PcaFeatureSelectionReason reason = ResolveReason(
                featureName,
                included,
                rowCount,
                presentCount,
                numericCount,
                numericValues,
                varianceThreshold);

            var detail = new PcaFeatureSelectionDetail
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

        private static PcaFeatureSelectionReason ResolveReason(
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
                return PcaFeatureSelectionReason.Included;
            }

            if (IsKnownMetadataFeature(featureName))
            {
                return PcaFeatureSelectionReason.Metadata;
            }

            if (presentCount < rowCount || numericCount < rowCount)
            {
                return presentCount < rowCount
                    ? PcaFeatureSelectionReason.MissingInRows
                    : PcaFeatureSelectionReason.NonNumeric;
            }

            if (numericValues == null || numericValues.Count == 0)
            {
                return PcaFeatureSelectionReason.NonNumeric;
            }

            return PcaFeatureSelectionReason.ConstantOrLowVariance;
        }

        private static void ApplyStatistics(PcaFeatureSelectionDetail detail, IList<double> numericValues)
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

    public sealed class PcaAnalysisResult
    {
        internal PcaAnalysisResult()
        {
            ScatterData = new List<ScatterSampleData>();
            FeatureNames = new string[0];
            ExcludedFeatureNames = new string[0];
            StandardizedMatrix = new double[0][];
            FeatureSelectionReport = PcaFeatureSelectionReport.Empty();
        }

        public IList<ScatterSampleData> ScatterData { get; internal set; }
        public string[] FeatureNames { get; internal set; }
        public string[] ExcludedFeatureNames { get; internal set; }
        public double[][] StandardizedMatrix { get; internal set; }
        public StandardScalerModel Scaler { get; internal set; }
        public PcaProjectionModel PcaModel { get; internal set; }
        public TsneProjectionModel TsneModel { get; internal set; }
        public DimensionalityReductionMethod ProjectionMethod { get; internal set; }
        public KnnSimilarityService Knn { get; internal set; }
        public PcaVerificationReport Verification { get; internal set; }
        public PcaAnalysisDiagnosticReport Diagnostic { get; internal set; }
        public PcaFeatureSelectionReport FeatureSelectionReport { get; internal set; }

        public IList<KnnNeighbor> FindNearest(string draftNo, int count)
        {
            return Knn.FindNearest(draftNo, count);
        }
    }

    internal sealed class PcaSourceRow
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
        public PcaFeatureSelectionReport FeatureSelectionReport { get; set; }
    }

    #endregion

    #region JSON Parsing and Numeric Feature Selection

    /// <summary>
    /// JSON 실험 데이터에서 메타데이터와 수치 feature를 분리하고 PCA 분석 행렬을 만든다.
    /// </summary>
    public sealed class PcaAnalysisPipeline
    {
        private static readonly string[] DraftNoAliases = { "Draft_NO", "Draft_No", "draft_No" };
        private static readonly string[] AiResultAliases = { "AI_RSLT_Val", "AI_RSLT_VAL", "ENGR_RSLT_VAL", "AiResultValue" };
        private static readonly HashSet<string> MetadataNames = new HashSet<string>(
            DraftNoAliases.Concat(AiResultAliases),
            StringComparer.OrdinalIgnoreCase);

        private readonly PcaAnalysisOptions options;

        public PcaAnalysisPipeline()
            : this(new PcaAnalysisOptions())
        {
        }

        public PcaAnalysisPipeline(PcaAnalysisOptions options)
        {
            this.options = options ?? new PcaAnalysisOptions();
        }

        /// <summary>
        /// DB ACT_DATA 컬럼에서 읽은 JSON 문서를 Dict/List 구조로 파싱하고,
        /// 내부 실험 객체를 개별 JSON 행으로 펼친 뒤 전체 분석을 수행한다.
        /// </summary>
        public PcaAnalysisResult AnalyzeActDataDocuments(IEnumerable<string> actDataDocuments)
        {
            var parser = new ActDataJsonParser();
            IList<string> experimentRows = parser.ExpandDocuments(actDataDocuments);
            return Analyze(experimentRows);
        }

        /// <summary>
        /// Service DataTable의 CONV_EXPER_CTN JSON 배열을 개별 실험 행으로 펼쳐 분석한다.
        /// 전체 데이터가 하나의 모집단으로 표준화되며 PCA와 KNN은 같은 결과를 공유한다.
        /// </summary>
        public PcaAnalysisResult AnalyzeConvExperimentDocuments(IEnumerable<string> convExperimentDocuments)
        {
            var parser = new ActDataJsonParser();
            IList<string> experimentRows = parser.ExpandDocuments(convExperimentDocuments, "CONV_EXPER_CTN");
            return Analyze(experimentRows);
        }

        /// <summary>
        /// 전체 분석 순서를 한 곳에서 보장한다.
        /// JSON 파싱 -> 수치 feature 행렬 생성 -> 정규화 -> PCA 2차원 좌표 생성 -> KNN 거리 인덱스 생성 -> 검증 순서다.
        /// PCA와 KNN은 같은 StandardizedMatrix를 공유하므로 특징 좌표계가 달라지지 않는다.
        /// </summary>
        public PcaAnalysisResult Analyze(IEnumerable<string> jsonSamples)
        {
            // rows: Draft별 원본 JSON에서 식별자/라벨과 수치 후보를 분리한 중간 데이터다.
            List<PcaSourceRow> rows = ParseRows(jsonSamples);
            // features.Matrix: 행은 Draft, 열은 살아남은 수치 feature인 PCA 입력 수치행렬이다.
            FeatureMatrixResult features = BuildFeatureMatrix(rows, options);
            StandardScalerModel scaler = StandardScalerModel.Fit(features.Matrix, features.FeatureNames);
            // standardized: 각 feature별 평균을 빼고 표준편차로 나눈 정규화 행렬이다.
            double[][] standardized = scaler.Transform(features.Matrix);
            PcaProjectionModel pca = null;
            TsneProjectionModel tsne = null;
            double[][] scores;
            if (options.ProjectionMethod == DimensionalityReductionMethod.Tsne)
            {
                tsne = TsneProjectionModel.FitTransform(
                    standardized,
                    options.TsnePerplexity,
                    options.TsneIterations,
                    options.TsneLearningRate,
                    options.TsneRandomSeed);
                scores = tsne.Coordinates;
            }
            else
            {
                pca = PcaProjectionModel.Fit(
                    standardized,
                    options.ComponentCount,
                    options.MaxIterations,
                    options.ConvergenceTolerance,
                    scaler);
                scores = pca.Transform(standardized);
            }

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
            PcaVerificationReport verification = options.ProjectionMethod == DimensionalityReductionMethod.Tsne
                ? PcaAlgorithmVerifier.VerifyTsne(
                    standardized,
                    scores,
                    scaler,
                    knn,
                    rows[0].DraftNo,
                    options.NeighborCount)
                : PcaAlgorithmVerifier.Verify(
                    standardized,
                    scaler,
                    pca,
                    knn,
                    rows[0].DraftNo,
                    options.NeighborCount);

            if (!verification.IsValid)
            {
                throw new InvalidOperationException("Projection/KNN verification failed: " + verification.Message);
            }

            var result = new PcaAnalysisResult
            {
                ScatterData = scatterData,
                FeatureNames = features.FeatureNames,
                ExcludedFeatureNames = features.ExcludedFeatureNames,
                StandardizedMatrix = standardized,
                Scaler = scaler,
                PcaModel = pca,
                TsneModel = tsne,
                ProjectionMethod = options.ProjectionMethod,
                Knn = knn,
                Verification = verification,
                FeatureSelectionReport = features.FeatureSelectionReport
            };
            result.Diagnostic = PcaAnalysisDiagnosticReport.Create(result, rows.Count, 0);
            return result;
        }

        private static List<PcaSourceRow> ParseRows(IEnumerable<string> jsonSamples)
        {
            string[] source = jsonSamples == null
                ? new string[0]
                : jsonSamples.Where(json => !string.IsNullOrWhiteSpace(json)).ToArray();
            if (source.Length < 3)
            {
                throw new ArgumentException("PCA requires at least three JSON samples.", "jsonSamples");
            }

            var rows = new List<PcaSourceRow>(source.Length);
            var draftNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < source.Length; index++)
            {
                object deserialized = PcaJsonUtility.DeserializeObject(source[index]);
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
                    // Draft_NO와 AI_RSLT_Val은 검색/라벨용 데이터라 PCA 계산 feature에서는 제외한다.
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

                rows.Add(new PcaSourceRow
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
        /// 모든 Draft에서 사용할 수 있는 수치 feature만 골라 PCA 입력 행렬을 만든다.
        /// 누락이 있더라도 옵션 기준을 통과하면 feature 평균값으로 보정한다.
        /// </summary>
        private static FeatureMatrixResult BuildFeatureMatrix(IList<PcaSourceRow> rows, PcaAnalysisOptions analysisOptions)
        {
            PcaAnalysisOptions effectiveOptions = analysisOptions ?? new PcaAnalysisOptions();
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
                FeatureSelectionReport = PcaFeatureSelectionReport.CreateFromSourceRows(
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

    #region PCA - Covariance Matrix, Eigenvectors and Projection

    public sealed class PcaProjectionModel
    {
        private PcaProjectionModel(double[][] components, double[] eigenValues, double[] ratios, int[] iterations, StandardScalerModel scaler)
        {
            Components = components;
            EigenValues = eigenValues;
            ExplainedVarianceRatios = ratios;
            Iterations = iterations;
            Scaler = scaler;
        }

        public double[][] Components { get; private set; }
        public double[] EigenValues { get; private set; }
        public double[] ExplainedVarianceRatios { get; private set; }
        public int[] Iterations { get; private set; }
        public StandardScalerModel Scaler { get; private set; }

        // 외부에서 계산한 component를 기존 차트/진단 DTO에 담기 위한 어댑터다.
        // 기본 수동 PCA 흐름에서는 Fit 메서드가 covariance/eigenvector를 직접 계산한다.
        internal static PcaProjectionModel FromComponents(double[][] components, double[] eigenValues, double[] ratios, StandardScalerModel scaler)
        {
            if (components == null || components.Length == 0)
            {
                throw new ArgumentException("PCA components are required.", "components");
            }

            return new PcaProjectionModel(
                components.Select(component => (double[])component.Clone()).ToArray(),
                eigenValues == null ? new double[0] : (double[])eigenValues.Clone(),
                ratios == null ? new double[0] : (double[])ratios.Clone(),
                Enumerable.Repeat(0, components.Length).ToArray(),
                scaler);
        }

        public static PcaProjectionModel Fit(double[][] standardizedMatrix, int componentCount, int maxIterations, double tolerance)
        {
            return Fit(
                standardizedMatrix,
                componentCount,
                maxIterations,
                tolerance,
                null);
        }

        public static PcaProjectionModel Fit(double[][] standardizedMatrix, int componentCount, int maxIterations, double tolerance, StandardScalerModel scaler)
        {
            if (standardizedMatrix == null || standardizedMatrix.Length < 3)
            {
                throw new ArgumentException("PCA requires at least three rows.", "standardizedMatrix");
            }

            int featureCount = standardizedMatrix[0].Length;
            int safeComponentCount = Math.Min(Math.Max(1, componentCount), Math.Min(featureCount, 2));
            // 공분산 행렬은 정규화된 feature들이 함께 증가/감소하는 방향을 찾기 위한 입력이다.
            double[,] covariance = BuildCovarianceMatrix(standardizedMatrix);
            double totalVariance = Enumerable.Range(0, featureCount).Sum(index => covariance[index, index]);
            if (featureCount == 2)
            {
                return FitTwoFeatureProjection(covariance, totalVariance, safeComponentCount, scaler);
            }

            var components = new List<double[]>();
            var eigenValues = new List<double>();
            var iterations = new List<int>();

            for (int componentIndex = 0; componentIndex < safeComponentCount; componentIndex++)
            {
                // 가장 큰 분산 방향부터 차례로 찾는다. 첫 번째가 X1, 두 번째가 X2 축 방향이다.
                EigenPair pair = FindDominantEigenPair(
                    covariance,
                    components,
                    componentIndex,
                    Math.Max(20, maxIterations),
                    Math.Max(1e-14d, tolerance));
                components.Add(pair.Vector);
                eigenValues.Add(Math.Max(0d, pair.Value));
                iterations.Add(pair.Iterations);
            }

            ReconcileFullComponentEigenValues(eigenValues, totalVariance, safeComponentCount, featureCount);
            double[] ratios = eigenValues
                .Select(value => totalVariance <= 0d ? 0d : value / totalVariance)
                .ToArray();
            return new PcaProjectionModel(
                components.ToArray(),
                eigenValues.ToArray(),
                ratios,
                iterations.ToArray(),
                scaler);
        }

        private static PcaProjectionModel FitTwoFeatureProjection(double[,] covariance, double totalVariance, int componentCount, StandardScalerModel scaler)
        {
            double a = covariance[0, 0];
            double b = covariance[0, 1];
            double c = covariance[1, 1];
            double trace = a + c;
            double root = Math.Sqrt(((a - c) * (a - c)) + (4d * b * b));
            double firstValue = Math.Max(0d, (trace + root) / 2d);
            double secondValue = Math.Max(0d, (trace - root) / 2d);
            double[] firstVector = ResolveTwoFeatureEigenVector(a, b, c, firstValue);
            double[] secondVector = new[] { -firstVector[1], firstVector[0] };
            CanonicalizeSign(firstVector);
            CanonicalizeSign(secondVector);

            var components = new List<double[]> { firstVector };
            var eigenValues = new List<double> { firstValue };
            if (componentCount > 1)
            {
                components.Add(secondVector);
                eigenValues.Add(secondValue);
            }

            ReconcileFullComponentEigenValues(eigenValues, totalVariance, componentCount, 2);
            double[] ratios = eigenValues
                .Select(value => totalVariance <= 0d ? 0d : value / totalVariance)
                .ToArray();
            return new PcaProjectionModel(
                components.ToArray(),
                eigenValues.ToArray(),
                ratios,
                Enumerable.Repeat(0, components.Count).ToArray(),
                scaler);
        }

        private static double[] ResolveTwoFeatureEigenVector(double a, double b, double c, double eigenValue)
        {
            double[] vector;
            if (Math.Abs(b) > 1e-14d || Math.Abs(eigenValue - a) > 1e-14d)
            {
                vector = new[] { b, eigenValue - a };
            }
            else
            {
                vector = a >= c ? new[] { 1d, 0d } : new[] { 0d, 1d };
            }

            if (Math.Sqrt(Dot(vector, vector)) <= 1e-14d)
            {
                vector = new[] { eigenValue - c, b };
            }

            Normalize(vector);
            return vector;
        }

        private static void ReconcileFullComponentEigenValues(IList<double> eigenValues, double totalVariance, int componentCount, int featureCount)
        {
            if (eigenValues == null || eigenValues.Count == 0 || totalVariance <= 0d || componentCount != featureCount)
            {
                return;
            }

            // 모든 feature를 component로 표시하는 경우 마지막 고유값은 전체 분산에서 앞선 고유값을 뺀 잔여 분산이다.
            double previous = 0d;
            for (int index = 0; index < eigenValues.Count - 1; index++)
            {
                previous += Math.Max(0d, eigenValues[index]);
            }

            eigenValues[eigenValues.Count - 1] = Math.Max(0d, totalVariance - previous);
        }

        public double[][] Transform(double[][] standardizedMatrix)
        {
            var scores = new double[standardizedMatrix.Length][];
            for (int row = 0; row < standardizedMatrix.Length; row++)
            {
                scores[row] = new double[Components.Length];
                for (int component = 0; component < Components.Length; component++)
                {
                    // 표준화 벡터와 PC 가중치 벡터를 내적한 값이 차트 좌표 X1/X2다.
                    scores[row][component] = Dot(standardizedMatrix[row], Components[component]);
                }
            }

            return scores;
        }

        private static double[,] BuildCovarianceMatrix(double[][] matrix)
        {
            int rowCount = matrix.Length;
            int columnCount = matrix[0].Length;
            var covariance = new double[columnCount, columnCount];
            double denominator = Math.Max(1d, rowCount - 1d);

            for (int left = 0; left < columnCount; left++)
            {
                for (int right = left; right < columnCount; right++)
                {
                    double sum = 0d;
                    for (int row = 0; row < rowCount; row++)
                    {
                        sum += matrix[row][left] * matrix[row][right];
                    }

                    double value = sum / denominator;
                    covariance[left, right] = value;
                    covariance[right, left] = value;
                }
            }

            return covariance;
        }

        private static EigenPair FindDominantEigenPair(double[,] matrix, IList<double[]> previousComponents, int componentIndex, int maxIterations, double tolerance)
        {
            int size = matrix.GetLength(0);
            double[] vector = Enumerable.Range(0, size)
                .Select(index => 1d + (((index + 1) * (componentIndex + 2)) % 11) * 0.01d)
                .ToArray();
            Orthogonalize(vector, previousComponents);
            Normalize(vector);

            int iteration;
            for (iteration = 1; iteration <= maxIterations; iteration++)
            {
                double[] next = Multiply(matrix, vector);
                // 두 번째 성분 이후는 이전 성분과 직교하도록 Gram-Schmidt 보정을 적용한다.
                Orthogonalize(next, previousComponents);
                Normalize(next);

                // 고유벡터 부호는 임의이므로 이전 벡터와 같은 방향으로 맞춘 뒤 차이를 계산한다.
                if (Dot(next, vector) < 0d)
                {
                    MultiplyInPlace(next, -1d);
                }

                double difference = EuclideanDistance(next, vector);
                vector = next;
                if (difference <= tolerance)
                {
                    break;
                }
            }

            CanonicalizeSign(vector);
            double[] projected = Multiply(matrix, vector);
            return new EigenPair
            {
                Value = Dot(vector, projected),
                Vector = vector,
                Iterations = Math.Min(iteration, maxIterations)
            };
        }

        private static void Orthogonalize(double[] vector, IEnumerable<double[]> basisVectors)
        {
            foreach (double[] basis in basisVectors)
            {
                double projection = Dot(vector, basis);
                for (int index = 0; index < vector.Length; index++)
                {
                    vector[index] -= projection * basis[index];
                }
            }
        }

        private static void Normalize(double[] vector)
        {
            double norm = Math.Sqrt(Dot(vector, vector));
            if (norm <= 1e-14d)
            {
                throw new InvalidOperationException("PCA eigenvector normalization failed.");
            }

            MultiplyInPlace(vector, 1d / norm);
        }

        private static void CanonicalizeSign(double[] vector)
        {
            int largestIndex = 0;
            for (int index = 1; index < vector.Length; index++)
            {
                if (Math.Abs(vector[index]) > Math.Abs(vector[largestIndex]))
                {
                    largestIndex = index;
                }
            }

            if (vector[largestIndex] < 0d)
            {
                MultiplyInPlace(vector, -1d);
            }
        }

        private static double[] Multiply(double[,] matrix, double[] vector)
        {
            int size = vector.Length;
            var result = new double[size];
            for (int row = 0; row < size; row++)
            {
                double sum = 0d;
                for (int column = 0; column < size; column++)
                {
                    sum += matrix[row, column] * vector[column];
                }

                result[row] = sum;
            }

            return result;
        }

        private static void MultiplyInPlace(double[] vector, double scalar)
        {
            for (int index = 0; index < vector.Length; index++)
            {
                vector[index] *= scalar;
            }
        }

        internal static double Dot(double[] left, double[] right)
        {
            double sum = 0d;
            for (int index = 0; index < left.Length; index++)
            {
                sum += left[index] * right[index];
            }

            return sum;
        }

        private static double EuclideanDistance(double[] left, double[] right)
        {
            double sum = 0d;
            for (int index = 0; index < left.Length; index++)
            {
                double difference = left[index] - right[index];
                sum += difference * difference;
            }

            return Math.Sqrt(sum);
        }

        private sealed class EigenPair
        {
            public double Value { get; set; }
            public double[] Vector { get; set; }
            public int Iterations { get; set; }
        }
    }

    #endregion

    #region KNN - Euclidean Distance in Standardized Feature Space

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
                throw new KeyNotFoundException("Draft_NO was not found: " + (draftNo ?? string.Empty));
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

    #endregion

    #region Algorithm Self Verification

    internal static class PcaAlgorithmVerifier
    {
        public static PcaVerificationReport VerifyTsne(
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

            return new PcaVerificationReport
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

        public static PcaVerificationReport Verify(
            double[][] standardized,
            StandardScalerModel scaler,
            PcaProjectionModel pca,
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
                maxStandardDeviationError = Math.Max(
                    maxStandardDeviationError,
                    Math.Abs(Math.Sqrt(variance) - 1d));
            }

            double componentDot = pca.Components.Length < 2
                ? 0d
                : Math.Abs(PcaProjectionModel.Dot(pca.Components[0], pca.Components[1]));
            bool eigenValuesDescending = pca.EigenValues.Length < 2
                || pca.EigenValues[0] + 1e-9d >= pca.EigenValues[1];
            double[][] scores = pca.Transform(standardized);
            bool allScoresFinite = scores.SelectMany(row => row)
                .All(value => !double.IsNaN(value) && !double.IsInfinity(value));
            IList<KnnNeighbor> neighbors = knn.FindNearest(firstDraftNo, neighborCount);
            bool knnValid = neighbors.Count == Math.Min(Math.Max(0, neighborCount), rowCount - 1)
                && neighbors.All(item => !string.Equals(item.DraftNo, firstDraftNo, StringComparison.OrdinalIgnoreCase))
                && neighbors.Select(item => item.Distance).SequenceEqual(
                    neighbors.Select(item => item.Distance).OrderBy(value => value));
            bool sharedScaler = scaler != null
                && object.ReferenceEquals(scaler, pca.Scaler)
                && object.ReferenceEquals(scaler, knn.Scaler)
                && scaler.FeatureNames != null
                && scaler.FeatureNames.Length == columnCount;

            bool valid = maxMean <= 1e-8d
                && maxStandardDeviationError <= 1e-8d
                && componentDot <= 1e-6d
                && eigenValuesDescending
                && allScoresFinite
                && knnValid
                && sharedScaler;
            return new PcaVerificationReport
            {
                IsValid = valid,
                MaximumAbsoluteStandardizedMean = maxMean,
                MaximumStandardDeviationError = maxStandardDeviationError,
                ComponentDotProduct = componentDot,
                EigenValuesDescending = eigenValuesDescending,
                AllScoresFinite = allScoresFinite,
                KnnResultValid = knnValid,
                SharedScalerInstance = sharedScaler,
                Message = valid
                    ? "Shared StandardScaler, PCA orthogonality, finite scores and KNN ordering verified."
                    : "One or more StandardScaler/PCA/KNN invariants failed, including shared scaler verification."
            };
        }
    }

    #endregion
}



