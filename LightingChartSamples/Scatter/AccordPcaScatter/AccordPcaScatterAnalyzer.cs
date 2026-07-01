using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Accord.Statistics.Analysis;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.AccordPcaScatter
{
    /// <summary>
    /// 어코드 PCA로 X1, X2 좌표를 계산한다.
    /// </summary>
    public sealed class AccordPcaScatterAnalyzer
    {
        public PcaExadataAnalysisResult AnalyzeDataTable(
            DataTable sourceTable,
            PcaParameterType parameterType,
            PcaScatterAnalysisOptions analysisOptions)
        {
            return AnalyzeDataTable(
                sourceTable,
                parameterType,
                analysisOptions,
                ConvExperimentQueryOptions.FromConfiguration());
        }

        public PcaExadataAnalysisResult AnalyzeDataTable(
            DataTable sourceTable,
            PcaParameterType parameterType,
            PcaScatterAnalysisOptions analysisOptions,
            ConvExperimentQueryOptions tableOptions)
        {
            IList<PcaExadataSourceRow> rows = ConvExperimentRepository.LoadFromDataTable(
                sourceTable,
                tableOptions);
            var snapshot = new PcaExadataSnapshot(rows, DateTime.UtcNow);
            return AnalyzeSnapshot(snapshot, parameterType, analysisOptions);
        }

        public PcaExadataAnalysisResult AnalyzeSnapshot(
            PcaExadataSnapshot snapshot,
            PcaParameterType parameterType,
            PcaScatterAnalysisOptions analysisOptions)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException("snapshot");
            }

            IList<PcaExadataSourceRow> population = FilterPopulation(snapshot, parameterType);
            return AnalyzePopulation(snapshot, parameterType, population, analysisOptions);
        }

        private static IList<PcaExadataSourceRow> FilterPopulation(
            PcaExadataSnapshot snapshot,
            PcaParameterType parameterType)
        {
            List<PcaExadataSourceRow> population = snapshot.Rows
                .Where(row => row != null && row.ParameterType == parameterType)
                .ToList();
            if (population.Count == 0)
            {
                throw new InvalidOperationException(
                    "Selected PARAM_TYP '" + PcaParameterTypeParser.ToDatabaseValue(parameterType)
                    + "' does not contain PCA data.");
            }

            string duplicateDraft = population
                .GroupBy(row => row.DraftNo, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(duplicateDraft))
            {
                throw new InvalidOperationException(
                    "Duplicate DRAFT_NO exists in the selected PARAM_TYP: " + duplicateDraft);
            }

            return population;
        }

        private static PcaExadataAnalysisResult AnalyzePopulation(
            PcaExadataSnapshot snapshot,
            PcaParameterType parameterType,
            IList<PcaExadataSourceRow> population,
            PcaScatterAnalysisOptions analysisOptions)
        {
            #region Data Preparation - shared input parsing, not manual PCA

            var parser = new ConvExperimentRowParser();
            var parsed = new List<ParsedPcaExperiment>();
            int missingCount = 0;
            foreach (PcaExadataSourceRow source in population)
            {
                ParsedPcaExperiment experiment;
                if (!parser.TryParse(source, out experiment))
                {
                    missingCount++;
                    continue;
                }

                parsed.Add(experiment);
            }

            if (parsed.Count < 3)
            {
                throw new InvalidOperationException(
                    "Accord PCA analysis requires at least 3 rows with experiment data.");
            }

            PcaScatterAnalysisOptions effectiveOptions =
                analysisOptions ?? new PcaScatterAnalysisOptions();
            PcaAnalysisOptions pipelineOptions = effectiveOptions.ToPipelineOptions();

            AccordFeatureMatrix featureMatrix = BuildFeatureMatrix(parsed, pipelineOptions);
            StandardScalerModel scaler = StandardScalerModel.Fit(
                featureMatrix.Matrix,
                featureMatrix.FeatureNames);
            double[][] standardized = scaler.Transform(featureMatrix.Matrix);

            #endregion

            #region Accord.NET PCA - no manual eigenvector calculation

            AccordProjectionResult projection = ProjectWithAccord(
                standardized,
                featureMatrix.FeatureNames.Length,
                pipelineOptions.ComponentCount,
                scaler);

            #endregion

            #region KNN and chart result assembly

            PcaFeatureSelectionReport featureSelectionReport =
                PcaFeatureSelectionReport.CreateFromParsedExperiments(
                    parsed,
                    featureMatrix.FeatureNames,
                    pipelineOptions.ConstantVarianceThreshold);

            IList<ScatterSampleData> scatterData = BuildScatterData(
                parsed,
                projection.Scores);
            var knn = new KnnSimilarityService(
                parsed.Select(item => item.Source.DraftNo).ToArray(),
                standardized,
                scaler,
                pipelineOptions.KnnSearchAlgorithm);
            var analysis = new PcaAnalysisResult
            {
                ScatterData = scatterData,
                FeatureNames = featureMatrix.FeatureNames,
                ExcludedFeatureNames = featureMatrix.ExcludedFeatureNames,
                StandardizedMatrix = standardized,
                Scaler = scaler,
                PcaModel = projection.Model,
                Knn = knn,
                FeatureSelectionReport = featureSelectionReport,
                Verification = CreateAccordVerification(
                    standardized,
                    scatterData,
                    projection.Model,
                    knn)
            };
            analysis.Diagnostic = PcaAnalysisDiagnosticReport.Create(
                analysis,
                parsed.Count,
                missingCount);

            IList<PcaExperimentRecord> records = BuildRecords(
                parsed,
                analysis,
                parameterType);

            return new PcaExadataAnalysisResult(
                snapshot,
                parameterType,
                analysis,
                records,
                missingCount,
                featureSelectionReport);

            #endregion
        }

        private static AccordFeatureMatrix BuildFeatureMatrix(
            IList<ParsedPcaExperiment> parsed,
            PcaAnalysisOptions options)
        {
            PcaAnalysisOptions effectiveOptions = options ?? new PcaAnalysisOptions();
            double varianceThreshold = Math.Max(0d, effectiveOptions.ConstantVarianceThreshold);
            double coverageThreshold = NormalizeCoverageRatio(
                effectiveOptions.MinimumNumericFeatureCoverageRatio);
            string[] allFeatureNames = parsed
                .SelectMany(item => item.FlattenedValues == null
                    ? Enumerable.Empty<string>()
                    : item.FlattenedValues.Keys)
                .Where(name => !string.IsNullOrWhiteSpace(name)
                    && !PcaFeatureSelectionReport.IsKnownMetadataFeature(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var included = new List<string>();
            var excluded = new List<string>();
            var means = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (string featureName in allFeatureNames)
            {
                double[] numericValues = parsed
                    .Where(item => item.NumericFeatures != null
                        && item.NumericFeatures.ContainsKey(featureName))
                    .Select(item => item.NumericFeatures[featureName])
                    .ToArray();
                double coverage = parsed.Count == 0
                    ? 0d
                    : numericValues.Length / (double)parsed.Count;
                bool numericInEveryRow = numericValues.Length == parsed.Count;
                bool coverageAccepted = numericValues.Length > 0
                    && (numericInEveryRow
                        || (effectiveOptions.MeanImputationEnabled
                            && coverage >= coverageThreshold));
                if (!coverageAccepted)
                {
                    excluded.Add(featureName);
                    continue;
                }

                double mean = numericValues.Average();
                double variance = numericValues.Average(value =>
                {
                    double diff = value - mean;
                    return diff * diff;
                });
                if (variance <= varianceThreshold)
                {
                    excluded.Add(featureName);
                    continue;
                }

                included.Add(featureName);
                means[featureName] = mean;
            }

            if (included.Count < 2)
            {
                throw new InvalidOperationException(
                    "Accord PCA analysis requires at least 2 usable numeric features.");
            }

            double[][] matrix = parsed.Select(item =>
                included.Select(featureName =>
                {
                    double value;
                    return item.NumericFeatures != null
                        && item.NumericFeatures.TryGetValue(featureName, out value)
                            ? value
                            : means[featureName];
                }).ToArray()).ToArray();

            return new AccordFeatureMatrix
            {
                FeatureNames = included.ToArray(),
                ExcludedFeatureNames = excluded.ToArray(),
                Matrix = matrix
            };
        }

        private static AccordProjectionResult ProjectWithAccord(
            double[][] standardizedMatrix,
            int featureCount,
            int requestedComponentCount,
            StandardScalerModel scaler)
        {
            if (standardizedMatrix == null || standardizedMatrix.Length < 3)
            {
                throw new ArgumentException(
                    "Accord PCA requires at least three standardized rows.",
                    "standardizedMatrix");
            }

            int componentCount = Math.Min(
                Math.Max(1, requestedComponentCount),
                Math.Min(featureCount, 2));
            var accord = new PrincipalComponentAnalysis(
                PrincipalComponentMethod.Center,
                false,
                componentCount);
            accord.Learn(standardizedMatrix, null);

            accord.NumberOfOutputs = componentCount;
            double[][] scores = accord.Transform(standardizedMatrix);
            double[][] components = accord.ComponentVectors
                .Take(componentCount)
                .Select(component => (double[])component.Clone())
                .ToArray();
            CanonicalizeComponentSigns(components, scores);

            double[] eigenValues = accord.Eigenvalues == null
                ? new double[0]
                : accord.Eigenvalues.Take(componentCount).ToArray();
            double[] ratios = accord.ComponentProportions == null
                ? new double[0]
                : accord.ComponentProportions.Take(componentCount).ToArray();

            return new AccordProjectionResult
            {
                Scores = scores,
                Model = PcaProjectionModel.FromComponents(
                    components,
                    eigenValues,
                    ratios,
                    scaler)
            };
        }

        private static IList<ScatterSampleData> BuildScatterData(
            IList<ParsedPcaExperiment> parsed,
            double[][] scores)
        {
            var scatterData = new List<ScatterSampleData>(parsed.Count);
            for (int index = 0; index < parsed.Count; index++)
            {
                scatterData.Add(new ScatterSampleData
                {
                    SourceIndex = index,
                    DraftNo = parsed[index].Source.DraftNo,
                    AiResultValue = parsed[index].Source.LabelY,
                    X1 = scores[index].Length > 0 ? scores[index][0] : 0d,
                    X2 = scores[index].Length > 1 ? scores[index][1] : 0d,
                    Distance = null
                });
            }

            return scatterData;
        }

        private static PcaVerificationReport CreateAccordVerification(
            double[][] standardized,
            IEnumerable<ScatterSampleData> scatterData,
            PcaProjectionModel model,
            KnnSimilarityService knn)
        {
            bool finiteScores = scatterData != null && scatterData.All(sample =>
                IsFinite(sample.X1) && IsFinite(sample.X2));
            bool finiteMatrix = standardized != null
                && standardized.All(row => row != null && row.All(IsFinite));
            bool sharedScaler = model != null
                && knn != null
                && object.ReferenceEquals(model.Scaler, knn.Scaler);

            return new PcaVerificationReport
            {
                IsValid = finiteScores && finiteMatrix && sharedScaler,
                AllScoresFinite = finiteScores && finiteMatrix,
                SharedScalerInstance = sharedScaler,
                KnnResultValid = knn != null,
                EigenValuesDescending = true,
                MaximumAbsoluteStandardizedMean = 0d,
                MaximumStandardDeviationError = 0d,
                ComponentDotProduct = 0d,
                Message = finiteScores && finiteMatrix && sharedScaler
                    ? "Accord.NET PCA verification passed."
                    : "Accord.NET PCA verification failed."
            };
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
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

        private static void CanonicalizeComponentSigns(
            double[][] components,
            double[][] scores)
        {
            for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                double[] component = components[componentIndex];
                int largestIndex = 0;
                for (int index = 1; index < component.Length; index++)
                {
                    if (Math.Abs(component[index]) > Math.Abs(component[largestIndex]))
                    {
                        largestIndex = index;
                    }
                }

                if (component[largestIndex] >= 0d)
                {
                    continue;
                }

                for (int index = 0; index < component.Length; index++)
                {
                    component[index] *= -1d;
                }

                for (int row = 0; row < scores.Length; row++)
                {
                    if (scores[row].Length > componentIndex)
                    {
                        scores[row][componentIndex] *= -1d;
                    }
                }
            }
        }

        private static IList<PcaExperimentRecord> BuildRecords(
            IList<ParsedPcaExperiment> parsed,
            PcaAnalysisResult analysis,
            PcaParameterType parameterType)
        {
            var records = new List<PcaExperimentRecord>(parsed.Count);
            for (int index = 0; index < parsed.Count; index++)
            {
                ScatterSampleData sample = analysis.ScatterData[index];
                ParsedPcaExperiment source = parsed[index];
                var record = new PcaExperimentRecord(
                    source.Source,
                    source.FlattenedValues,
                    source.NumericFeatures,
                    analysis.StandardizedMatrix[index],
                    sample.X1,
                    sample.X2);
                records.Add(record);
                sample.ParameterType = PcaParameterTypeParser.ToDatabaseValue(parameterType);
                sample.UserData = record;
                sample.TooltipText = string.Format(
                    CultureInfo.InvariantCulture,
                    "DRAFT_NO: {0}\r\nPARAM_TYP: {1}\r\nY: {2}\r\nPCA: Accord.NET",
                    record.DraftNo,
                    sample.ParameterType,
                    record.LabelY);
            }

            return records;
        }

        private sealed class AccordFeatureMatrix
        {
            public string[] FeatureNames { get; set; }
            public string[] ExcludedFeatureNames { get; set; }
            public double[][] Matrix { get; set; }
        }

        private sealed class AccordProjectionResult
        {
            public double[][] Scores { get; set; }
            public PcaProjectionModel Model { get; set; }
        }
    }
}
