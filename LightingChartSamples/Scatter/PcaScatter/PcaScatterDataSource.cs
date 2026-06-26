using System;
using System.Collections.Generic;
using System.Data;
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
            JsonColumnName = "CONV_EXPER_CTN";
            DraftNoColumnName = "DRAFT_NO";
            ParameterTypeColumnName = "PARAM_TYP";
            LabelColumnName = "LABEL_Y";
            ParameterType = PcaParameterType.Response;
        }

        public DataTable SourceTable { get; set; }
        public string JsonColumnName { get; set; }
        public string DraftNoColumnName { get; set; }
        public string ParameterTypeColumnName { get; set; }
        public string LabelColumnName { get; set; }
        public PcaParameterType ParameterType { get; set; }

        public static PcaScatterExadataOptions CreateDefault()
        {
            ConvExperimentQueryOptions configured = ConvExperimentQueryOptions.FromConfiguration();
            return new PcaScatterExadataOptions
            {
                JsonColumnName = configured.JsonColumnName,
                DraftNoColumnName = configured.DraftNoColumnName,
                ParameterTypeColumnName = configured.ParameterTypeColumnName,
                LabelColumnName = configured.LabelColumnName,
                ParameterType = PcaParameterType.Response
            };
        }

        public static PcaScatterExadataOptions FromDataTable(DataTable sourceTable)
        {
            return new PcaScatterExadataOptions
            {
                SourceTable = sourceTable
            };
        }

        public ConvExperimentQueryOptions ToQueryOptions()
        {
            return new ConvExperimentQueryOptions
            {
                JsonColumnName = string.IsNullOrWhiteSpace(JsonColumnName)
                    ? "CONV_EXPER_CTN"
                    : JsonColumnName.Trim(),
                DraftNoColumnName = string.IsNullOrWhiteSpace(DraftNoColumnName)
                    ? "DRAFT_NO"
                    : DraftNoColumnName.Trim(),
                ParameterTypeColumnName = string.IsNullOrWhiteSpace(ParameterTypeColumnName)
                    ? "PARAM_TYP"
                    : ParameterTypeColumnName.Trim(),
                LabelColumnName = string.IsNullOrWhiteSpace(LabelColumnName)
                    ? "LABEL_Y"
                    : LabelColumnName.Trim()
            };
        }
    }

    public sealed class PcaScatterDatabaseOptions
    {
        public PcaScatterDatabaseOptions()
        {
            ActDataColumnName = "ACT_DATA";
        }

        public DataTable SourceTable { get; set; }
        public string ActDataColumnName { get; set; }

        public static PcaScatterDatabaseOptions CreateDefault()
        {
            return new PcaScatterDatabaseOptions();
        }

        public static PcaScatterDatabaseOptions FromDataTable(DataTable sourceTable)
        {
            return new PcaScatterDatabaseOptions
            {
                SourceTable = sourceTable
            };
        }

        internal ActDataQueryOptions ToActDataQueryOptions()
        {
            return new ActDataQueryOptions
            {
                ActDataColumnName = string.IsNullOrWhiteSpace(ActDataColumnName)
                    ? "ACT_DATA"
                    : ActDataColumnName.Trim()
            };
        }
    }
}
