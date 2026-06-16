using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LightingChartSamples
{
    public partial class MainWindowForm : Form
    {
        public MainWindowForm()
        {
            InitializeComponent();
            AddTabbedLayoutSampleButton();
            AddDynamicDocumentBarChartButton();
            AddNewBarChartGuideButton();
            AddBarChartImageExportSampleButton();
        }

        private void btnTextDocument_Click(object sender, EventArgs e)
        {
            using (var form = new TextDocument())
            {
                form.ShowDialog(this);
            }
        }

        private void btnLightningBar_Click(object sender, EventArgs e)
        {
            using (var form = new LightningBarSample())
            {
                form.ShowDialog(this);
            }
        }

        private void btnLightningRadar_Click(object sender, EventArgs e)
        {
            using (var form = new LightningRadarSample())
            {
                form.ShowDialog(this);
            }
        }

        private void btnRadarChartSample_Click(object sender, EventArgs e)
        {
            using (var form = new Form1())
            {
                form.ShowDialog(this);
            }
        }

        private void btnTabbedLayoutSample_Click(object sender, EventArgs e)
        {
            using (var form = new TabbedLayoutSampleForm())
            {
                form.ShowDialog(this);
            }
        }

        private void btnDynamicDocumentBarChart_Click(object sender, EventArgs e)
        {
            using (var form = new DynamicDocumentBarChartForm())
            {
                form.ShowDialog(this);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnNewBarChartGuide_Click(object sender, EventArgs e)
        {
            using (var form = new NewBarChartGuideForm())
            {
                form.ShowDialog(this);
            }
        }

        private void btnBarChartImageExportSample_Click(object sender, EventArgs e)
        {
            using (var form = new LightningBarImageExportSampleForm())
            {
                form.ShowDialog(this);
            }
        }

        private void AddTabbedLayoutSampleButton()
        {
            var button = new Button
            {
                BackColor = Color.FromArgb(60, 132, 150),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(32, 306),
                Name = "btnTabbedLayoutSample",
                Size = new Size(456, 46),
                TabIndex = 5,
                Text = "탭 레이아웃 샘플 열기 (TabbedLayoutSampleForm)",
                UseVisualStyleBackColor = false
            };

            button.Click += btnTabbedLayoutSample_Click;
            pnlContainer.Controls.Add(button);

            btnClose.Location = new Point(392, 365);
            btnClose.TabIndex = 6;
            ClientSize = new Size(520, 414);
        }

        private void AddDynamicDocumentBarChartButton()
        {
            var button = new Button
            {
                BackColor = Color.FromArgb(74, 118, 164),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(32, 364),
                Name = "btnDynamicDocumentBarChart",
                Size = new Size(456, 46),
                TabIndex = 6,
                Text = "동적 문서/바 차트 화면 열기",
                UseVisualStyleBackColor = false
            };

            button.Click += btnDynamicDocumentBarChart_Click;
            pnlContainer.Controls.Add(button);

            btnClose.Location = new Point(392, 481);
            btnClose.TabIndex = 8;
            ClientSize = new Size(520, 530);
        }

        private void AddNewBarChartGuideButton()
        {
            var button = new Button
            {
                BackColor = Color.FromArgb(56, 149, 138),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(32, 422),
                Name = "btnNewBarChartGuide",
                Size = new Size(456, 46),
                TabIndex = 7,
                Text = "신규 Bar Chart 가이드 열기 (5개 시리즈)",
                UseVisualStyleBackColor = false
            };

            button.Click += btnNewBarChartGuide_Click;
            pnlContainer.Controls.Add(button);

            btnClose.Location = new Point(392, 481);
            btnClose.TabIndex = 8;
            ClientSize = new Size(520, 530);
        }

        private void AddBarChartImageExportSampleButton()
        {
            var button = new Button
            {
                BackColor = Color.FromArgb(118, 91, 166),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(32, 480),
                Name = "btnBarChartImageExportSample",
                Size = new Size(456, 46),
                TabIndex = 8,
                Text = "Bar Chart Image Export Sample",
                UseVisualStyleBackColor = false
            };

            button.Click += btnBarChartImageExportSample_Click;
            pnlContainer.Controls.Add(button);

            btnClose.Location = new Point(392, 539);
            btnClose.TabIndex = 9;
            ClientSize = new Size(520, 588);
        }
    }

    public class NewBarChartGuideForm : Form
    {
        private readonly LightningBar barChart;
        private readonly Panel pnlTop;
        private readonly Panel pnlChartHost;
        private readonly Button btnChangeTitle;

        public NewBarChartGuideForm()
        {
            BackColor = Color.White;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "New Bar Chart Guide";
            ClientSize = new Size(1100, 690);

            pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.White
            };

            var lblGuide = new Label
            {
                AutoSize = true,
                Font = new Font("맑은 고딕", 9F),
                Location = new Point(12, 20),
                Text = "5개 시리즈 데이터를 폼에서 생성하여 바인딩한 신규 Bar Chart 샘플"
            };

            btnChangeTitle = new Button
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(934, 13),
                Size = new Size(154, 30),
                Text = "타이틀 변경 예시"
            };
            btnChangeTitle.Click += btnChangeTitle_Click;

            pnlTop.Controls.Add(lblGuide);
            pnlTop.Controls.Add(btnChangeTitle);

            pnlChartHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            Controls.Add(pnlChartHost);
            Controls.Add(pnlTop);

            barChart = LightningBar.Create(pnlChartHost, CreateCategories(), CreateSeries(), CreateOptions());
            barChart.SeriesClicked += BarChart_SeriesClicked;
        }

        private static LightningBarOptions CreateOptions()
        {
            return new LightningBarOptions
            {
                BackgroundColor = Color.White,
                TitleOptions = new LightningBarTitleOptions
                {
                    Text = "신규 Bar Chart 사용 가이드",
                    Position = LightningBarTitlePosition.TopCenter,
                    FontSize = 13f,
                    MarginTop = 10f,
                    MarginHorizontal = 16f
                },
                Layout = new LightningBarLayoutOptions
                {
                    ChartPadding = 32,
                    TopOffset = 88,
                    LegendReservedWidth = 180,
                    CategoryLabelReservedWidth = 120f
                },
                Legend = new LightningBarLegendOptions
                {
                    Position = LightningBarLegendPosition.Top,
                    Alignment = LightningBarLegendAlignment.Center,
                    MarginFromChart = 10f,
                    FontSize = 8f,
                    LabelMaxWidth = 120f,
                    LabelMaxLines = 2
                },
                CategoryLabels = new LightningBarCategoryLabelOptions
                {
                    FontSize = 8.5f,
                    MaxLines = 2,
                    LineSpacing = 2f
                },
                Bars = new LightningBarBarOptions
                {
                    HeightMode = LightningBarHeightMode.Manual,
                    FixedHeight = 16f,
                    ReferenceSeriesCount = 5,
                    Gap = 6f,
                    GroupPaddingRatio = 0.2f
                },
                Scale = new LightningBarScaleOptions
                {
                    GridLineCount = 5,
                    MaxValue = 100f
                },
                RawData = new LightningBarRawDataOptions
                {
                    ButtonMode = LightningBarRawDataButtonMode.Hidden,
                    ButtonText = "RawData"
                },
                NoData = new LightningBarNoDataOptions
                {
                    Text = "가이드 샘플 데이터가 없습니다.",
                    FontName = "맑은 고딕",
                    TextColor = Color.Gray,
                    IncludeTitle = false,
                    ShowWhenDataMissing = true,
                    ShowWhenAllValuesZero = true
                }
            };
        }

        private static IEnumerable<string> CreateCategories()
        {
            return new[] { "품질", "생산성", "안전", "원가", "납기" };
        }

        private static IEnumerable<LightningBarSeries> CreateSeries()
        {
            return new[]
            {
                new LightningBarSeries { Name = "Series 1", LegendLabel = "Series 1", Values = new[] { 82f, 74f, 88f, 69f, 90f }, FillColor = Color.FromArgb(165, 255, 196, 214), BorderColor = Color.FromArgb(230, 225, 104, 150) }
            };
        }

        private void BarChart_SeriesClicked(object sender, LightningBarSeriesClickEventArgs e)
        {
            MessageBox.Show(this,
                string.Format("Series: {0}\nCategory: {1}\nValue: {2:0.#}", e.Series.Name, e.CategoryName, e.Value),
                "선택된 데이터",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnChangeTitle_Click(object sender, EventArgs e)
        {
            barChart.UpdateOptions(options =>
            {
                options.TitleOptions.Text = string.Format("신규 Bar Chart 사용 가이드 ({0:HH:mm:ss})", DateTime.Now);
                options.TitleOptions.Position = LightningBarTitlePosition.TopCenter;
            });
        }
    }
}
