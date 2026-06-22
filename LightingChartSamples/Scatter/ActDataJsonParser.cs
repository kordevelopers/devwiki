using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;

namespace LightingChartSamples.Scatter
{
    #region ACT_DATA Dict/List JSON Expansion

    /// <summary>
    /// DB의 ACT_DATA 문자열을 Dictionary/List 구조로 파싱한 뒤 실험 JSON 객체 목록으로 정규화한다.
    /// 단일 객체, 최상위 배열, wrapper 객체의 items/data 목록, 이중 인코딩 JSON 문자열을 처리한다.
    /// </summary>
    public sealed class ActDataJsonParser
    {
        private static readonly string[] DraftNoAliases = { "Draft_NO", "Draft_No", "draft_No" };
        private readonly JavaScriptSerializer serializer;

        public ActDataJsonParser()
        {
            serializer = new JavaScriptSerializer
            {
                MaxJsonLength = int.MaxValue,
                RecursionLimit = 256
            };
        }

        public IList<string> ExpandDocuments(IEnumerable<string> actDataDocuments)
        {
            string[] source = actDataDocuments == null
                ? new string[0]
                : actDataDocuments.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            if (source.Length == 0)
            {
                throw new ArgumentException("ACT_DATA JSON 문자열이 없습니다.", "actDataDocuments");
            }

            var normalizedRows = new List<string>();
            for (int documentIndex = 0; documentIndex < source.Length; documentIndex++)
            {
                object root;
                try
                {
                    root = serializer.DeserializeObject(RemoveBom(source[documentIndex].Trim()));
                }
                catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
                {
                    throw new FormatException(
                        string.Format("ACT_DATA[{0}] JSON 파싱에 실패했습니다: {1}", documentIndex, ex.Message),
                        ex);
                }

                int beforeCount = normalizedRows.Count;
                CollectExperimentRows(root, normalizedRows, "ACT_DATA[" + documentIndex + "]", 0);
                if (normalizedRows.Count == beforeCount)
                {
                    throw new FormatException(
                        string.Format("ACT_DATA[{0}]에서 Draft_NO를 가진 실험 객체를 찾지 못했습니다.", documentIndex));
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
                throw new FormatException(path + "의 JSON 중첩 깊이가 허용 범위를 초과했습니다.");
            }

            var dictionary = node as IDictionary<string, object>;
            if (dictionary != null)
            {
                if (ContainsDraftNo(dictionary))
                {
                    // 중첩 객체/숫자 배열은 점 표기 및 [index] 표기로 평탄화한다.
                    // 이후 PCA 파이프라인은 평탄화된 사전의 수치 leaf 값만 특징으로 사용한다.
                    var flattened = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    FlattenDictionary(dictionary, flattened, string.Empty, 0);
                    rows.Add(serializer.Serialize(flattened));
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
                        serializer.DeserializeObject(RemoveBom(nestedJson.Trim())),
                        rows,
                        path + "(json-string)",
                        depth + 1);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
                {
                    throw new FormatException(path + "의 중첩 JSON 문자열 파싱에 실패했습니다.", ex);
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
                throw new FormatException("실험 JSON 객체의 중첩 깊이가 허용 범위를 초과했습니다.");
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
            return string.IsNullOrEmpty(value) ? string.Empty : value.TrimStart('\uFEFF');
        }
    }

    #endregion
}
