using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart;

// Run against separately built baseline/current assemblies, never in one process.
// JSON snapshots deliberately select stable output fields and omit timing telemetry.
internal static class TsnePerformanceVerification
{
    private static readonly List<object> Measurements = new List<object>();
    private static readonly Dictionary<string, object> Cases = new Dictionary<string, object>();

    public static int Run(string[] args)
    {
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            if (args.Length == 3 && args[0] == "--compare") return Compare(args[1], args[2]);
            if (args.Length != 1) throw new ArgumentException("Expected result JSON path or --compare baseline current.");
            Run();
            File.WriteAllText(args[0], JsonConvert.SerializeObject(new { Cases, Measurements }, Formatting.Indented));
            Console.WriteLine("PASS: " + Cases.Count + " cases; numeric, audit, labels, neighbors and exports recorded.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Run()
    {
        // The first small run warms the runtime; it is excluded from timings.
        AnalyzeService("warmup", 12, 4, false, false);
        Cases.Clear(); Measurements.Clear();
        AnalyzeService("exadata-rich", 40, 12, true, true);
        AnalyzeService("exadata-wide", 160, 80, false, true);
        AnalyzeService("exadata-metadata-collision", 20, 4, true, true, true);
        AnalyzeJson("json-default", true);
        AnalyzeJson("json-complete-only", false);
        AnalyzeActData();
        ErrorCase("duplicate-draft", () => {
            var rows = MakeRows(12, 4, false, false);
            rows[1] = new TSNEExadataSourceRow(1, rows[0].DraftNo, TSNEParameterType.Response, "", rows[1].RawConvExperimentJson);
            return new TSNEExadataService().AnalyzeSnapshot(Snapshot(rows), TSNEParameterType.Response, null);
        });
        ErrorCase("invalid-json", () => {
            var rows = MakeRows(12, 4, false, false);
            rows[0] = new TSNEExadataSourceRow(0, rows[0].DraftNo, TSNEParameterType.Response, "", "{invalid");
            return new TSNEExadataService().AnalyzeSnapshot(Snapshot(rows), TSNEParameterType.Response, null);
        });
        ErrorCase("multiple-experiments", () => {
            var rows = MakeRows(12, 4, false, false);
            rows[0] = new TSNEExadataSourceRow(0, rows[0].DraftNo, TSNEParameterType.Response, "", "[{\"f\":1},{\"f\":2}]");
            return new TSNEExadataService().AnalyzeSnapshot(Snapshot(rows), TSNEParameterType.Response, null);
        });
        ErrorCase("decimal-overflow-string", () => {
            var rows = MakeRows(12, 4, false, false);
            var payload = JObject.Parse(rows[0].RawConvExperimentJson);
            payload["huge"] = "1e100";
            rows[0] = new TSNEExadataSourceRow(0, rows[0].DraftNo, TSNEParameterType.Response, "", payload.ToString(Formatting.None));
            return new TSNEExadataService().AnalyzeSnapshot(Snapshot(rows), TSNEParameterType.Response, null);
        });
        VerifyProjectionCacheWhenAvailable();
    }

    private static void AnalyzeService(string name, int count, int features, bool rich, bool timed, bool collision = false)
    {
        var rows = MakeRows(count, features, rich, collision);
        var table = new DataTable();
        table.Columns.Add("DRAFT_NO"); table.Columns.Add("PARAM_TYP"); table.Columns.Add("AI_RSLT_VAL");
        table.Columns.Add("ENGR_RSLT_VAL"); table.Columns.Add("CONV_EXPER_CTN");
        foreach (var row in rows) table.Rows.Add(row.DraftNo, "Response", row.AiResultValue, row.LabelY, row.RawConvExperimentJson);
        var service = new TSNEExadataService(table);
        var snapshot = service.SetDataTable(table);
        Accord.Math.Random.Generator.Seed = 42;
        var timer = Stopwatch.StartNew();
        var result = service.AnalyzeSnapshot(snapshot, TSNEParameterType.Response, new TSNEScatterAnalysisOptions());
        timer.Stop();
        if (timed) RecordTime(name, "initial-analysis", timer.Elapsed.TotalMilliseconds);
        Assert(result.Records.Count == count, name + " row count");
        Assert(result.AnalysisResult.TSNEModel.Iterations == 1000, "Accord iteration count changed.");
        var details = new {
            Analysis = Describe(result.AnalysisResult), result.MissingExperimentCount,
            Records = result.Records.Select(r => new {
                r.SourceRowIndex, r.DraftNo, r.ParameterType, r.AiResultValue, r.LabelY,
                r.FlattenedValues, r.NumericFeatures, r.StandardizedVector, r.X1, r.X2
            }).ToArray(),
            FeatureAudit = result.FeatureSelectionReport.Details,
            Population = Table(result.CreateSurvivingPopulationDataTable()),
            Raw = Table(result.CreateRawDataTable()),
            FeatureTable = Table(result.CreateFeatureSelectionDataTable())
        };
        Cases[name] = details;
        if (rich && !collision)
        {
            Assert(result.MissingExperimentCount == 2, "Expected empty and metadata-only experiments to be skipped.");
            Assert(result.AnalysisResult.FeatureNames.Contains("nested.rate"), "Nested feature lost.");
            Assert(result.FeatureSelectionReport.Details.Any(d => d.FeatureName == "PUB_NO" && d.Reason == TSNEFeatureSelectionReason.Metadata), "Metadata audit lost.");
            Assert(result.FeatureSelectionReport.Details.Any(d => d.FeatureName == "nullable" && d.NonNumericCount > 0), "Null audit count lost.");
        }
        if (!collision)
        {
            Accord.Math.Random.Generator.Seed = 42;
            timer.Restart();
            var query = service.QueryDraftAsync(rows[3].DraftNo, TSNEParameterType.Response, TSNEExadataRefreshMode.PreferMemorySnapshot).GetAwaiter().GetResult();
            timer.Stop();
            if (timed) RecordTime(name, "first-draft-query", timer.Elapsed.TotalMilliseconds);
            Assert(query.Target.DraftNo == rows[3].DraftNo, "Draft query target mismatch.");
            Cases[name + "-query"] = new { query.Target.DraftNo, query.Neighbors, Analysis = Describe(query.AnalysisResult) };
            Accord.Math.Random.Generator.Seed = 42;
            timer.Restart();
            var repeat = service.QueryDraftAsync(rows[4].DraftNo, TSNEParameterType.Response, TSNEExadataRefreshMode.PreferMemorySnapshot).GetAwaiter().GetResult();
            timer.Stop();
            if (timed) RecordTime(name, "repeat-draft-query", timer.Elapsed.TotalMilliseconds);
            Assert(repeat.Target.DraftNo == rows[4].DraftNo, "Repeated draft query target mismatch.");
            Cases[name + "-repeat-query"] = new { repeat.Target.DraftNo, repeat.Neighbors, Analysis = Describe(repeat.AnalysisResult) };
        }
    }

    private static void AnalyzeJson(string name, bool impute)
    {
        var docs = Enumerable.Range(0, 20).Select(i => {
            var payload = Payload(i, 6, true);
            payload["Draft_NO"] = " J" + i.ToString("D3") + " ";
            payload["AI_RSLT_Val"] = i == 0 ? " Pass " : "";
            return JsonConvert.SerializeObject(payload);
        }).ToArray();
        Accord.Math.Random.Generator.Seed = 42;
        var result = new TSNEAnalysisPipeline(new TSNEAnalysisOptions { MeanImputationEnabled = impute }).Analyze(docs);
        Assert(result.ScatterData[0].DraftNo == "J000" && result.ScatterData[0].AiResultValue == "Pass", "Identifier/label trim changed.");
        Cases[name] = Describe(result);
    }

    private static void AnalyzeActData()
    {
        var objects = Enumerable.Range(0, 12).Select(i => {
            var payload = Payload(i, 4, false);
            payload["Draft_NO"] = "A" + i.ToString("D3"); payload["AI_RSLT_Val"] = i == 0 ? "Pass" : "";
            return payload;
        }).ToArray();
        Accord.Math.Random.Generator.Seed = 42;
        Cases["act-data-array"] = Describe(new TSNEAnalysisPipeline().AnalyzeActDataDocuments(new[] { JsonConvert.SerializeObject(objects) }));
    }

    private static IList<TSNEExadataSourceRow> MakeRows(int count, int features, bool rich, bool collision)
    {
        var result = new List<TSNEExadataSourceRow>();
        for (int i = 0; i < count; i++)
        {
            var payload = Payload(i, features, rich);
            if (collision) { payload["Draft_NO"] = 1000 + i; payload["AI_RSLT_Val"] = i + 0.1d; }
            string json = JsonConvert.SerializeObject(payload);
            if (rich && i % 3 == 0) json = "[" + json + "]";
            result.Add(new TSNEExadataSourceRow(i, "D" + i.ToString("D4"), TSNEParameterType.Response,
                " AI " + i, i == 0 ? "Pass" : (i % 3 == 0 ? "Review" : ""), json));
        }
        if (rich)
        {
            result.Add(new TSNEExadataSourceRow(count, "EMPTY", TSNEParameterType.Response, "", "[]"));
            result.Add(new TSNEExadataSourceRow(count + 1, "META", TSNEParameterType.Response, "", "{\"PUB_NO\":1,\"_VERSION_NM\":2}"));
        }
        return result;
    }

    private static Dictionary<string, object> Payload(int i, int features, bool rich)
    {
        var payload = new Dictionary<string, object>();
        for (int j = 0; j < features; j++) payload["F" + j.ToString("D3")] = Math.Sin((i + 1) * (j + 1) * 0.071d) + (i % 4) * 1.7d + j * 0.013d;
        if (!rich) return payload;
        payload["PUB_NO"] = 1200 + i; payload["_VERSION_NM"] = "v1";
        payload["constant"] = 3; payload["note"] = "text"; payload["flag"] = i % 2 == 0;
        payload["nullable"] = i == 1 ? null : (object)(i * 0.4d);
        if (i > 0) payload["missing"] = i * 0.7d;
        if (i % 2 == 0) payload["sparse"] = i;
        payload["mixed"] = i == 2 ? "not numeric" : (object)i;
        payload["numeric-string"] = " " + (i * 0.019831274650173d).ToString("R", CultureInfo.InvariantCulture) + " ";
        payload["tiny-string"] = ((i + 1) * 1e-30d).ToString("R", CultureInfo.InvariantCulture);
        payload["precise-string"] = "1.234567890123456789";
        payload["nested"] = new Dictionary<string, object> { { "rate", i * i * 0.11d }, { "Draft_NO", 200 + i }, { "PUB_NO", 500 + i } };
        payload["array"] = new object[] { i * 0.3d, new Dictionary<string, object> { { "reading", i * i * 0.003d } } };
        if (i == 1) payload["f000"] = 7.321d;
        return payload;
    }

    private static TSNEExadataSnapshot Snapshot(IList<TSNEExadataSourceRow> rows)
    {
        return new TSNEExadataSnapshot(rows, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    private static void VerifyProjectionCacheWhenAvailable()
    {
        var cacheProperty = typeof(TSNEProjectionModel).GetProperty("CacheHit");
        if (cacheProperty == null) { Console.WriteLine("Cache assertions: baseline has no projection cache."); return; }
        Func<TSNEProjectionModel, bool> hit = model => (bool)cacheProperty.GetValue(model, null);
        var input = Enumerable.Range(0, 12).Select(i => Enumerable.Range(0, 5).Select(j =>
            Math.Sin((i + 2) * (j + 3) * 0.17d) + i * 0.03d).ToArray()).ToArray();
        var originalInput = Clone(input);
        var cold = TSNEProjectionModel.FitTransform(input, 2, 1000, 200, 187);
        Assert(!hit(cold), "First projection unexpectedly hit the cache.");
        AssertMatrixEqual(originalInput, input, "Optimizer mutated caller input.");
        var originalCoordinates = cold.Coordinates;
        var exposedCoordinates = cold.Coordinates;
        exposedCoordinates[0][0] = 123456d;
        var warm = TSNEProjectionModel.FitTransform(Clone(input), 2, 1000, 200, 187);
        Assert(hit(warm), "Equal independently allocated matrix did not reuse projection.");
        AssertMatrixEqual(originalCoordinates, warm.Coordinates, "Returned coordinates changed cached result.");
        Assert(warm.Iterations == 1000, "Cache changed effective iteration count.");
        Assert((double)typeof(TSNEProjectionModel).GetProperty("OptimizationMilliseconds").GetValue(warm, null) == 0d,
            "Cache hit ran the optimizer.");

        var changed = Clone(input); changed[11][4] += 0.125d;
        Assert(!hit(TSNEProjectionModel.FitTransform(changed, 2, 1000, 200, 187)), "Changed value reused stale coordinates.");
        Assert(!hit(TSNEProjectionModel.FitTransform(changed, 1, 1000, 200, 187)), "Changed effective perplexity reused stale coordinates.");
        Assert(!hit(TSNEProjectionModel.FitTransform(changed, 1, 1000, 200, 188)), "Changed seed did not invalidate cache.");
        Array.Reverse(changed);
        Assert(!hit(TSNEProjectionModel.FitTransform(changed, 1, 1000, 200, 188)), "Changed row order reused stale coordinates.");

        // A feature row spans multiple hash buffers; changing its tail must miss.
        var wide = Enumerable.Range(0, 8).Select(i => Enumerable.Range(0, 1031).Select(j =>
            Math.Sin((i + 1) * (j + 1) * 0.09d) + i * 0.02d).ToArray()).ToArray();
        Assert(!hit(TSNEProjectionModel.FitTransform(wide, 1, 1000, 200, 981)), "Wide initial projection unexpectedly cached.");
        Assert(hit(TSNEProjectionModel.FitTransform(Clone(wide), 1, 1000, 200, 981)), "Wide exact copy did not hit cache.");
        wide[7][1030] += 0.1d;
        Assert(!hit(TSNEProjectionModel.FitTransform(wide, 1, 1000, 200, 981)), "Last value after hash buffer boundary was ignored.");
        wide[0][1024] += 0.2d;
        Assert(!hit(TSNEProjectionModel.FitTransform(wide, 1, 1000, 200, 981)), "First value after hash buffer boundary was ignored.");

        var rows = MakeRows(12, 4, false, false);
        var service = new TSNEExadataService();
        var initial = service.AnalyzeSnapshot(Snapshot(rows), TSNEParameterType.Response, null);
        var renamedRows = rows.Select(r => new TSNEExadataSourceRow(r.SourceRowIndex, "NEW-" + r.DraftNo,
            r.ParameterType, "NEW-AI", "NEW-LABEL", r.RawConvExperimentJson)).ToList();
        var relabeled = service.AnalyzeSnapshot(Snapshot(renamedRows), TSNEParameterType.Response, null);
        Assert(hit(relabeled.AnalysisResult.TSNEModel), "Identifier/label-only change unnecessarily reran projection.");
        Assert(relabeled.Records.All(r => r.DraftNo.StartsWith("NEW-") && r.LabelY == "NEW-LABEL" && r.AiResultValue == "NEW-AI"),
            "Cache returned stale source identifiers or labels.");
        Assert(relabeled.AnalysisResult.ScatterData.All(p => p.DraftNo.StartsWith("NEW-") && p.AiResultValue == "NEW-LABEL"),
            "Cache returned stale scatter identifiers or labels.");
        AssertMatrixEqual(initial.AnalysisResult.TSNEModel.Coordinates, relabeled.AnalysisResult.TSNEModel.Coordinates,
            "Metadata-only change changed projection.");
        Console.WriteLine("PASS: cache hit, invalidation, row order, multi-buffer input, mutation isolation and fresh labels.");
    }

    private static double[][] Clone(double[][] matrix) { return matrix.Select(row => (double[])row.Clone()).ToArray(); }

    private static void AssertMatrixEqual(double[][] expected, double[][] actual, string message)
    {
        Assert(expected.Length == actual.Length, message);
        for (int row = 0; row < expected.Length; row++)
        {
            Assert(expected[row].Length == actual[row].Length, message);
            for (int col = 0; col < expected[row].Length; col++) Assert(expected[row][col] == actual[row][col], message);
        }
    }

    private static object Describe(TSNEAnalysisResult result)
    {
        Assert(result.Verification.IsValid, "Projection verification failed.");
        return new {
            result.FeatureNames, result.ExcludedFeatureNames, result.StandardizedMatrix,
            Means = result.Scaler.Means, StandardDeviations = result.Scaler.StandardDeviations,
            Coordinates = result.TSNEModel.Coordinates, result.TSNEModel.EffectivePerplexity, result.TSNEModel.Iterations,
            Points = result.ScatterData.Select(p => new { p.SourceIndex, p.DraftNo, p.AiResultValue, p.X1, p.X2 }).ToArray(),
            Audit = result.FeatureSelectionReport.Details,
            Neighbors = result.FindNearest(result.ScatterData[0].DraftNo, 5),
            result.Verification
        };
    }

    private static object Table(DataTable table)
    {
        return new {
            Columns = table.Columns.Cast<DataColumn>().Select(c => new { c.ColumnName, Type = c.DataType.FullName }).ToArray(),
            Rows = table.Rows.Cast<DataRow>().Select(r => r.ItemArray).ToArray()
        };
    }

    private static void ErrorCase(string name, Func<object> action)
    {
        try { action(); throw new Exception(name + " unexpectedly succeeded."); }
        catch (Exception ex) {
            if (ex.Message == name + " unexpectedly succeeded.") throw;
            Cases[name] = new { ExceptionType = ex.GetType().FullName };
        }
    }

    private static void RecordTime(string name, string operation, double milliseconds)
    {
        Measurements.Add(new { Case = name, Operation = operation, Milliseconds = milliseconds });
        Console.WriteLine(name + " / " + operation + ": " + milliseconds.ToString("F1") + " ms");
    }

    private static int Compare(string baselinePath, string currentPath)
    {
        JObject baseline = JObject.Parse(File.ReadAllText(baselinePath));
        JObject current = JObject.Parse(File.ReadAllText(currentPath));
        var differences = new List<string>();
        CompareToken(baseline["Cases"], current["Cases"], "Cases", differences);
        foreach (string difference in differences.Take(20)) Console.Error.WriteLine(difference);
        if (differences.Count > 0) { Console.Error.WriteLine(differences.Count + " output differences."); return 1; }
        Console.WriteLine("PASS: baseline/current case outputs match (relative numeric tolerance 1e-12).");
        return 0;
    }

    private static void CompareToken(JToken expected, JToken actual, string path, List<string> differences)
    {
        if (expected == null || actual == null) { if (expected != actual) differences.Add(path + ": missing token"); return; }
        if (expected.Type == JTokenType.Float || expected.Type == JTokenType.Integer)
        {
            double a = expected.Value<double>(), b = actual.Value<double>();
            if (Math.Abs(a - b) > 1e-12d * Math.Max(1d, Math.Abs(a))) differences.Add(path + ": " + a.ToString("R") + " != " + b.ToString("R"));
            return;
        }
        var expectedObject = expected as JObject; var actualObject = actual as JObject;
        if (expectedObject != null && actualObject != null)
        {
            if (expectedObject.Properties().Count() != actualObject.Properties().Count()) differences.Add(path + ": property count mismatch");
            foreach (var property in expectedObject.Properties()) CompareToken(property.Value, actualObject[property.Name], path + "." + property.Name, differences);
            return;
        }
        var expectedArray = expected as JArray; var actualArray = actual as JArray;
        if (expectedArray != null && actualArray != null)
        {
            if (expectedArray.Count != actualArray.Count) differences.Add(path + ": array length mismatch");
            for (int i = 0; i < Math.Min(expectedArray.Count, actualArray.Count); i++) CompareToken(expectedArray[i], actualArray[i], path + "[" + i + "]", differences);
            return;
        }
        if (!JToken.DeepEquals(expected, actual)) differences.Add(path + ": " + expected + " != " + actual);
    }

    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
}
