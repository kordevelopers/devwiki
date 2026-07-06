using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.PCAChart.Common;

namespace LightingChartSamples.Scatter
{
    public class PcaScatterSampleForm : Form
    {
        private readonly LightningScatter scatterChart;
        private readonly DataGridView grdSelectedPoint;
        private readonly List<PcaSampleData> allSamples;
        private readonly Random random;

        public PcaScatterSampleForm()
        {
            BackColor = Color.White;
            Font = new Font(LightningScatter.DefaultChartFontName, 9F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PCA Scatter Sample";
            ClientSize = new Size(1180, 840);

            random = new Random();
            allSamples = new List<PcaSampleData>();

            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.White
            };

            var lblGuide = new Label
            {
                AutoSize = true,
                Font = new Font(LightningScatter.DefaultChartFontName, 9F),
                Location = new Point(12, 19),
                Text = "랜덤 PCA 샘플 데이터를 생성해 Fail/Pass/Insufficient 스캐터로 표시하고, 포인트 클릭 시 하단 그리드에 상세를 표시합니다."
            };

            var btnRegenerate = new Button
            {
                Location = new Point(930, 14),
                Size = new Size(220, 28),
                Text = "랜덤 데이터 다시 생성"
            };
            btnRegenerate.Click += BtnRegenerate_Click;

            pnlTop.Controls.Add(lblGuide);
            pnlTop.Controls.Add(btnRegenerate);

            var pnlChartHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            grdSelectedPoint = new DataGridView
            {
                Dock = DockStyle.Bottom,
                Height = 230,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false
            };

            Controls.Add(pnlChartHost);
            Controls.Add(grdSelectedPoint);
            Controls.Add(pnlTop);

            grdSelectedPoint.DataSource = CreateEmptyTable();

            scatterChart = LightningScatter.Create(pnlChartHost, CreateSeries(), CreateOptions());
            scatterChart.PointClicked += ScatterChart_PointClicked;
        }

        #region Legacy Demo PCA - Not Used by ManualPcaScatterMain

        // 중요: 이 영역은 기존 2변수 데모 전용 구현이다.
        // 현재 시작 Form인 ManualPcaScatterMain에서는 호출하지 않는다.
        // 약 80개 특징, StandardScaler, 상수 컬럼 제거, KNN 요구사항을 충족하지 않으므로
        // 운영용 PCA 알고리즘으로 재사용하면 안 된다.

        private void BtnRegenerate_Click(object sender, EventArgs e)
        {
            scatterChart.UpdateData(CreateSeries(), CreateOptions());
            grdSelectedPoint.DataSource = CreateEmptyTable();
        }

        private IEnumerable<LightningScatterSeries> CreateSeries()
        {
            allSamples.Clear();

            CreateCategoryRawData("Fail", 22, 120, 92, 11, 9);
            CreateCategoryRawData("Pass", 40, 178, 138, 10, 10);
            CreateCategoryRawData("Insufficient", 14, 150, 114, 15, 13);

            ApplyPcaScores(allSamples);

            var failData = allSamples.Where(item => string.Equals(item.Category, "Fail", StringComparison.OrdinalIgnoreCase)).ToList();
            var passData = allSamples.Where(item => string.Equals(item.Category, "Pass", StringComparison.OrdinalIgnoreCase)).ToList();
            var insufficientData = allSamples.Where(item => string.Equals(item.Category, "Insufficient", StringComparison.OrdinalIgnoreCase)).ToList();

            return new[]
            {
                ToSeries("Fail", failData, Color.FromArgb(216, 69, 69)),
                ToSeries("Pass", passData, Color.FromArgb(70, 168, 96)),
                ToSeries("Insufficient", insufficientData, Color.FromArgb(245, 171, 58))
            };
        }

        private void CreateCategoryRawData(string category, int count, double rawXCenter, double rawYCenter, double rawXSpread, double rawYSpread)
        {
            for (var index = 0; index < count; index++)
            {
                var sample = new PcaSampleData
                {
                    SampleId = category.Substring(0, 1).ToUpper() + "-" + (index + 1).ToString("000"),
                    Category = category,
                    RawX = NextGaussian(rawXCenter, rawXSpread),
                    RawY = NextGaussian(rawYCenter, rawYSpread),
                    Score = Math.Round(60 + random.NextDouble() * 40, 2)
                };
                allSamples.Add(sample);
            }
        }

        private void ApplyPcaScores(IList<PcaSampleData> samples)
        {
            if (samples == null || samples.Count == 0)
            {
                return;
            }

            // 두 입력 변수 RawX/RawY를 평균 중심화한다.
            // 표준편차로 나누지 않으므로 StandardScaler 정규화는 아니다.
            var meanX = samples.Average(item => item.RawX);
            var meanY = samples.Average(item => item.RawY);
            var denominator = Math.Max(1, samples.Count - 1);

            // 2x2 표본 공분산 행렬의 세 독립 성분을 계산한다.
            var covXX = samples.Sum(item => (item.RawX - meanX) * (item.RawX - meanX)) / denominator;
            var covXY = samples.Sum(item => (item.RawX - meanX) * (item.RawY - meanY)) / denominator;
            var covYY = samples.Sum(item => (item.RawY - meanY) * (item.RawY - meanY)) / denominator;

            // 2x2 행렬의 특성방정식을 직접 풀어 가장 큰 고유값과 주축을 구한다.
            var trace = covXX + covYY;
            var determinant = (covXX * covYY) - (covXY * covXY);
            var root = Math.Sqrt(Math.Max(0d, (trace * trace * 0.25d) - determinant));

            var eigenValue1 = (trace * 0.5d) + root;
            var eigenVector1 = SolveEigenVector(covXX, covXY, covYY, eigenValue1);
            var eigenVector2 = Tuple.Create(-eigenVector1.Item2, eigenVector1.Item1);

            foreach (var sample in samples)
            {
                // 평균 중심화된 원본 값을 두 고유벡터에 내적하여 PC1/PC2에 투영한다.
                var centeredX = sample.RawX - meanX;
                var centeredY = sample.RawY - meanY;
                sample.Pca1Raw = (centeredX * eigenVector1.Item1) + (centeredY * eigenVector1.Item2);
                sample.Pca2Raw = (centeredX * eigenVector2.Item1) + (centeredY * eigenVector2.Item2);
            }

            // 데모 화면의 1~100 고정 축에 맞추기 위한 Min-Max 변환이다.
            // 실제 PCA 좌표를 보존하지 않으므로 신규 요구사항에서는 사용하지 않는다.
            ScalePcaRange(samples, item => item.Pca1Raw, (item, value) => item.Pca1 = value);
            ScalePcaRange(samples, item => item.Pca2Raw, (item, value) => item.Pca2 = value);
        }

        private static Tuple<double, double> SolveEigenVector(double covXX, double covXY, double covYY, double eigenValue)
        {
            // (공분산 행렬 - 고유값 I)v = 0을 만족하는 2차원 고유벡터를 구성한다.
            var vx = covXY;
            var vy = eigenValue - covXX;

            if (Math.Abs(vx) < 0.000001d && Math.Abs(vy) < 0.000001d)
            {
                vx = eigenValue - covYY;
                vy = covXY;
            }

            if (Math.Abs(vx) < 0.000001d && Math.Abs(vy) < 0.000001d)
            {
                vx = 1d;
                vy = 0d;
            }

            var norm = Math.Sqrt((vx * vx) + (vy * vy));
            // 투영 크기가 축 길이에 의존하지 않도록 단위벡터로 정규화한다.
            // 이것도 데이터의 StandardScaler 정규화와는 다른 연산이다.
            return Tuple.Create(vx / norm, vy / norm);
        }

        private static void ScalePcaRange(IEnumerable<PcaSampleData> samples, Func<PcaSampleData, double> selector, Action<PcaSampleData, double> setter)
        {
            var values = samples.Select(selector).ToList();
            var min = values.Min();
            var max = values.Max();
            var range = max - min;

            foreach (var sample in samples)
            {
                if (range < 0.000001d)
                {
                    setter(sample, 50d);
                    continue;
                }

                var scaled = 1d + ((selector(sample) - min) * 99d / range);
                setter(sample, ClampRange(scaled, 1d, 100d));
            }
        }

        #endregion

        private static LightningScatterSeries ToSeries(string name, IEnumerable<PcaSampleData> source, Color color)
        {
            return new LightningScatterSeries
            {
                Name = name,
                LegendLabel = name,
                PointColor = color,
                LineColor = color,
                ShowLine = false,
                ShowPoints = true,
                PointSize = 16f,
                Points = source.Select(sample => new LightningScatterPoint(sample.Pca1, sample.Pca2, sample.SampleId)).ToList()
            };
        }

        private static LightningScatterOptions CreateOptions()
        {
            var options = LightningScatterOptions.CreateDefaultBubble();
            options.BackgroundColor = Color.White;
            options.GraphBackgroundColor = Color.White;
            options.ShowTitle = true;
            options.Title = "PCA Scatter (Fail / Pass / Insufficient)";
            options.XAxis.Title = "PCA 1";
            options.XAxis.AutoFit = false;
            options.XAxis.Minimum = 1;
            options.XAxis.Maximum = 100;
            options.XAxis.MajorDivCount = 10;
            options.XAxis.LabelFormat = "0";
            options.YAxis.Title = "PCA 2";
            options.YAxis.AutoFit = false;
            options.YAxis.Minimum = 1;
            options.YAxis.Maximum = 100;
            options.YAxis.MajorDivCount = 10;
            options.YAxis.LabelFormat = "0";
            options.Legend.Position = LightningScatterLegendPosition.TopCenter;
            options.Legend.ShowCheckboxes = true;
            options.Legend.BackgroundColor = Color.FromArgb(250, 250, 250);
            options.Legend.BorderColor = Color.FromArgb(220, 220, 220);
            options.Legend.TextColor = Color.FromArgb(85, 85, 85);
            options.Legend.FontSize = 8f;
            options.Style.UsePastelPalette = false;
            options.Tooltip.Format = "{0}\r\nPCA1:{1:0.###}, PCA2:{2:0.###}";
            options.NoData.Text = "PCA 데이터가 없습니다.";
            options.NoData.ShowWhenAllValuesZero = true;
            return options;
        }

        #region Interaction & View

        private void ScatterChart_PointClicked(object sender, LightningScatterPointClickEventArgs e)
        {
            var sampleId = e.Point.Tag == null ? string.Empty : e.Point.Tag.ToString();
            var selected = allSamples.FirstOrDefault(item => string.Equals(item.SampleId, sampleId, StringComparison.OrdinalIgnoreCase));
            if (selected == null)
            {
                return;
            }

            var table = CreateEmptyTable();
            var row = table.NewRow();
            row["SampleId"] = selected.SampleId;
            row["Series"] = selected.Category;
            row["Pca1"] = selected.Pca1;
            row["Pca2"] = selected.Pca2;
            row["RawX"] = selected.RawX;
            row["RawY"] = selected.RawY;
            row["Score"] = selected.Score;
            row["ClickedTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            table.Rows.Add(row);

            grdSelectedPoint.DataSource = table;
        }

        #endregion

        #region Utility

        private static DataTable CreateEmptyTable()
        {
            var table = new DataTable();
            table.Columns.Add("SampleId", typeof(string));
            table.Columns.Add("Series", typeof(string));
            table.Columns.Add("Pca1", typeof(double));
            table.Columns.Add("Pca2", typeof(double));
            table.Columns.Add("RawX", typeof(double));
            table.Columns.Add("RawY", typeof(double));
            table.Columns.Add("Score", typeof(double));
            table.Columns.Add("ClickedTime", typeof(string));
            return table;
        }

        private double NextGaussian(double mean, double stdDev)
        {
            var u1 = 1.0 - random.NextDouble();
            var u2 = 1.0 - random.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return Math.Round(mean + stdDev * randStdNormal, 4);
        }

        private static double ClampRange(double value, double min, double max)
        {
            return Math.Min(max, Math.Max(min, value));
        }

        private class PcaSampleData
        {
            public string SampleId { get; set; }
            public string Category { get; set; }
            public double Pca1Raw { get; set; }
            public double Pca2Raw { get; set; }
            public double Pca1 { get; set; }
            public double Pca2 { get; set; }
            public double RawX { get; set; }
            public double RawY { get; set; }
            public double Score { get; set; }
        }

        #endregion
    }
}
