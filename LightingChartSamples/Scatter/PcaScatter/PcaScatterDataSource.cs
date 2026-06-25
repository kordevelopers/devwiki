using System;
using System.Collections.Generic;
using System.Linq;

namespace LightingChartSamples.Scatter
{
    public enum PcaScatterDataSourceKind
    {
        JsonSamples,
        ActDataJsonDocuments,
        ConvExperimentJsonDocuments,
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
                : documents.ToList();
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

        public static PcaScatterDataSource FromConvExperimentJson(
            IEnumerable<string> convExperimentDocuments)
        {
            return new PcaScatterDataSource(
                PcaScatterDataSourceKind.ConvExperimentJsonDocuments,
                convExperimentDocuments,
                null);
        }

        public static PcaScatterDataSource FromAnalysisResult(PcaAnalysisResult analysisResult)
        {
            if (analysisResult == null)
            {
                throw new ArgumentNullException("analysisResult");
            }

            return new PcaScatterDataSource(PcaScatterDataSourceKind.AnalysisResult, null, analysisResult);
        }

        public PcaAnalysisResult Analyze(PcaScatterAnalysisOptions analysisOptions)
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

            if (Kind == PcaScatterDataSourceKind.ConvExperimentJsonDocuments)
            {
                return pipeline.AnalyzeConvExperimentDocuments(documents);
            }

            return pipeline.Analyze(documents);
        }
    }

    public sealed class PcaScatterExadataOptions
    {
        public PcaScatterExadataOptions()
        {
            ConnectionStringName = "PcaExadataDatabase";
            Query = new ConvExperimentQueryOptions().QueryText;
            JsonColumnName = "CONV_EXPER_CTN";
            CommandTimeoutSeconds = 120;
            ParameterType = PcaParameterType.Response;
        }

        public string ConnectionStringName { get; set; }
        public string Query { get; set; }
        public string JsonColumnName { get; set; }
        public int CommandTimeoutSeconds { get; set; }
        public PcaParameterType ParameterType { get; set; }

        public static PcaScatterExadataOptions CreateDefault()
        {
            ConvExperimentQueryOptions configured = ConvExperimentQueryOptions.FromConfiguration();
            return new PcaScatterExadataOptions
            {
                ConnectionStringName = configured.ConnectionStringName,
                Query = configured.QueryText,
                JsonColumnName = configured.JsonColumnName,
                CommandTimeoutSeconds = configured.CommandTimeoutSeconds,
                ParameterType = PcaParameterType.Response
            };
        }

        public ConvExperimentQueryOptions ToQueryOptions()
        {
            return new ConvExperimentQueryOptions
            {
                ConnectionStringName = string.IsNullOrWhiteSpace(ConnectionStringName)
                    ? "PcaExadataDatabase"
                    : ConnectionStringName.Trim(),
                QueryText = string.IsNullOrWhiteSpace(Query)
                    ? new ConvExperimentQueryOptions().QueryText
                    : Query.Trim(),
                JsonColumnName = string.IsNullOrWhiteSpace(JsonColumnName)
                    ? "CONV_EXPER_CTN"
                    : JsonColumnName.Trim(),
                CommandTimeoutSeconds = Math.Max(1, CommandTimeoutSeconds)
            };
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
