using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.ManualPcaScatter
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
            : this(sourceRowIndex, draftNo, parameterType, string.Empty, labelY, rawConvExperimentJson)
        {
        }

        public PcaExadataSourceRow(int sourceRowIndex, string draftNo, PcaParameterType parameterType, string aiResultValue, string labelY, string rawConvExperimentJson)
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
        public PcaParameterType ParameterType { get; private set; }
        public string AiResultValue { get; private set; }
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

        internal PcaExperimentRecord(
            PcaExadataSourceRow source,
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
        public PcaParameterType ParameterType { get; private set; }
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

    public sealed class PcaExadataAnalysisResult
    {
        internal PcaExadataAnalysisResult(
            PcaExadataSnapshot snapshot,
            PcaParameterType parameterType,
            PcaAnalysisResult analysisResult,
            IList<PcaExperimentRecord> records,
            int missingExperimentCount,
            PcaFeatureSelectionReport featureSelectionReport)
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

            foreach (PcaExperimentRecord record in Records)
            {
                DataRow row = table.NewRow();
                row["DRAFT_NO"] = record.DraftNo;
                row["PARAM_TYP"] = PcaParameterTypeParser.ToDatabaseValue(record.ParameterType);
                row["CONV_EXPER_CTN"] = record.RawConvExperimentJson;
                row["AI_RSLT_VAL"] = record.AiResultValue;
                row["ENGR_RSLT_VAL"] = record.LabelY;
                row["X1"] = record.X1;
                row["X2"] = record.X2;
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
            : base("DRAFT_NO '" + (draftNo ?? string.Empty) + "'의 실험 데이터가 없습니다.")
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
        /// <summary>
        /// CONV_EXPER_CTN JSON 한 건을 PCA가 사용할 수 있는 원본값 사전과 수치 feature 사전으로 바꾼다.
        /// </summary>
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
                // Newtonsoft 기반 유틸로 JSON 문자열을 Dictionary/List 구조로 변환한다.
                root = PcaJsonUtility.DeserializeObject(
                    source.RawConvExperimentJson.Trim().TrimStart('\uFEFF'));
            }
            catch (Exception ex) when (PcaJsonUtility.IsJsonException(ex))
            {
                throw new FormatException(
                    string.Format(
                        "CONV_EXPER_CTN[{0}] JSON 파싱에 실패했습니다. DRAFT_NO={1}: {2}",
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
                        "CONV_EXPER_CTN[{0}]에는 실험 객체가 한 건이어야 합니다. DRAFT_NO={1}, Count={2}",
                        source.SourceRowIndex,
                        source.DraftNo,
                        items.Count));
            }

            var dictionary = items[0] as IDictionary<string, object>;
            if (dictionary == null)
            {
                throw new FormatException(
                    string.Format(
                        "CONV_EXPER_CTN[{0}]의 배열 요소가 JSON 객체가 아닙니다. DRAFT_NO={1}",
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
                // 메타데이터와 문자열 설명값은 제외하고, 유한한 숫자로 바뀌는 값만 PCA feature가 된다.
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
                throw new FormatException("CONV_EXPER_CTN의 JSON 중첩 깊이가 허용 범위를 초과했습니다.");
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

    public sealed class PcaExadataService
    {
        private sealed class DelegateRowRepository : IPcaExadataRowRepository
        {
            private readonly Func<IList<PcaExadataSourceRow>> loader;

            public DelegateRowRepository(Func<IList<PcaExadataSourceRow>> loader)
            {
                this.loader = loader;
            }

            public IList<PcaExadataSourceRow> LoadAll()
            {
                return loader();
            }
        }

        private readonly IPcaExadataRowRepository repository;
        private readonly object snapshotSync;
        private PcaExadataSnapshot currentSnapshot;

        public PcaExadataService()
            : this(new ConvExperimentRepository())
        {
        }

        public PcaExadataService(DataTable sourceTable)
            : this(new ConvExperimentRepository(sourceTable))
        {
        }

        public PcaExadataService(DataTable sourceTable, ConvExperimentQueryOptions tableOptions)
            : this(new ConvExperimentRepository(sourceTable, tableOptions))
        {
        }

        public PcaExadataService(IPcaExadataRowRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            this.repository = repository;
            snapshotSync = new object();
        }

        public PcaExadataService(Func<IList<PcaExadataSourceRow>> rowLoader)
            : this(new DelegateRowRepository(ValidateRowLoader(rowLoader)))
        {
        }

        private static Func<IList<PcaExadataSourceRow>> ValidateRowLoader(Func<IList<PcaExadataSourceRow>> rowLoader)
        {
            if (rowLoader == null)
            {
                throw new ArgumentNullException("rowLoader");
            }

            return rowLoader;
        }

        public PcaExadataSnapshot CurrentSnapshot
        {
            get
            {
                lock (snapshotSync)
                {
                    return currentSnapshot;
                }
            }
        }

        public void SetSnapshot(PcaExadataSnapshot snapshot)
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

        public PcaExadataSnapshot SetDataTable(DataTable sourceTable)
        {
            return SetDataTable(sourceTable, ConvExperimentQueryOptions.FromConfiguration());
        }

        public PcaExadataSnapshot SetDataTable(DataTable sourceTable, ConvExperimentQueryOptions tableOptions)
        {
            IList<PcaExadataSourceRow> rows = ConvExperimentRepository.LoadFromDataTable(
                sourceTable,
                tableOptions);
            var snapshot = new PcaExadataSnapshot(rows, DateTime.UtcNow);
            SetSnapshot(snapshot);
            return snapshot;
        }

        public Task<PcaExadataSnapshot> LoadAllAsync()
        {
            return Task.Run(delegate
            {
                IList<PcaExadataSourceRow> rows = repository.LoadAll();
                var snapshot = new PcaExadataSnapshot(rows, DateTime.UtcNow);
                lock (snapshotSync)
                {
                    currentSnapshot = snapshot;
                }

                return snapshot;
            });
        }

        public Task<PcaExadataSnapshot> LoadFromDataTableAsync(DataTable sourceTable)
        {
            return LoadFromDataTableAsync(
                sourceTable,
                ConvExperimentQueryOptions.FromConfiguration());
        }

        public Task<PcaExadataSnapshot> LoadFromDataTableAsync(DataTable sourceTable, ConvExperimentQueryOptions tableOptions)
        {
            return Task.Run(delegate
            {
                return SetDataTable(sourceTable, tableOptions);
            });
        }

        public Task<PcaExadataAnalysisResult> RefreshAndAnalyzeAsync(PcaParameterType parameterType, PcaScatterAnalysisOptions analysisOptions)
        {
            return Task.Run(delegate
            {
                IList<PcaExadataSourceRow> rows = repository.LoadAll();
                var snapshot = new PcaExadataSnapshot(rows, DateTime.UtcNow);
                PcaExadataAnalysisResult result = AnalyzePopulation(
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

        public Task<PcaExadataAnalysisResult> AnalyzeDataTableAsync(DataTable sourceTable, PcaParameterType parameterType, PcaScatterAnalysisOptions analysisOptions)
        {
            return AnalyzeDataTableAsync(
                sourceTable,
                parameterType,
                analysisOptions,
                ConvExperimentQueryOptions.FromConfiguration());
        }

        public Task<PcaExadataAnalysisResult> AnalyzeDataTableAsync(
            DataTable sourceTable,
            PcaParameterType parameterType,
            PcaScatterAnalysisOptions analysisOptions,
            ConvExperimentQueryOptions tableOptions)
        {
            return Task.Run(delegate
            {
                PcaExadataSnapshot snapshot = SetDataTable(sourceTable, tableOptions);
                return AnalyzeSnapshot(snapshot, parameterType, analysisOptions);
            });
        }

        public Task<PcaDraftQueryResult> QueryDraftAsync(string draftNo, PcaParameterType parameterType, PcaExadataRefreshMode refreshMode)
        {
            return QueryDraftAsync(
                draftNo,
                parameterType,
                refreshMode,
                new PcaScatterAnalysisOptions());
        }

        public Task<PcaDraftQueryResult> QueryDraftAsync(
            string draftNo,
            PcaParameterType parameterType,
            PcaExadataRefreshMode refreshMode,
            PcaScatterAnalysisOptions analysisOptions)
        {
            string resolvedDraftNo = (draftNo ?? string.Empty).Trim();
            if (resolvedDraftNo.Length == 0)
            {
                throw new ArgumentException("조회할 DRAFT_NO를 입력해야 합니다.", "draftNo");
            }

            return Task.Run(delegate
            {
                bool usedMemorySnapshot;
                PcaExadataSnapshot snapshot = ResolveSnapshot(refreshMode, out usedMemorySnapshot);
                IList<PcaExadataSourceRow> population = FilterPopulation(snapshot, parameterType);
                PcaExadataSourceRow targetSource = population.FirstOrDefault(row =>
                    string.Equals(row.DraftNo, resolvedDraftNo, StringComparison.OrdinalIgnoreCase));
                if (targetSource == null)
                {
                    throw new KeyNotFoundException(
                        string.Format(
                            "선택한 PARAM_TYP '{0}'에 DRAFT_NO '{1}'가 없습니다.",
                            PcaParameterTypeParser.ToDatabaseValue(parameterType),
                            resolvedDraftNo));
                }

                PcaExadataAnalysisResult analysis = AnalyzePopulation(
                    snapshot,
                    parameterType,
                    population,
                    analysisOptions,
                    targetSource.DraftNo);
                PcaExperimentRecord target = analysis.Records.First(record =>
                    string.Equals(record.DraftNo, resolvedDraftNo, StringComparison.OrdinalIgnoreCase));
                IList<KnnNeighbor> neighbors = analysis.AnalysisResult.FindNearest(
                    target.DraftNo,
                    Math.Max(1, (analysisOptions ?? new PcaScatterAnalysisOptions()).NeighborCount));
                if (!usedMemorySnapshot)
                {
                    lock (snapshotSync)
                    {
                        currentSnapshot = snapshot;
                    }
                }

                return new PcaDraftQueryResult(
                    analysis,
                    target,
                    neighbors,
                    usedMemorySnapshot);
            });
        }

        public Task<PcaDraftQueryResult> QueryDraftFromDataTableAsync(
            string draftNo,
            PcaParameterType parameterType,
            DataTable sourceTable,
            PcaScatterAnalysisOptions analysisOptions)
        {
            return QueryDraftFromDataTableAsync(
                draftNo,
                parameterType,
                sourceTable,
                analysisOptions,
                ConvExperimentQueryOptions.FromConfiguration());
        }

        public Task<PcaDraftQueryResult> QueryDraftFromDataTableAsync(
            string draftNo,
            PcaParameterType parameterType,
            DataTable sourceTable,
            PcaScatterAnalysisOptions analysisOptions,
            ConvExperimentQueryOptions tableOptions)
        {
            string resolvedDraftNo = (draftNo ?? string.Empty).Trim();
            if (resolvedDraftNo.Length == 0)
            {
                throw new ArgumentException("조회할 DRAFT_NO를 입력해야 합니다.", "draftNo");
            }

            return Task.Run(delegate
            {
                PcaExadataSnapshot snapshot = SetDataTable(sourceTable, tableOptions);
                IList<PcaExadataSourceRow> population = FilterPopulation(snapshot, parameterType);
                PcaExadataSourceRow targetSource = population.FirstOrDefault(row =>
                    string.Equals(row.DraftNo, resolvedDraftNo, StringComparison.OrdinalIgnoreCase));
                if (targetSource == null)
                {
                    throw new KeyNotFoundException(
                        string.Format(
                            "선택한 PARAM_TYP '{0}'에 DRAFT_NO '{1}'가 없습니다.",
                            PcaParameterTypeParser.ToDatabaseValue(parameterType),
                            resolvedDraftNo));
                }

                PcaScatterAnalysisOptions effectiveAnalysisOptions =
                    analysisOptions ?? new PcaScatterAnalysisOptions();
                PcaExadataAnalysisResult analysis = AnalyzePopulation(
                    snapshot,
                    parameterType,
                    population,
                    effectiveAnalysisOptions,
                    targetSource.DraftNo);
                PcaExperimentRecord target = analysis.Records.First(record =>
                    string.Equals(record.DraftNo, resolvedDraftNo, StringComparison.OrdinalIgnoreCase));
                IList<KnnNeighbor> neighbors = analysis.AnalysisResult.FindNearest(
                    target.DraftNo,
                    Math.Max(1, effectiveAnalysisOptions.NeighborCount));

                return new PcaDraftQueryResult(
                    analysis,
                    target,
                    neighbors,
                    false);
            });
        }

        public PcaExadataAnalysisResult AnalyzeSnapshot(PcaExadataSnapshot snapshot, PcaParameterType parameterType, PcaScatterAnalysisOptions analysisOptions)
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

        private PcaExadataSnapshot ResolveSnapshot(PcaExadataRefreshMode refreshMode, out bool usedMemorySnapshot)
        {
            if (refreshMode == PcaExadataRefreshMode.PreferMemorySnapshot)
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

            IList<PcaExadataSourceRow> rows = repository.LoadAll();
            usedMemorySnapshot = false;
            return new PcaExadataSnapshot(rows, DateTime.UtcNow);
        }

        private static IList<PcaExadataSourceRow> FilterPopulation(PcaExadataSnapshot snapshot, PcaParameterType parameterType)
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
            PcaScatterAnalysisOptions analysisOptions,
            string requiredDraftNo)
        {
            // 1단계: DB 행의 CONV_EXPER_CTN JSON을 파싱해 Draft별 실험 객체와 수치 feature 사전을 만든다.
            var parser = new ConvExperimentRowParser();
            var parsed = new List<ParsedPcaExperiment>();
            int missingCount = 0;
            foreach (PcaExadataSourceRow source in population)
            {
                ParsedPcaExperiment experiment;
                if (!parser.TryParse(source, out experiment))
                {
                    missingCount++;
                    if (!string.IsNullOrEmpty(requiredDraftNo)
                        && string.Equals(source.DraftNo, requiredDraftNo, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new PcaExperimentDataMissingException(source.DraftNo);
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
                throw new PcaExperimentDataMissingException(requiredDraftNo);
            }

            if (parsed.Count < 3)
            {
                throw new InvalidOperationException(
                    "PCA 분석에는 실험 데이터가 있는 행이 최소 3건 필요합니다.");
            }

            // 2단계: 파싱된 수치 feature를 PCA 파이프라인이 받는 JSON row 형식으로 정규화한다.
            // Draft_NO와 AI_RSLT_Val은 식별/라벨로만 쓰고, 실제 PCA feature에서는 제외된다.
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

                return PcaJsonUtility.SerializeObject(row);
            }).ToList();

            PcaAnalysisOptions pipelineOptions =
                (analysisOptions ?? new PcaScatterAnalysisOptions()).ToPipelineOptions();
            // 3단계: 수치 feature 선택, 정규화, PCA 좌표 생성, KNN 인덱스 생성을 한 번에 수행한다.
            PcaAnalysisResult analysis = new PcaAnalysisPipeline(pipelineOptions).Analyze(normalizedRows);
            PcaFeatureSelectionReport featureSelectionReport =
                PcaFeatureSelectionReport.CreateFromParsedExperiments(
                    parsed,
                    analysis.FeatureNames,
                    pipelineOptions.ConstantVarianceThreshold);
            analysis.FeatureSelectionReport = featureSelectionReport;
            var records = new List<PcaExperimentRecord>(parsed.Count);
            for (int index = 0; index < parsed.Count; index++)
            {
                ScatterSampleData sample = analysis.ScatterData[index];
                ParsedPcaExperiment source = parsed[index];
                // 4단계: 화면 좌표(X1/X2), 원본 feature, 정규화 벡터를 한 record에 묶어 클릭/로그/그리드에서 재사용한다.
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
                    "DRAFT_NO: {0}\r\nPARAM_TYP: {1}\r\nY: {2}",
                    record.DraftNo,
                    sample.ParameterType,
                    string.IsNullOrWhiteSpace(record.LabelY) ? "-" : record.LabelY);
            }

            return new PcaExadataAnalysisResult(
                snapshot,
                parameterType,
                analysis,
                records,
                missingCount,
                featureSelectionReport);
        }
    }
}
