using System.Collections.Generic;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace LightingChartSamples
{
    public partial class Form1 : Form
    {
        private readonly LightningRadar radar;
        private readonly LightningRadarImagePathInfo imagePathInfo;

        public Form1()
        {
            InitializeComponent();

            BackColor = Color.White;
            Text = "Radar Chart Sample";

            radar = LightningRadar.AttachTo<SampleLightningRadar>(pnlChartHost, DockStyle.Fill, null, CreateDefaultOptions());
            radar.SetData(CreateCategories(), CreateSeries());
            imagePathInfo = CreateImagePathInfo();
        }

        private static LightningRadarOptions CreateDefaultOptions()
        {
            return new LightningRadarOptions
            {
                Title = "Radar Chart Sample",
                ShowTitle = false,
                ShowLegend = true,
                LegendLabelLocation = LightningRadarLegendLabelLocation.TopCenter,
                ChartPadding = 8,
                TopOffset = 34,
                RadiusPadding = 2f,
                CategoryLabelOffset = 4f,
                GridRingCount = 10,
                MarkerTooltipEnabled = true
            };
        }

        private static LightningRadarImagePathInfo CreateImagePathInfo()
        {
            return new LightningRadarImagePathInfo
            {
                FabId = "FAB1",
                LotCd = "LOT1001",
                DraftNo = "DRAFT01",
                FileNamePrefix = "radar"
            };
        }

        private void btnSaveImage_Click(object sender, EventArgs e)
        {
            try
            {
                string savedFilePath = radar.SaveImage(imagePathInfo);
                MessageBox.Show(this, string.Format("이미지가 저장되었습니다.\n{0}", savedFilePath), "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format("이미지 저장 중 오류가 발생했습니다.\n{0}", ex.Message), "저장 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static IEnumerable<string> CreateCategories()
        {
            return new[] { "Category A", "Category B", "Category C", "Category D", "Category E" };
        }

        private static IEnumerable<LightningRadarSeries> CreateSeries()
        {
            return new[]
            {
                new LightningRadarSeries
                {
                    Name = "Series 1",
                    Values = new[] { 88f, 82f, 91f, 79f, 95f },
                    FillColor = Color.FromArgb(110, 255, 196, 214),
                    LineColor = Color.FromArgb(230, 225, 104, 150)
                },
                new LightningRadarSeries
                {
                    Name = "Series 2",
                    Values = new[] { 76f, 73f, 86f, 70f, 84f },
                    FillColor = Color.FromArgb(95, 186, 235, 255),
                    LineColor = Color.FromArgb(230, 74, 166, 224)
                }
            };
        }

        private class SampleLightningRadar : LightningRadar
        {
        }
    }
}
