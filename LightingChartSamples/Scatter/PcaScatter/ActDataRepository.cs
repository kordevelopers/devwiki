using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;

namespace LightingChartSamples.Scatter
{
    #region Database Query Options

    public sealed class ActDataQueryOptions
    {
        public ActDataQueryOptions()
        {
            ConnectionStringName = "AiInferenceDatabase";
            QueryText = "SELECT ACT_DATA FROM AI_INFERNECE";
            CommandTimeoutSeconds = 30;
        }

        public string ConnectionStringName { get; set; }
        public string QueryText { get; set; }
        public int CommandTimeoutSeconds { get; set; }

        public static ActDataQueryOptions FromConfiguration()
        {
            var options = new ActDataQueryOptions();
            string configuredQuery = ConfigurationManager.AppSettings["PcaActDataQuery"];
            if (!string.IsNullOrWhiteSpace(configuredQuery))
            {
                options.QueryText = configuredQuery.Trim();
            }

            int timeout;
            if (int.TryParse(ConfigurationManager.AppSettings["PcaActDataCommandTimeoutSeconds"], out timeout))
            {
                options.CommandTimeoutSeconds = Math.Max(1, timeout);
            }

            return options;
        }
    }

    #endregion

    #region ACT_DATA Database Repository

    /// <summary>
    /// providerName에 지정된 ADO.NET 공급자를 사용해 ACT_DATA만 조회한다.
    /// SQL Server가 기본이지만 DbProviderFactory를 사용하므로 설정만 바꾸면 다른 공급자도 사용할 수 있다.
    /// </summary>
    public sealed class ActDataRepository
    {
        private readonly ActDataQueryOptions options;

        public ActDataRepository()
            : this(ActDataQueryOptions.FromConfiguration())
        {
        }

        public ActDataRepository(ActDataQueryOptions options)
        {
            this.options = options ?? new ActDataQueryOptions();
        }

        public IList<string> LoadActData()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[options.ConnectionStringName];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "App.config connectionStrings에 '" + options.ConnectionStringName
                    + "' 연결 문자열을 설정해야 합니다.");
            }

            string providerName = string.IsNullOrWhiteSpace(settings.ProviderName)
                ? "System.Data.SqlClient"
                : settings.ProviderName.Trim();
            DbProviderFactory factory;
            try
            {
                factory = DbProviderFactories.GetFactory(providerName);
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException(
                    "ADO.NET provider를 찾을 수 없습니다: " + providerName,
                    ex);
            }

            using (DbConnection connection = factory.CreateConnection())
            using (DbCommand command = factory.CreateCommand())
            {
                if (connection == null || command == null)
                {
                    throw new InvalidOperationException("ADO.NET provider가 연결 또는 명령 객체를 만들지 못했습니다.");
                }

                connection.ConnectionString = settings.ConnectionString;
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = options.QueryText;
                command.CommandTimeout = Math.Max(1, options.CommandTimeoutSeconds);
                connection.Open();

                using (DbDataReader reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    int actDataOrdinal = FindColumnOrdinal(reader, "ACT_DATA");
                    var documents = new List<string>();
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(actDataOrdinal))
                        {
                            continue;
                        }

                        string json = Convert.ToString(reader.GetValue(actDataOrdinal));
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            documents.Add(json.Trim());
                        }
                    }

                    if (documents.Count == 0)
                    {
                        throw new InvalidOperationException("조회 결과의 ACT_DATA 컬럼에 JSON 데이터가 없습니다.");
                    }

                    return documents;
                }
            }
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

            throw new InvalidOperationException("조회 결과에 ACT_DATA 컬럼이 없습니다.");
        }
    }

    #endregion
}
