using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart;

namespace SKhynix.TAS.UI.Report.Pccb
{
    public sealed class OracleLoadProgress
    {
        public string Stage { get; internal set; }
        public int Rows { get; internal set; }
        public int ElapsedSeconds { get; internal set; }
    }

    /// <summary>Direct Oracle reader with WinForms-owned configuration.</summary>
    public sealed class OracleTsneDataProvider : ITSNEScatterPopupDataProvider
    {
        public string SourceDescription { get { return "Oracle"; } }

        public Task<DataTable> LoadAllAsync()
        {
            return LoadAllAsync(CancellationToken.None, null);
        }

        public Task<DataTable> LoadAllAsync(CancellationToken cancellationToken, IProgress<OracleLoadProgress> progress)
        {
            // This driver performs synchronous I/O, including Read and CLOB retrieval.
            return Task.Run(() => Load(OracleTsneSettings.Load(), cancellationToken, progress), cancellationToken);
        }

        internal static DataTable Load(OracleTsneSettings settings, CancellationToken cancellationToken,
            IProgress<OracleLoadProgress> progress)
        {
            settings.Validate();
            return RunWithProgress(settings, cancellationToken, progress, (token, update) => LoadCore(settings, token, update));
        }

        internal static DataTable RunWithProgress(OracleTsneSettings settings, CancellationToken cancellationToken,
            IProgress<OracleLoadProgress> progress, Func<CancellationToken, Action<int, string>, DataTable> operation)
        {
            using (var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(settings.QueryTimeoutSeconds)))
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token))
            {
                var watch = Stopwatch.StartNew();
                string stage = "Oracle 연결 중";
                int rows = 0;
                Action<int, string> update = (count, message) => { Volatile.Write(ref rows, count); Volatile.Write(ref stage, message); };
                // Independent heartbeat continues even when reader.Read() is waiting for the server.
                using (var heartbeat = new Timer(_ =>
                {
                    if (progress != null) progress.Report(new OracleLoadProgress
                    {
                        Stage = linked.IsCancellationRequested
                            ? (deadline.IsCancellationRequested ? "제한 시간 초과 · Oracle 취소 응답 대기" : "Oracle 취소 응답 대기")
                            : Volatile.Read(ref stage),
                        Rows = Volatile.Read(ref rows), ElapsedSeconds = (int)watch.Elapsed.TotalSeconds
                    });
                }, null, 0, 1000))
                {
                    try
                    {
                        linked.Token.ThrowIfCancellationRequested();
                        DataTable result = operation(linked.Token, update);
                        linked.Token.ThrowIfCancellationRequested();
                        return result;
                    }
                    catch (Exception error)
                    {
                        if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);
                        if (deadline.IsCancellationRequested) throw new TimeoutException(string.Format(CultureInfo.InvariantCulture,
                            "Oracle 조회 제한 시간({0}초)을 초과했습니다. {1:N0}행 수신, 마지막 단계: {2}. oracle.env의 SQL 및 QUERY_TIMEOUT_SECONDS를 확인하세요.",
                            settings.QueryTimeoutSeconds, rows, stage));
                        var oracleError = error as OracleException;
                        if (oracleError == null) throw;
                        // Never print the connection string, password, query or raw server response.
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                            "Oracle 조회 실패 (ORA-{0:00000}). {1:N0}행 수신, 단계: {2}. {3}의 접속정보, 네트워크 및 SQL을 확인하세요.",
                            Math.Abs(oracleError.Number), rows, stage, settings.SettingsSource));
                    }
                }
            }
        }

        private static DataTable LoadCore(OracleTsneSettings settings, CancellationToken token, Action<int, string> update)
        {
            token.ThrowIfCancellationRequested();
            var builder = new OracleConnectionStringBuilder
            {
                DataSource = settings.Host + ":" + settings.Port.ToString(CultureInfo.InvariantCulture) + "/" + settings.Database,
                UserID = settings.Username,
                Password = settings.Password,
                Pooling = false,
                ConnectionTimeout = 15
            };
            using (var connection = new OracleConnection(builder.ConnectionString))
            {
                connection.Open();
                token.ThrowIfCancellationRequested();
                using (var readOnly = connection.CreateCommand())
                {
                    update(0, "읽기 전용 트랜잭션 시작 중");
                    readOnly.CommandText = "SET TRANSACTION READ ONLY";
                    readOnly.CommandTimeout = 15;
                    readOnly.ExecuteNonQuery();
                }
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = settings.Sql;
                    command.CommandTimeout = settings.QueryTimeoutSeconds;
                    command.BindByName = true;
                    command.InitialLOBFetchSize = settings.LobPrefetchCharacters;
                    command.FetchSize = settings.FetchSizeBytes;
                    // Dedicated connection: cancellation cannot affect another query.
                    using (token.Register(() =>
                    {
                        try { command.Cancel(); }
                        catch (OracleException) { }
                        catch (InvalidOperationException) { }
                    }))
                    {
                        token.ThrowIfCancellationRequested();
                        update(0, "SQL 실행 · 첫 결과 대기");
                        using (var reader = command.ExecuteReader())
                        {
                            reader.FetchSize = Math.Max(reader.RowSize, settings.FetchSizeBytes);
                            return ReadTable(reader, token, update);
                        }
                    }
                }
            }
        }

        internal static DataTable ReadTable(IDataReader reader, CancellationToken token = default(CancellationToken),
            Action<int, string> update = null)
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
            var oracleReader = reader as OracleDataReader;
            var lobColumns = Enumerable.Range(0, reader.FieldCount).Select(i =>
                string.Equals(reader.GetDataTypeName(i), "CLOB", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reader.GetDataTypeName(i), "NCLOB", StringComparison.OrdinalIgnoreCase)).ToArray();
            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (update != null) update(table.Rows.Count, "행 수신 대기 (Read)");
                token.ThrowIfCancellationRequested();
                if (!reader.Read()) break;
                token.ThrowIfCancellationRequested();
                if (update != null) update(table.Rows.Count, "현재 행 데이터 읽는 중");
                var values = new object[reader.FieldCount];
                for (int i = 0; i < values.Length; i++)
                {
                    token.ThrowIfCancellationRequested();
                    if (reader.IsDBNull(i)) { values[i] = DBNull.Value; continue; }
                    if (oracleReader != null && lobColumns[i])
                    {
                        if (update != null) update(table.Rows.Count, "현재 행 CLOB/NCLOB 읽는 중");
                        using (OracleClob clob = oracleReader.GetOracleClob(i))
                            values[i] = clob.IsNull ? (object)DBNull.Value : clob.Value;
                    }
                    else values[i] = Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture);
                }
                table.Rows.Add(values);
            }
            token.ThrowIfCancellationRequested();
            if (update != null) update(table.Rows.Count, "수신 완료");
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
        internal int QueryTimeoutSeconds, FetchSizeBytes, LobPrefetchCharacters;

        internal static string DefaultFilePath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oracle.env"); } }

        internal static OracleTsneSettings Load(string envFile = null)
        {
            envFile = Path.GetFullPath(envFile ?? DefaultFilePath);
            if (!File.Exists(envFile)) throw new InvalidOperationException(
                "WinForms 전용 설정이 없습니다. EXE 옆의 oracle.env.example을 oracle.env로 복사하고 접속정보를 입력하세요. 설정 경로: " + envFile);
            var values = ParseEnvironment(File.ReadAllText(envFile, Encoding.UTF8));
            Func<string, string, string> get = (key, fallback) => values.ContainsKey(key) ? values[key] : fallback;
            string sql = get("SQL", DefaultSql);
            string sqlFile = get("SQL_FILE", "").Trim();
            if (sqlFile.Length > 0)
            {
                string resolved = Path.IsPathRooted(sqlFile) ? sqlFile : Path.Combine(Path.GetDirectoryName(envFile), sqlFile);
                if (!File.Exists(resolved)) throw new FileNotFoundException("oracle.env의 SQL_FILE을 찾을 수 없습니다.");
                sql = File.ReadAllText(resolved, Encoding.UTF8);
            }
            sql = sql.Trim().TrimStart('\uFEFF').Trim();
            if (sql.EndsWith(";", StringComparison.Ordinal)) sql = sql.Substring(0, sql.Length - 1).TrimEnd();
            return new OracleTsneSettings
            {
                Host = get("ORACLE_HOST", "").Trim(), Database = get("ORACLE_SERVICE_NAME", "").Trim(),
                Port = ReadNumber(get("ORACLE_PORT", "1521"), "ORACLE_PORT", 1, 65535),
                Username = get("ORACLE_USERNAME", "").Trim(), Password = get("ORACLE_PASSWORD", ""),
                QueryTimeoutSeconds = ReadNumber(get("QUERY_TIMEOUT_SECONDS", "180"), "QUERY_TIMEOUT_SECONDS", 1, 86400),
                FetchSizeBytes = ReadNumber(get("FETCH_SIZE_BYTES", "1048576"), "FETCH_SIZE_BYTES", 65536, 16777216),
                LobPrefetchCharacters = ReadNumber(get("LOB_PREFETCH_CHARACTERS", "32768"), "LOB_PREFETCH_CHARACTERS", 0, 1048576),
                Sql = sql, SettingsSource = envFile
            };
        }

        private static int ReadNumber(string text, string key, int min, int max)
        {
            int value;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value < min || value > max)
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    "oracle.env의 {0}은 {1}~{2} 범위의 숫자여야 합니다.", key, min, max));
            return value;
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(Database) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password))
                throw new InvalidOperationException("Oracle 접속정보가 없습니다. " + SettingsSource + "에 ORACLE_HOST, ORACLE_SERVICE_NAME, ORACLE_PORT, ORACLE_USERNAME, ORACLE_PASSWORD를 입력하세요.");
            if (Username == "test_user" || Password == "test_password")
                throw new InvalidOperationException("Oracle 설정이 아직 테스트용 값입니다. " + SettingsSource + "에 실제 접속정보를 입력한 뒤 Oracle 조회를 다시 누르세요.");
            // This screen runs source queries only. Also use a read-only transaction below the SQL boundary.
            if (!Regex.IsMatch(Sql, @"\A\s*(?:(?:--[^\n]*(?:\n|$)|/\*[\s\S]*?\*/)\s*)*(SELECT|WITH)\b", RegexOptions.IgnoreCase))
                throw new InvalidOperationException("oracle.env의 SQL 또는 SQL_FILE에는 조회용 SELECT/WITH SQL을 지정하세요.");
        }

        internal static Dictionary<string, string> ParseEnvironment(string text)
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
                // Values are literal: Python/process environment must not override credentials,
                // including passwords containing a literal ${...} sequence.
                values[match.Groups["key"].Value] = value;
            }
            return values;
        }
    }
}
