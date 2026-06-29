using System;
using System.Collections.Generic;
using System.Globalization;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.AccordPcaScatter
{
    #region PCA Scatter Output Contract

    /// <summary>
    /// 차트와 KNN 그리드에 표시할 한 건의 결과다.
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
    /// 회사 JSON 구조와 비슷한 PCA 테스트 데이터를 만든다.
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
        /// 공통 요인이 섞인 수치 데이터를 만들어 PCA 확인에 사용한다.
        /// </summary>
        public IList<string> CreateDefaultJsonSamples()
        {
            var result = new List<string>(DefaultSampleCount);

            for (int sampleIndex = 0; sampleIndex < DefaultSampleCount; sampleIndex++)
            {
                bool isPass = sampleIndex < 52;
                string aiResult = isPass ? PassResult : ReviewResult;

                // 라벨 차이와 공정 변동이 함께 보이도록 값을 만든다.
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

                    // 숫자 문자열도 수치로 읽히는지 확인한다.
                    row[featureName] = featureNumber % 10 == 0
                        ? (object)value.ToString("0.######", CultureInfo.InvariantCulture)
                        : value;
                }

                // 상수 컬럼은 분산 필터에서 제거되어야 한다.
                row["CONST_ZERO"] = 0d;
                row["CONST_ONE"] = 1d;
                result.Add(PcaJsonUtility.SerializeObject(row));
            }

            return result;
        }

        private double NextGaussian(double mean, double standardDeviation)
        {
            // 정규분포 형태의 샘플 값을 만든다.
            double u1 = 1d - random.NextDouble();
            double u2 = 1d - random.NextDouble();
            double standardNormal = Math.Sqrt(-2d * Math.Log(u1)) * Math.Sin(2d * Math.PI * u2);
            return mean + (standardDeviation * standardNormal);
        }
    }

    #endregion
}


