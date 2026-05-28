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
                GridLineCount = 5,
                BarBorderWidth = 1.2f,
                BarGap = 8f,
                GroupPaddingRatio = 0.2f,
                MaxValue = 100f,
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
                    Values = new[] { 88f, 82f, 91f, 79f, 95f },
                    FillColor = Color.FromArgb(170, 255, 196, 214),
                    BorderColor = Color.FromArgb(230, 225, 104, 150)
                },
                new LightningBarSeries
                {
                    Name = "설비 B",
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
    }
}
