using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LightingChartSamples
{
    public partial class LightningBarSample : Form
    {
        private readonly LightningBar barChart;
        private readonly LightningBarImagePathInfo imagePathInfo;

        public LightningBarSample()
        {
            InitializeComponent();

            BackColor = Color.White;
            Text = "LightningBar Common Sample";

            barChart = LightningBar.Create(pnlChartHost, CreateCategories(), CreateSeries(), CreateOptions());
            barChart.SeriesClicked += BarChart_SeriesClicked;
            imagePathInfo = CreateImagePathInfo();
        }

        private static LightningBarOptions CreateOptions()
        {
            return new LightningBarOptions
            {
                Title = "LightningBar Common Sample",
                ChartPadding = 36,
                TopOffset = 90,
                LegendWidth = 170,
                LegendFontSize = 7f,
                LegendMarkerWidth = 30f,
                LegendMarkerHeight = 22f,
                LegendTextMaxWidth = 130f,
                LegendTextMaxLines = 3,
                GridLineCount = 5,
                BarBorderWidth = 1.2f,
                BarGap = 8f,
                GroupPaddingRatio = 0.2f,
                MaxValue = 100f,
                // Tooltip placeholders:
                // {0}: series label, {1}: category label, {2}: value, {3}: series index, {4}: category index
                SeriesTooltipEnabled = true,
                SeriesTooltipFormat = "Value:{2:0.#} (* 클릭할 경우 해당 계측 데이터 차트로 가 보입니다.)",
                ImageStorage = new LightningRadarImageStorageOptions
                {
                    RootType = LightningRadarStorageRootType.Documents,
                    CustomRootPath = string.Empty,
                    RootFolderName = "LightningBarImages",
                    ImageFormat = LightningRadarImageFormat.Png,
                    ImageWidth = 1280,
                    ImageHeight = 720,
                    JpegQuality = 90L
                }
            };
        }

        private static LightningBarImagePathInfo CreateImagePathInfo()
        {
            return new LightningBarImagePathInfo
            {
                FabId = "FAB1",
                LotCd = "LOT1001",
                DraftNo = "DRAFT01",
                FileNamePrefix = "bar"
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

        private void btnSaveImage_Click(object sender, EventArgs e)
        {
            try
            {
                string savedFilePath = barChart.SaveImage(imagePathInfo);
                MessageBox.Show(this, string.Format("이미지가 저장되었습니다.\n{0}", savedFilePath), "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format("이미지 저장 중 오류가 발생했습니다.\n{0}", ex.Message), "저장 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
