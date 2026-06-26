using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.PcaScatter
{
    #region ACT_DATA Dict/List JSON Expansion

    /// <summary>
    /// DB??ACT_DATA 臾몄옄?댁쓣 Dictionary/List 援ъ“濡??뚯떛?????ㅽ뿕 JSON 媛앹껜 紐⑸줉?쇰줈 ?뺢퇋?뷀븳??
    /// ?⑥씪 媛앹껜, 理쒖긽??諛곗뿴, wrapper 媛앹껜??items/data 紐⑸줉, ?댁쨷 ?몄퐫??JSON 臾몄옄?댁쓣 泥섎━?쒕떎.
    /// </summary>
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
                throw new ArgumentException(
                    resolvedSourceName + " JSON 臾몄옄?댁씠 ?놁뒿?덈떎.",
                    "jsonDocuments");
            }

            var normalizedRows = new List<string>();
            for (int documentIndex = 0; documentIndex < source.Length; documentIndex++)
            {
                if (string.IsNullOrWhiteSpace(source[documentIndex]))
                {
                    throw new FormatException(
                        string.Format("{0}[{1}] JSON 臾몄옄?댁씠 鍮꾩뼱 ?덉뒿?덈떎.", resolvedSourceName, documentIndex));
                }

                object root;
                try
                {
                    root = PcaJsonUtility.DeserializeObject(
                        PcaJsonUtility.RemoveBom(source[documentIndex].Trim()));
                }
                catch (Exception ex) when (PcaJsonUtility.IsJsonException(ex))
                {
                    throw new FormatException(
                        string.Format(
                            "{0}[{1}] JSON ?뚯떛???ㅽ뙣?덉뒿?덈떎: {2}",
                            resolvedSourceName,
                            documentIndex,
                            ex.Message),
                        ex);
                }

                int beforeCount = normalizedRows.Count;
                CollectExperimentRows(
                    root,
                    normalizedRows,
                    resolvedSourceName + "[" + documentIndex + "]",
                    0);
                if (normalizedRows.Count == beforeCount)
                {
                    throw new FormatException(
                        string.Format(
                            "{0}[{1}]?먯꽌 Draft_NO瑜?媛吏??ㅽ뿕 媛앹껜瑜?李얠? 紐삵뻽?듬땲??",
                            resolvedSourceName,
                            documentIndex));
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
                throw new FormatException(path + "??JSON 以묒꺽 源딆씠媛 ?덉슜 踰붿쐞瑜?珥덇낵?덉뒿?덈떎.");
            }

            var dictionary = node as IDictionary<string, object>;
            if (dictionary != null)
            {
                if (ContainsDraftNo(dictionary))
                {
                    // 以묒꺽 媛앹껜/?レ옄 諛곗뿴? ???쒓린 諛?[index] ?쒓린濡??됲깂?뷀븳??
                    // ?댄썑 PCA ?뚯씠?꾨씪?몄? ?됲깂?붾맂 ?ъ쟾???섏튂 leaf 媛믩쭔 ?뱀쭠?쇰줈 ?ъ슜?쒕떎.
                    var flattened = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    FlattenDictionary(dictionary, flattened, string.Empty, 0);
                    rows.Add(PcaJsonUtility.SerializeObject(flattened));
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
                        PcaJsonUtility.DeserializeObject(
                            PcaJsonUtility.RemoveBom(nestedJson.Trim())),
                        rows,
                        path + "(json-string)",
                        depth + 1);
                }
                catch (Exception ex) when (PcaJsonUtility.IsJsonException(ex))
                {
                    throw new FormatException(path + "??以묒꺽 JSON 臾몄옄???뚯떛???ㅽ뙣?덉뒿?덈떎.", ex);
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

        private static void FlattenDictionary(
            IDictionary<string, object> source,
            IDictionary<string, object> target,
            string prefix,
            int depth)
        {
            if (depth > 64)
            {
                throw new FormatException("?ㅽ뿕 JSON 媛앹껜??以묒꺽 源딆씠媛 ?덉슜 踰붿쐞瑜?珥덇낵?덉뒿?덈떎.");
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
            return PcaJsonUtility.RemoveBom(value);
        }
    }

    #endregion
}
