using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LightingChartSamples
{
    public class TabbedLayoutSampleForm : Form
    {
        private const int ChartColumnCount = 2;
        private const int ChartSlotCount = 5;
        private const int ChartRowCount = 3;
        private const int FixedFirstRowHeight = 450;
        private const int ActiveChartRowHeight = 450;
        private const int EmptyChartRowHeight = 100;

        private readonly TabControl tabControl;
        private readonly RichTextBox richTextBox;
        private readonly PictureBox pictureBox;
        private readonly Panel chartScrollPanel;
        private readonly TableLayoutPanel chartTable;
        private readonly List<Control> chartSlotControls = new List<Control>();

        public TabbedLayoutSampleForm()
        {
            Text = "Tabbed Layout Sample";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(920, 680);
            ClientSize = new Size(1120, 760);
            BackColor = Color.White;

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F)
            };

            richTextBox = new RichTextBox();
            pictureBox = new PictureBox();
            chartScrollPanel = new Panel();
            chartTable = new TableLayoutPanel();

            tabControl.TabPages.Add(CreateDocumentTab());
            tabControl.TabPages.Add(CreateChartTab());
            Controls.Add(tabControl);

            LoadDocumentSample();
            LoadCharts(CreateChartData(ChartSlotCount));
        }

        public void LoadCharts(IList<ChartLayoutData> chartData)
        {
            chartTable.SuspendLayout();
            chartTable.Controls.Clear();
            chartSlotControls.Clear();

            var normalizedData = NormalizeChartData(chartData);

            for (int slotIndex = 0; slotIndex < ChartSlotCount; slotIndex++)
            {
                int row = slotIndex / ChartColumnCount;
                int column = slotIndex % ChartColumnCount;
                Control slotControl = CreateChartSlot(slotIndex, normalizedData[slotIndex]);
                chartSlotControls.Add(slotControl);
                chartTable.Controls.Add(slotControl, column, row);
            }

            ApplyChartRowHeights(normalizedData);
            chartTable.ResumeLayout(true);
        }

        private TabPage CreateDocumentTab()
        {
            var tabPage = new TabPage("Document / Image")
            {
                BackColor = Color.White,
                Padding = new Padding(8)
            };

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 6,
                SplitterDistance = 520,
                Panel1MinSize = 260,
                Panel2MinSize = 260
            };

            richTextBox.Dock = DockStyle.Fill;
            richTextBox.BorderStyle = BorderStyle.FixedSingle;
            richTextBox.Font = new Font("Segoe UI", 10F);
            richTextBox.BackColor = Color.White;

            pictureBox.Dock = DockStyle.Fill;
            pictureBox.BackColor = Color.FromArgb(245, 247, 250);
            pictureBox.BorderStyle = BorderStyle.FixedSingle;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.Image = CreateSampleImage(720, 420);

            splitContainer.Panel1.Controls.Add(richTextBox);
            splitContainer.Panel2.Controls.Add(pictureBox);
            tabPage.Controls.Add(splitContainer);

            return tabPage;
        }

        private TabPage CreateChartTab()
        {
            var tabPage = new TabPage("Charts")
            {
                BackColor = Color.White,
                Padding = new Padding(8)
            };

            chartScrollPanel.Dock = DockStyle.Fill;
            chartScrollPanel.AutoScroll = true;
            chartScrollPanel.BackColor = Color.White;

            chartTable.ColumnCount = ChartColumnCount;
            chartTable.RowCount = ChartRowCount;
            chartTable.Dock = DockStyle.Top;
            chartTable.AutoSize = false;
            chartTable.BackColor = Color.White;
            chartTable.Padding = new Padding(0);
            chartTable.Margin = new Padding(0);
            chartTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            chartTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            for (int i = 0; i < ChartRowCount; i++)
            {
                chartTable.RowStyles.Add(new RowStyle(SizeType.Absolute, FixedFirstRowHeight));
            }

            chartScrollPanel.Controls.Add(chartTable);
            tabPage.Controls.Add(chartScrollPanel);

            return tabPage;
        }

        private static IList<ChartLayoutData> NormalizeChartData(IList<ChartLayoutData> chartData)
        {
            var result = new List<ChartLayoutData>();

            for (int i = 0; i < ChartSlotCount; i++)
            {
                ChartLayoutData item = chartData != null && i < chartData.Count ? chartData[i] : null;
                result.Add(item);
            }

            return result;
        }

        private Control CreateChartSlot(int slotIndex, ChartLayoutData chartData)
        {
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(8)
            };

            if (chartData == null || !chartData.HasData)
            {
                host.Controls.Add(CreateEmptySlotLabel(slotIndex + 1));
                return host;
            }

            var chart = new LightningRadarCanvasControl
            {
                Dock = DockStyle.Fill,
                ChartAlignment = HorizontalAlignment.Center,
                UserControlBorderColor = Color.FromArgb(170, 180, 196),
                UserControlBackgroundColor = Color.White,
                HeaderBackgroundColor = Color.FromArgb(238, 242, 248),
                CanvasBackgroundColor = Color.White,
                ChartBorderColor = Color.FromArgb(80, 120, 170),
                ChartBorderWidth = 1
            };

            chart.Radar.SetOptions(CreateRadarOptions());
            chart.SetData(chartData.Categories, chartData.Series);
            host.Controls.Add(chart);
            return host;
        }

        private static Control CreateEmptySlotLabel(int slotNumber)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = string.Format("Chart {0}: No data", slotNumber),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 128, 140),
                BackColor = Color.FromArgb(248, 249, 251),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void ApplyChartRowHeights(IList<ChartLayoutData> chartData)
        {
            int totalHeight = 0;

            for (int row = 0; row < ChartRowCount; row++)
            {
                int rowHeight = GetRowHeight(row, chartData);
                chartTable.RowStyles[row].SizeType = SizeType.Absolute;
                chartTable.RowStyles[row].Height = rowHeight;
                totalHeight += rowHeight;
            }

            chartTable.Height = totalHeight;
        }

        private static int GetRowHeight(int row, IList<ChartLayoutData> chartData)
        {
            if (row == 0)
            {
                return FixedFirstRowHeight;
            }

            bool hasChartData = Enumerable.Range(row * ChartColumnCount, ChartColumnCount)
                .Where(index => index < ChartSlotCount)
                .Any(index => chartData[index] != null && chartData[index].HasData);

            return hasChartData ? ActiveChartRowHeight : EmptyChartRowHeight;
        }

        private static LightningRadarOptions CreateRadarOptions()
        {
            return new LightningRadarOptions
            {
                ShowTitle = false,
                ShowLegend = false,
                BackgroundColor = Color.White,
                ChartPadding = 6,
                TopOffset = 6,
                CategoryLabelOffset = 4f,
                TopCategoryLabelVerticalOffset = 0f,
                RadiusPadding = 2f,
                GridRingCount = 5,
                SeriesLineWidth = 2f,
                SeriesPointSize = 6f,
                CategoryFontSize = 8f,
                ScaleFontSize = 7f,
                ScaleLabelDisplayMode = LightningRadarScaleLabelDisplayMode.All,
                ScaleLabelValueMode = LightningRadarScaleLabelValueMode.RingIndex0ToGridRingCount,
                LegendLabelLocation = LightningRadarLegendLabelLocation.TopCenter,
                CategoryLabelColor = Color.FromArgb(92, 99, 112),
                ScaleLabelColor = Color.FromArgb(110, 116, 128)
            };
        }

        private static IList<ChartLayoutData> CreateChartData(int count)
        {
            var result = new List<ChartLayoutData>();
            var categories = new[] { "Q", "P", "S", "C", "D" };
            var palette = new[]
            {
                Color.FromArgb(225, 104, 150),
                Color.FromArgb(74, 166, 224),
                Color.FromArgb(84, 174, 121),
                Color.FromArgb(235, 150, 64),
                Color.FromArgb(135, 116, 202)
            };

            for (int i = 0; i < Math.Min(count, ChartSlotCount); i++)
            {
                Color lineColor = palette[i % palette.Length];
                result.Add(new ChartLayoutData
                {
                    HasData = true,
                    Title = string.Format("Chart {0}", i + 1),
                    Categories = categories,
                    Series = new[]
                    {
                        new LightningRadarSeries
                        {
                            Name = "A",
                            Values = new[] { 72f + i, 84f - i, 78f + (i * 2), 69f + i, 88f - i },
                            FillColor = Color.FromArgb(95, lineColor),
                            LineColor = lineColor
                        },
                        new LightningRadarSeries
                        {
                            Name = "B",
                            Values = new[] { 66f + i, 74f + i, 82f - i, 73f + i, 79f + i },
                            FillColor = Color.FromArgb(65, 80, 100, 120),
                            LineColor = Color.FromArgb(120, 135, 155)
                        }
                    }
                });
            }

            return result;
        }

        private void LoadDocumentSample()
        {
            richTextBox.Clear();
            richTextBox.SelectionFont = new Font("Segoe UI", 15F, FontStyle.Bold);
            richTextBox.SelectionColor = Color.FromArgb(36, 53, 84);
            richTextBox.AppendText("Tabbed layout document area" + Environment.NewLine + Environment.NewLine);

            richTextBox.SelectionFont = new Font("Segoe UI", 10F, FontStyle.Regular);
            richTextBox.SelectionColor = Color.FromArgb(56, 64, 78);
            richTextBox.AppendText("The first tab is split by a movable splitter. ");
            richTextBox.AppendText("The left pane is a RichTextBox and the right pane is a PictureBox.");
            richTextBox.AppendText(Environment.NewLine + Environment.NewLine);
            richTextBox.AppendText("The second tab uses a scrollable chart layout. Row 1 keeps a fixed height. ");
            richTextBox.AppendText("Rows 2 and 3 shrink to 100 pixels when the row has no chart data.");
        }

        private static Image CreateSampleImage(int width, int height)
        {
            var bitmap = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (var backgroundBrush = new SolidBrush(Color.FromArgb(244, 247, 251)))
            using (var accentBrush = new SolidBrush(Color.FromArgb(75, 123, 213)))
            using (var secondaryBrush = new SolidBrush(Color.FromArgb(100, 165, 99)))
            using (var pen = new Pen(Color.FromArgb(185, 195, 210), 2f))
            using (var titleFont = new Font("Segoe UI", 22F, FontStyle.Bold))
            using (var bodyFont = new Font("Segoe UI", 11F, FontStyle.Regular))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.FillRectangle(backgroundBrush, 0, 0, width, height);
                graphics.DrawRectangle(pen, 24, 24, width - 48, height - 48);
                graphics.FillRectangle(accentBrush, 70, 92, 210, 120);
                graphics.FillRectangle(secondaryBrush, 310, 150, 260, 90);
                graphics.DrawString("PictureBox", titleFont, Brushes.White, 92, 126);
                graphics.DrawString("Resizable preview pane", bodyFont, Brushes.White, 330, 182);
            }

            return bitmap;
        }
    }

    public class ChartLayoutData
    {
        public bool HasData { get; set; }

        public string Title { get; set; }

        public IEnumerable<string> Categories { get; set; }

        public IEnumerable<LightningRadarSeries> Series { get; set; }
    }
}
