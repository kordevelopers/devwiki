using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.ManualPcaScatter
{
    public sealed class PcaExadataSampleDataFactory
    {
        private const int DefaultVisibleSampleCount = 300;
        private const int DefaultHiddenSampleCount = 30;

        private readonly Random random;

        public PcaExadataSampleDataFactory(int seed)
        {
            random = new Random(seed);
        }

        public DataTable CreateDefaultDataTable()
        {
            PcaExadataSnapshot snapshot = CreateDefaultSnapshot();
            return ToDataTable(snapshot);
        }

        public DataTable CreateDefaultDataTable(int countPerVisibleParameterType)
        {
            PcaExadataSnapshot snapshot = CreateDefaultSnapshot(countPerVisibleParameterType);
            return ToDataTable(snapshot);
        }

        public PcaExadataSnapshot CreateDefaultSnapshot()
        {
            return CreateDefaultSnapshot(DefaultVisibleSampleCount);
        }

        public PcaExadataSnapshot CreateDefaultSnapshot(int countPerVisibleParameterType)
        {
            var rows = new List<PcaExadataSourceRow>();
            int visibleCount = Math.Max(0, countPerVisibleParameterType);
            AddRows(rows, PcaParameterType.Response, "SAMPLE-R", visibleCount, -0.9d);
            AddRows(rows, PcaParameterType.Defect, "SAMPLE-D", visibleCount, 0.8d);
            AddRows(rows, PcaParameterType.Epm, "SAMPLE-E", DefaultHiddenSampleCount, -0.2d);
            AddRows(rows, PcaParameterType.Probe, "SAMPLE-P", DefaultHiddenSampleCount, 0.2d);
            return new PcaExadataSnapshot(rows, DateTime.UtcNow);
        }

        public DataTable CreateDatabaseLikeDataTable(int responseCount, int defectCount, int epmCount, int probeCount)
        {
            var rows = new List<PcaExadataSourceRow>();
            AddRows(rows, PcaParameterType.Response, "DRAFT-R", Math.Max(0, responseCount), -0.9d);
            AddRows(rows, PcaParameterType.Defect, "DRAFT-D", Math.Max(0, defectCount), 0.8d);
            AddRows(rows, PcaParameterType.Epm, "DRAFT-E", Math.Max(0, epmCount), -0.2d);
            AddRows(rows, PcaParameterType.Probe, "DRAFT-P", Math.Max(0, probeCount), 0.2d);
            return ToDataTable(new PcaExadataSnapshot(rows, DateTime.UtcNow));
        }

        public static DataTable ToDataTable(PcaExadataSnapshot snapshot)
        {
            DataTable table = new DataTable("PCCB_INFER_RSLT_INF");
            table.Columns.Add("DRAFT_NO", typeof(string));
            table.Columns.Add("PARAM_TYP", typeof(string));
            table.Columns.Add("ENGR_RSLT_VAL", typeof(string));
            table.Columns.Add("CONV_EXPER_CTN", typeof(string));
            table.Columns.Add("CHG_TM", typeof(DateTime));

            if (snapshot == null || snapshot.Rows == null)
            {
                return table;
            }

            foreach (PcaExadataSourceRow row in snapshot.Rows)
            {
                DataRow dataRow = table.NewRow();
                dataRow["DRAFT_NO"] = row.DraftNo;
                dataRow["PARAM_TYP"] = PcaParameterTypeParser.ToDatabaseValue(row.ParameterType);
                dataRow["ENGR_RSLT_VAL"] = row.LabelY;
                dataRow["CONV_EXPER_CTN"] = row.RawConvExperimentJson;
                dataRow["CHG_TM"] = DateTime.Now;
                table.Rows.Add(dataRow);
            }

            return table;
        }

        private void AddRows(IList<PcaExadataSourceRow> rows, PcaParameterType parameterType, string draftPrefix, int count, double typeOffset)
        {
            for (int rowIndex = 0; rowIndex < count; rowIndex++)
            {
                SampleSeriesProfile profile = ResolveSampleSeriesProfile(rowIndex);
                string draftNo = string.Format(CultureInfo.InvariantCulture, "{0}-{1:000}", draftPrefix, rowIndex + 1);
                double subClusterOffset = ResolveSubClusterOffset(rowIndex, profile);
                double clusterCenterX = profile.CenterX;
                double clusterCenterY = profile.CenterY;
                double pcaFactorX = clusterCenterX + typeOffset + subClusterOffset + NextGaussian(0d, 0.42d);
                double pcaFactorY = clusterCenterY - (subClusterOffset * 0.45d) + NextGaussian(0d, 0.38d);
                double batchNoise = NextGaussian(0d, 0.18d);

                var experiment = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "PUB_NO", draftNo },
                    { "_VERSION_NM", "SAMPLE-V1" }
                };

                for (int featureIndex = 0; featureIndex < 80; featureIndex++)
                {
                    int featureNumber = featureIndex + 1;
                    double angle = featureNumber * 0.17d;
                    double loadingX = Math.Cos(angle) * 4.2d;
                    double loadingY = Math.Sin(angle * 0.9d) * 3.4d;
                    double value = 40d + (featureNumber * 0.35d) + (pcaFactorX * loadingX) + (pcaFactorY * loadingY) + batchNoise + NextGaussian(0d, 0.28d);
                    experiment[string.Format(CultureInfo.InvariantCulture, "FEATURE_{0:000}", featureNumber)] = Math.Round(value, 6);
                }

                string json = PcaJsonUtility.SerializeObject(new[] { experiment });
                rows.Add(new PcaExadataSourceRow(rows.Count, draftNo, parameterType, profile.Label, json));
            }
        }

        private static SampleSeriesProfile ResolveSampleSeriesProfile(int rowIndex)
        {
            switch (Math.Abs(rowIndex) % 4)
            {
                case 0:
                    return new SampleSeriesProfile("N/A", -3.2d, 2.1d);
                case 1:
                    return new SampleSeriesProfile("Pass", -1.0d, -1.4d);
                case 2:
                    return new SampleSeriesProfile("Review", 1.2d, 1.1d);
                default:
                    return new SampleSeriesProfile("FAIL", 3.1d, -1.8d);
            }
        }

        private static double ResolveSubClusterOffset(int rowIndex, SampleSeriesProfile profile)
        {
            int clusterIndex = rowIndex % 3;
            if (string.Equals(profile.Label, "N/A", StringComparison.OrdinalIgnoreCase))
            {
                return clusterIndex == 0 ? -0.30d : clusterIndex == 1 ? 0.08d : 0.36d;
            }

            if (string.Equals(profile.Label, "Pass", StringComparison.OrdinalIgnoreCase))
            {
                return clusterIndex == 0 ? -0.45d : clusterIndex == 1 ? 0.05d : 0.48d;
            }

            if (string.Equals(profile.Label, "Review", StringComparison.OrdinalIgnoreCase))
            {
                return clusterIndex == 0 ? -0.40d : clusterIndex == 1 ? 0.15d : 0.54d;
            }

            return clusterIndex == 0 ? -0.35d : clusterIndex == 1 ? 0.25d : 0.62d;
        }

        private double NextGaussian(double mean, double standardDeviation)
        {
            double u1 = 1d - random.NextDouble();
            double u2 = 1d - random.NextDouble();
            double normal = Math.Sqrt(-2d * Math.Log(u1))
                * Math.Sin(2d * Math.PI * u2);
            return mean + (standardDeviation * normal);
        }

        private sealed class SampleSeriesProfile
        {
            public SampleSeriesProfile(string label, double centerX, double centerY)
            {
                Label = label;
                CenterX = centerX;
                CenterY = centerY;
            }

            public string Label { get; private set; }
            public double CenterX { get; private set; }
            public double CenterY { get; private set; }
        }
    }
}
