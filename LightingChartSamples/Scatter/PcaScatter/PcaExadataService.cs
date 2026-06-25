using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace LightingChartSamples.Scatter
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
        public PcaExadataSourceRow(
            int sourceRowIndex,
            string draftNo,
            PcaParameterType parameterType,
            string labelY,
            string rawConvExperimentJson)
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
        public PcaExadataSnapshot(
            IEnumerable<PcaExadataSourceRow> rows,
            DateTime loadedAtUtc)
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
        internal PcaExadataAnalysisResult(
            PcaExadataSnapshot snapshot,
            PcaParameterType parameterType,
            PcaAnalysisResult analysisResult,
            IList<PcaExperimentRecord> records,
            int missingExperimentCount)
        {
            Snapshot = snapshot;
            ParameterType = parameterType;
            AnalysisResult = analysisResult;
            Records = new ReadOnlyCollection<PcaExperimentRecord>(
                (records ?? new List<PcaExperimentRecord>()).ToList());
            MissingExperimentCount = missingExperimentCount;
        }

        public PcaExadataSnapshot Snapshot { get; private set; }
        public PcaParameterType ParameterType { get; private set; }
        public PcaAnalysisResult AnalysisResult { get; private set; }
        public IList<PcaExperimentRecord> Records { get; private set; }
        public int MissingExperimentCount { get; private set; }
    }

    public sealed class PcaDraftQueryResult
    {
        internal PcaDraftQueryResult(
            PcaExadataAnalysisResult analysis,
            PcaExperimentRecord target,
            IList<KnnNeighbor> neighbors,
            bool usedMemorySnapshot)
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
        private readonly JavaScriptSerializer serializer;

        public ConvExperimentRowParser()
        {
            serializer = new JavaScriptSerializer
            {
                MaxJsonLength = int.MaxValue,
                RecursionLimit = 256
            };
        }

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
                root = serializer.DeserializeObject(
                    source.RawConvExperimentJson.Trim().TrimStart('\uFEFF'));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
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
                        "CONV_EXPER_CTN[{0}]의 배열 원소가 JSON 객체가 아닙니다. DRAFT_NO={1}",
                        source.SourceRowIndex,
                        source.DraftNo));
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

        private static void Flatten(
            IDictionary<string, object> source,
            IDictionary<string, object> target,
            string prefix,
            int depth)
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
            if (value == null || value is bool || value is string)
            {
                return false;
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

        private static Func<IList<PcaExadataSourceRow>> ValidateRowLoader(
            Func<IList<PcaExadataSourceRow>> rowLoader)
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

        public Task<PcaExadataAnalysisResult> RefreshAndAnalyzeAsync(
            PcaParameterType parameterType,
            PcaScatterAnalysisOptions analysisOptions)
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

        public Task<PcaDraftQueryResult> QueryDraftAsync(
            string draftNo,
            PcaParameterType parameterType,
            PcaExadataRefreshMode refreshMode)
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

        public PcaExadataAnalysisResult AnalyzeSnapshot(
            PcaExadataSnapshot snapshot,
            PcaParameterType parameterType,
            PcaScatterAnalysisOptions analysisOptions)
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

        private PcaExadataSnapshot ResolveSnapshot(
            PcaExadataRefreshMode refreshMode,
            out bool usedMemorySnapshot)
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
                    "선택한 PARAM_TYP에서 DRAFT_NO가 중복되었습니다: " + duplicateDraft);
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

            var serializer = new JavaScriptSerializer
            {
                MaxJsonLength = int.MaxValue,
                RecursionLimit = 128
            };
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

                return serializer.Serialize(row);
            }).ToList();

            PcaAnalysisOptions pipelineOptions =
                (analysisOptions ?? new PcaScatterAnalysisOptions()).ToPipelineOptions();
            PcaAnalysisResult analysis = new PcaAnalysisPipeline(pipelineOptions).Analyze(normalizedRows);
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
                    "DRAFT_NO: {0}\r\nPARAM_TYP: {1}\r\nY: {2}",
                    record.DraftNo,
                    sample.ParameterType,
                    record.LabelY);
            }

            return new PcaExadataAnalysisResult(
                snapshot,
                parameterType,
                analysis,
                records,
                missingCount);
        }
    }
}
