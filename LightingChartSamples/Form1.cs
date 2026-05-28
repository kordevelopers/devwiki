using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LightingChartSamples
{
    public partial class Form1 : Form
    {
        private readonly LightningRadar radar;

        public Form1()
        {
            InitializeComponent();

            BackColor = Color.White;
            Text = "Radar Chart Sample";

            radar = LightningRadar.AttachTo<SampleLightningRadar>(this, DockStyle.Fill, null, CreateDefaultOptions());
            radar.SetData(CreateCategories(), CreateSeries());
        }

        private static LightningRadarOptions CreateDefaultOptions()
        {
            return new LightningRadarOptions
            {
                Title = "Radar Chart Sample",
                CategoryLabelOffset = 32f,
                GridRingCount = 10
            };
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
