using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SKhynix.TAS.UI.Report.Pccb;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart;

internal static class OracleSourceVerification
{
    private static int assertions;
    public static int Run(bool live)
    {
        try
        {
            if (live)
            {
                var table = new OracleTsneDataProvider().LoadAllAsync().GetAwaiter().GetResult();
                Console.WriteLine("Oracle live query succeeded. Rows=" + table.Rows.Count + "; Columns=" + table.Columns.Count);
                return 0;
            }
            var parsed = OracleTsneSettings.ParseEnvironment("# comment\nORACLE_HOST=local\nORACLE_PASSWORD='a # b=;c${LITERAL}'\nSQL=\"SELECT 1\nFROM DUAL\"\nEMPTY=\nVALUE=hello # note\n");
            Check(parsed["ORACLE_HOST"] == "local" && parsed["ORACLE_PASSWORD"] == "a # b=;c${LITERAL}", "literal password changed");
            Check(parsed["SQL"] == "SELECT 1\nFROM DUAL" && parsed["EMPTY"] == "" && parsed["VALUE"] == "hello", "multiline/comment mismatch");
            string dir = Path.Combine(Path.GetTempPath(), "tas-oracle-config-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "oracle.env");
            string config = "ORACLE_HOST=localhost\nORACLE_PORT=1522\nORACLE_SERVICE_NAME=service\nORACLE_USERNAME=sample\nORACLE_PASSWORD='sample-only'\nSQL_FILE=query.sql\n";
            File.WriteAllText(path, config);
            File.WriteAllText(Path.Combine(dir, "query.sql"), "SELECT 1 FROM DUAL;\n");
            var settings = OracleTsneSettings.Load(path);
            settings.Validate();
            Check(settings.Port == 1522 && settings.Sql == "SELECT 1 FROM DUAL", "relative SQL path or port failed");
            Check(settings.Password == "sample-only", "password changed");
            Check(settings.QueryTimeoutSeconds == 180 && settings.FetchSizeBytes == 1048576 && settings.LobPrefetchCharacters == 32768, "fetch/deadline defaults incorrect");
            string[] keys = { "TSNE_ENV_FILE", "TSNE_DB_HOST", "TSNE_DB_PORT", "TSNE_SQL_FILE", "ORACLE_HOST" };
            string[] saved = keys.Select(Environment.GetEnvironmentVariable).ToArray();
            try
            {
                foreach (string key in keys) Environment.SetEnvironmentVariable(key, "must-not-be-used");
                var independent = OracleTsneSettings.Load(path);
                Check(independent.Host == "localhost" && independent.Port == 1522 && independent.Sql == settings.Sql, "process/Python environment leaked into WinForms");
                File.WriteAllText(Path.Combine(dir, ".env"), "TSNE_DB_HOST=must-not-be-used\n");
                Check(OracleTsneSettings.DefaultFilePath == Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oracle.env"), "settings location not EXE-owned");
                Reject(() => OracleTsneSettings.Load(Path.Combine(dir, "missing.env")), "missing own config must not use Python fallback");
            }
            finally { for (int i = 0; i < keys.Length; i++) Environment.SetEnvironmentVariable(keys[i], saved[i]); }
            foreach (string invalid in new[] { "ORACLE_PORT=70000", "QUERY_TIMEOUT_SECONDS=0", "FETCH_SIZE_BYTES=-1", "LOB_PREFETCH_CHARACTERS=-1" })
            {
                File.WriteAllText(path, config + invalid + "\n");
                Reject(() => OracleTsneSettings.Load(path), "invalid setting " + invalid.Split('=')[0]);
            }
            File.WriteAllText(path, config + "SQL_FILE=\nSQL=SELECT 2 FROM DUAL;\n");
            Check(OracleTsneSettings.Load(path).Sql == "SELECT 2 FROM DUAL", "inline SQL failed");
            settings.Password = "test_password";
            Reject(settings.Validate, "placeholder credentials");
            settings.Password = "sample-only";
            settings.Sql = "DELETE FROM example";
            Reject(settings.Validate, "non-query SQL");

            var source = new DataTable();
            foreach (string name in new[] { "draft_no", "param_typ", "label_y", "conv_exper_ctn", "rslt_cd" }) source.Columns.Add(name);
            string longJson = "{\"feature\":1,\"note\":\"" + new string('x', 100000) + "\"}";
            source.Rows.Add("D0001", "RESPONSE", "Pass", longJson, DBNull.Value);
            DataTable materialized;
            using (var reader = source.CreateDataReader()) materialized = OracleTsneDataProvider.ReadTable(reader);
            Check(materialized.Columns.Contains("LABEL_Y") && (string)materialized.Rows[0]["CONV_EXPER_CTN"] == longJson, "reader truncated JSON or lost labels");
            Check(materialized.Rows[0].IsNull("RSLT_CD"), "reader changed null");
            var rows = ConvExperimentRepository.LoadFromDataTable(materialized);
            Check(rows.Count == 1 && rows[0].DraftNo == "D0001", "Oracle result incompatible with analysis repository");
            using (var cancelled = new CancellationTokenSource())
            {
                cancelled.Cancel();
                Cancelled(() => { using (var reader = source.CreateDataReader()) OracleTsneDataProvider.ReadTable(reader, cancelled.Token); }, "before reading");
                Cancelled(() => new OracleTsneDataProvider().LoadAllAsync(cancelled.Token, null).GetAwaiter().GetResult(), "before connecting");
            }
            // All rows must survive; settings are buffer sizes, never a sample limit.
            source.Clear();
            for (int i = 0; i < 5610; i++) source.Rows.Add("D" + i, "RESPONSE", "Pass", "{\"feature\":1}", DBNull.Value);
            int progressRows = -1;
            string lastStage = null;
            using (var reader = source.CreateDataReader())
                materialized = OracleTsneDataProvider.ReadTable(reader, CancellationToken.None, (count, stage) =>
                {
                    if (count < progressRows) throw new Exception("progress row count moved backwards");
                    progressRows = count; lastStage = stage;
                });
            Check(materialized.Rows.Count == 5610 && progressRows == 5610 && lastStage == "수신 완료", "row count/progress mismatch");
            Check((string)materialized.Rows[5609]["DRAFT_NO"] == "D5609", "row order changed");
            using (var cancelled = new CancellationTokenSource())
                Cancelled(() =>
                {
                    using (var reader = source.CreateDataReader())
                        OracleTsneDataProvider.ReadTable(reader, cancelled.Token, (count, stage) => { if (count == 100) cancelled.Cancel(); });
                }, "during materialization (must not return partial data)");
            VerifyBlockedOperation(settings);
            source.Clear();
            Reject(() => { using (var reader = source.CreateDataReader()) OracleTsneDataProvider.ReadTable(reader); }, "empty query result");
            source.Columns.Remove("conv_exper_ctn");
            Reject(() => { using (var reader = source.CreateDataReader()) OracleTsneDataProvider.ReadTable(reader); }, "missing required column");
            Console.WriteLine("PASS: independent Oracle config, 5,610 rows, long JSON, cancellation, progress and DataTable integration; " + assertions + " assertions. Live database not required.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }
    private static void Reject(Action action, string label) { try { action(); } catch (InvalidOperationException) { assertions++; return; } throw new Exception("Expected rejection: " + label); }
    private static void Cancelled(Action action, string label) { try { action(); } catch (OperationCanceledException) { assertions++; return; } throw new Exception("Expected cancellation: " + label); }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); assertions++; }

    private static void VerifyBlockedOperation(OracleTsneSettings settings)
    {
        using (var entered = new ManualResetEventSlim())
        using (var heartbeat = new ManualResetEventSlim())
        using (var release = new ManualResetEventSlim())
        using (var cancelled = new CancellationTokenSource())
        {
            settings.QueryTimeoutSeconds = 10;
            var progress = new ImmediateProgress(state =>
            {
                if (state.Rows == 17 && state.Stage == "blocked Read" && state.ElapsedSeconds >= 1) heartbeat.Set();
            });
            var task = Task.Run(() => OracleTsneDataProvider.RunWithProgress(settings, cancelled.Token, progress, (token, update) =>
            {
                using (token.Register(() => release.Set()))
                {
                    update(17, "blocked Read");
                    entered.Set();
                    if (!release.Wait(5000)) throw new Exception("simulated read was never cancelled");
                    token.ThrowIfCancellationRequested();
                    throw new Exception("partial data must not return");
                }
            }));
            Check(entered.Wait(3000), "worker did not start");
            bool heartbeatObserved = heartbeat.Wait(3000);
            cancelled.Cancel();
            Cancelled(() => task.GetAwaiter().GetResult(), "blocked read");
            Check(heartbeatObserved, "progress stopped while waiting for Read");
        }
        settings.QueryTimeoutSeconds = 1;
        try
        {
            OracleTsneDataProvider.RunWithProgress(settings, CancellationToken.None, null, (token, update) =>
            {
                update(23, "blocked Read");
                if (!token.WaitHandle.WaitOne(5000)) throw new Exception("deadline did not signal");
                token.ThrowIfCancellationRequested();
                throw new Exception("partial data must not return");
            });
            throw new Exception("expected timeout");
        }
        catch (TimeoutException error)
        {
            Check(error.Message.Contains("23") && error.Message.Contains("blocked Read"), "timeout lost row count/stage");
        }
    }

    private sealed class ImmediateProgress : IProgress<OracleLoadProgress>
    {
        private readonly Action<OracleLoadProgress> report;
        internal ImmediateProgress(Action<OracleLoadProgress> report) { this.report = report; }
        public void Report(OracleLoadProgress value) { report(value); }
    }
}
