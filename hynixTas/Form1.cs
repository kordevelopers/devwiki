using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace hynixTas
{
    public partial class Form1 : Form
    {
        private readonly string[] categories = { "Category A", "Category B", "Category C", "Category D", "Category E" };
        private readonly float[] series1 = { 88f, 82f, 91f, 79f, 95f };
        private readonly float[] series2 = { 76f, 73f, 86f, 70f, 84f };
        private readonly Color series1FillColor = Color.FromArgb(110, 244, 194, 210);
        private readonly Color series1LineColor = Color.FromArgb(220, 229, 145, 175);
        private readonly Color series2FillColor = Color.FromArgb(95, 255, 214, 224);
        private readonly Color series2LineColor = Color.FromArgb(220, 214, 112, 151);

        public Form1()
        {
            InitializeComponent();

            DoubleBuffered = true;
            BackColor = Color.White;
            Text = "Radar Chart Sample";

            Paint += Form1_Paint;
            Resize += Form1_Resize;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Color.White);

            DrawTitle(e.Graphics);
            DrawRadarChart(e.Graphics);
            DrawLegend(e.Graphics);
        }

        private void DrawTitle(Graphics graphics)
        {
            using (var titleFont = new Font(Font.FontFamily, 12f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(90, 90, 90)))
            {
                graphics.DrawString("Radar Chart Sample", titleFont, titleBrush, 20f, 15f);
            }
        }

        private void DrawRadarChart(Graphics graphics)
        {
            const int padding = 40;
            const int legendWidth = 180;
            const int topOffset = 50;
            int diameter = Math.Min(ClientSize.Width - legendWidth - (padding * 2), ClientSize.Height - topOffset - padding);

            if (diameter <= 100)
            {
                return;
            }

            float radius = (diameter / 2f) - 15f;
            PointF center = new PointF(padding + (diameter / 2f), topOffset + (diameter / 2f));

            DrawGrid(graphics, center, radius);
            DrawCategories(graphics, center, radius);
            DrawSeries(graphics, center, radius, series2, series2FillColor, series2LineColor);
            DrawSeries(graphics, center, radius, series1, series1FillColor, series1LineColor);

            using (var centerFont = new Font(Font.FontFamily, 8.5f, FontStyle.Regular))
            using (var centerBrush = new SolidBrush(Color.FromArgb(110, 110, 110)))
            {
                SizeF scaleTextSize = graphics.MeasureString("0~100", centerFont);
                graphics.DrawString("0~100", centerFont, centerBrush, center.X - (scaleTextSize.Width / 2f), center.Y - (scaleTextSize.Height / 2f));
            }
        }

        private void DrawGrid(Graphics graphics, PointF center, float radius)
        {
            using (var gridPen = new Pen(Color.FromArgb(215, 215, 215), 1f))
            using (var spokePen = new Pen(Color.FromArgb(225, 225, 225), 1f))
            {
                for (int i = 1; i <= 5; i++)
                {
                    float currentRadius = radius * i / 5f;
                    graphics.DrawEllipse(gridPen, center.X - currentRadius, center.Y - currentRadius, currentRadius * 2f, currentRadius * 2f);
                }

                for (int i = 0; i < categories.Length; i++)
                {
                    PointF outerPoint = GetRadarPoint(center, radius, i, categories.Length);
                    graphics.DrawLine(spokePen, center, outerPoint);
                }
            }
        }

        private void DrawCategories(Graphics graphics, PointF center, float radius)
        {
            using (var labelFont = new Font(Font.FontFamily, 8.5f, FontStyle.Regular))
            using (var labelBrush = new SolidBrush(Color.FromArgb(95, 95, 95)))
            {
                for (int i = 0; i < categories.Length; i++)
                {
                    PointF point = GetRadarPoint(center, radius + 18f, i, categories.Length);
                    SizeF labelSize = graphics.MeasureString(categories[i], labelFont);
                    graphics.DrawString(categories[i], labelFont, labelBrush, point.X - (labelSize.Width / 2f), point.Y - (labelSize.Height / 2f));
                }
            }
        }

        private void DrawSeries(Graphics graphics, PointF center, float radius, float[] values, Color fillColor, Color lineColor)
        {
            PointF[] points = values
                .Select((value, index) => GetRadarPoint(center, radius * Math.Max(0f, Math.Min(100f, value)) / 100f, index, values.Length))
                .ToArray();

            using (var fillBrush = new SolidBrush(fillColor))
            using (var linePen = new Pen(lineColor, 2f))
            using (var pointBrush = new SolidBrush(lineColor))
            {
                graphics.FillPolygon(fillBrush, points);
                graphics.DrawPolygon(linePen, points);

                foreach (PointF point in points)
                {
                    graphics.FillEllipse(pointBrush, point.X - 4f, point.Y - 4f, 8f, 8f);
                }
            }
        }

        private void DrawLegend(Graphics graphics)
        {
            float legendX = ClientSize.Width - 150f;
            float legendY = 85f;

            using (var legendFont = new Font(Font.FontFamily, 9f, FontStyle.Regular))
            using (var textBrush = new SolidBrush(Color.FromArgb(90, 90, 90)))
            {
                DrawLegendItem(graphics, legendFont, textBrush, legendX, legendY, series1FillColor, series1LineColor, "Series 1");
                DrawLegendItem(graphics, legendFont, textBrush, legendX, legendY + 34f, series2FillColor, series2LineColor, "Series 2");
            }
        }

        private void DrawLegendItem(Graphics graphics, Font font, Brush textBrush, float x, float y, Color fillColor, Color lineColor, string text)
        {
            RectangleF markerRect = new RectangleF(x, y, 20f, 14f);

            using (var fillBrush = new SolidBrush(fillColor))
            using (var borderPen = new Pen(lineColor, 1.5f))
            {
                graphics.FillRectangle(fillBrush, markerRect);
                graphics.DrawRectangle(borderPen, markerRect.X, markerRect.Y, markerRect.Width, markerRect.Height);
            }

            graphics.DrawString(text, font, textBrush, x + 28f, y - 2f);
        }

        private PointF GetRadarPoint(PointF center, float radius, int index, int totalCount)
        {
            double angle = (-Math.PI / 2d) + ((Math.PI * 2d * index) / totalCount);
            float x = center.X + (float)(Math.Cos(angle) * radius);
            float y = center.Y + (float)(Math.Sin(angle) * radius);

            return new PointF(x, y);
        }
    }
}
