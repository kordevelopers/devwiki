using System;
using System.Collections.Generic;
using System.Linq;

namespace LightingChartSamples.Scatter
{
    public enum PcaScatterDataSourceKind
    {
        JsonSamples,
        ActDataJsonDocuments,
        AnalysisResult
    }

    public sealed class PcaScatterDataSource
    {
        private readonly IList<string> documents;
        private readonly PcaAnalysisResult analysisResult;

        private PcaScatterDataSource(PcaScatterDataSourceKind kind, IEnumerable<string> documents, PcaAnalysisResult analysisResult)
        {
            Kind = kind;
            this.documents = documents == null
                ? new List<string>()
                : documents.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList();
            this.analysisResult = analysisResult;
        }

        public PcaScatterDataSourceKind Kind { get; private set; }

        public static PcaScatterDataSource FromJsonSamples(IEnumerable<string> jsonSamples)
        {
            return new PcaScatterDataSource(PcaScatterDataSourceKind.JsonSamples, jsonSamples, null);
        }

        public static PcaScatterDataSource FromActDataJson(IEnumerable<string> actDataDocuments)
        {
            return new PcaScatterDataSource(PcaScatterDataSourceKind.ActDataJsonDocuments, actDataDocuments, null);
        }

        public static PcaScatterDataSource FromActDataJson(string actDataDocument)
        {
            return FromActDataJson(new[] { actDataDocument });
        }

        public static PcaScatterDataSource FromAnalysisResult(PcaAnalysisResult analysisResult)
        {
            if (analysisResult == null)
            {
                throw new ArgumentNullException("analysisResult");
            }

            return new PcaScatterDataSource(PcaScatterDataSourceKind.AnalysisResult, null, analysisResult);
        }

        internal PcaAnalysisResult Analyze(PcaScatterAnalysisOptions analysisOptions)
        {
            if (Kind == PcaScatterDataSourceKind.AnalysisResult)
            {
                return analysisResult;
            }

            PcaAnalysisOptions pipelineOptions = (analysisOptions ?? new PcaScatterAnalysisOptions()).ToPipelineOptions();
            PcaAnalysisPipeline pipeline = new PcaAnalysisPipeline(pipelineOptions);
            if (Kind == PcaScatterDataSourceKind.ActDataJsonDocuments)
            {
                return pipeline.AnalyzeActDataDocuments(documents);
            }

            return pipeline.Analyze(documents);
        }
    }

    public sealed class PcaScatterDatabaseOptions
    {
        public PcaScatterDatabaseOptions()
        {
            ConnectionStringName = "AiInferenceDatabase";
            Query = "SELECT ACT_DATA FROM AI_INFERNECE";
            CommandTimeoutSeconds = 30;
        }

        public string ConnectionStringName { get; set; }
        public string Query { get; set; }
        public int CommandTimeoutSeconds { get; set; }

        public static PcaScatterDatabaseOptions CreateDefault()
        {
            return new PcaScatterDatabaseOptions();
        }

        internal ActDataQueryOptions ToActDataQueryOptions()
        {
            return new ActDataQueryOptions
            {
                ConnectionStringName = string.IsNullOrWhiteSpace(ConnectionStringName)
                    ? "AiInferenceDatabase"
                    : ConnectionStringName.Trim(),
                QueryText = string.IsNullOrWhiteSpace(Query)
                    ? "SELECT ACT_DATA FROM AI_INFERNECE"
                    : Query.Trim(),
                CommandTimeoutSeconds = Math.Max(1, CommandTimeoutSeconds)
            };
        }
    }
}
