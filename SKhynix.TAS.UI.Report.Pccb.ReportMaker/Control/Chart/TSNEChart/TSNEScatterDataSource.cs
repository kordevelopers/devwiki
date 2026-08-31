using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public enum TSNEScatterDataSourceKind
    {
        JsonSamples,
        ActDataJsonDocuments,
        ConvExperimentJsonDocuments,
        AnalysisResult
    }

    public sealed class TSNEScatterDataSource
    {
        private readonly IList<string> documents;
        private readonly TSNEAnalysisResult analysisResult;

        private TSNEScatterDataSource(TSNEScatterDataSourceKind kind, IEnumerable<string> documents, TSNEAnalysisResult analysisResult)
        {
            Kind = kind;
            this.documents = documents == null
                ? new List<string>()
                : documents.ToList();
            this.analysisResult = analysisResult;
        }

        public TSNEScatterDataSourceKind Kind { get; private set; }

        public static TSNEScatterDataSource FromJsonSamples(IEnumerable<string> jsonSamples)
        {
            return new TSNEScatterDataSource(TSNEScatterDataSourceKind.JsonSamples, jsonSamples, null);
        }

        public static TSNEScatterDataSource FromActDataJson(IEnumerable<string> actDataDocuments)
        {
            return new TSNEScatterDataSource(TSNEScatterDataSourceKind.ActDataJsonDocuments, actDataDocuments, null);
        }

        public static TSNEScatterDataSource FromActDataJson(string actDataDocument)
        {
            return FromActDataJson(new[] { actDataDocument });
        }

        public static TSNEScatterDataSource FromConvExperimentJson(IEnumerable<string> convExperimentDocuments)
        {
            return new TSNEScatterDataSource(
                TSNEScatterDataSourceKind.ConvExperimentJsonDocuments,
                convExperimentDocuments,
                null);
        }

        public static TSNEScatterDataSource FromAnalysisResult(TSNEAnalysisResult analysisResult)
        {
            if (analysisResult == null)
            {
                throw new ArgumentNullException("analysisResult");
            }

            return new TSNEScatterDataSource(TSNEScatterDataSourceKind.AnalysisResult, null, analysisResult);
        }

        public TSNEAnalysisResult Analyze(TSNEScatterAnalysisOptions analysisOptions)
        {
            if (Kind == TSNEScatterDataSourceKind.AnalysisResult)
            {
                return analysisResult;
            }

            TSNEAnalysisOptions pipelineOptions = (analysisOptions ?? new TSNEScatterAnalysisOptions()).ToPipelineOptions();
            // This assembly is intentionally t-SNE-only; callers cannot switch the projection back to TSNE.
            pipelineOptions.ProjectionMethod = DimensionalityReductionMethod.TSNE;
            TSNEAnalysisPipeline pipeline = new TSNEAnalysisPipeline(pipelineOptions);
            if (Kind == TSNEScatterDataSourceKind.ActDataJsonDocuments)
            {
                return pipeline.AnalyzeActDataDocuments(documents);
            }

            if (Kind == TSNEScatterDataSourceKind.ConvExperimentJsonDocuments)
            {
                return pipeline.AnalyzeConvExperimentDocuments(documents);
            }

            return pipeline.Analyze(documents);
        }
    }

    public sealed class TSNEScatterExadataOptions
    {
        public TSNEScatterExadataOptions()
        {
            JsonColumnName = "CONV_EXPER_CTN";
            DraftNoColumnName = "DRAFT_NO";
            ParameterTypeColumnName = "PARAM_TYP";
            AiResultColumnName = "AI_RSLT_VAL";
            LabelColumnName = "ENGR_RSLT_VAL";
            ParameterType = TSNEParameterType.Response;
        }

        public DataTable SourceTable { get; set; }
        public string JsonColumnName { get; set; }
        public string DraftNoColumnName { get; set; }
        public string ParameterTypeColumnName { get; set; }
        public string AiResultColumnName { get; set; }
        public string LabelColumnName { get; set; }
        public TSNEParameterType ParameterType { get; set; }

        public static TSNEScatterExadataOptions CreateDefault()
        {
            ConvExperimentQueryOptions configured = ConvExperimentQueryOptions.FromConfiguration();
            return new TSNEScatterExadataOptions
            {
                JsonColumnName = configured.JsonColumnName,
                DraftNoColumnName = configured.DraftNoColumnName,
                ParameterTypeColumnName = configured.ParameterTypeColumnName,
                AiResultColumnName = configured.AiResultColumnName,
                LabelColumnName = configured.LabelColumnName,
                ParameterType = TSNEParameterType.Response
            };
        }

        public static TSNEScatterExadataOptions FromDataTable(DataTable sourceTable)
        {
            return new TSNEScatterExadataOptions
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
                AiResultColumnName = string.IsNullOrWhiteSpace(AiResultColumnName)
                    ? "AI_RSLT_VAL"
                    : AiResultColumnName.Trim(),
                LabelColumnName = string.IsNullOrWhiteSpace(LabelColumnName)
                    ? "ENGR_RSLT_VAL"
                    : LabelColumnName.Trim()
            };
        }
    }

    public sealed class TSNEScatterDatabaseOptions
    {
        public TSNEScatterDatabaseOptions()
        {
            ActDataColumnName = "ACT_DATA";
        }

        public DataTable SourceTable { get; set; }
        public string ActDataColumnName { get; set; }

        public static TSNEScatterDatabaseOptions CreateDefault()
        {
            return new TSNEScatterDatabaseOptions();
        }

        public static TSNEScatterDatabaseOptions FromDataTable(DataTable sourceTable)
        {
            return new TSNEScatterDatabaseOptions
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





