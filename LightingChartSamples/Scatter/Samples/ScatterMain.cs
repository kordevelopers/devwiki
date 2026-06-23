using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LightingChartSamples.Scatter
{
    public class ScatterMain : Form
    {
        private readonly Panel chartHost;
        private readonly DataGridView nearestNeighborGrid;
        private readonly Label summaryLabel;
        private readonly TextBox draftNoTextBox;
        private readonly Button searchButton;
        private readonly Button loadDatabaseButton;
        private readonly Button regenerateButton;
        private readonly PcaScatterChart pcaChart;
        private PcaAnalysisResult analysisResult;
        private IList<ScatterSampleData> currentSamples;
        private int sampleSeed;

        public ScatterMain()
        {
            sampleSeed = 20260622;
            currentSamples = new List<ScatterSampleData>();

            BackColor = Color.White;
            ClientSize = new Size(1180, 820);
            Font = new Font("맑은 고딕", 9F, FontStyle.Regular);
            MinimumSize = new Size(900, 650);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ScatterMain - LightningChart 8 PCA Scatter";

            TableLayoutPanel rootLayout = CreateRootLayout();
            TableLayoutPanel toolbarLayout = CreateToolbarLayout();
            Label titleLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                Margin = new Padding(0),
                Text = "PCA Scatter",
                TextAlign = ContentAlignment.MiddleLeft
            };

            summaryLabel = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(95, 95, 95),
                Margin = new Padding(0),
                Padding = new Padding(8, 0, 8, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            draftNoTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 5, 4, 4),
                Name = "draftNoTextBox"
            };
            draftNoTextBox.KeyDown += DraftNoTextBox_KeyDown;

            searchButton = CreateToolbarButton("유사 검색", SearchButton_Click, new Padding(0));
            loadDatabaseButton = CreateToolbarButton("DB 조회", LoadDatabaseButton_Click, new Padding(4, 0, 0, 0));
            regenerateButton = CreateToolbarButton("샘플 재생성", RegenerateButton_Click, new Padding(4, 0, 0, 0));

            toolbarLayout.Controls.Add(titleLabel, 0, 0);
            toolbarLayout.Controls.Add(summaryLabel, 1, 0);
            toolbarLayout.Controls.Add(draftNoTextBox, 2, 0);
            toolbarLayout.Controls.Add(searchButton, 3, 0);
            toolbarLayout.Controls.Add(loadDatabaseButton, 4, 0);
            toolbarLayout.Controls.Add(regenerateButton, 5, 0);

            chartHost = new Panel
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };

            nearestNeighborGrid = CreateNearestNeighborGrid();
            rootLayout.Controls.Add(toolbarLayout, 0, 0);
            rootLayout.Controls.Add(chartHost, 0, 1);
            rootLayout.Controls.Add(nearestNeighborGrid, 0, 2);
            Controls.Add(rootLayout);

            BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
            pcaChart = PcaScatterChart.Create(chartHost, CreateChartOptions());
            pcaChart.SampleClicked += PcaChart_SampleClicked;
            pcaChart.AnalysisCompleted += PcaChart_AnalysisCompleted;
            pcaChart.AnalysisFailed += PcaChart_AnalysisFailed;
            BindSampleData();
        }

        private static TableLayoutPanel CreateRootLayout()
        {
            TableLayoutPanel layout = new TableLayoutPanel
            {
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                RowCount = 3
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 172F));
            return layout;
        }

        private static TableLayoutPanel CreateToolbarLayout()
        {
            TableLayoutPanel layout = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 6,
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 12, 12, 10),
                RowCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            return layout;
        }

        private static Button CreateToolbarButton(string text, EventHandler clickHandler, Padding margin)
        {
            Button button = new Button
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.System,
                Margin = margin,
                Text = text,
                UseVisualStyleBackColor = true
            };
            button.Click += clickHandler;
            return button;
        }

        private void BindSampleData()
        {
            PcaJsonSampleDataFactory factory = new PcaJsonSampleDataFactory(sampleSeed++);
            IList<string> jsonSamples = factory.CreateDefaultJsonSamples();
            pcaChart.Bind(PcaScatterDataSource.FromJsonSamples(jsonSamples), CreateChartOptions());
            draftNoTextBox.Clear();
            BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
        }

        private async void LoadDatabaseButton_Click(object sender, EventArgs e)
        {
            SetToolbarEnabled(false);
            summaryLabel.Text = "DB ACT_DATA 조회 및 JSON 분석 중...";

            try
            {
                PcaScatterOptions chartOptions = CreateChartOptions();
                PcaScatterDatabaseOptions databaseOptions = PcaScatterDatabaseOptions.CreateDefault();
                PcaAnalysisResult databaseResult = await Task.Run(delegate
                {
                    ActDataRepository repository = new ActDataRepository(databaseOptions.ToActDataQueryOptions());
                    IList<string> actDataDocuments = repository.LoadActData();
                    return PcaScatterDataSource
                        .FromActDataJson(actDataDocuments)
                        .Analyze(chartOptions.Analysis);
                });

                pcaChart.Bind(databaseResult, chartOptions);
                draftNoTextBox.Clear();
                BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
            }
            catch (Exception ex)
            {
                summaryLabel.Text = "DB 조회 실패";
                MessageBox.Show(
                    this,
                    ex.Message,
                    "ACT_DATA 조회/분석 실패",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                SetToolbarEnabled(true);
            }
        }

        private void RegenerateButton_Click(object sender, EventArgs e)
        {
            BindSampleData();
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            ShowNearestNeighbors(draftNoTextBox.Text);
        }

        private void DraftNoTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
            ShowNearestNeighbors(draftNoTextBox.Text);
        }

        private void PcaChart_SampleClicked(object sender, PcaScatterSampleClickedEventArgs e)
        {
            if (e.Sample == null)
            {
                return;
            }

            draftNoTextBox.Text = e.Sample.DraftNo;
            BindNearestNeighborTable(CreateNearestNeighborTable(e.Sample, e.Neighbors));
        }

        private void PcaChart_AnalysisCompleted(object sender, PcaScatterAnalysisCompletedEventArgs e)
        {
            analysisResult = e.AnalysisResult;
            currentSamples = analysisResult == null || analysisResult.ScatterData == null
                ? new List<ScatterSampleData>()
                : analysisResult.ScatterData.ToList();
            UpdateSummary();
        }

        private void PcaChart_AnalysisFailed(object sender, PcaScatterAnalysisFailedEventArgs e)
        {
            summaryLabel.Text = e.Exception == null ? "분석 실패" : "분석 실패: " + e.Exception.Message;
        }

        private void ShowNearestNeighbors(string draftNo)
        {
            if (string.IsNullOrWhiteSpace(draftNo))
            {
                BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
                draftNoTextBox.Focus();
                return;
            }

            try
            {
                ScatterSampleData target = currentSamples.FirstOrDefault(item =>
                    string.Equals(item.DraftNo, draftNo.Trim(), StringComparison.OrdinalIgnoreCase));
                IList<KnnNeighbor> neighbors = pcaChart.FindNearest(draftNo.Trim(), 3);
                BindNearestNeighborTable(CreateNearestNeighborTable(target, neighbors));
            }
            catch (KeyNotFoundException ex)
            {
                BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
                MessageBox.Show(this, ex.Message, "Draft_NO 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                draftNoTextBox.Focus();
                draftNoTextBox.SelectAll();
            }
        }

        private void SetToolbarEnabled(bool enabled)
        {
            draftNoTextBox.Enabled = enabled;
            searchButton.Enabled = enabled;
            loadDatabaseButton.Enabled = enabled;
            regenerateButton.Enabled = enabled;
        }

        private static PcaScatterOptions CreateChartOptions()
        {
            PcaScatterOptions options = PcaScatterOptions.CreateDefault600x400();
            options.Display.ShowTitle = false;
            options.Display.XAxisTitle = "X1";
            options.Display.YAxisTitle = "X2";
            options.Display.MajorDivCount = 8;
            options.Display.AxisLabelFormat = "0.##";
            options.Display.GridLinesVisible = true;
            options.Display.GridColor = Color.FromArgb(232, 234, 238);
            options.Legend.Position = LightningScatterLegendPosition.TopCenter;
            options.Legend.ShowCheckboxes = true;
            options.Legend.BackgroundColor = Color.White;
            options.Legend.BorderColor = Color.FromArgb(220, 220, 220);
            options.Tooltip.Enabled = true;
            options.Tooltip.HitPixelTolerance = 14;
            options.Tooltip.Format = "{5}\r\nX1:{1:0.###}, X2:{2:0.###}\r\nAI_RSLT_Val:{0}";
            options.NoData.Text = "PCA Scatter 데이터가 없습니다.";
            options.NoData.ShowWhenAllValuesZero = false;
            return options;
        }

        private void UpdateSummary()
        {
            if (analysisResult == null)
            {
                summaryLabel.Text = "분석 결과 없음";
                return;
            }

            double pc1Ratio = analysisResult.PcaModel != null && analysisResult.PcaModel.ExplainedVarianceRatios.Length > 0
                ? analysisResult.PcaModel.ExplainedVarianceRatios[0] * 100d
                : 0d;
            double pc2Ratio = analysisResult.PcaModel != null && analysisResult.PcaModel.ExplainedVarianceRatios.Length > 1
                ? analysisResult.PcaModel.ExplainedVarianceRatios[1] * 100d
                : 0d;
            summaryLabel.Text = string.Format(
                "JSON {0} | Features {1} | Excluded {2} | PCA {3:0.0}% + {4:0.0}% | 검증 정상",
                currentSamples.Count,
                analysisResult.FeatureNames == null ? 0 : analysisResult.FeatureNames.Length,
                analysisResult.ExcludedFeatureNames == null ? 0 : analysisResult.ExcludedFeatureNames.Length,
                pc1Ratio,
                pc2Ratio);
        }

        private static DataGridView CreateNearestNeighborGrid()
        {
            return new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ColumnHeadersHeight = 30,
                Dock = DockStyle.Fill,
                EnableHeadersVisualStyles = false,
                Margin = new Padding(12, 8, 12, 12),
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
        }

        private void BindNearestNeighborTable(DataTable table)
        {
            nearestNeighborGrid.DataSource = table;
            if (nearestNeighborGrid.Columns.Contains("X1"))
            {
                nearestNeighborGrid.Columns["X1"].DefaultCellStyle.Format = "0.####";
            }

            if (nearestNeighborGrid.Columns.Contains("X2"))
            {
                nearestNeighborGrid.Columns["X2"].DefaultCellStyle.Format = "0.####";
            }

            if (nearestNeighborGrid.Columns.Contains("Distance"))
            {
                nearestNeighborGrid.Columns["Distance"].DefaultCellStyle.Format = "0.0000";
            }
        }

        private DataTable CreateNearestNeighborTable(ScatterSampleData target, IEnumerable<KnnNeighbor> neighbors)
        {
            DataTable table = new DataTable();
            table.Columns.Add("Target_Draft_NO", typeof(string));
            table.Columns.Add("Rank", typeof(int));
            table.Columns.Add("Similar_Draft", typeof(string));
            table.Columns.Add("X1", typeof(double));
            table.Columns.Add("X2", typeof(double));
            table.Columns.Add("AI_RSLT_Val", typeof(string));
            table.Columns.Add("Distance", typeof(double));

            if (target == null || neighbors == null)
            {
                return table;
            }

            foreach (KnnNeighbor neighbor in neighbors)
            {
                if (neighbor.SourceIndex < 0 || neighbor.SourceIndex >= currentSamples.Count)
                {
                    continue;
                }

                ScatterSampleData similar = currentSamples[neighbor.SourceIndex];
                DataRow row = table.NewRow();
                row["Target_Draft_NO"] = target.DraftNo;
                row["Rank"] = neighbor.Rank;
                row["Similar_Draft"] = neighbor.DraftNo;
                row["X1"] = similar.X1;
                row["X2"] = similar.X2;
                row["AI_RSLT_Val"] = similar.AiResultValue;
                row["Distance"] = neighbor.Distance;
                table.Rows.Add(row);
            }

            return table;
        }
    }
}
