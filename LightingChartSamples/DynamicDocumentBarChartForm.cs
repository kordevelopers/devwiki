using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LightingChartSamples
{
    public class DynamicDocumentBarChartForm : Form
    {
        private const int TopSectionHeight = 400;
        private const int ChartColumnCount = 2;
        private const int MinimumChartRowCount = 3;
        private const int EmptyChartRowHeight = 100;
        private static readonly Size DefaultChartSize = new Size(400, 400);

        private readonly Panel scrollPanel;
        private readonly TableLayoutPanel contentTable;
        private readonly SplitContainer documentSplit;
        private readonly RichTextBox documentBox;
        private readonly PictureBox previewBox;
        private readonly TableLayoutPanel chartTable;
        private readonly List<Control> chartHosts = new List<Control>();
        private IList<BarChartLayoutData> currentChartData;
        private bool splitDistanceInitialized;

        public DynamicDocumentBarChartForm()
        {
            Text = "Dynamic Document and Bar Chart Layout";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(960, 700);
            ClientSize = new Size(1200, 850);
            BackColor = Color.White;

            scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(242, 245, 249),
                Padding = new Padding(12)
            };

            contentTable = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            contentTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentTable.RowStyles.Add(new RowStyle(SizeType.Absolute, TopSectionHeight));
            contentTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            documentSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 6,
                Margin = new Padding(0),
                BackColor = Color.FromArgb(185, 193, 205)
            };
            documentSplit.Resize += DocumentSplit_Resize;

            documentBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle, 
                BackColor = Color.White,
                Font = new Font("맑은 고딕", 10F),
                DetectUrls = true,
                ReadOnly = true
            };

            previewBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = CreatePreviewImage(900, 560)
            };

            chartTable = CreateChartTable();

            documentSplit.Panel1.Controls.Add(documentBox);
            documentSplit.Panel2.Controls.Add(previewBox);
            contentTable.Controls.Add(documentSplit, 0, 0);
            contentTable.Controls.Add(chartTable, 0, 1);
            scrollPanel.Controls.Add(contentTable);
            Controls.Add(scrollPanel);

            Resize += DynamicDocumentBarChartForm_Resize;
            PopulateRichDocument();
            LoadCharts(CreateSampleCharts());
            UpdateContentWidth();
        }

        public void LoadCharts(IList<BarChartLayoutData> chartData)
        {
            int rowCount = CalculateRequiredRowCount(chartData);
            IList<BarChartLayoutData> normalizedData = NormalizeChartData(chartData, rowCount);
            currentChartData = normalizedData;

            chartTable.SuspendLayout();
            chartTable.Controls.Clear();
            chartHosts.Clear();
            ConfigureChartRows(rowCount);

            for (int index = 0; index < ChartColumnCount * rowCount; index++)
            {
                int row = index / ChartColumnCount;
                int column = index % ChartColumnCount;
                Control host = CreateChartHost(normalizedData[index], index + 1);
                chartHosts.Add(host);
                chartTable.Controls.Add(host, column, row);
            }

            ApplyDynamicChartRows(normalizedData);
            chartTable.ResumeLayout(true);
            contentTable.PerformLayout();
        }

        private static TableLayoutPanel CreateChartTable()
        {
            var table = new TableLayoutPanel
            {
                ColumnCount = ChartColumnCount,
                RowCount = MinimumChartRowCount,
                Dock = DockStyle.Top,
                AutoSize = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            for (int row = 0; row < MinimumChartRowCount; row++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, EmptyChartRowHeight));
            }

            return table;
        }

        private Control CreateChartHost(BarChartLayoutData chartData, int slotNumber)
        {
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(6),
                Padding = new Padding(1)
            };
            host.Paint += ChartHost_Paint;

            if (chartData == null || !chartData.HasData)
            {
                host.BackColor = Color.FromArgb(247, 249, 252);
                host.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    Text = string.Format("차트 {0}: 데이터 없음", slotNumber),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(125, 133, 146),
                    BackColor = Color.Transparent
                });
                return host;
            }

            LightningBar.Create(host, chartData.Categories, chartData.Series, CreateBarOptions(chartData.Title));
            return host;
        }

        private void ApplyDynamicChartRows(IList<BarChartLayoutData> chartData)
        {
            int totalHeight = 0;

            for (int row = 0; row < chartTable.RowCount; row++)
            {
                IEnumerable<BarChartLayoutData> rowData = Enumerable.Range(row * ChartColumnCount, ChartColumnCount)
                    .Select(index => chartData[index]);
                bool hasChart = rowData.Any(item => item != null && item.HasData);
                int rowHeight = hasChart ? CalculateActiveChartRowHeight(rowData) : EmptyChartRowHeight;
                chartTable.RowStyles[row].SizeType = SizeType.Absolute;
                chartTable.RowStyles[row].Height = rowHeight;
                totalHeight += rowHeight;
            }

            chartTable.Height = totalHeight;
            chartTable.Visible = totalHeight > 0;
        }

        private void DynamicDocumentBarChartForm_Resize(object sender, EventArgs e)
        {
            UpdateContentWidth();
            if (currentChartData != null)
            {
                ApplyDynamicChartRows(currentChartData);
            }
        }

        private void DocumentSplit_Resize(object sender, EventArgs e)
        {
            int availableWidth = documentSplit.ClientSize.Width - documentSplit.SplitterWidth;
            if (availableWidth <= 0)
            {
                return;
            }

            int minSize = Math.Min(280, availableWidth / 2);
            documentSplit.Panel1MinSize = minSize;
            documentSplit.Panel2MinSize = minSize;

            if (!splitDistanceInitialized)
            {
                int minDistance = documentSplit.Panel1MinSize;
                int maxDistance = availableWidth - documentSplit.Panel2MinSize;
                if (maxDistance >= minDistance)
                {
                    int targetDistance = Math.Max(minDistance, Math.Min(availableWidth / 2, maxDistance));
                    documentSplit.SplitterDistance = targetDistance;
                    splitDistanceInitialized = true;
                }
            }
        }

        private void UpdateContentWidth()
        {
            int width = scrollPanel.ClientSize.Width - scrollPanel.Padding.Horizontal;
            if (scrollPanel.VerticalScroll.Visible)
            {
                width -= SystemInformation.VerticalScrollBarWidth;
            }

            contentTable.Width = Math.Max(600, width);
        }

        private static int CalculateActiveChartRowHeight(IEnumerable<BarChartLayoutData> rowData)
        {
            return rowData
                .Where(item => item != null && item.HasData)
                .Select(item => Math.Max(100, item.ChartSize.Height))
                .DefaultIfEmpty(DefaultChartSize.Height)
                .Max();
        }

        private void ChartHost_Paint(object sender, PaintEventArgs e)
        {
            Control control = sender as Control;
            if (control == null || !control.Visible)
            {
                return;
            }

            using (var pen = new Pen(Color.FromArgb(182, 190, 202)))
            {
                pen.Alignment = PenAlignment.Inset;
                e.Graphics.DrawRectangle(pen, 0, 0, control.Width - 1, control.Height - 1);
            }
        }

        private static int CalculateRequiredRowCount(IList<BarChartLayoutData> chartData)
        {
            int dataSlotCount = chartData == null ? 0 : chartData.Count;
            return Math.Max(MinimumChartRowCount, (int)Math.Ceiling(dataSlotCount / (double)ChartColumnCount));
        }

        private void ConfigureChartRows(int rowCount)
        {
            chartTable.RowCount = rowCount;
            chartTable.RowStyles.Clear();

            for (int row = 0; row < rowCount; row++)
            {
                chartTable.RowStyles.Add(new RowStyle(SizeType.Absolute, EmptyChartRowHeight));
            }
        }

        private static IList<BarChartLayoutData> NormalizeChartData(IList<BarChartLayoutData> chartData, int rowCount)
        {
            var normalized = new List<BarChartLayoutData>();
            int slotCount = ChartColumnCount * rowCount;

            for (int index = 0; index < slotCount; index++)
            {
                normalized.Add(chartData != null && index < chartData.Count ? chartData[index] : null);
            }

            return normalized;
        }

        private static LightningBarOptions CreateBarOptions(string title)
        {
            return new LightningBarOptions
            {
                Title = title ?? string.Empty,
                TitleFontSize = 11F,
                ChartPadding = 22,
                TopOffset = 62,
                LegendWidth = 130,
                GridLineCount = 5,
                CategoryFontSize = 8F,
                CategoryLabelMaxLines = 3,
                ScaleFontSize = 8F,
                BarBorderWidth = 1F,
                BarGap = 5F,
                GroupPaddingRatio = 0.15F,
                MaxValue = 100F,
                SeriesLabelEnabled = false,
                SeriesLabelMaxLines = 3,
                SeriesLabelMaxWidth = 120F,
                BackgroundColor = Color.White
            };
        }

        private static IList<BarChartLayoutData> CreateSampleCharts()
        {
            string[] categories = { "품\n질\n ", "생\n산\n성", "안\n전\n ", "원\n가\n ", "납\n기\n " };

            return new[]
            {
                CreateChartData("생산 지표", categories, Color.FromArgb(76, 132, 210), new[] { 88f, 82f, 91f, 79f, 95f }),
                CreateChartData("운영 지표", categories, Color.FromArgb(86, 166, 112), new[] { 76f, 90f, 84f, 72f, 88f }),
                CreateChartData("품질 지표", categories, Color.FromArgb(229, 148, 65), new[] { 92f, 74f, 89f, 85f, 81f })
            };
        }

        private static BarChartLayoutData CreateChartData(string title, IEnumerable<string> categories, Color color, float[] values)
        {
            return new BarChartLayoutData
            {
                HasData = true,
                Title = title,
                Categories = categories,
                Series = new[]
                {
                    new LightningBarSeries
                    {
                        Name = "현재",
                        LegendLabel = "1234567890\nABCDEFGHIJ\nKLMNOPQRST",
                        Values = values,
                        FillColor = Color.FromArgb(175, color),
                        BorderColor = color
                    }
                }
            };
        }

        private void PopulateRichDocument()
        {
            documentBox.Clear();
            documentBox.SelectionIndent = 0;
            documentBox.SelectionHangingIndent = 0;
            Color ivoryBackColor = Color.FromArgb(255, 255, 240);

            Font titleFont = new Font("맑은 고딕", 14F, FontStyle.Bold);
            Font headingFont = new Font("맑은 고딕", 12F, FontStyle.Bold);
            Font bodyFont = new Font("맑은 고딕", 11F);
            Font tipFont = new Font("맑은 고딕", 10.5F);
            Font chipFont = new Font("맑은 고딕", 9.5F, FontStyle.Bold);

            AppendStyledText("3. 비즈니스 보고서 요약 샘플 (개조식)\n", titleFont, Color.Black);
            AppendStyledText("  LIVE  ", chipFont, Color.White, Color.FromArgb(236, 95, 80));
            AppendStyledText("  KPI TRACKING  ", chipFont, Color.White, Color.FromArgb(58, 136, 255));
            AppendStyledText("\n\n", bodyFont, Color.Black);
            AppendStyledText("실제 업무에서 사용하는 결론 중심의 핵심 보고서 텍스트 양식입니다.\n", bodyFont, Color.FromArgb(57, 67, 89));
            AppendStyledText("────────────────────────────────────────\n\n", bodyFont, Color.FromArgb(225, 229, 236));

            AppendBulletHeading("[추진 배경] ", headingFont);
            AppendStyledText("분기별 매출 감소에 따른 온·오프라인 채널 통합 마케팅 전략 변경 필요성 대두\n\n", bodyFont, Color.Black, ivoryBackColor);
            AppendBulletHeading("[개선 방안]\n", headingFont);
            AppendSubBullet("모바일 앱 UI/UX 개편을 통한 사용자 편의성 증대", bodyFont, ivoryBackColor);
            AppendSubBullet("타겟 고객층(2030 세대) 맞춤형 프로모션 전개\n", bodyFont, ivoryBackColor);
            AppendBulletHeading("[기대 효과] ", headingFont);
            AppendStyledText("신규 고객 유입률 ", bodyFont, Color.Black, ivoryBackColor);
            AppendStyledText("15%", new Font("맑은 고딕", 11F, FontStyle.Bold), Color.FromArgb(35, 84, 148), ivoryBackColor);
            AppendStyledText(" 증가 및 기존 고객 이탈율 감소 기대\n\n", bodyFont, Color.Black, ivoryBackColor);
            AppendStyledText("Next Action: ", new Font("맑은 고딕", 10.5F, FontStyle.Bold), Color.FromArgb(60, 85, 128));
            AppendStyledText("다음 주까지 A/B 테스트 2종과 채널별 CAC 리포트 업데이트\n\n", bodyFont, Color.FromArgb(72, 84, 104));

            Color tipBackColor = Color.FromArgb(237, 239, 242);
            Color tipForeColor = Color.FromArgb(35, 44, 62);
            AppendStyledText("  Tip: ", new Font("맑은 고딕", 10.5F, FontStyle.Bold), tipForeColor, tipBackColor, false);
            AppendStyledText("한글 문장의 다양한 더미 텍스트가 필요하다면 한글 Lorem Ipsum 생성기를 활용할 수 있습니다.  \n", tipFont, tipForeColor, tipBackColor, false);

            documentBox.SelectionStart = 0;
            documentBox.SelectionLength = 0;
            documentBox.ScrollToCaret();
        }

        private void AppendBulletHeading(string text, Font font)
        {
            documentBox.SelectionBullet = true;
            documentBox.SelectionIndent = 0;
            documentBox.SelectionHangingIndent = 0;
            AppendStyledText(text, font, Color.Red);
            documentBox.SelectionBullet = false;
        }

        private void AppendSubBullet(string text, Font font, Color backColor)
        {
            documentBox.SelectionIndent = 25;
            documentBox.SelectionHangingIndent = 15;
            documentBox.SelectionBullet = true;
            AppendStyledText(text + "\n", font, Color.Black, backColor);
            documentBox.SelectionBullet = false;
            documentBox.SelectionIndent = 0;
            documentBox.SelectionHangingIndent = 0;
        }

        private void AppendStyledText(string text, Font font, Color foreColor, Color? backColor = null, bool normalizeParagraph = true)
        {
            string resolvedText = normalizeParagraph ? NormalizeParagraphText(text) : text;
            documentBox.SelectionStart = documentBox.TextLength;
            documentBox.SelectionLength = 0;
            documentBox.SelectionFont = font;
            documentBox.SelectionColor = foreColor;
            documentBox.SelectionBackColor = backColor ?? documentBox.BackColor;
            documentBox.AppendText(resolvedText);
            documentBox.SelectionBackColor = documentBox.BackColor;
        }

        private static string NormalizeParagraphText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", " ");
            string[] lines = normalized.Split(new[] { '\n' }, StringSplitOptions.None);
            var builder = new StringBuilder();

            for (int index = 0; index < lines.Length; index++)
            {
                string line = index > 0 ? lines[index].Trim() : lines[index].TrimEnd();
                if (index > 0)
                {
                    builder.Append("\n");
                }

                builder.Append(line);
            }

            return builder.ToString();
        }

        private static Image CreatePreviewImage(int width, int height)
        {
            var bitmap = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (var background = new SolidBrush(Color.FromArgb(246, 248, 251)))
            using (var blue = new SolidBrush(Color.FromArgb(76, 132, 210)))
            using (var green = new SolidBrush(Color.FromArgb(86, 166, 112)))
            using (var orange = new SolidBrush(Color.FromArgb(229, 148, 65)))
            using (var border = new Pen(Color.FromArgb(190, 198, 210), 2F))
            using (var titleFont = new Font("맑은 고딕", 24F, FontStyle.Bold))
            using (var bodyFont = new Font("맑은 고딕", 12F))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillRectangle(background, 0, 0, width, height);
                graphics.DrawRectangle(border, 25, 25, width - 50, height - 50);
                graphics.DrawString("업무 현황 미리보기", titleFont, Brushes.DimGray, 55, 55);
                graphics.FillRectangle(blue, 70, 150, 190, 230);
                graphics.FillRectangle(green, 300, 210, 190, 170);
                graphics.FillRectangle(orange, 530, 265, 190, 115);
                graphics.DrawString("품질", bodyFont, Brushes.White, 140, 392);
                graphics.DrawString("생산성", bodyFont, Brushes.White, 365, 392);
                graphics.DrawString("납기", bodyFont, Brushes.White, 600, 392);
            }

            return bitmap;
        }
    }

    public class BarChartLayoutData
    {
        public BarChartLayoutData()
        {
            ChartSize = new Size(400, 400);
        }

        public bool HasData { get; set; }

        public string Title { get; set; }

        public Size ChartSize { get; set; }

        public IEnumerable<string> Categories { get; set; }

        public IEnumerable<LightningBarSeries> Series { get; set; }
    }
}
