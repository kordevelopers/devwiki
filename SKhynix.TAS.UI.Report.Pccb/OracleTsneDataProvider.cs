using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart;

namespace SKhynix.TAS.UI.Report.Pccb
{
    /// <summary>Direct Oracle reader using the same .env keys and query as python_tsne.</summary>
    public sealed class OracleTsneDataProvider : ITSNEScatterPopupDataProvider
    {
        public string SourceDescription { get { return "Oracle"; } }

        public Task<DataTable> LoadAllAsync()
        {
            // Managed ODP.NET on this framework has synchronous I/O; keep it off the UI thread.
            return Task.Run(() => Load(OracleTsneSettings.Load()));
        }

        internal static DataTable Load(OracleTsneSettings settings)
        {
            settings.Validate();
            var builder = new OracleConnectionStringBuilder
            {
                DataSource = settings.Host + ":" + settings.Port.ToString(CultureInfo.InvariantCulture) + "/" + settings.Database,
                UserID = settings.Username,
                Password = settings.Password,
                Pooling = false,
                ConnectionTimeout = 15
            };
            try
            {
                using (var connection = new OracleConnection(builder.ConnectionString))
                {
                    connection.Open();
                    using (var readOnly = connection.CreateCommand())
                    {
                        readOnly.CommandText = "SET TRANSACTION READ ONLY";
                        readOnly.CommandTimeout = 15;
                        readOnly.ExecuteNonQuery();
                    }
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = settings.Sql;
                        command.CommandTimeout = 120;
                        command.BindByName = true;
                        using (var reader = command.ExecuteReader())
                            return ReadTable(reader);
                    }
                }
            }
            catch (OracleException error)
            {
                // Do not expose the connection string, password, query or server response in UI logs.
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    "Oracle 조회 실패 (ORA-{0:00000}). {1}의 TSNE_DB_* 접속정보, 네트워크 및 SQL 조회 권한을 확인하세요.",
                    Math.Abs(error.Number), settings.SettingsSource));
            }
        }

        internal static DataTable ReadTable(IDataReader reader)
        {
            var table = new DataTable("OracleTsneSource") { Locale = CultureInfo.InvariantCulture };
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string name = reader.GetName(i).ToUpperInvariant();
                if (table.Columns.Contains(name))
                    throw new InvalidOperationException("조회 SQL에 중복 컬럼이 있습니다: " + name);
                table.Columns.Add(name, typeof(string));
            }
            foreach (string name in new[] { "DRAFT_NO", "PARAM_TYP", "CONV_EXPER_CTN" })
                if (!table.Columns.Contains(name)) throw new InvalidOperationException("조회 SQL에 필요한 컬럼이 없습니다: " + name);
            if (!new[] { "LABEL_Y", "ENGR_RSLT_VAL", "AI_RSLT_VAL" }.Any(table.Columns.Contains))
                throw new InvalidOperationException("조회 SQL에 LABEL_Y 또는 ENGR_RSLT_VAL 컬럼이 필요합니다.");
            while (reader.Read())
            {
                var values = new object[reader.FieldCount];
                for (int i = 0; i < values.Length; i++)
                {
                    if (reader.IsDBNull(i)) { values[i] = DBNull.Value; continue; }
                    var oracleReader = reader as OracleDataReader;
                    string type = reader.GetDataTypeName(i);
                    if (oracleReader != null && (string.Equals(type, "CLOB", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(type, "NCLOB", StringComparison.OrdinalIgnoreCase)))
                    {
                        using (OracleClob clob = oracleReader.GetOracleClob(i))
                            values[i] = clob.IsNull ? (object)DBNull.Value : clob.Value;
                    }
                    else values[i] = Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture);
                }
                table.Rows.Add(values);
            }
            if (table.Rows.Count == 0) throw new InvalidOperationException("Oracle 조회 결과가 0행입니다. SQL의 기간 및 조회 조건을 확인하세요.");
            return table;
        }
    }

    internal sealed class OracleTsneSettings
    {
        internal const string DefaultSql = "SELECT M.DRAFT_NO, M.PARAM_TYP, J.ENGR_RSLT_VAL AS LABEL_Y, " +
            "J.RSLT_CD, M.CONV_EXPER_CTN FROM TASADM.PCCB_INFER_RSLT_INF M " +
            "JOIN TASADM.PCCB_JUDGE_RSLT_INF J ON M.DRAFT_NO = J.DRAFT_NO AND M.PARAM_TYP = J.PARAM_TYP " +
            "WHERE M.CHG_TM > SYSDATE - 10 AND J.ENGR_RSLT_VAL IS NOT NULL " +
            "AND M.CONV_EXPER_CTN IS NOT NULL ORDER BY M.DRAFT_NO, M.PARAM_TYP";

        internal string Host, Database, Username, Password, Sql, SettingsSource;
        internal int Port;

        internal static OracleTsneSettings Load(string envFile = null, Func<string, string> environment = null)
        {
            environment = environment ?? Environment.GetEnvironmentVariable;
            envFile = envFile ?? environment("TSNE_ENV_FILE") ?? FindEnvironmentFile();
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(envFile))
            {
                envFile = Path.GetFullPath(envFile);
                if (!File.Exists(envFile)) throw new FileNotFoundException("Oracle 설정 파일을 찾을 수 없습니다.", envFile);
                values = ParseEnvironment(File.ReadAllText(envFile, Encoding.UTF8), environment);
            }
            Func<string, string, string> get = (key, fallback) => environment(key) ?? (values.ContainsKey(key) ? values[key] : fallback);
            int port;
            if (!int.TryParse(get("TSNE_DB_PORT", "1521"), out port) || port < 1 || port > 65535)
                throw new InvalidOperationException("TSNE_DB_PORT는 1~65535 범위의 숫자여야 합니다.");
            string sql = get("TSNE_SQL", DefaultSql);
            string sqlFile = get("TSNE_SQL_FILE", "").Trim();
            if (sqlFile.Length > 0)
            {
                string[] roots = { Directory.GetCurrentDirectory(), envFile == null ? AppDomain.CurrentDomain.BaseDirectory : Path.GetDirectoryName(envFile), AppDomain.CurrentDomain.BaseDirectory };
                string resolved = Path.IsPathRooted(sqlFile) ? sqlFile : roots.Select(root => Path.Combine(root, sqlFile)).FirstOrDefault(File.Exists);
                if (resolved == null || !File.Exists(resolved)) throw new FileNotFoundException("TSNE_SQL_FILE을 찾을 수 없습니다.");
                sql = File.ReadAllText(resolved, Encoding.UTF8);
            }
            sql = sql.Trim().TrimStart('\uFEFF').Trim();
            if (sql.EndsWith(";", StringComparison.Ordinal)) sql = sql.Substring(0, sql.Length - 1).TrimEnd();
            return new OracleTsneSettings
            {
                Host = get("TSNE_DB_HOST", "").Trim(), Database = get("TSNE_DB_DATABASE", "").Trim(), Port = port,
                Username = get("TSNE_DB_USERNAME", "").Trim(), Password = get("TSNE_DB_PASSWORD", ""),
                Sql = sql, SettingsSource = envFile ?? "TSNE_DB_* 환경변수"
            };
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(Database) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password))
                throw new InvalidOperationException("Oracle 접속정보가 없습니다. python_tsne/.env 또는 실행 폴더의 .env에 TSNE_DB_HOST, TSNE_DB_DATABASE, TSNE_DB_PORT, TSNE_DB_USERNAME, TSNE_DB_PASSWORD를 입력하세요.");
            if (Username == "test_user" || Password == "test_password")
                throw new InvalidOperationException("Oracle 설정이 아직 테스트용 값입니다. " + SettingsSource + "에 실제 접속정보를 입력한 뒤 Oracle 조회를 다시 누르세요.");
            // This screen runs source queries only. Also use a read-only transaction below the SQL boundary.
            if (!Regex.IsMatch(Sql, @"\A\s*(?:(?:--[^\n]*(?:\n|$)|/\*[\s\S]*?\*/)\s*)*(SELECT|WITH)\b", RegexOptions.IgnoreCase))
                throw new InvalidOperationException("TSNE_SQL 또는 TSNE_SQL_FILE에는 조회용 SELECT/WITH SQL을 지정하세요.");
        }

        internal static string FindEnvironmentFile()
        {
            foreach (string root in new[] { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory })
            {
                string direct = Path.Combine(root, ".env");
                if (File.Exists(direct)) return direct;
            }
            // Locate the existing Python configuration when F5 starts from bin/Debug or bin/Release.
            foreach (string root in new[] { AppDomain.CurrentDomain.BaseDirectory, Directory.GetCurrentDirectory() })
                for (var directory = new DirectoryInfo(root); directory != null; directory = directory.Parent)
                {
                    string candidate = Path.Combine(directory.FullName, "python_tsne", ".env");
                    if (File.Exists(candidate)) return candidate;
                }
            return null;
        }

        internal static Dictionary<string, string> ParseEnvironment(string text, Func<string, string> environment)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            // Quoted values can span lines (custom SQL); unquoted inline comments follow whitespace.
            var pattern = new Regex(@"\G(?:[ \t]*\r?\n|[ \t]*\#[^\r\n]*(?:\r?\n|$)|[ \t]*(?:export[ \t]+)?(?<key>[A-Za-z_][A-Za-z0-9_]*)[ \t]*=[ \t]*(?:'(?<single>(?:\\.|[^'\\])*)'|""(?<double>(?:\\.|[^""\\])*)""|(?<plain>[^\r\n]*))[ \t]*(?:\#[^\r\n]*)?(?:\r?\n|$))");
            text = text.TrimStart('\uFEFF');
            int position = 0;
            while (position < text.Length)
            {
                if (string.IsNullOrWhiteSpace(text.Substring(position))) break;
                Match match = pattern.Match(text, position);
                if (!match.Success) throw new InvalidOperationException(".env 형식을 읽을 수 없습니다. KEY=value 형식과 따옴표를 확인하세요.");
                position += match.Length;
                if (!match.Groups["key"].Success) continue;
                string value;
                if (match.Groups["single"].Success) value = match.Groups["single"].Value.Replace(@"\'", "'").Replace(@"\\", @"\");
                else if (match.Groups["double"].Success) value = Regex.Replace(match.Groups["double"].Value, @"\\([\\""nrt])", m => m.Groups[1].Value == "n" ? "\n" : m.Groups[1].Value == "r" ? "\r" : m.Groups[1].Value == "t" ? "\t" : m.Groups[1].Value);
                else value = Regex.Replace(match.Groups["plain"].Value, @"\s+#.*$", "").TrimEnd();
                value = Regex.Replace(value, @"\$\{([A-Za-z_][A-Za-z0-9_]*)\}", m => environment(m.Groups[1].Value) ?? (values.ContainsKey(m.Groups[1].Value) ? values[m.Groups[1].Value] : ""));
                values[match.Groups["key"].Value] = value;
            }
            return values;
        }
    }
}
