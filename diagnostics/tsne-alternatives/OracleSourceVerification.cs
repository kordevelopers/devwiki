using System;
using System.Data;
using System.IO;
using System.Linq;
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
            var parsed = OracleTsneSettings.ParseEnvironment("# comment\nexport HOST=local\nTSNE_DB_HOST=${HOST}\nTSNE_DB_PASSWORD='a # b=;c'\nTSNE_SQL=\"SELECT 1\nFROM DUAL\"\nEMPTY=\nVALUE=hello # note\n", key => null);
            Check(parsed["TSNE_DB_HOST"] == "local" && parsed["TSNE_DB_PASSWORD"] == "a # b=;c", "dotenv quoting/interpolation lost values");
            Check(parsed["TSNE_SQL"] == "SELECT 1\nFROM DUAL" && parsed["EMPTY"] == "" && parsed["VALUE"] == "hello", "dotenv multiline/comment mismatch");
            string dir = Path.Combine(Path.GetTempPath(), "tas-oracle-config-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, ".env");
            File.WriteAllText(path, "TSNE_DB_HOST=localhost\nTSNE_DB_DATABASE=service\nTSNE_DB_USERNAME=sample\nTSNE_DB_PASSWORD='sample-only'\nTSNE_SQL_FILE=query.sql\n");
            File.WriteAllText(Path.Combine(dir, "query.sql"), "SELECT 1 FROM DUAL;\n");
            var settings = OracleTsneSettings.Load(path, key => key == "TSNE_DB_PORT" ? "1522" : null);
            settings.Validate();
            Check(settings.Port == 1522 && settings.Sql == "SELECT 1 FROM DUAL", "relative SQL path or environment override failed");
            Check(settings.Password == "sample-only", "password changed");
            var overrideSql = OracleTsneSettings.Load(path, key => key == "TSNE_SQL_FILE" ? "" : key == "TSNE_SQL" ? "SELECT 2 FROM DUAL;" : null);
            Check(overrideSql.Sql == "SELECT 2 FROM DUAL", "empty environment override ignored");
            Reject(() => OracleTsneSettings.Load(path, key => key == "TSNE_DB_PORT" ? "70000" : null), "invalid port");
            settings.Password = "test_password";
            Reject(settings.Validate, "placeholder credentials");
            settings.Password = "sample-only";
            settings.Sql = "DELETE FROM example";
            Reject(settings.Validate, "non-query SQL");

            var source = new DataTable();
            foreach (string name in new[] { "draft_no", "param_typ", "label_y", "conv_exper_ctn", "rslt_cd" }) source.Columns.Add(name);
            string longJson = "{\"feature\":1,\"note\":\"" + new string('x', 20000) + "\"}";
            source.Rows.Add("D0001", "RESPONSE", "Pass", longJson, DBNull.Value);
            DataTable materialized;
            using (var reader = source.CreateDataReader()) materialized = OracleTsneDataProvider.ReadTable(reader);
            Check(materialized.Columns.Contains("LABEL_Y") && (string)materialized.Rows[0]["CONV_EXPER_CTN"] == longJson, "reader truncated JSON or lost labels");
            Check(materialized.Rows[0].IsNull("RSLT_CD"), "reader changed null");
            var rows = ConvExperimentRepository.LoadFromDataTable(materialized);
            Check(rows.Count == 1 && rows[0].DraftNo == "D0001", "Oracle result incompatible with analysis repository");
            source.Clear();
            Reject(() => { using (var reader = source.CreateDataReader()) OracleTsneDataProvider.ReadTable(reader); }, "empty query result");
            source.Columns.Remove("conv_exper_ctn");
            Reject(() => { using (var reader = source.CreateDataReader()) OracleTsneDataProvider.ReadTable(reader); }, "missing required column");
            Console.WriteLine("PASS: Oracle config, query file, materialized text, nulls and DataTable integration; " + assertions + " assertions. Live database not required.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }
    private static void Reject(Action action, string label) { try { action(); } catch (InvalidOperationException) { assertions++; return; } throw new Exception("Expected rejection: " + label); }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); assertions++; }
}
