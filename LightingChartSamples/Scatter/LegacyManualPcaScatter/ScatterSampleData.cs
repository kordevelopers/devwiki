using System;
using System.Collections.Generic;
using System.Globalization;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.PcaScatter
{
    #region PCA Scatter Output Contract

    /// <summary>
    /// PCA 寃곌낵 ??嫄댁쓣 LightningChart? KNN 寃곌낵 洹몃━?쒖뿉 ?꾨떖?섎뒗 ?붾㈃ ?곗씠??怨꾩빟?대떎.
    /// X1/X2???꾩쓽 醫뚰몴媛 ?꾨땲??PcaAnalysisPipeline??怨꾩궛??PC1/PC2 ?먯닔??
    /// </summary>
    public sealed class ScatterSampleData
    {
        public int SourceIndex { get; set; }
        public string DraftNo { get; set; }
        public double X1 { get; set; }
        public double X2 { get; set; }
        public string AiResultValue { get; set; }
        public double? Distance { get; set; }
        public string ParameterType { get; set; }
        public string TooltipText { get; set; }
        public object UserData { get; set; }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(TooltipText)
                ? DraftNo ?? string.Empty
                : TooltipText;
        }
    }

    #endregion

    #region JSON Sample Data Generation

    /// <summary>
    /// ?뚯궗 ?곗씠??援ъ“瑜??됰궡 ??PCA ?뚯뒪??JSON ?앹꽦湲곕떎.
    /// 湲곕낯媛믪? ?ㅽ뿕 80嫄댁씠硫?媛?JSON???섏튂 ?뱀쭠 Feature_001~Feature_080???ｋ뒗??
    /// ???곸닔 而щ읆怨?臾몄옄??而щ읆???ｌ뼱 ?꾩쿂由??쒓굅 濡쒖쭅???④퍡 寃利앺븳??
    /// </summary>
    public sealed class PcaJsonSampleDataFactory
    {
        public const string PassResult = "Pass";
        public const string ReviewResult = "Review";
        public const int DefaultSampleCount = 80;
        public const int DefaultFeatureCount = 80;

        private readonly Random random;

        public PcaJsonSampleDataFactory(int seed)
        {
            random = new Random(seed);
        }

        /// <summary>
        /// ?좎옱 ?덉쭏/怨듭젙/?ㅻ퉬 ?몄옄瑜?議고빀?섏뿬 ?쒕줈 ?곴?愿怨꾧? ?덈뒗 80媛??뱀쭠??留뚮뱺??
        /// PCA媛 ?щ윭 ?먮낯 ?뱀쭠??怨듯넻 蹂??諛⑺뼢??X1/X2濡?異뺤빟?섎뒗 怨쇱젙??寃利앺븯湲??꾪븳 ?곗씠?곕떎.
        /// </summary>
        public IList<string> CreateDefaultJsonSamples()
        {
            var result = new List<string>(DefaultSampleCount);

            for (int sampleIndex = 0; sampleIndex < DefaultSampleCount; sampleIndex++)
            {
                bool isPass = sampleIndex < 52;
                string aiResult = isPass ? PassResult : ReviewResult;

                // ?대옒??李⑥씠瑜?留뚮뱶???좎옱 ?덉쭏 ?몄옄? ?대옒?ㅼ? 臾닿???怨듭젙/?ㅻ퉬 ?몄옄??
                double qualityFactor = (isPass ? -1.05d : 1.25d) + NextGaussian(0d, 0.38d);
                double processFactor = NextGaussian(0d, 1d);
                double equipmentFactor = NextGaussian(0d, 0.75d);

                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Draft_NO", string.Format("DRAFT-{0:000}", sampleIndex + 1) },
                    { "AI_RSLT_Val", aiResult },
                    { "LOT_ID", string.Format("LOT-{0:0000}", 1000 + sampleIndex) },
                    { "TEXT_NOTE", isPass ? "stable" : "review-required" }
                };

                for (int featureIndex = 0; featureIndex < DefaultFeatureCount; featureIndex++)
                {
                    int featureNumber = featureIndex + 1;
                    string featureName = string.Format("Feature_{0:000}", featureNumber);
                    double qualityLoading = 0.55d + Math.Sin(featureNumber * 0.37d);
                    double processLoading = Math.Cos(featureNumber * 0.23d);
                    double equipmentLoading = Math.Sin(featureNumber * 0.11d);
                    double baseline = 50d + (featureNumber * 0.65d);
                    double measurementNoise = NextGaussian(0d, 0.55d + ((featureNumber % 5) * 0.06d));
                    double value = baseline
                        + (qualityFactor * qualityLoading * 5.5d)
                        + (processFactor * processLoading * 2.8d)
                        + (equipmentFactor * equipmentLoading * 2.2d)
                        + measurementNoise;
                    value = Math.Round(value, 6);

                    // ?쇰? ?뱀쭠? ?ㅼ젣 JSON?먯꽌 ?먯＜ 蹂댁씠???レ옄 臾몄옄???뺥깭濡??ｋ뒗??
                    // ?꾩쿂由??④퀎媛 ?レ옄? ?レ옄 臾몄옄?댁쓣 ?숈씪?섍쾶 ?섏튂?뷀븯?붿? 寃利앺븳??
                    row[featureName] = featureNumber % 10 == 0
                        ? (object)value.ToString("0.######", CultureInfo.InvariantCulture)
                        : value;
                }

                // 紐⑤뱺 ?됱뿉??媛믪씠 媛숈쑝誘濡?遺꾩궛 湲곗? ?꾪꽣?먯꽌 諛섎뱶???쒓굅?섏뼱???쒕떎.
                row["CONST_ZERO"] = 0d;
                row["CONST_ONE"] = 1d;
                result.Add(PcaJsonUtility.SerializeObject(row));
            }

            return result;
        }

        private double NextGaussian(double mean, double standardDeviation)
        {
            // Box-Muller 蹂?섏쑝濡??쒖??뺢퇋遺꾪룷 ?쒖닔瑜??앹꽦?쒕떎.
            double u1 = 1d - random.NextDouble();
            double u2 = 1d - random.NextDouble();
            double standardNormal = Math.Sqrt(-2d * Math.Log(u1)) * Math.Sin(2d * Math.PI * u2);
            return mean + (standardDeviation * standardNormal);
        }
    }

    #endregion
}
