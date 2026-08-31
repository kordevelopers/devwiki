using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsControl = System.Windows.Forms.Control;
using Accord.MachineLearning.Clustering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.Common;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public sealed class ActDataJsonParser
    {
        private static readonly string[] DraftNoAliases = { "Draft_NO", "Draft_No", "draft_No" };

        public IList<string> ExpandDocuments(IEnumerable<string> actDataDocuments)
        {
            return ExpandDocuments(actDataDocuments, "ACT_DATA");
        }

        public IList<string> ExpandDocuments(IEnumerable<string> jsonDocuments, string sourceName)
        {
            string resolvedSourceName = string.IsNullOrWhiteSpace(sourceName)
                ? "JSON_DATA"
                : sourceName.Trim();
            string[] source = jsonDocuments == null
                ? new string[0]
                : jsonDocuments.ToArray();
            if (source.Length == 0)
            {
                throw new ArgumentException(resolvedSourceName + " JSON document is empty.", "jsonDocuments");
            }

            var normalizedRows = new List<string>();
            for (int documentIndex = 0; documentIndex < source.Length; documentIndex++)
            {
                if (string.IsNullOrWhiteSpace(source[documentIndex]))
                {
                    throw new FormatException(string.Format("{0}[{1}] JSON string is empty.", resolvedSourceName, documentIndex));
                }

                object root;
                try
                {
                    root = TSNEJsonUtility.DeserializeObject(TSNEJsonUtility.RemoveBom(source[documentIndex].Trim()));
                }
                catch (Exception ex) when (TSNEJsonUtility.IsJsonException(ex))
                {
                    throw new FormatException(string.Format("{0}[{1}] JSON parsing failed: {2}",
                        resolvedSourceName, documentIndex, ex.Message), ex);
                }

                int beforeCount = normalizedRows.Count;
                CollectExperimentRows(root, normalizedRows, resolvedSourceName + "[" + documentIndex + "]", 0);
                if (normalizedRows.Count == beforeCount)
                {
                    throw new FormatException(string.Format("{0}[{1}] does not contain an experiment object with Draft_NO.",
                        resolvedSourceName, documentIndex));
                }
            }

            return normalizedRows;
        }

        private void CollectExperimentRows(object node, ICollection<string> rows, string path, int depth)
        {
            if (node == null)
            {
                return;
            }

            if (depth > 64)
            {
                throw new FormatException(path + " exceeds the allowed JSON nesting depth.");
            }

            var dictionary = node as IDictionary<string, object>;
            if (dictionary != null)
            {
                if (ContainsDraftNo(dictionary))
                {
                    // 중첩 객체와 숫자 배열은 점 표기와 [index] 표기로 평탄화한다.
                    // 이후 TSNE 파이프라인은 평탄화된 사전의 수치 leaf 값만 특징으로 사용한다.
                    var flattened = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    FlattenDictionary(dictionary, flattened, string.Empty, 0);
                    rows.Add(TSNEJsonUtility.SerializeObject(flattened));
                    return;
                }

                foreach (KeyValuePair<string, object> pair in dictionary)
                {
                    CollectExperimentRows(pair.Value, rows, path + "." + pair.Key, depth + 1);
                }

                return;
            }

            string nestedJson = node as string;
            if (nestedJson != null && LooksLikeJson(nestedJson))
            {
                try
                {
                    CollectExperimentRows(
                        TSNEJsonUtility.DeserializeObject(TSNEJsonUtility.RemoveBom(nestedJson.Trim())),
                        rows,
                        path + "(json-string)",
                        depth + 1);
                }
                catch (Exception ex) when (TSNEJsonUtility.IsJsonException(ex))
                {
                    throw new FormatException(path + " failed to parse nested JSON string.", ex);
                }

                return;
            }

            var enumerable = node as IEnumerable;
            if (enumerable == null || node is string)
            {
                return;
            }

            int itemIndex = 0;
            foreach (object item in enumerable)
            {
                CollectExperimentRows(item, rows, path + "[" + itemIndex + "]", depth + 1);
                itemIndex++;
            }
        }

        private static bool ContainsDraftNo(IDictionary<string, object> dictionary)
        {
            return dictionary.Keys.Any(key => DraftNoAliases.Any(alias =>
                string.Equals(key, alias, StringComparison.OrdinalIgnoreCase)));
        }

        private static void FlattenDictionary(IDictionary<string, object> source, IDictionary<string, object> target, string prefix, int depth)
        {
            if (depth > 64)
            {
                throw new FormatException("Experiment JSON nesting depth exceeds the allowed limit.");
            }

            foreach (KeyValuePair<string, object> pair in source)
            {
                string key = string.IsNullOrEmpty(prefix) ? pair.Key : prefix + "." + pair.Key;
                var childDictionary = pair.Value as IDictionary<string, object>;
                if (childDictionary != null)
                {
                    FlattenDictionary(childDictionary, target, key, depth + 1);
                    continue;
                }

                var enumerable = pair.Value as IEnumerable;
                if (enumerable != null && !(pair.Value is string))
                {
                    int index = 0;
                    foreach (object item in enumerable)
                    {
                        string itemKey = string.Format("{0}[{1}]", key, index);
                        var itemDictionary = item as IDictionary<string, object>;
                        if (itemDictionary != null)
                        {
                            FlattenDictionary(itemDictionary, target, itemKey, depth + 1);
                        }
                        else
                        {
                            target[itemKey] = item;
                        }

                        index++;
                    }

                    continue;
                }

                target[key] = pair.Value;
            }
        }

        private static bool LooksLikeJson(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string trimmed = RemoveBom(value.Trim());
            return (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
                || (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal));
        }

        private static string RemoveBom(string value)
        {
            return TSNEJsonUtility.RemoveBom(value);
        }
    }

}






namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{

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



    /// <summary>
    /// Converts caller-supplied ACT_DATA service results into JSON documents.
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
                    "ACT_DATA DataTable is required. Load data in the UI/service layer and pass the DataTable.");
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

}






namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
}
