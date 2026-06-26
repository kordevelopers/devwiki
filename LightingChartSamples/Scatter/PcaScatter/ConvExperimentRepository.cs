using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;

namespace LightingChartSamples.Scatter
{
    /// <summary>
    /// Oracle Exadata에서 PCA 대상 행과 CONV_EXPER_CTN JSON을 함께 읽는다.
    /// 실제 연결 문자열과 providerName은 App.config에서 관리한다.
    /// </summary>
    public sealed class ConvExperimentQueryOptions
    {
        public ConvExperimentQueryOptions()
        {
            ConnectionStringName = "PcaExadataDatabase";
            QueryText =
                "SELECT J.ENGR_RSLT_VAL AS LABEL_Y, M.* "
                + "FROM TASADM.PCCB_INFER_RSLT_INF M "
                + "LEFT JOIN TASDEV.PCCB_JUDGE_RSLT_INF J "
                + "ON M.DRAFT_NO = J.DRAFT_NO AND M.PARAM_TYP = J.PARAM_TYP "
                + "WHERE M.CHG_TM > SYSDATE - 10 "
                + "AND J.ENGR_RSLT_VAL IS NOT NULL";
            JsonColumnName = "CONV_EXPER_CTN";
            DraftNoColumnName = "DRAFT_NO";
            ParameterTypeColumnName = "PARAM_TYP";
            LabelColumnName = "LABEL_Y";
            CommandTimeoutSeconds = 120;
        }

        public string ConnectionStringName { get; set; }
        public string QueryText { get; set; }
        public string JsonColumnName { get; set; }
        public string DraftNoColumnName { get; set; }
        public string ParameterTypeColumnName { get; set; }
        public string LabelColumnName { get; set; }
        public int CommandTimeoutSeconds { get; set; }

        public static ConvExperimentQueryOptions FromConfiguration()
        {
            var options = new ConvExperimentQueryOptions();
            string configuredQuery = ConfigurationManager.AppSettings["PcaExadataQuery"];
            if (!string.IsNullOrWhiteSpace(configuredQuery))
            {
                options.QueryText = configuredQuery.Trim();
            }

            string configuredColumn = ConfigurationManager.AppSettings["PcaExadataJsonColumn"];
            if (!string.IsNullOrWhiteSpace(configuredColumn))
            {
                options.JsonColumnName = configuredColumn.Trim();
            }

            int timeout;
            if (int.TryParse(ConfigurationManager.AppSettings["PcaExadataCommandTimeoutSeconds"], out timeout))
            {
                options.CommandTimeoutSeconds = Math.Max(1, timeout);
            }

            return options;
        }
    }

    public interface IPcaExadataRowRepository
    {
        IList<PcaExadataSourceRow> LoadAll();
    }

    public sealed class ConvExperimentRepository : IPcaExadataRowRepository
    {
        private readonly ConvExperimentQueryOptions options;

        public ConvExperimentRepository()
            : this(ConvExperimentQueryOptions.FromConfiguration())
        {
        }

        public ConvExperimentRepository(ConvExperimentQueryOptions options)
        {
            this.options = options ?? ConvExperimentQueryOptions.FromConfiguration();
        }

        public IList<PcaExadataSourceRow> LoadAll()
        {
            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings[options.ConnectionStringName];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "App.config connectionStrings에 '" + options.ConnectionStringName
                    + "' Oracle Exadata 연결 문자열을 설정해야 합니다.");
            }

            ValidateConnectionString(settings.ConnectionString);

            string providerName = string.IsNullOrWhiteSpace(settings.ProviderName)
                ? "Oracle.ManagedDataAccess.Client"
                : settings.ProviderName.Trim();
            DbProviderFactory factory;
            try
            {
                factory = DbProviderFactories.GetFactory(providerName);
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException(
                    "Oracle ADO.NET provider를 찾을 수 없습니다: " + providerName
                    + ". 배포 환경에 ODP.NET 공급자를 설치하거나 등록해야 합니다.",
                    ex);
            }

            using (DbConnection connection = factory.CreateConnection())
            using (DbCommand command = factory.CreateCommand())
            {
                if (connection == null || command == null)
                {
                    throw new InvalidOperationException(
                        "Oracle provider가 연결 또는 명령 객체를 만들지 못했습니다.");
                }

                connection.ConnectionString = settings.ConnectionString;
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = options.QueryText;
                command.CommandTimeout = Math.Max(1, options.CommandTimeoutSeconds);
                connection.Open();

                using (DbDataReader reader = command.ExecuteReader())
                {
                    int jsonOrdinal = FindColumnOrdinal(reader, options.JsonColumnName);
                    int draftNoOrdinal = FindColumnOrdinal(reader, options.DraftNoColumnName);
                    int parameterTypeOrdinal = FindColumnOrdinal(reader, options.ParameterTypeColumnName);
                    int labelOrdinal = FindColumnOrdinal(reader, options.LabelColumnName);
                    var rows = new List<PcaExadataSourceRow>();
                    int rowIndex = 0;
                    while (reader.Read())
                    {
                        string draftNo = ReadRequiredText(
                            reader,
                            draftNoOrdinal,
                            options.DraftNoColumnName,
                            rowIndex);
                        string parameterTypeText = ReadRequiredText(
                            reader,
                            parameterTypeOrdinal,
                            options.ParameterTypeColumnName,
                            rowIndex);
                        string labelY = ReadRequiredText(
                            reader,
                            labelOrdinal,
                            options.LabelColumnName,
                            rowIndex);
                        PcaParameterType parameterType;
                        if (!PcaParameterTypeParser.TryParse(parameterTypeText, out parameterType))
                        {
                            throw new InvalidOperationException(
                                string.Format(
                                    "PCCB_INFER_RSLT_INF의 PARAM_TYP[{0}] 값 '{1}'은 지원하지 않습니다.",
                                    rowIndex,
                                    parameterTypeText));
                        }

                        string json = reader.IsDBNull(jsonOrdinal)
                            ? string.Empty
                            : ReadJsonText(reader, jsonOrdinal);

                        rows.Add(new PcaExadataSourceRow(
                            rowIndex,
                            draftNo,
                            parameterType,
                            labelY,
                            json));
                        rowIndex++;
                    }

                    if (rows.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "PCCB_INFER_RSLT_INF에 PCA 분석할 CONV_EXPER_CTN 데이터가 없습니다.");
                    }

                    return rows;
                }
            }
        }

        private static void ValidateConnectionString(string connectionString)
        {
            string[] placeholders =
            {
                "YOUR_USER_ID",
                "YOUR_PASSWORD",
                "YOUR_EXADATA_HOST",
                "YOUR_SERVICE_NAME",
                "YOUR_TNS_ALIAS"
            };
            string placeholder = placeholders.FirstOrDefault(value =>
                connectionString.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!string.IsNullOrEmpty(placeholder))
            {
                throw new ConfigurationErrorsException(
                    "App.config의 PcaExadataDatabase 연결 문자열에서 샘플 값 '"
                    + placeholder + "'을 실제 Oracle Exadata 접속 정보로 변경해야 합니다.");
            }
        }

        private static string ReadRequiredText(
            IDataRecord reader,
            int ordinal,
            string columnName,
            int rowIndex)
        {
            if (reader.IsDBNull(ordinal))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "PCCB_INFER_RSLT_INF 조회 결과의 {0}[{1}] 값이 NULL입니다.",
                        columnName,
                        rowIndex));
            }

            string value = Convert.ToString(reader.GetValue(ordinal));
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "PCCB_INFER_RSLT_INF 조회 결과의 {0}[{1}] 값이 비어 있습니다.",
                        columnName,
                        rowIndex));
            }

            return value.Trim();
        }

        private static string ReadJsonText(DbDataReader reader, int ordinal)
        {
            try
            {
                using (TextReader textReader = reader.GetTextReader(ordinal))
                {
                    if (textReader != null)
                    {
                        return textReader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex) when (
                ex is InvalidCastException
                || ex is NotSupportedException
                || ex is NotImplementedException)
            {
                // 일부 Oracle provider는 GetTextReader를 지원하지 않아 GetValue로 재시도한다.
            }

            object value = reader.GetValue(ordinal);
            return value == null || value == DBNull.Value
                ? string.Empty
                : Convert.ToString(value);
        }

        private static int FindColumnOrdinal(IDataRecord record, string columnName)
        {
            for (int index = 0; index < record.FieldCount; index++)
            {
                if (string.Equals(record.GetName(index), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            throw new InvalidOperationException(
                "조회 결과에 " + columnName + " 컬럼이 없습니다.");
        }
    }
}
