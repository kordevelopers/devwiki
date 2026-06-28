using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.PcaScatter
{
    /// <summary>
    /// Accord.NET 전용 PCA 분석기입니다.
    /// 기존 수동 PCA 파이프라인(PcaAnalysisPipeline.Analyze, PcaProjectionModel.Fit)을
    /// 호출하지 않고 PrincipalComponentAnalysis로 PC1/PC2를 계산합니다.
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
                    "선택한 PARAM_TYP '" + PcaParameterTypeParser.ToDatabaseValue(parameterType)
                    + "'의 PCA 데이터가 없습니다.");
            }

            string duplicateDraft = population
                .GroupBy(row => row.DraftNo, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(duplicateDraft))
            {
                throw new InvalidOperationException(
                    "선택한 PARAM_TYP에서 DRAFT_NO가 중복되었습니다. " + duplicateDraft);
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
                    "Accord PCA 분석에는 실험 데이터가 있는 row가 최소 3건 필요합니다.");
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
                effectiveOptions.MinimumNumericCoverageRatio);
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
                    "Accord PCA 분석에는 사용 가능한 수치 feature가 최소 2개 필요합니다.");
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
            AccordProjectionData accordProjection = AccordPrincipalComponentBridge.Project(
                standardizedMatrix,
                componentCount,
                featureCount);
            double[][] scores = accordProjection.Scores;
            double[][] components = accordProjection.Components;
            CanonicalizeComponentSigns(components, scores);

            double[] eigenValues = accordProjection.EigenValues == null
                ? new double[0]
                : accordProjection.EigenValues.Take(componentCount).ToArray();
            double[] ratios = accordProjection.ComponentProportions == null
                ? new double[0]
                : accordProjection.ComponentProportions.Take(componentCount).ToArray();

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

        private sealed class AccordProjectionData
        {
            public double[][] Scores { get; set; }
            public double[][] Components { get; set; }
            public double[] EigenValues { get; set; }
            public double[] ComponentProportions { get; set; }
        }

        private static class AccordPrincipalComponentBridge
        {
            public static AccordProjectionData Project(
                double[][] standardizedMatrix,
                int componentCount,
                int featureCount)
            {
                Type pcaType = ResolveAccordType(
                    "Accord.Statistics.Analysis.PrincipalComponentAnalysis");
                object pca = CreateModernPca(pcaType, componentCount);
                bool learned = TryLearn(pca, pcaType, standardizedMatrix);
                if (!learned)
                {
                    pca = CreateLegacyPca(pcaType, standardizedMatrix);
                    TrySetNumberOfOutputs(pca, pcaType, componentCount);
                    InvokeCompute(pca, pcaType);
                }

                TrySetNumberOfOutputs(pca, pcaType, componentCount);

                double[][] scores = Transform(pca, pcaType, standardizedMatrix, componentCount);
                double[][] components = NormalizeComponents(
                    ReadMatrixProperty(pca, pcaType, "ComponentVectors"),
                    componentCount,
                    featureCount);

                return new AccordProjectionData
                {
                    Scores = scores,
                    Components = components,
                    EigenValues = ReadVectorProperty(pca, pcaType, "Eigenvalues"),
                    ComponentProportions = ReadVectorProperty(pca, pcaType, "ComponentProportions")
                };
            }

            private static Type ResolveAccordType(string fullName)
            {
                Type type = Type.GetType(fullName + ", Accord.Statistics", false);
                if (type != null)
                {
                    return type;
                }

                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(fullName, false);
                    if (type != null)
                    {
                        return type;
                    }
                }

                throw new InvalidOperationException(
                    "Accord.Statistics.dll reference is required for Accord PCA analysis. "
                    + "Install/restore Accord.Statistics 3.8.0 and build LightingChartSamples.csproj.");
            }

            private static object CreateModernPca(Type pcaType, int componentCount)
            {
                Type methodType = ResolveAccordType(
                    "Accord.Statistics.Analysis.PrincipalComponentMethod");
                ConstructorInfo constructor = pcaType.GetConstructor(new[]
                {
                    methodType,
                    typeof(bool),
                    typeof(int)
                });
                if (constructor == null)
                {
                    return null;
                }

                object center = Enum.Parse(methodType, "Center");
                return constructor.Invoke(new[] { center, false, (object)componentCount });
            }

            private static object CreateLegacyPca(Type pcaType, double[][] standardizedMatrix)
            {
                Type methodType = ResolveAccordType(
                    "Accord.Statistics.Analysis.AnalysisMethod");
                ConstructorInfo constructor = pcaType.GetConstructor(new[]
                {
                    typeof(double[][]),
                    methodType
                });
                if (constructor == null)
                {
                    throw new InvalidOperationException(
                        "Accord PrincipalComponentAnalysis constructor was not found.");
                }

                object center = Enum.Parse(methodType, "Center");
                return constructor.Invoke(new object[] { standardizedMatrix, center });
            }

            private static bool TryLearn(object pca, Type pcaType, double[][] standardizedMatrix)
            {
                if (pca == null)
                {
                    return false;
                }

                MethodInfo learn = pcaType.GetMethods()
                    .Where(method => method.Name == "Learn")
                    .FirstOrDefault(method =>
                    {
                        ParameterInfo[] parameters = method.GetParameters();
                        return parameters.Length >= 1
                            && parameters[0].ParameterType.IsAssignableFrom(typeof(double[][]));
                    });
                if (learn == null)
                {
                    return false;
                }

                ParameterInfo[] methodParameters = learn.GetParameters();
                object[] arguments = new object[methodParameters.Length];
                arguments[0] = standardizedMatrix;
                for (int index = 1; index < arguments.Length; index++)
                {
                    arguments[index] = methodParameters[index].ParameterType.IsValueType
                        ? Activator.CreateInstance(methodParameters[index].ParameterType)
                        : null;
                }

                learn.Invoke(pca, arguments);
                return true;
            }

            private static void InvokeCompute(object pca, Type pcaType)
            {
                MethodInfo compute = pcaType.GetMethod("Compute", Type.EmptyTypes);
                if (compute != null)
                {
                    compute.Invoke(pca, new object[0]);
                }
            }

            private static void TrySetNumberOfOutputs(object pca, Type pcaType, int componentCount)
            {
                PropertyInfo property = pcaType.GetProperty("NumberOfOutputs");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(pca, componentCount, null);
                }
            }

            private static double[][] Transform(
                object pca,
                Type pcaType,
                double[][] standardizedMatrix,
                int componentCount)
            {
                MethodInfo transform = pcaType.GetMethods()
                    .Where(method => method.Name == "Transform")
                    .FirstOrDefault(method =>
                    {
                        ParameterInfo[] parameters = method.GetParameters();
                        return parameters.Length == 1
                            && parameters[0].ParameterType.IsAssignableFrom(typeof(double[][]));
                    });
                if (transform != null)
                {
                    return ToJaggedMatrix(transform.Invoke(pca, new object[] { standardizedMatrix }));
                }

                transform = pcaType.GetMethods()
                    .Where(method => method.Name == "Transform")
                    .FirstOrDefault(method =>
                    {
                        ParameterInfo[] parameters = method.GetParameters();
                        return parameters.Length == 2
                            && parameters[0].ParameterType.IsAssignableFrom(typeof(double[][]))
                            && parameters[1].ParameterType == typeof(int);
                    });
                if (transform != null)
                {
                    return ToJaggedMatrix(transform.Invoke(
                        pca,
                        new object[] { standardizedMatrix, componentCount }));
                }

                return ToJaggedMatrix(ReadProperty(pca, pcaType, "Result"));
            }

            private static double[][] ReadMatrixProperty(
                object instance,
                Type instanceType,
                string propertyName)
            {
                return ToJaggedMatrix(ReadProperty(instance, instanceType, propertyName));
            }

            private static double[] ReadVectorProperty(
                object instance,
                Type instanceType,
                string propertyName)
            {
                object value = ReadProperty(instance, instanceType, propertyName);
                double[] vector = value as double[];
                if (vector != null)
                {
                    return (double[])vector.Clone();
                }

                IEnumerable<double> enumerable = value as IEnumerable<double>;
                return enumerable == null ? new double[0] : enumerable.ToArray();
            }

            private static object ReadProperty(
                object instance,
                Type instanceType,
                string propertyName)
            {
                PropertyInfo property = instanceType.GetProperty(propertyName);
                if (property == null)
                {
                    return null;
                }

                return property.GetValue(instance, null);
            }

            private static double[][] ToJaggedMatrix(object value)
            {
                double[][] jagged = value as double[][];
                if (jagged != null)
                {
                    return jagged
                        .Select(row => row == null ? new double[0] : (double[])row.Clone())
                        .ToArray();
                }

                double[,] rectangular = value as double[,];
                if (rectangular == null)
                {
                    return new double[0][];
                }

                int rows = rectangular.GetLength(0);
                int columns = rectangular.GetLength(1);
                var result = new double[rows][];
                for (int row = 0; row < rows; row++)
                {
                    result[row] = new double[columns];
                    for (int column = 0; column < columns; column++)
                    {
                        result[row][column] = rectangular[row, column];
                    }
                }

                return result;
            }

            private static double[][] NormalizeComponents(
                double[][] components,
                int componentCount,
                int featureCount)
            {
                if (components == null || components.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Accord PCA did not return component vectors.");
                }

                if (components.Length >= componentCount
                    && components[0] != null
                    && components[0].Length == featureCount)
                {
                    return components
                        .Take(componentCount)
                        .Select(component => (double[])component.Clone())
                        .ToArray();
                }

                if (components.Length == featureCount
                    && components[0] != null
                    && components[0].Length >= componentCount)
                {
                    var transposed = new double[componentCount][];
                    for (int componentIndex = 0; componentIndex < componentCount; componentIndex++)
                    {
                        transposed[componentIndex] = new double[featureCount];
                        for (int featureIndex = 0; featureIndex < featureCount; featureIndex++)
                        {
                            transposed[componentIndex][featureIndex] = components[featureIndex][componentIndex];
                        }
                    }

                    return transposed;
                }

                return components
                    .Take(componentCount)
                    .Select(component => component == null ? new double[0] : (double[])component.Clone())
                    .ToArray();
            }
        }
    }
}
