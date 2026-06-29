using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.AccordPcaScatter
{
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
            MinimumNumericCoverageRatio = 0.90d;
            MeanImputationEnabled = true;
            ComponentCount = 2;
            MaxIterations = 2000;
            ConvergenceTolerance = 1e-10d;
            NeighborCount = 3;
            KnnSearchAlgorithm = KnnSearchAlgorithm.Auto;
        }

        public double ConstantVarianceThreshold { get; set; }
        public double MinimumNumericCoverageRatio { get; set; }
        public bool MeanImputationEnabled { get; set; }
        public int ComponentCount { get; set; }
        public int MaxIterations { get; set; }
        public double ConvergenceTolerance { get; set; }
        public int NeighborCount { get; set; }
        public KnnSearchAlgorithm KnnSearchAlgorithm { get; set; }
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
            KnnSearchAlgorithm knnAlgorithm = analysisResult == null || analysisResult.Knn == null
                ? KnnSearchAlgorithm.Auto
                : analysisResult.Knn.ActualAlgorithm;
            string knnReason = analysisResult == null || analysisResult.Knn == null
                ? string.Empty
                : analysisResult.Knn.SelectionReason;
            string shapeCode = ResolveShapeCode(rowCount, featureCount, pc1, pc2);
            string compactText = string.Format(
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
                    // 학습한 평균과 표준편차로 같은 기준의 z값을 만든다.
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



    #region PCA Projection Model - Accord result adapter only

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

        internal static PcaProjectionModel FromComponents(
            double[][] components,
            double[] eigenValues,
            double[] ratios,
            StandardScalerModel scaler)
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

        public double[][] Transform(double[][] standardizedMatrix)
        {
            var scores = new double[standardizedMatrix.Length][];
            for (int row = 0; row < standardizedMatrix.Length; row++)
            {
                scores[row] = new double[Components.Length];
                for (int component = 0; component < Components.Length; component++)
                {
                    scores[row][component] = Dot(standardizedMatrix[row], Components[component]);
                }
            }

            return scores;
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

        public KnnSimilarityService(
            string[] draftNos,
            double[][] standardizedMatrix,
            StandardScalerModel scaler)
            : this(draftNos, standardizedMatrix, scaler, KnnSearchAlgorithm.Auto)
        {
        }

        public KnnSimilarityService(
            string[] draftNos,
            double[][] standardizedMatrix,
            StandardScalerModel scaler,
            KnnSearchAlgorithm requestedAlgorithm)
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
                throw new KeyNotFoundException("議댁옱?섏? ?딅뒗 Draft_NO?낅땲?? " + (draftNo ?? string.Empty));
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

        private static KnnSearchAlgorithm ResolveAlgorithm(
            KnnSearchAlgorithm requestedAlgorithm,
            int rowCount,
            int dimensionCount,
            out string reason)
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

        private static IKnnSearchIndex CreateSearchIndex(
            KnnSearchAlgorithm algorithm,
            double[][] matrix,
            string[] draftNos)
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
                double difference = left[index] - right[index];
                squaredDistance += difference * difference;
            }

            return squaredDistance;
        }

        private static int CompareCandidate(
            NeighborCandidate left,
            NeighborCandidate right,
            string[] draftNumbers)
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

            private void Search(
                KdNode node,
                int targetIndex,
                double[] target,
                NeighborCandidateQueue queue)
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

            private void Search(
                BallNode node,
                int targetIndex,
                double[] target,
                NeighborCandidateQueue queue)
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

}
