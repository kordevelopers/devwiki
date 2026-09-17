using System;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart;
using Alternative = SKhynix.TAS.Analysis.Tsne.Tsne;

internal static class AlternativeTsneIntegrationVerification
{
    private static int assertions;
    public static int Run()
    {
        try
        {
            var table = new DataTable();
            foreach (string name in new[] { "DRAFT_NO", "PARAM_TYP", "AI_RSLT_VAL", "ENGR_RSLT_VAL", "CONV_EXPER_CTN" }) table.Columns.Add(name);
            for (int i = 0; i < 24; i++)
            {
                if (i == 5) table.Rows.Add("EMPTY", "Response", "", "", "[]");
                table.Rows.Add("D" + i.ToString("D3"), "Response", "AI" + i, i % 2 == 0 ? "Pass" : "Review", JsonConvert.SerializeObject(new {
                    F0 = Math.Sin(i * 0.4) + i * 0.01, F1 = Math.Cos(i * 0.17), F2 = i % 3 + i * 0.07, F3 = (i + 1) * (i + 1) * 0.031, constant = 4, note = "excluded"
                }));
            }
            string originalTable = JsonConvert.SerializeObject(table);
            var service = new TSNEExadataService(table);
            var snapshot = service.SetDataTable(table);
            TSNEAnalysisResult reference = null;
            double[][] firstCSharp = null;
            foreach (Alternative.Engine? engine in new Alternative.Engine?[] { null, Alternative.Engine.CSharp, Alternative.Engine.Hybrid, Alternative.Engine.CSharp })
            {
                var options = new TSNEScatterAnalysisOptions { TSNELibraryEngine = engine, TSNEIterations = 80, TSNEPerplexity = 30, TSNELearningRate = 50, TSNERandomSeed = 19, NeighborCount = 7 };
                Check(options.Clone().TSNELibraryEngine == engine, "Clone lost selected engine");
                var result = service.AnalyzeSnapshot(snapshot, TSNEParameterType.Response, options);
                var analysis = result.AnalysisResult;
                var coordinates = analysis.TSNEModel.Coordinates;
                string expectedEngine = !engine.HasValue ? "Accord" : (engine.Value == Alternative.Engine.CSharp ? "tsne-csharp" : "Hybrid");
                Check(analysis.TSNEModel.EngineName.Contains(expectedEngine), "selected engine ignored: " + expectedEngine);
                var diagnostic = result.Diagnostic;
                Check(diagnostic.CompactText.Contains("ENGINE=" + analysis.TSNEModel.EngineName + " SHAPE="), "diagnostic misreported actual engine: " + diagnostic.CompactText);
                Check(result.Records.Count == 24 && result.MissingExperimentCount == 1, "population filtering mismatch");
                Check(analysis.Verification.AllScoresFinite && analysis.Verification.SharedScalerInstance && analysis.Verification.KnnResultValid, "pipeline verification failed");
                var exported = result.CreateSurvivingPopulationDataTable();
                var raw = result.CreateRawDataTable();
                for (int i = 0; i < 24; i++)
                {
                    string draft = "D" + i.ToString("D3");
                    Check(result.Records[i].SourceRowIndex == (i < 5 ? i : i + 1), "source index lost after skipped row");
                    Check(result.Records[i].DraftNo == draft && analysis.ScatterData[i].DraftNo == draft && (string)exported.Rows[i]["DRAFT_NO"] == draft && (string)raw.Rows[i]["DRAFT_NO"] == draft, "draft/export row alignment mismatch");
                    Check(result.Records[i].X1 == coordinates[i][0] && result.Records[i].X2 == coordinates[i][1] && (double)exported.Rows[i]["X1"] == coordinates[i][0] && (double)raw.Rows[i]["X2"] == coordinates[i][1], "coordinate/export row alignment mismatch");
                }
                if (reference == null) reference = analysis;
                else
                {
                    Equal(reference.StandardizedMatrix, analysis.StandardizedMatrix, "engine switch changed standardization");
                    Check(reference.FeatureNames.SequenceEqual(analysis.FeatureNames), "engine switch changed features");
                    Check(reference.ExcludedFeatureNames.SequenceEqual(analysis.ExcludedFeatureNames), "engine switch changed exclusions");
                    Check(JsonConvert.SerializeObject(reference.FindNearest("D009", 7)) == JsonConvert.SerializeObject(analysis.FindNearest("D009", 7)), "engine switch changed standardized-space KNN");
                }
                if (engine == Alternative.Engine.CSharp)
                {
                    Check(analysis.TSNEModel.LearningRate == 500 && analysis.TSNEModel.RandomSeed == 1 && analysis.TSNEModel.Iterations == 80, "pipeline CSharp metadata mismatch");
                    if (firstCSharp == null) firstCSharp = coordinates;
                    else Equal(firstCSharp, coordinates, "switching back to CSharp changed its deterministic projection");
                }
                if (engine == Alternative.Engine.Hybrid) Check(!firstCSharp.Zip(coordinates, (a, b) => a.SequenceEqual(b)).All(equal => equal), "Hybrid reused CSharp coordinates");
                Console.WriteLine("Integration " + analysis.TSNEModel.EngineName + ": 24 aligned rows; standardization and KNN preserved.");
            }
            Check(JsonConvert.SerializeObject(table) == originalTable, "analysis mutated source DataTable");
            VerifyGlobalDefault(service, snapshot, reference.StandardizedMatrix);
            VerifyLargeLegacyCaller();
            Console.WriteLine("PASS: Accord/CSharp/Hybrid/CSharp DataTable integration; " + assertions + " assertions.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void VerifyLargeLegacyCaller()
    {
        var table = new DataTable();
        foreach (string name in new[] { "DRAFT_NO", "PARAM_TYP", "CONV_EXPER_CTN", "AI_RSLT_VAL" }) table.Columns.Add(name);
        for (int i = 0; i < 5610; i++)
            table.Rows.Add("L" + i, "Response", JsonConvert.SerializeObject(new { F0 = Math.Sin(i * 0.4), F1 = Math.Cos(i * 0.17), F2 = i * 0.01 }), "");
        var service = new TSNEExadataService(table);
        var snapshot = service.SetDataTable(table);
        // Reproduce a different UI that still explicitly passes CSharp despite the global Hybrid default.
        var options = new TSNEScatterAnalysisOptions { TSNELibraryEngine = Alternative.Engine.CSharp, TSNEIterations = 80 };
        var result = service.AnalyzeSnapshot(snapshot, TSNEParameterType.Response, options);
        Check(result.Records.Count == 5610 && result.AnalysisResult.TSNEModel.Coordinates.Length == 5610, "large UI call dropped rows");
        Check(result.AnalysisResult.TSNEModel.EngineName.Contains("Hybrid"), "legacy CSharp caller did not switch to Hybrid");
        Check(result.AnalysisResult.TSNEModel.EngineSelectionReason.Contains("5610"), "pipeline lost fallback reason");
        Check(result.Diagnostic.CompactText.Contains("ENGINE=Hybrid"), "diagnostic misreported fallback engine");
        Check(result.Records[5609].DraftNo == "L5609" && result.Records[5609].SourceRowIndex == 5609, "large UI call lost source alignment");
        Check(result.AnalysisResult.Verification.AllScoresFinite, "large UI call returned invalid coordinates");
        Check(options.TSNELibraryEngine == Alternative.Engine.CSharp, "pipeline mutated caller engine");
        Console.WriteLine("PASS: legacy CSharp DataTable call processed all 5610 rows using Hybrid; engine and reason propagated.");
    }

    private static void VerifyGlobalDefault(TSNEExadataService service, TSNEExadataSnapshot snapshot, double[][] matrix)
    {
        var original = Alternative.DefaultEngine;
        try
        {
            Check(original == Alternative.Engine.Hybrid, "startup default must be Hybrid for larger populations");
            foreach (var engine in new[] { Alternative.Engine.Hybrid, Alternative.Engine.CSharp })
            {
                Alternative.DefaultEngine = engine;
                string name = engine == Alternative.Engine.Hybrid ? "Hybrid" : "tsne-csharp";
                Check(new Alternative.Options().Engine == engine, "standalone options ignored global default");
                Check(new TSNEAnalysisOptions().TSNELibraryEngine == engine, "pipeline options ignored global default");
                var chartOptions = new TSNEScatterOptions();
                Check(chartOptions.Clone().Analysis.TSNELibraryEngine == engine, "chart clone lost global default");
                var result = service.AnalyzeSnapshot(snapshot, TSNEParameterType.Response, chartOptions.Analysis);
                Check(result.AnalysisResult.TSNEModel.EngineName.Contains(name), "chart/service ignored global default");
                Check(service.AnalyzeSnapshot(snapshot, TSNEParameterType.Response, null).AnalysisResult.TSNEModel.EngineName.Contains(name), "null options ignored global default");
                var docs = matrix.Select((row, i) => JsonConvert.SerializeObject(new { Draft_NO = "G" + i, F0 = row[0], F1 = row[1], F2 = row[2] })).ToArray();
                Check(new TSNEAnalysisPipeline().Analyze(docs).TSNEModel.EngineName.Contains(name), "parameterless pipeline ignored global default");
                Check(TSNEScatterDataSource.FromJsonSamples(docs).Analyze(null).TSNEModel.EngineName.Contains(name), "data source ignored global default");
                Check(service.QueryDraftAsync("D009", TSNEParameterType.Response, TSNEExadataRefreshMode.PreferMemorySnapshot).GetAwaiter().GetResult().AnalysisResult.TSNEModel.EngineName.Contains(name), "draft query ignored global default");
                Check(TSNEProjectionModel.FitTransform(matrix, 3, 80, 50, 19).EngineName.Contains(name), "five-argument projection ignored global default");
                Check(Alternative.FitTransform(matrix).Engine == engine, "standalone call ignored global default");
                Check(Alternative.FitTransform(matrix, new Alternative.Options { Engine = Alternative.Engine.CSharp, Iterations = 80 }).Engine == Alternative.Engine.CSharp, "global default overrode explicit selection");
                var accord = new TSNEScatterAnalysisOptions { TSNELibraryEngine = null };
                Check(service.AnalyzeSnapshot(snapshot, TSNEParameterType.Response, accord.Clone()).AnalysisResult.TSNEModel.EngineName.Contains("Accord"), "explicit Accord selection lost");
                Console.WriteLine("Global default " + name + ": chart, service, pipeline, data source, query and direct calls passed.");
            }
            bool rejected = false;
            try { Alternative.DefaultEngine = (Alternative.Engine)123; }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected && Alternative.DefaultEngine == Alternative.Engine.CSharp, "invalid global engine changed configuration");
        }
        finally { Alternative.DefaultEngine = original; }
    }

    private static void Equal(double[][] a, double[][] b, string message) { Check(a.Length == b.Length && a.Zip(b, (x, y) => x.SequenceEqual(y)).All(equal => equal), message); }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); assertions++; }
}
