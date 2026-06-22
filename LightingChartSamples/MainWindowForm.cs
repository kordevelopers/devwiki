using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using LightingChartSamples.Scatter;

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
            AddLightningScatterSampleButton();
            AddPcaScatterSampleButton();
            AddScatterMainButton();
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

        private void btnLightningScatterSample_Click(object sender, EventArgs e)
        {
            using (var form = new LightningScatterSampleForm())
            {
                form.ShowDialog(this);
            }
        }

        private void btnPcaScatterSample_Click(object sender, EventArgs e)
        {
            using (var form = new PcaScatterSampleForm())
            {
                form.ShowDialog(this);
            }
        }

        private void btnScatterMain_Click(object sender, EventArgs e)
        {
            using (var form = new ScatterMain())
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
                Text = "Bar Chart 기능 샘플 열기 (1개 시리즈/5개 데이터)",
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
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
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

        private void AddLightningScatterSampleButton()
        {
            var button = new Button
            {
                BackColor = Color.FromArgb(54, 132, 156),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(32, 538),
                Name = "btnLightningScatterSample",
                Size = new Size(456, 46),
                TabIndex = 9,
                Text = "Scatter Chart 샘플 열기 (LightningChart API)",
                UseVisualStyleBackColor = false
            };

            button.Click += btnLightningScatterSample_Click;
            pnlContainer.Controls.Add(button);

            btnClose.Location = new Point(392, 597);
            btnClose.TabIndex = 10;
            ClientSize = new Size(520, 646);
        }

        private void AddPcaScatterSampleButton()
        {
            var button = new Button
            {
                BackColor = Color.FromArgb(92, 111, 186),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(32, 596),
                Name = "btnPcaScatterSample",
                Size = new Size(456, 46),
                TabIndex = 10,
                Text = "PCA Scatter 샘플 열기 (Fail/Pass/Insufficient)",
                UseVisualStyleBackColor = false
            };

            button.Click += btnPcaScatterSample_Click;
            pnlContainer.Controls.Add(button);

            btnClose.Location = new Point(392, 655);
            btnClose.TabIndex = 11;
            ClientSize = new Size(520, 704);
        }

        private void AddScatterMainButton()
        {
            var button = new Button
            {
                BackColor = Color.FromArgb(70, 142, 154),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(32, 654),
                Name = "btnScatterMain",
                Size = new Size(456, 46),
                TabIndex = 11,
                Text = "ScatterMain 열기 (LightningChart 8)",
                UseVisualStyleBackColor = false
            };

            button.Click += btnScatterMain_Click;
            pnlContainer.Controls.Add(button);

            btnClose.Location = new Point(392, 713);
            btnClose.TabIndex = 12;
            ClientSize = new Size(520, 762);
        }
    }

    public class NewBarChartGuideForm : Form
    {
        private const string ColumnCategory = "CATEGORY";
        private const string ColumnValue = "VALUE";
        private const string ColumnEquipmentId = "EQUIPMENT_ID";
        private const string ColumnMetricCode = "METRIC_CODE";
        private const string ColumnLotId = "LOT_ID";

        private readonly LightningBar barChart;
        private readonly Panel pnlTop;
        private readonly Panel pnlChartHost;
        private readonly Button btnChangeTitle;
        private readonly Button btnSaveImage;
        private readonly ComboBox cboSaveFolder;
        private readonly Label lblSavedPath;
        private readonly TextBox txtEventLog;

        public NewBarChartGuideForm()
        {
            BackColor = Color.White;
            Font = new Font("맑은 고딕", 9F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Bar Chart Feature Sample";
            ClientSize = new Size(1100, 760);

            pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = Color.White
            };

            var lblGuide = new Label
            {
                AutoSize = true,
                Font = new Font("맑은 고딕", 9F),
                Location = new Point(12, 14),
                Text = "1개 시리즈, 5개 데이터로 Tooltip/막대 클릭/범례 클릭/이미지 저장 이벤트를 확인하는 Bar Chart 샘플"
            };

            var lblSaveFolder = new Label
            {
                AutoSize = true,
                Font = new Font("맑은 고딕", 9F),
                Location = new Point(12, 51),
                Text = "저장 위치"
            };

            cboSaveFolder = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(78, 47),
                Size = new Size(210, 23)
            };
            foreach (LightningBarImageSaveFolder saveFolder in Enum.GetValues(typeof(LightningBarImageSaveFolder)))
            {
                cboSaveFolder.Items.Add(saveFolder);
            }
            cboSaveFolder.SelectedItem = LightningBarImageSaveFolder.LocalApplicationData;

            btnSaveImage = new Button
            {
                Location = new Point(300, 45),
                Size = new Size(118, 28),
                Text = "이미지 저장"
            };
            btnSaveImage.Click += delegate { SaveChartImage(); };

            btnChangeTitle = new Button
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(934, 13),
                Size = new Size(154, 30),
                Text = "타이틀 변경 예시"
            };
            btnChangeTitle.Click += btnChangeTitle_Click;

            lblSavedPath = new Label
            {
                AutoEllipsis = true,
                Location = new Point(430, 50),
                Size = new Size(650, 20),
                Text = "저장 경로: -",
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            pnlTop.Controls.Add(lblGuide);
            pnlTop.Controls.Add(lblSaveFolder);
            pnlTop.Controls.Add(cboSaveFolder);
            pnlTop.Controls.Add(btnSaveImage);
            pnlTop.Controls.Add(btnChangeTitle);
            pnlTop.Controls.Add(lblSavedPath);

            txtEventLog = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 118,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White,
                Font = new Font("맑은 고딕", 9F)
            };

            pnlChartHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            Controls.Add(pnlChartHost);
            Controls.Add(txtEventLog);
            Controls.Add(pnlTop);

            barChart = LightningBar.Create(pnlChartHost, CreateCategories(), CreateSeries(), CreateOptions());
            barChart.BarClicked += BarChart_BarClicked;
            barChart.LegendClicked += BarChart_LegendClicked;
            barChart.ImageSaving += BarChart_ImageSaving;
            barChart.ImageSaved += BarChart_ImageSaved;
            Shown += delegate { SaveChartImage(); };
        }

        private static LightningBarOptions CreateOptions()
        {
            return new LightningBarOptions
            {
                BackgroundColor = Color.White,
                TitleOptions = new LightningBarTitleOptions
                {
                    Text = "신규 Bar Chart 사용 가이드",
                    Visible = false,
                    Position = LightningBarTitlePosition.TopCenter,
                    FontSize = 13f,
                    MarginTop = 10f,
                    MarginHorizontal = 16f
                },
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
                    MarginFromChart = 8f,
                    FontSize = 7.5f,
                    MarkerWidth = 22f,
                    MarkerHeight = 14f,
                    LabelMaxWidth = 110f,
                    LabelMaxLines = 3,
                    ItemSpacing = 6f,
                    SectionSpacing = 20f
                },
                CategoryLabels = new LightningBarCategoryLabelOptions
                {
                    FontSize = 8f,
                    MaxLines = 3,
                    LineSpacing = 1.5f
                },
                Bars = new LightningBarBarOptions
                {
                    HeightMode = LightningBarHeightMode.Manual,
                    FixedHeight = 30f,
                    ClampFixedHeightToGroup = true,
                    ReferenceSeriesCount = 5,
                    Gap = 5f,
                    GroupPaddingRatio = 0.16f
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
                Tooltip = new LightningBarTooltipOptions
                {
                    Enabled = true,
                    Format = "Value:{2:0.#} (* 클릭할 경우 해당 계측 데이터 차트로 가 보입니다.)"
                },
                NoData = new LightningBarNoDataOptions
                {
                    Text = "가이드 샘플 데이터가 없습니다.",
                    FontName = "맑은 고딕",
                    TextColor = Color.Gray,
                    IncludeTitle = false,
                    ShowWhenDataMissing = true,
                    ShowWhenAllValuesZero = true
                },
                Image = new LightningBarImageOptions
                {
                    Width = 600,
                    Height = 400,
                    DpiX = 150f,
                    DpiY = 150f,
                    FileFormat = LightningBarImageFileFormat.Png,
                    SaveFolder = LightningBarImageSaveFolder.LocalApplicationData,
                    SubDirectoryName = "LightningBarFeatureSample",
                    UseDateFolder = true,
                    UseGuidFileName = true,
                    HideRawDataButtonOnImage = true
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
                new LightningBarSeries
                {
                    Name = "Series 1",
                    LegendLabel = "Series 1",
                    ValueSource = CreateRawDataTable("EQ-FEATURE", new[] { 82f, 74f, 88f, 69f, 90f }),
                    ValueColumnName = ColumnValue,
                    FillColor = Color.FromArgb(165, 255, 196, 214),
                    BorderColor = Color.FromArgb(230, 225, 104, 150)
                }
            };
        }

        private static DataTable CreateRawDataTable(string equipmentId, float[] values)
        {
            DataTable table = new DataTable();
            table.Columns.Add(ColumnCategory, typeof(string));
            table.Columns.Add(ColumnValue, typeof(float));
            table.Columns.Add(ColumnEquipmentId, typeof(string));
            table.Columns.Add(ColumnMetricCode, typeof(string));
            table.Columns.Add(ColumnLotId, typeof(string));

            string[] categories = new List<string>(CreateCategories()).ToArray();
            for (int i = 0; i < values.Length; i++)
            {
                DataRow row = table.NewRow();
                row[ColumnCategory] = i < categories.Length ? categories[i] : string.Empty;
                row[ColumnValue] = values[i];
                row[ColumnEquipmentId] = equipmentId;
                row[ColumnMetricCode] = string.Format("FEATURE-{0:00}", i + 1);
                row[ColumnLotId] = string.Format("{0}-LOT-{1:000}", equipmentId, i + 1);
                table.Rows.Add(row);
            }

            return table;
        }

        private LightningBarImageOptions CreateImageOptions()
        {
            LightningBarImageSaveFolder saveFolder = cboSaveFolder.SelectedItem is LightningBarImageSaveFolder
                ? (LightningBarImageSaveFolder)cboSaveFolder.SelectedItem
                : LightningBarImageSaveFolder.LocalApplicationData;

            return new LightningBarImageOptions
            {
                Preset = LightningBarImagePreset.Default,
                Width = 600,
                Height = 400,
                DpiX = 150f,
                DpiY = 150f,
                FileFormat = LightningBarImageFileFormat.Png,
                SaveFolder = saveFolder,
                SubDirectoryName = "LightningBarFeatureSample",
                UseDateFolder = true,
                UseGuidFileName = true,
                HideRawDataButtonOnImage = true
            };
        }

        private void SaveChartImage()
        {
            try
            {
                string imagePath = barChart.SaveImage(CreateImageOptions());
                lblSavedPath.Text = "저장 경로: " + imagePath;
            }
            catch (Exception ex)
            {
                AppendEventLog("이미지 저장 실패: " + ex.Message);
                MessageBox.Show(this, ex.Message, "이미지 저장 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BarChart_BarClicked(object sender, LightningBarSeriesClickEventArgs e)
        {
            DataRow rawData = e.RawData as DataRow;
            string rawDataText = rawData == null
                ? "RawData=-"
                : string.Format(
                    "RawData={0}/{1}/{2}",
                    rawData[ColumnEquipmentId],
                    rawData[ColumnMetricCode],
                    rawData[ColumnLotId]);

            AppendEventLog(string.Format(
                "BarClicked: Series={0}, Category={1}, Value={2:0.#}, {3}, SeriesRawDataRows={4}",
                e.Series.Name,
                e.CategoryName,
                e.Value,
                rawDataText,
                e.SeriesRawData.Length));
        }

        private void BarChart_LegendClicked(object sender, LightningBarLegendClickEventArgs e)
        {
            AppendEventLog(string.Format("LegendClicked: Series={0}, Label={1}", e.Series.Name, e.LegendLabel));
        }

        private void BarChart_ImageSaving(object sender, LightningBarImageSavingEventArgs e)
        {
            AppendEventLog("ImageSaving: " + (e.IsFileSave ? e.ImagePath : "memory"));
        }

        private void BarChart_ImageSaved(object sender, LightningBarImageSavedEventArgs e)
        {
            if (e.IsFileSave)
            {
                lblSavedPath.Text = "저장 경로: " + e.ImagePath;
            }

            AppendEventLog("ImageSaved: " + (e.IsFileSave ? e.ImagePath : "memory"));
        }

        private void AppendEventLog(string message)
        {
            if (txtEventLog.IsDisposed)
            {
                return;
            }

            txtEventLog.AppendText(string.Format("[{0:HH:mm:ss}] {1}{2}", DateTime.Now, message, Environment.NewLine));
        }

        private void btnChangeTitle_Click(object sender, EventArgs e)
        {
            barChart.UpdateOptions(options =>
            {
                options.TitleOptions.Text = string.Format("신규 Bar Chart 사용 가이드 ({0:HH:mm:ss})", DateTime.Now);
                options.TitleOptions.Visible = true;
                options.TitleOptions.Position = LightningBarTitlePosition.TopCenter;
            });
        }
    }
}
