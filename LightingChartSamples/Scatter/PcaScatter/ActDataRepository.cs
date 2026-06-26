using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace LightingChartSamples.Scatter
{
    #region ACT_DATA DataTable Options

    public sealed class ActDataQueryOptions
    {
        public ActDataQueryOptions()
        {
            ActDataColumnName = "ACT_DATA";
        }

        public string ActDataColumnName { get; set; }

        public static ActDataQueryOptions FromConfiguration()
        {
            return new ActDataQueryOptions();
        }
    }

    #endregion

    #region ACT_DATA DataTable Repository

    /// <summary>
    /// Converts caller-supplied ACT_DATA query results into JSON documents.
    /// DB access is intentionally outside this class.
    /// </summary>
    public sealed class ActDataRepository
    {
        private readonly ActDataQueryOptions options;
        private DataTable sourceTable;

        public ActDataRepository()
            : this(null, ActDataQueryOptions.FromConfiguration())
        {
        }

        public ActDataRepository(DataTable sourceTable)
            : this(sourceTable, ActDataQueryOptions.FromConfiguration())
        {
        }

        public ActDataRepository(ActDataQueryOptions options)
            : this(null, options)
        {
        }

        public ActDataRepository(DataTable sourceTable, ActDataQueryOptions options)
        {
            this.sourceTable = sourceTable;
            this.options = options ?? ActDataQueryOptions.FromConfiguration();
        }

        public void SetSourceTable(DataTable table)
        {
            sourceTable = table;
        }

        public IList<string> LoadActData()
        {
            return LoadFromDataTable(sourceTable, options);
        }

        public static IList<string> LoadFromDataTable(DataTable table)
        {
            return LoadFromDataTable(table, ActDataQueryOptions.FromConfiguration());
        }

        public static IList<string> LoadFromDataTable(DataTable table, ActDataQueryOptions options)
        {
            if (table == null)
            {
                throw new InvalidOperationException(
                    "ACT_DATA DataTable is required. Query data in the UI/service layer and pass the DataTable.");
            }

            ActDataQueryOptions effectiveOptions = options ?? ActDataQueryOptions.FromConfiguration();
            DataColumn actDataColumn = FindColumn(table, effectiveOptions.ActDataColumnName);
            var documents = new List<string>();
            for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                object value = table.Rows[rowIndex][actDataColumn];
                if (value == null || value == DBNull.Value)
                {
                    continue;
                }

                string json = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    documents.Add(json.Trim());
                }
            }

            if (documents.Count == 0)
            {
                throw new InvalidOperationException(
                    "The ACT_DATA DataTable contains no JSON data.");
            }

            return documents;
        }

        private static DataColumn FindColumn(DataTable table, string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                throw new ArgumentException("Column name is required.", "columnName");
            }

            foreach (DataColumn column in table.Columns)
            {
                if (string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return column;
                }
            }

            throw new InvalidOperationException(
                "The DataTable does not contain required column '" + columnName + "'.");
        }
    }

    #endregion
}
