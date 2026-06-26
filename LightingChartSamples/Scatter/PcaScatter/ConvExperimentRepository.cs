using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.PcaScatter
{
    /// <summary>
    /// Converts caller-supplied CONV_EXPER_CTN service results into PCA source rows.
    /// DB access is intentionally outside this class. The UI or service layer should
    /// call the company data service and pass the completed DataTable here.
    /// </summary>
    public sealed class ConvExperimentQueryOptions
    {
        public ConvExperimentQueryOptions()
        {
            JsonColumnName = "CONV_EXPER_CTN";
            DraftNoColumnName = "DRAFT_NO";
            ParameterTypeColumnName = "PARAM_TYP";
            LabelColumnName = "ENGR_RSLT_VAL";
        }

        public string JsonColumnName { get; set; }
        public string DraftNoColumnName { get; set; }
        public string ParameterTypeColumnName { get; set; }
        public string LabelColumnName { get; set; }

        public static ConvExperimentQueryOptions FromConfiguration()
        {
            return new ConvExperimentQueryOptions();
        }
    }

    public interface IPcaExadataRowRepository
    {
        IList<PcaExadataSourceRow> LoadAll();
    }

    public sealed class ConvExperimentRepository : IPcaExadataRowRepository
    {
        private readonly ConvExperimentQueryOptions options;
        private DataTable sourceTable;

        public ConvExperimentRepository()
            : this(null, ConvExperimentQueryOptions.FromConfiguration())
        {
        }

        public ConvExperimentRepository(DataTable sourceTable)
            : this(sourceTable, ConvExperimentQueryOptions.FromConfiguration())
        {
        }

        public ConvExperimentRepository(ConvExperimentQueryOptions options)
            : this(null, options)
        {
        }

        public ConvExperimentRepository(
            DataTable sourceTable,
            ConvExperimentQueryOptions options)
        {
            this.sourceTable = sourceTable;
            this.options = options ?? ConvExperimentQueryOptions.FromConfiguration();
        }

        public void SetSourceTable(DataTable table)
        {
            sourceTable = table;
        }

        public IList<PcaExadataSourceRow> LoadAll()
        {
            return LoadFromDataTable(sourceTable, options);
        }

        public static IList<PcaExadataSourceRow> LoadFromDataTable(DataTable table)
        {
            return LoadFromDataTable(table, ConvExperimentQueryOptions.FromConfiguration());
        }

        public static IList<PcaExadataSourceRow> LoadFromDataTable(
            DataTable table,
            ConvExperimentQueryOptions options)
        {
            if (table == null)
            {
                throw new InvalidOperationException(
                    "CONV_EXPER_CTN DataTable is required. Load data through the company service and pass the DataTable.");
            }

            ConvExperimentQueryOptions effectiveOptions =
                options ?? ConvExperimentQueryOptions.FromConfiguration();
            DataColumn jsonColumn = FindColumn(table, effectiveOptions.JsonColumnName);
            DataColumn draftNoColumn = FindColumn(table, effectiveOptions.DraftNoColumnName);
            DataColumn parameterTypeColumn = FindColumn(table, effectiveOptions.ParameterTypeColumnName);
            DataColumn labelColumn = FindColumn(
                table,
                effectiveOptions.LabelColumnName,
                "ENGR_RSLT_VAL",
                "LABEL_Y");

            var rows = new List<PcaExadataSourceRow>();
            for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                DataRow dataRow = table.Rows[rowIndex];
                string draftNo = ReadRequiredText(
                    dataRow,
                    draftNoColumn,
                    rowIndex);
                string parameterTypeText = ReadRequiredText(
                    dataRow,
                    parameterTypeColumn,
                    rowIndex);
                string labelY = ReadRequiredText(
                    dataRow,
                    labelColumn,
                    rowIndex);

                PcaParameterType parameterType;
                if (!PcaParameterTypeParser.TryParse(parameterTypeText, out parameterType))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "PARAM_TYP[{0}] value '{1}' is not supported.",
                            rowIndex,
                            parameterTypeText));
                }

                rows.Add(new PcaExadataSourceRow(
                    rowIndex,
                    draftNo,
                    parameterType,
                    labelY,
                    ReadJsonText(dataRow, jsonColumn)));
            }

            if (rows.Count == 0)
            {
                throw new InvalidOperationException(
                    "The CONV_EXPER_CTN DataTable contains no rows for PCA analysis.");
            }

            return rows;
        }

        private static DataColumn FindColumn(
            DataTable table,
            string columnName,
            params string[] fallbackColumnNames)
        {
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(columnName))
            {
                candidates.Add(columnName.Trim());
            }

            if (fallbackColumnNames != null)
            {
                foreach (string fallback in fallbackColumnNames)
                {
                    if (!string.IsNullOrWhiteSpace(fallback)
                        && !candidates.Exists(candidate => string.Equals(
                            candidate,
                            fallback.Trim(),
                            StringComparison.OrdinalIgnoreCase)))
                    {
                        candidates.Add(fallback.Trim());
                    }
                }
            }

            if (candidates.Count == 0)
            {
                throw new ArgumentException("Column name is required.", "columnName");
            }

            foreach (string candidate in candidates)
            {
                foreach (DataColumn column in table.Columns)
                {
                    if (string.Equals(column.ColumnName, candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        return column;
                    }
                }
            }

            throw new InvalidOperationException(
                "The DataTable does not contain required column '" + string.Join("' or '", candidates.ToArray()) + "'.");
        }

        private static string ReadRequiredText(
            DataRow row,
            DataColumn column,
            int rowIndex)
        {
            object value = row[column];
            if (value == null || value == DBNull.Value)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "DataTable column {0}[{1}] is NULL.",
                        column.ColumnName,
                        rowIndex));
            }

            string text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "DataTable column {0}[{1}] is empty.",
                        column.ColumnName,
                        rowIndex));
            }

            return text.Trim();
        }

        private static string ReadJsonText(DataRow row, DataColumn column)
        {
            object value = row[column];
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            TextReader textReader = value as TextReader;
            if (textReader != null)
            {
                return textReader.ReadToEnd();
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}
