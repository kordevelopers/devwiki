using System;
using System.Collections.Generic;
using System.Globalization;

namespace LightingChartSamples.Scatter
{
    /// <summary>
    /// 화면 시연용 CONV_EXPER_CTN 행을 생성한다.
    /// 운영 DataTable과 같은 PcaExadataSourceRow/PcaExadataService 경로를 사용한다.
    /// </summary>
    public sealed class PcaExadataSampleDataFactory
    {
        private readonly Random random;

        public PcaExadataSampleDataFactory(int seed)
        {
            random = new Random(seed);
        }

        public PcaExadataSnapshot CreateDefaultSnapshot()
        {
            var rows = new List<PcaExadataSourceRow>();
            AddRows(rows, PcaParameterType.Response, "SAMPLE-R", 30, -0.9d);
            AddRows(rows, PcaParameterType.Defect, "SAMPLE-D", 30, 0.8d);
            AddRows(rows, PcaParameterType.Epm, "SAMPLE-E", 6, -0.2d);
            AddRows(rows, PcaParameterType.Probe, "SAMPLE-P", 6, 0.2d);
            return new PcaExadataSnapshot(rows, DateTime.UtcNow);
        }

        private void AddRows(
            IList<PcaExadataSourceRow> rows,
            PcaParameterType parameterType,
            string draftPrefix,
            int count,
            double typeOffset)
        {
            for (int rowIndex = 0; rowIndex < count; rowIndex++)
            {
                string draftNo = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}-{1:000}",
                    draftPrefix,
                    rowIndex + 1);
                bool isPass = rowIndex < (count * 2 / 3);
                string labelY = isPass ? "PASS" : "FAIL";
                double qualityFactor = typeOffset
                    + (isPass ? -0.7d : 1.0d)
                    + NextGaussian(0d, 0.35d);
                double processFactor = NextGaussian(0d, 0.8d);

                var experiment = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "PUB_NO", draftNo },
                    { "_VERSION_NM", "SAMPLE-V1" }
                };
                for (int featureIndex = 0; featureIndex < 80; featureIndex++)
                {
                    int featureNumber = featureIndex + 1;
                    double value = 40d
                        + (featureNumber * 0.4d)
                        + (qualityFactor * (0.7d + Math.Sin(featureNumber * 0.19d)) * 4.5d)
                        + (processFactor * Math.Cos(featureNumber * 0.13d) * 2.2d)
                        + NextGaussian(0d, 0.45d);
                    experiment[string.Format(
                        CultureInfo.InvariantCulture,
                        "FEATURE_{0:000}",
                        featureNumber)] = Math.Round(value, 6);
                }

                string json = PcaJsonUtility.SerializeObject(new[] { experiment });
                rows.Add(new PcaExadataSourceRow(
                    rows.Count,
                    draftNo,
                    parameterType,
                    labelY,
                    json));
            }
        }

        private double NextGaussian(double mean, double standardDeviation)
        {
            double u1 = 1d - random.NextDouble();
            double u2 = 1d - random.NextDouble();
            double normal = Math.Sqrt(-2d * Math.Log(u1))
                * Math.Sin(2d * Math.PI * u2);
            return mean + (standardDeviation * normal);
        }
    }
}
