using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace LightingChartSamples
{
    public partial class LightningRadarCanvasControl : UserControl
    {
        private readonly LightningRadar radar;
        private Color userControlBorderColor = Color.FromArgb(120, 128, 148);
        private int userControlBorderWidth = 2;
        private Color userControlBackgroundColor = Color.FromArgb(238, 241, 247);
        private Color headerBackgroundColor = Color.FromArgb(224, 232, 245);
        private Color canvasBackgroundColor = Color.FromArgb(245, 247, 251);
        private Color chartBorderColor = Color.FromArgb(72, 104, 168);
        private int chartBorderWidth = 2;
        private Color chartBackgroundColor = Color.White;
        private bool useTransparentBackground;

        public LightningRadarCanvasControl()
        {
            InitializeComponent();

            BackColor = Color.White;
            DoubleBuffered = true;
            MinimumSize = new Size(300, 300);
            lblTitle.AutoSize = true;
            lblTitle.Visible = false;
            lblTitle.Font = new Font("맑은 고딕", 10f, FontStyle.Bold);
            flpLegend.AutoSize = true;
            flpLegend.Dock = DockStyle.None;
            pnlHeader.Height = 34;

            radar = LightningRadar.AttachTo<LightningRadar>(pnlCanvas, DockStyle.None, null, new LightningRadarOptions
            {
                // 제목/범례는 외부 UI로 분리
                ShowTitle = false,
                ShowLegend = false,

                // 캔버스형으로 깔끔하게
                BackgroundColor = Color.White,
                ChartPadding = 4,
                TopOffset = 4,
                RadiusPadding = 2f,
                CategoryLabelOffset = 4f,
                CategoryFontSize = 9f,
                ScaleFontSize = 7f,
                ScaleLabelDisplayMode = LightningRadarScaleLabelDisplayMode.All,
                ScaleLabelValueMode = LightningRadarScaleLabelValueMode.RingIndex0ToGridRingCount
            });

            Resize += LightningRadarCanvasControl_Resize;
            pnlCanvas.Resize += PnlCanvas_Resize;
            pnlHeader.Resize += PnlHeader_Resize;
            pnlRoot.Paint += PnlRoot_Paint;
            pnlCanvas.Paint += PnlCanvas_Paint;

            ApplySampleData();
            ApplyAppearance();
            UpdateRadarBounds();
        }

        public Color UserControlBorderColor
        {
            get { return userControlBorderColor; }
            set
            {
                userControlBorderColor = value;
                pnlRoot.Invalidate();
            }
        }

        public int UserControlBorderWidth
        {
            get { return userControlBorderWidth; }
            set
            {
                userControlBorderWidth = Math.Max(0, value);
                pnlRoot.Invalidate();
            }
        }

        public Color UserControlBackgroundColor
        {
            get { return userControlBackgroundColor; }
            set
            {
                userControlBackgroundColor = value;
                ApplyAppearance();
            }
        }

        public Color HeaderBackgroundColor
        {
            get { return headerBackgroundColor; }
            set
            {
                headerBackgroundColor = value;
                ApplyAppearance();
            }
        }

        public Color CanvasBackgroundColor
        {
            get { return canvasBackgroundColor; }
            set
            {
                canvasBackgroundColor = value;
                ApplyAppearance();
            }
        }

        public Color ChartBorderColor
        {
            get { return chartBorderColor; }
            set
            {
                chartBorderColor = value;
                pnlCanvas.Invalidate();
            }
        }

        public int ChartBorderWidth
        {
            get { return chartBorderWidth; }
            set
            {
                chartBorderWidth = Math.Max(0, value);
                pnlCanvas.Invalidate();
            }
        }

        public Color ChartBackgroundColor
        {
            get { return chartBackgroundColor; }
            set
            {
                chartBackgroundColor = value;
                ApplyAppearance();
            }
        }

        public bool UseTransparentBackground
        {
            get { return useTransparentBackground; }
            set
            {
                useTransparentBackground = value;
                ApplyAppearance();
            }
        }

        public void SetData(IEnumerable<string> categories, IEnumerable<LightningRadarSeries> series)
        {
            radar.SetData(categories, series);
            RebuildLegend(series);
        }

        public void SetChartAlignment(HorizontalAlignment alignment)
        {
            ChartAlignment = alignment;
            UpdateRadarBounds();
        }

        public HorizontalAlignment ChartAlignment { get; set; } = HorizontalAlignment.Center;

        public string ChartTitle
        {
            get { return lblTitle.Text; }
            set { lblTitle.Text = value ?? string.Empty; }
        }

        public LightningRadar Radar
        {
            get { return radar; }
        }

        private void LightningRadarCanvasControl_Resize(object sender, EventArgs e)
        {
            UpdateRadarBounds();
        }

        private void PnlHeader_Resize(object sender, EventArgs e)
        {
            UpdateHeaderLayout();
        }

        private void PnlCanvas_Resize(object sender, EventArgs e)
        {
            UpdateRadarBounds();
        }

        private void UpdateRadarBounds()
        {
            if (pnlCanvas.ClientSize.Width <= 1 || pnlCanvas.ClientSize.Height <= 1)
            {
                return;
            }

            radar.Bounds = pnlCanvas.ClientRectangle;
            pnlCanvas.Invalidate();
            UpdateHeaderLayout();
        }

        private void UpdateHeaderLayout()
        {
            if (radar == null)
            {
                return;
            }

            int legendY = Math.Max(0, (pnlHeader.Height - flpLegend.Height) / 2);
            int legendX;
            switch (radar.Options.LegendLabelLocation)
            {
                case LightningRadarLegendLabelLocation.TopLeft:
                    legendX = 8;
                    break;
                case LightningRadarLegendLabelLocation.TopRight:
                    legendX = pnlHeader.ClientSize.Width - flpLegend.Width - 8;
                    break;
                case LightningRadarLegendLabelLocation.TopCenter:
                default:
                    legendX = (pnlHeader.ClientSize.Width - flpLegend.Width) / 2;
                    break;
            }

            legendX = Math.Max(4, legendX);

            flpLegend.Location = new Point(legendX, legendY);
        }

        private void ApplyAppearance()
        {
            Color baseBackColor = useTransparentBackground ? Color.Transparent : userControlBackgroundColor;
            Color headerBackColor = useTransparentBackground ? Color.Transparent : headerBackgroundColor;
            Color canvasBackColor = useTransparentBackground ? Color.Transparent : canvasBackgroundColor;

            BackColor = baseBackColor;
            pnlRoot.BackColor = baseBackColor;
            pnlHeader.BackColor = headerBackColor;
            pnlCanvas.BackColor = canvasBackColor;
            flpLegend.BackColor = Color.Transparent;
            lblTitle.ForeColor = Color.FromArgb(45, 58, 86);

            var currentOptions = radar.Options;
            currentOptions.BackgroundColor = useTransparentBackground ? Color.Transparent : chartBackgroundColor;
            radar.SetOptions(currentOptions);

            Invalidate(true);
        }

        private void PnlRoot_Paint(object sender, PaintEventArgs e)
        {
            if (userControlBorderWidth <= 0)
            {
                return;
            }

            using (var pen = new Pen(userControlBorderColor, userControlBorderWidth))
            {
                pen.Alignment = PenAlignment.Inset;
                e.Graphics.DrawRectangle(pen, 0, 0, pnlRoot.Width - 1, pnlRoot.Height - 1);
            }
        }

        private void PnlCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (chartBorderWidth <= 0 || radar == null)
            {
                return;
            }

            Rectangle rect = radar.Bounds;
            if (rect.Width <= 1 || rect.Height <= 1)
            {
                return;
            }

            using (var pen = new Pen(chartBorderColor, chartBorderWidth))
            {
                pen.Alignment = PenAlignment.Inset;
                e.Graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
            }
        }

        private void ApplySampleData()
        {
            var categories = new[] { "품질", "생산성", "안전", "원가", "납기" };
            var series = new[]
            {
                new LightningRadarSeries
                {
                    Name = "설비 A",
                    Values = new[] { 88f, 82f, 91f, 79f, 95f },
                    FillColor = Color.FromArgb(110, 255, 196, 214),
                    LineColor = Color.FromArgb(230, 225, 104, 150)
                },
                new LightningRadarSeries
                {
                    Name = "설비 B",
                    Values = new[] { 76f, 73f, 86f, 70f, 84f },
                    FillColor = Color.FromArgb(95, 186, 235, 255),
                    LineColor = Color.FromArgb(230, 74, 166, 224)
                }
            };

            radar.SetData(categories, series);
            RebuildLegend(series);
        }

        private void RebuildLegend(IEnumerable<LightningRadarSeries> series)
        {
            flpLegend.SuspendLayout();
            flpLegend.Controls.Clear();

            if (series != null)
            {
                foreach (var item in series)
                {
                    flpLegend.Controls.Add(CreateLegendItem(item));
                }
            }

            flpLegend.ResumeLayout();
            UpdateHeaderLayout();
        }

        private static Control CreateLegendItem(LightningRadarSeries series)
        {
            var panel = new Panel
            {
                Width = 120,
                Height = 24,
                Margin = new Padding(0, 0, 14, 0),
                BackColor = Color.Transparent
            };

            var swatch = new Panel
            {
                Width = 18,
                Height = 12,
                Left = 0,
                Top = 6,
                BackColor = series == null ? Color.LightGray : series.FillColor
            };

            var label = new Label
            {
                AutoSize = true,
                Left = 24,
                Top = 4,
                Font = new Font("맑은 고딕", 9f),
                ForeColor = Color.FromArgb(70, 70, 70),
                Text = series == null ? string.Empty : (series.Name ?? string.Empty)
            };

            panel.Controls.Add(swatch);
            panel.Controls.Add(label);
            return panel;
        }
    }
}
