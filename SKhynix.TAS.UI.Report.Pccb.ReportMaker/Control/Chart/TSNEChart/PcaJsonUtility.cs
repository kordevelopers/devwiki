using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    internal static class PcaJsonUtility
    {
        private const int DefaultMaxDepth = 256;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Culture = CultureInfo.InvariantCulture,
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Decimal,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Include
        };

        public static object DeserializeObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON text is empty.", "json");
            }

            using (var stringReader = new StringReader(RemoveBom(json.Trim())))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                jsonReader.DateParseHandling = DateParseHandling.None;
                jsonReader.FloatParseHandling = FloatParseHandling.Decimal;
                jsonReader.MaxDepth = DefaultMaxDepth;

                JToken token = JToken.ReadFrom(jsonReader);
                return ConvertToken(token);
            }
        }

        public static string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value, SerializerSettings);
        }

        public static bool IsJsonException(Exception ex)
        {
            return ex is JsonException
                || ex is ArgumentException
                || ex is InvalidOperationException;
        }

        public static string RemoveBom(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.TrimStart('\uFEFF');
        }

        private static object ConvertToken(JToken token)
        {
            if (token == null)
            {
                return null;
            }

            switch (token.Type)
            {
                case JTokenType.Object:
                    var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (JProperty property in token.Children<JProperty>())
                    {
                        dictionary[property.Name] = ConvertToken(property.Value);
                    }

                    return dictionary;

                case JTokenType.Array:
                    var list = new List<object>();
                    foreach (JToken item in token.Children())
                    {
                        list.Add(ConvertToken(item));
                    }

                    return list;

                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;

                default:
                    JValue value = token as JValue;
                    return value == null ? null : value.Value;
            }
        }
    }
}



