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
            // 메모리 이미지 사용:
            // using (Bitmap chartImage = barChart.RenderImage()) { pictureBox.Image = new Bitmap(chartImage); }
            // 파일 저장 후 로드:
            // string imagePath = barChart.SaveImage();
            // using (Image loadedImage = LightningBar.LoadImage(imagePath)) { pictureBox.Image = new Bitmap(loadedImage); }
        }

        private static LightningBarOptions CreateOptions()
        {
            return new LightningBarOptions
            {
                Title = string.Empty,
                Layout = new LightningBarLayoutOptions
                {
                    ChartPadding = 20,
                    TopOffset = 72,
                    LegendReservedWidth = 120,
                    LegendReservedWidthMode = LightningBarLegendReservedWidthMode.CollapseForTopBottomLegend,
                    CategoryLabelReservedWidth = 110f,
                    AutoCategoryLabelReservedWidth = true,
                    MinCategoryLabelReservedWidth = 78f,
                    MaxCategoryLabelReservedWidth = 150f,
                    BottomScaleAreaHeight = 30f
                },
                Legend = new LightningBarLegendOptions
                {
                    Position = LightningBarLegendPosition.Top,
                    Alignment = LightningBarLegendAlignment.Center,
                    FontSize = 7.5f,
                    MarkerWidth = 22f,
                    MarkerHeight = 14f,
                    LabelMaxWidth = 110f,
                    LabelMaxLines = 3,
                    MarginFromChart = 8f,
                    ItemSpacing = 6f,
                    SectionSpacing = 20f
                },
                Scale = new LightningBarScaleOptions
                {
                    GridLineCount = 5,
                    MaxValue = 100f
                },
                Bars = new LightningBarBarOptions
                {
                    BorderWidth = 1.2f,
                    Gap = 5f,
                    GroupPaddingRatio = 0.16f,
                    HeightMode = LightningBarHeightMode.Manual,
                    FixedHeight = 30f,
                    ClampFixedHeightToGroup = true,
                    ReferenceSeriesCount = 5
                },
                CategoryLabels = new LightningBarCategoryLabelOptions
                {
                    FontSize = 8f,
                    MaxLines = 3,
                    LineSpacing = 1.5f
                },
                TitleOptions = new LightningBarTitleOptions
                {
                    Text = string.Empty,
                    Position = LightningBarTitlePosition.TopLeft,
                    FontSize = 12f,
                    MarginTop = 12f,
                    MarginHorizontal = 20f
                },
                NoData = new LightningBarNoDataOptions
                {
                    Text = "데이터가 없습니다.",
                    FontName = "맑은 고딕",
                    TextColor = Color.Gray,
                    IncludeTitle = false,
                    ShowWhenDataMissing = true,
                    ShowWhenAllValuesZero = true
                },
                RawData = new LightningBarRawDataOptions
                {
                    ButtonMode = LightningBarRawDataButtonMode.Hidden,
                    ButtonText = "RawData"
                },
                Image = new LightningBarImageOptions
                {
                    Width = 600,
                    Height = 400,
                    FileFormat = LightningBarImageFileFormat.Png,
                    SaveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    FileName = string.Empty
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
