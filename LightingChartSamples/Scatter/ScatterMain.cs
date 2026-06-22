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
        private readonly LightningScatter scatterChart;
        private PcaAnalysisResult analysisResult;
        private IList<ScatterSampleData> currentSamples;
        private int sampleSeed;

        #region Form Initialization and Responsive Layout

        public ScatterMain()
        {
            sampleSeed = 20260622;

            BackColor = Color.White;
            ClientSize = new Size(1180, 820);
            Font = new Font(LightningScatter.DefaultChartFontName, 9F, FontStyle.Regular);
            MinimumSize = new Size(900, 650);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ScatterMain - LightningChart 8 PCA Scatter";

            var rootLayout = new TableLayoutPanel
            {
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                RowCount = 3
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 172F));

            var toolbarLayout = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 6,
                Dock = DockStyle.Fill,
                Name = "toolbarLayout",
                Padding = new Padding(12, 12, 12, 10),
                RowCount = 1
            };
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104F));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
            toolbarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var titleLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font(LightningScatter.DefaultChartFontName, 11F, FontStyle.Bold),
                Margin = new Padding(0),
                Name = "titleLabel",
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
                Name = "summaryLabel",
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

            searchButton = new Button
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.System,
                Margin = new Padding(0),
                Name = "searchButton",
                Text = "유사 검색",
                UseVisualStyleBackColor = true
            };
            searchButton.Click += SearchButton_Click;

            loadDatabaseButton = new Button
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.System,
                Margin = new Padding(4, 0, 0, 0),
                Name = "loadDatabaseButton",
                Text = "DB 조회",
                UseVisualStyleBackColor = true
            };
            loadDatabaseButton.Click += LoadDatabaseButton_Click;

            regenerateButton = new Button
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.System,
                Margin = new Padding(4, 0, 0, 0),
                Name = "regenerateButton",
                Text = "샘플 재생성",
                UseVisualStyleBackColor = true
            };
            regenerateButton.Click += RegenerateButton_Click;

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

            analysisResult = CreateAnalysisResult();
            currentSamples = analysisResult.ScatterData.ToList();
            BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
            scatterChart = LightningScatter.Create(
                chartHost,
                CreateSeries(currentSamples),
                CreateOptions(currentSamples));
            scatterChart.PointClicked += ScatterChart_PointClicked;
            UpdateSummary();
        }

        #endregion

        #region Full Analysis Pipeline - JSON to Standardized PCA Coordinates

        /// <summary>
        /// 테스트 JSON을 생성하고 실제 전처리/StandardScaler/PCA/KNN 파이프라인을 실행한다.
        /// seed를 증가시키므로 재생성할 때 원본 값은 바뀌지만 동일 seed에서는 결과가 재현된다.
        /// </summary>
        private PcaAnalysisResult CreateAnalysisResult()
        {
            var factory = new PcaJsonSampleDataFactory(sampleSeed++);
            IList<string> jsonSamples = factory.CreateDefaultJsonSamples();
            var pipeline = new PcaAnalysisPipeline(new PcaAnalysisOptions
            {
                ConstantVarianceThreshold = 1e-10d,
                ComponentCount = 2,
                MaxIterations = 2000,
                ConvergenceTolerance = 1e-10d,
                NeighborCount = 3
            });
            return pipeline.Analyze(jsonSamples);
        }

        /// <summary>
        /// SELECT ACT_DATA FROM AI_INFERNECE 결과를 읽고 Dict/List JSON을 개별 실험 행으로 펼친다.
        /// 이후 샘플과 동일한 StandardScaler/PCA/KNN 파이프라인을 사용한다.
        /// </summary>
        private static PcaAnalysisResult CreateDatabaseAnalysisResult()
        {
            var repository = new ActDataRepository();
            IList<string> actDataDocuments = repository.LoadActData();
            var pipeline = new PcaAnalysisPipeline(new PcaAnalysisOptions
            {
                ConstantVarianceThreshold = 1e-10d,
                ComponentCount = 2,
                MaxIterations = 2000,
                ConvergenceTolerance = 1e-10d,
                NeighborCount = 3
            });
            return pipeline.AnalyzeActDataDocuments(actDataDocuments);
        }

        private void RegenerateButton_Click(object sender, EventArgs e)
        {
            analysisResult = CreateAnalysisResult();
            currentSamples = analysisResult.ScatterData.ToList();
            scatterChart.UpdateData(CreateSeries(currentSamples), CreateOptions(currentSamples));
            draftNoTextBox.Clear();
            BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
            UpdateSummary();
        }

        private async void LoadDatabaseButton_Click(object sender, EventArgs e)
        {
            SetToolbarEnabled(false);
            summaryLabel.Text = "DB ACT_DATA 조회 및 JSON 분석 중...";

            try
            {
                PcaAnalysisResult databaseResult = await Task.Run(
                    new Func<PcaAnalysisResult>(CreateDatabaseAnalysisResult));
                ApplyAnalysisResult(databaseResult);
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

        private void ApplyAnalysisResult(PcaAnalysisResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            analysisResult = result;
            currentSamples = analysisResult.ScatterData.ToList();
            scatterChart.UpdateData(CreateSeries(currentSamples), CreateOptions(currentSamples));
            draftNoTextBox.Clear();
            BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
            UpdateSummary();
        }

        private void SetToolbarEnabled(bool enabled)
        {
            draftNoTextBox.Enabled = enabled;
            searchButton.Enabled = enabled;
            loadDatabaseButton.Enabled = enabled;
            regenerateButton.Enabled = enabled;
        }

        #endregion

        #region LightningChart Series Binding

        /// <summary>
        /// PCA가 계산한 X1/X2를 AI_RSLT_Val 기준으로 Pass/Review 시리즈에 바인딩한다.
        /// 차트 클래스는 분석을 수행하지 않고 계산 완료된 결과만 표시한다.
        /// </summary>
        private static IEnumerable<LightningScatterSeries> CreateSeries(IEnumerable<ScatterSampleData> samples)
        {
            IList<ScatterSampleData> source = samples == null
                ? new List<ScatterSampleData>()
                : samples.ToList();

            return new[]
            {
                CreateSeries(
                    PcaJsonSampleDataFactory.PassResult,
                    source,
                    Color.FromArgb(151, 211, 169)),
                CreateSeries(
                    PcaJsonSampleDataFactory.ReviewResult,
                    source,
                    Color.FromArgb(238, 171, 210))
            };
        }

        private static LightningScatterSeries CreateSeries(
            string result,
            IEnumerable<ScatterSampleData> samples,
            Color color)
        {
            return new LightningScatterSeries
            {
                Name = result,
                LegendLabel = result,
                LineColor = color,
                PointColor = color,
                PointSize = 15F,
                ShowLine = false,
                ShowPoints = true,
                Points = samples
                    .Where(item => item != null
                        && string.Equals(item.AiResultValue, result, StringComparison.OrdinalIgnoreCase))
                    // Tag의 원본 결과 모델은 포인트 클릭 시 Draft_NO 검색 키로 사용한다.
                    .Select(item => new LightningScatterPoint(item.X1, item.X2, item))
                    .ToList()
            };
        }

        #endregion

        #region Chart Options and PCA Axis Range

        private static LightningScatterOptions CreateOptions(IEnumerable<ScatterSampleData> samples)
        {
            IList<ScatterSampleData> source = samples == null
                ? new List<ScatterSampleData>()
                : samples.Where(item => item != null).ToList();
            AxisRange xRange = CalculateRange(source.Select(item => item.X1));
            AxisRange yRange = CalculateRange(source.Select(item => item.X2));

            LightningScatterOptions options = LightningScatterOptions.CreateDefaultBubble();
            options.ShowTitle = false;
            options.BackgroundColor = Color.White;
            options.GraphBackgroundColor = Color.White;
            options.XAxis.Title = "X1";
            options.XAxis.AutoFit = false;
            options.XAxis.Minimum = xRange.Minimum;
            options.XAxis.Maximum = xRange.Maximum;
            options.XAxis.MajorDivCount = 8;
            options.XAxis.LabelFormat = "0.##";
            options.XAxis.GridLinesVisible = true;
            options.XAxis.MinorGridLinesVisible = false;
            options.XAxis.GridColor = Color.FromArgb(232, 234, 238);
            options.YAxis.Title = "X2";
            options.YAxis.AutoFit = false;
            options.YAxis.Minimum = yRange.Minimum;
            options.YAxis.Maximum = yRange.Maximum;
            options.YAxis.MajorDivCount = 8;
            options.YAxis.LabelFormat = "0.##";
            options.YAxis.GridLinesVisible = true;
            options.YAxis.MinorGridLinesVisible = false;
            options.YAxis.GridColor = Color.FromArgb(232, 234, 238);
            options.Legend.Position = LightningScatterLegendPosition.TopCenter;
            options.Legend.ShowCheckboxes = true;
            options.Legend.BackgroundColor = Color.White;
            options.Legend.BorderColor = Color.FromArgb(220, 220, 220);
            options.Style.UsePastelPalette = false;
            options.Tooltip.Enabled = true;
            options.Tooltip.HitPixelTolerance = 14;
            options.Tooltip.Format = "{5}\r\nX1:{1:0.###}, X2:{2:0.###}\r\nAI_RSLT_Val:{0}";
            options.NoData.Text = "PCA Scatter 데이터가 없습니다.";
            return options;
        }

        /// <summary>
        /// PCA 점수의 실제 최소/최대 범위에 8% 여백을 추가하고 원점 0을 포함한다.
        /// 이 계산은 화면 범위이며 StandardScaler 정규화와는 별개다.
        /// </summary>
        private static AxisRange CalculateRange(IEnumerable<double> values)
        {
            List<double> cleanValues = values == null
                ? new List<double>()
                : values.Where(value => !double.IsNaN(value) && !double.IsInfinity(value)).ToList();

            if (cleanValues.Count == 0)
            {
                return new AxisRange(-1d, 1d);
            }

            double minimum = Math.Min(0d, cleanValues.Min());
            double maximum = Math.Max(0d, cleanValues.Max());
            if (Math.Abs(maximum - minimum) < 0.000001d)
            {
                minimum -= 1d;
                maximum += 1d;
            }

            double padding = Math.Max(0.2d, (maximum - minimum) * 0.08d);
            return new AxisRange(minimum - padding, maximum + padding);
        }

        #endregion

        #region KNN Search Interaction

        private void ScatterChart_PointClicked(object sender, LightningScatterPointClickEventArgs e)
        {
            ScatterSampleData selected = e.Point.Tag as ScatterSampleData;
            if (selected == null)
            {
                return;
            }

            draftNoTextBox.Text = selected.DraftNo;
            ShowNearestNeighbors(selected.DraftNo);
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

        private void ShowNearestNeighbors(string draftNo)
        {
            try
            {
                ScatterSampleData target = currentSamples.FirstOrDefault(item =>
                    string.Equals(item.DraftNo, draftNo, StringComparison.OrdinalIgnoreCase));
                IList<KnnNeighbor> neighbors = analysisResult.FindNearest(draftNo, 3);
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

        #endregion

        #region KNN Result Grid and Status

        private void UpdateSummary()
        {
            double pc1Ratio = analysisResult.PcaModel.ExplainedVarianceRatios[0] * 100d;
            double pc2Ratio = analysisResult.PcaModel.ExplainedVarianceRatios[1] * 100d;
            summaryLabel.Text = string.Format(
                "JSON {0} | Features {1} | Excluded {2} | PCA {3:0.0}% + {4:0.0}% | 검증 정상",
                currentSamples.Count,
                analysisResult.FeatureNames.Length,
                analysisResult.ExcludedFeatureNames.Length,
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

        private DataTable CreateNearestNeighborTable(
            ScatterSampleData target,
            IEnumerable<KnnNeighbor> neighbors)
        {
            var table = new DataTable();
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

        #endregion

        #region Internal View Models

        private sealed class AxisRange
        {
            public AxisRange(double minimum, double maximum)
            {
                Minimum = minimum;
                Maximum = maximum;
            }

            public double Minimum { get; private set; }
            public double Maximum { get; private set; }
        }

        #endregion
    }
}
