using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LightingChartSamples
{
    public partial class LightningBarSample : Form
    {
        private readonly LightningBar barChart;

        public LightningBarSample()
        {
            InitializeComponent();

            BackColor = Color.White;
            Text = "LightningBar Common Sample";

            barChart = LightningBar.Create(pnlChartHost, CreateCategories(), CreateSeries(), CreateOptions());
            barChart.SeriesClicked += BarChart_SeriesClicked;
            barChart.UpdateOptions(options =>
            {
                options.TitleOptions.Text = "LightningBar Common Sample (Runtime Updated)";
                options.TitleOptions.Position = LightningBarTitlePosition.TopCenter;
            });
        }

        private static LightningBarOptions CreateOptions()
        {
            return new LightningBarOptions
            {
                Title = "LightningBar Common Sample",
                Layout = new LightningBarLayoutOptions
                {
                    ChartPadding = 36,
                    TopOffset = 90,
                    LegendReservedWidth = 170
                },
                Legend = new LightningBarLegendOptions
                {
                    Position = LightningBarLegendPosition.Top,
                    Alignment = LightningBarLegendAlignment.Center,
                    FontSize = 7f,
                    MarkerWidth = 30f,
                    MarkerHeight = 22f,
                    LabelMaxWidth = 130f,
                    LabelMaxLines = 3,
                    MarginFromChart = 10f
                },
                Scale = new LightningBarScaleOptions
                {
                    GridLineCount = 5,
                    MaxValue = 100f
                },
                Bars = new LightningBarBarOptions
                {
                    BorderWidth = 1.2f,
                    Gap = 8f,
                    GroupPaddingRatio = 0.2f,
                    HeightMode = LightningBarHeightMode.Manual,
                    FixedHeight = 18f,
                    ReferenceSeriesCount = 5
                },
                CategoryLabels = new LightningBarCategoryLabelOptions
                {
                    FontSize = 8.5f,
                    MaxLines = 3,
                    LineSpacing = 3f
                },
                TitleOptions = new LightningBarTitleOptions
                {
                    Text = "LightningBar Common Sample",
                    Position = LightningBarTitlePosition.TopLeft,
                    FontSize = 12f,
                    MarginTop = 12f,
                    MarginHorizontal = 20f
                },
                NoData = new LightningBarNoDataOptions
                {
                    Text = "데이터가 없습니다.",
                    IncludeTitle = true,
                    ShowMessage = true
                },
                RawData = new LightningBarRawDataOptions
                {
                    ButtonMode = LightningBarRawDataButtonMode.Visible,
                    ButtonText = "RawData"
                },
                // Tooltip placeholders:
                // {0}: series label, {1}: category label, {2}: value, {3}: series index, {4}: category index
                SeriesTooltipEnabled = true,
                SeriesTooltipFormat = "Value:{2:0.#} (* 클릭할 경우 해당 계측 데이터 차트로 가 보입니다.)"
            };
        }

        private static IEnumerable<string> CreateCategories()
        {
            return new[]
            {
                "품질",
                "생산성",
                "안전",
                "원가",
                "납기"
            };
        }

        private static IEnumerable<LightningBarSeries> CreateSeries()
        {
            return new[]
            {
                new LightningBarSeries
                {
                    Name = "설비 A",
                    LegendLabel = "Series A\nCurrent value\nTarget line",
                    Values = new[] { 88f, 82f, 91f, 79f, 95f },
                    FillColor = Color.FromArgb(170, 255, 196, 214),
                    BorderColor = Color.FromArgb(230, 225, 104, 150)
                },
                new LightningBarSeries
                {
                    Name = "설비 B",
                    LegendLabel = "Series B\nPrevious value\nBaseline",
                    Values = new[] { 76f, 73f, 86f, 70f, 84f },
                    FillColor = Color.FromArgb(160, 186, 235, 255),
                    BorderColor = Color.FromArgb(230, 74, 166, 224)
                }
            };
        }

        private void BarChart_SeriesClicked(object sender, LightningBarSeriesClickEventArgs e)
        {
            // 여기에서 상세 계측 데이터 폼을 열거나, 다른 화면 이동 이벤트를 실행하면 됩니다.
            // 예: using (var form = new DetailChartForm(e.CategoryName, e.Series.Name)) { form.ShowDialog(this); }
            MessageBox.Show(this,
                string.Format("Series: {0}\nCategory: {1}\nValue: {2:0.#}", e.Series.Name, e.CategoryName, e.Value),
                "Bar Series Clicked",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
