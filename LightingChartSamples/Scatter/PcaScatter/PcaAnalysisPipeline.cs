using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.PcaScatter
{
    #region Analysis Result Models

    public sealed class PcaAnalysisOptions
    {
        public PcaAnalysisOptions()
        {
            ConstantVarianceThreshold = 1e-10d;
            ComponentCount = 2;
            MaxIterations = 2000;
            ConvergenceTolerance = 1e-10d;
            NeighborCount = 3;
        }

        public double ConstantVarianceThreshold { get; set; }
        public int ComponentCount { get; set; }
        public int MaxIterations { get; set; }
        public double ConvergenceTolerance { get; set; }
        public int NeighborCount { get; set; }
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
        public string CompactText { get; private set; }

        public static PcaAnalysisDiagnosticReport Create(
            PcaAnalysisResult analysisResult,
            int rowCount,
            int missingExperimentCount)
        {
            int featureCount = analysisResult == null || analysisResult.FeatureNames == null
                ? 0
                : analysisResult.FeatureNames.Length;
            int excludedCount = ResolveExcludedFeatureCount(analysisResult);
            double pc1 = GetExplainedVariancePercent(analysisResult, 0);
            double pc2 = GetExplainedVariancePercent(analysisResult, 1);
            string shapeCode = ResolveShapeCode(rowCount, featureCount, pc1, pc2);
            string compactText = string.Format(
                CultureInfo.InvariantCulture,
                "DIAG R={0} F={1} X={2} M={3} PC1={4:0.0} PC2={5:0.0} SUM={6:0.0} SHAPE={7}",
                rowCount,
                featureCount,
                excludedCount,
                missingExperimentCount,
                pc1,
                pc2,
                pc1 + pc2,
                shapeCode);

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

        private static string ResolveShapeCode(
            int rowCount,
            int featureCount,
            double pc1Percent,
            double pc2Percent)
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
            "AiResultValue",
            "PUB_NO",
            "_VERSION_NM"
        };

        private readonly ReadOnlyCollection<PcaFeatureSelectionDetail> details;
        private readonly ReadOnlyCollection<string> includedFeatureNames;
        private readonly ReadOnlyCollection<string> excludedFeatureNames;

        private PcaFeatureSelectionReport(
            int rowCount,
            IEnumerable<PcaFeatureSelectionDetail> detailItems)
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

        internal static PcaFeatureSelectionReport CreateFromSourceRows(
            IList<PcaSourceRow> rows,
            IEnumerable<string> includedFeatureNames,
            double varianceThreshold)
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

        private static PcaFeatureSelectionDetail CreateDetail(
            IList<FeatureSelectionAuditRow> rows,
            string featureName,
            ISet<string> includedFeatureNames,
            double varianceThreshold)
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

        private static void ApplyStatistics(
            PcaFeatureSelectionDetail detail,
            IList<double> numericValues)
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
    /// JSON ?ㅽ뿕 ?곗씠?곗뿉???앸퀎?먯? ?섏튂 ?뱀쭠??遺꾨━?섍퀬 遺꾩꽍 ?됰젹??留뚮뱺??
    /// </summary>
    public sealed class PcaAnalysisPipeline
    {
        private static readonly string[] DraftNoAliases = { "Draft_NO", "Draft_No", "draft_No" };
        private static readonly string[] AiResultAliases = { "AI_RSLT_Val", "AI_RSLT_VAL", "AiResultValue" };
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
        /// DB ACT_DATA 而щ읆?먯꽌 ?쎌? JSON 臾몄꽌瑜?Dict/List 援ъ“濡??뚯떛?섍퀬,
        /// ?대????ㅽ뿕 媛앹껜瑜?媛쒕퀎 JSON ?됱쑝濡??쇱튇 ???꾩껜 遺꾩꽍???ㅽ뻾?쒕떎.
        /// </summary>
        public PcaAnalysisResult AnalyzeActDataDocuments(IEnumerable<string> actDataDocuments)
        {
            var parser = new ActDataJsonParser();
            IList<string> experimentRows = parser.ExpandDocuments(actDataDocuments);
            return Analyze(experimentRows);
        }

        /// <summary>
        /// Service DataTable??CONV_EXPER_CTN JSON 諛곗뿴??媛쒕퀎 ?ㅽ뿕 ?됱쑝濡??쇱퀜 遺꾩꽍?쒕떎.
        /// ?꾩껜 ?곗씠?곌? ?섎굹???ㅻ깄?룹쑝濡??쒖??붾릺硫?PCA? KNN??媛숈? 寃곌낵瑜??ъ슜?쒕떎.
        /// </summary>
        public PcaAnalysisResult AnalyzeConvExperimentDocuments(
            IEnumerable<string> convExperimentDocuments)
        {
            var parser = new ActDataJsonParser();
            IList<string> experimentRows = parser.ExpandDocuments(
                convExperimentDocuments,
                "CONV_EXPER_CTN");
            return Analyze(experimentRows);
        }

        /// <summary>
        /// ?꾩껜 遺꾩꽍 ?쒖꽌瑜???怨녹뿉??蹂댁옣?쒕떎.
        /// JSON 異붿텧 -> ?遺꾩궛 ?쒓굅 -> StandardScaler -> PCA -> KNN -> 寃利??쒖꽌??
        /// PCA? KNN? 媛숈? StandardizedMatrix瑜?怨듭쑀?섎?濡??뱀쭠 ?쒖꽌媛 ?щ씪吏????녿떎.
        /// </summary>
        public PcaAnalysisResult Analyze(IEnumerable<string> jsonSamples)
        {
            List<PcaSourceRow> rows = ParseRows(jsonSamples);
            FeatureMatrixResult features = BuildFeatureMatrix(rows, options.ConstantVarianceThreshold);
            StandardScalerModel scaler = StandardScalerModel.Fit(features.Matrix, features.FeatureNames);
            double[][] standardized = scaler.Transform(features.Matrix);
            PcaProjectionModel pca = PcaProjectionModel.Fit(
                standardized,
                options.ComponentCount,
                options.MaxIterations,
                options.ConvergenceTolerance,
                scaler);
            double[][] scores = pca.Transform(standardized);

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
                scaler);
            PcaVerificationReport verification = PcaAlgorithmVerifier.Verify(
                standardized,
                scaler,
                pca,
                knn,
                rows[0].DraftNo,
                options.NeighborCount);

            if (!verification.IsValid)
            {
                throw new InvalidOperationException("PCA/KNN verification failed: " + verification.Message);
            }

            var result = new PcaAnalysisResult
            {
                ScatterData = scatterData,
                FeatureNames = features.FeatureNames,
                ExcludedFeatureNames = features.ExcludedFeatureNames,
                StandardizedMatrix = standardized,
                Scaler = scaler,
                PcaModel = pca,
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
                string aiResult = GetRequiredText(dictionary, AiResultAliases, "AI_RSLT_Val", index);
                if (!draftNos.Add(draftNo))
                {
                    throw new InvalidOperationException("Duplicate Draft_NO: " + draftNo);
                }

                var numericValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, object> pair in dictionary)
                {
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

        private static string GetRequiredText(
            IDictionary<string, object> dictionary,
            IEnumerable<string> aliases,
            string displayName,
            int rowIndex)
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

        private static FeatureMatrixResult BuildFeatureMatrix(
            IList<PcaSourceRow> rows,
            double varianceThreshold)
        {
            string[] allFields = rows
                .SelectMany(row => row.DataFieldNames)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var included = new List<string>();
            var excluded = new List<string>();

            foreach (string fieldName in allFields)
            {
                bool numericInEveryRow = rows.All(row => row.NumericValues.ContainsKey(fieldName));
                if (!numericInEveryRow)
                {
                    excluded.Add(fieldName);
                    continue;
                }

                double mean = rows.Average(row => row.NumericValues[fieldName]);
                double variance = rows.Average(row =>
                {
                    double difference = row.NumericValues[fieldName] - mean;
                    return difference * difference;
                });

                // 遺꾩궛??1e-10 ?댄븯??而щ읆? ?뺣낫?됱씠 ?녾퀬 ?쒖?????0?쇰줈 ?섎늻寃??섎?濡??쒓굅?쒕떎.
                if (variance <= Math.Max(0d, varianceThreshold))
                {
                    excluded.Add(fieldName);
                    continue;
                }

                included.Add(fieldName);
            }

            if (included.Count < 2)
            {
                throw new InvalidOperationException("At least two numeric non-constant features are required.");
            }

            double[][] matrix = rows
                .Select(row => included.Select(feature => row.NumericValues[feature]).ToArray())
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
                    // StandardScaler 怨듭떇: z = (?먮낯媛?- ?숈뒿 ?됯퇏) / ?숈뒿 ?쒖??몄감
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
        private PcaProjectionModel(
            double[][] components,
            double[] eigenValues,
            double[] ratios,
            int[] iterations,
            StandardScalerModel scaler)
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

        public static PcaProjectionModel Fit(
            double[][] standardizedMatrix,
            int componentCount,
            int maxIterations,
            double tolerance)
        {
            return Fit(
                standardizedMatrix,
                componentCount,
                maxIterations,
                tolerance,
                null);
        }

        public static PcaProjectionModel Fit(
            double[][] standardizedMatrix,
            int componentCount,
            int maxIterations,
            double tolerance,
            StandardScalerModel scaler)
        {
            if (standardizedMatrix == null || standardizedMatrix.Length < 3)
            {
                throw new ArgumentException("PCA requires at least three rows.", "standardizedMatrix");
            }

            int featureCount = standardizedMatrix[0].Length;
            int safeComponentCount = Math.Min(Math.Max(1, componentCount), Math.Min(featureCount, 2));
            double[,] covariance = BuildCovarianceMatrix(standardizedMatrix);
            double totalVariance = Enumerable.Range(0, featureCount).Sum(index => covariance[index, index]);
            var components = new List<double[]>();
            var eigenValues = new List<double>();
            var iterations = new List<int>();

            for (int componentIndex = 0; componentIndex < safeComponentCount; componentIndex++)
            {
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

        public double[][] Transform(double[][] standardizedMatrix)
        {
            var scores = new double[standardizedMatrix.Length][];
            for (int row = 0; row < standardizedMatrix.Length; row++)
            {
                scores[row] = new double[Components.Length];
                for (int component = 0; component < Components.Length; component++)
                {
                    // 媛??쒖???踰≫꽣瑜?怨좎쑀踰≫꽣???댁쟻?섎㈃ ?대떦 二쇱꽦遺?醫뚰몴媛 ?쒕떎.
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

        private static EigenPair FindDominantEigenPair(
            double[,] matrix,
            IList<double[]> previousComponents,
            int componentIndex,
            int maxIterations,
            double tolerance)
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
                // ??踰덉㎏ ?깅텇? 泥?踰덉㎏ ?깅텇怨?吏곴탳?섎룄濡?Gram-Schmidt 蹂댁젙?쒕떎.
                Orthogonalize(next, previousComponents);
                Normalize(next);

                // 怨좎쑀踰≫꽣 遺?몃뒗 ?꾩쓽?대?濡??댁쟾 踰≫꽣? 媛숈? 諛⑺뼢?쇰줈 留욎텣 ???섎졃 ?ㅼ감瑜?怨꾩궛?쒕떎.
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

        public KnnSimilarityService(string[] draftNos, double[][] standardizedMatrix)
            : this(draftNos, standardizedMatrix, null)
        {
        }

        public KnnSimilarityService(
            string[] draftNos,
            double[][] standardizedMatrix,
            StandardScalerModel scaler)
        {
            if (draftNos == null || standardizedMatrix == null || draftNos.Length != standardizedMatrix.Length)
            {
                throw new ArgumentException("Draft numbers and standardized matrix must have the same row count.");
            }

            this.draftNos = (string[])draftNos.Clone();
            this.standardizedMatrix = standardizedMatrix.Select(row => (double[])row.Clone()).ToArray();
            Scaler = scaler;
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

        public IList<KnnNeighbor> FindNearest(string draftNo, int count)
        {
            int targetIndex;
            if (string.IsNullOrWhiteSpace(draftNo) || !indexByDraftNo.TryGetValue(draftNo.Trim(), out targetIndex))
            {
                throw new KeyNotFoundException("議댁옱?섏? ?딅뒗 Draft_NO?낅땲?? " + (draftNo ?? string.Empty));
            }

            int safeCount = Math.Max(0, count);
            var candidates = new List<KnnNeighbor>();
            for (int sourceIndex = 0; sourceIndex < standardizedMatrix.Length; sourceIndex++)
            {
                if (sourceIndex == targetIndex)
                {
                    continue;
                }

                // KNN 嫄곕━??PCA 2李⑥썝 醫뚰몴媛 ?꾨땲???숈씪 scaler濡?蹂?섑븳 80李⑥썝 ?뱀쭠?먯꽌 怨꾩궛?쒕떎.
                double distance = CalculateEuclideanDistance(
                    standardizedMatrix[targetIndex],
                    standardizedMatrix[sourceIndex]);
                candidates.Add(new KnnNeighbor
                {
                    SourceIndex = sourceIndex,
                    DraftNo = draftNos[sourceIndex],
                    Distance = distance
                });
            }

            KnnNeighbor[] nearest = candidates
                .OrderBy(item => item.Distance)
                .ThenBy(item => item.DraftNo, StringComparer.OrdinalIgnoreCase)
                .Take(safeCount)
                .ToArray();
            for (int index = 0; index < nearest.Length; index++)
            {
                nearest[index].Rank = index + 1;
            }

            return nearest;
        }

        private static double CalculateEuclideanDistance(double[] left, double[] right)
        {
            double squaredDistance = 0d;
            for (int index = 0; index < left.Length; index++)
            {
                double difference = left[index] - right[index];
                squaredDistance += difference * difference;
            }

            return Math.Sqrt(squaredDistance);
        }
    }

    #endregion

    #region Algorithm Self Verification

    internal static class PcaAlgorithmVerifier
    {
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
