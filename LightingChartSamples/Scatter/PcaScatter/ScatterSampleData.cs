using System;
using System.Collections.Generic;
using System.Globalization;

namespace LightingChartSamples.Scatter
{
    #region PCA Scatter Output Contract

    /// <summary>
    /// PCA 결과 한 건을 LightningChart와 KNN 결과 그리드에 전달하는 화면 데이터 계약이다.
    /// X1/X2는 임의 좌표가 아니라 PcaAnalysisPipeline이 계산한 PC1/PC2 점수다.
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
    /// 회사 데이터 구조를 흉내 낸 PCA 테스트 JSON 생성기다.
    /// 기본값은 실험 80건이며 각 JSON에 수치 특징 Feature_001~Feature_080을 넣는다.
    /// 두 상수 컬럼과 문자열 컬럼도 넣어 전처리 제거 로직을 함께 검증한다.
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
        /// 잠재 품질/공정/설비 인자를 조합하여 서로 상관관계가 있는 80개 특징을 만든다.
        /// PCA가 여러 원본 특징의 공통 변동 방향을 X1/X2로 축약하는 과정을 검증하기 위한 데이터다.
        /// </summary>
        public IList<string> CreateDefaultJsonSamples()
        {
            var result = new List<string>(DefaultSampleCount);

            for (int sampleIndex = 0; sampleIndex < DefaultSampleCount; sampleIndex++)
            {
                bool isPass = sampleIndex < 52;
                string aiResult = isPass ? PassResult : ReviewResult;

                // 클래스 차이를 만드는 잠재 품질 인자와 클래스와 무관한 공정/설비 인자다.
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

                    // 일부 특징은 실제 JSON에서 자주 보이는 숫자 문자열 형태로 넣는다.
                    // 전처리 단계가 숫자와 숫자 문자열을 동일하게 수치화하는지 검증한다.
                    row[featureName] = featureNumber % 10 == 0
                        ? (object)value.ToString("0.######", CultureInfo.InvariantCulture)
                        : value;
                }

                // 모든 행에서 값이 같으므로 분산 기준 필터에서 반드시 제거되어야 한다.
                row["CONST_ZERO"] = 0d;
                row["CONST_ONE"] = 1d;
                result.Add(PcaJsonUtility.SerializeObject(row));
            }

            return result;
        }

        private double NextGaussian(double mean, double standardDeviation)
        {
            // Box-Muller 변환으로 표준정규분포 난수를 생성한다.
            double u1 = 1d - random.NextDouble();
            double u2 = 1d - random.NextDouble();
            double standardNormal = Math.Sqrt(-2d * Math.Log(u1)) * Math.Sin(2d * Math.PI * u2);
            return mean + (standardDeviation * standardNormal);
        }
    }

    #endregion
}
