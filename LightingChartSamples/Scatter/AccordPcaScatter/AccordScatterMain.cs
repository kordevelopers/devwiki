using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LightingChartSamples.Scatter;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.AccordPcaScatter;

namespace LightingChartSamples.Scatter.AccordPcaScatter
{
    public sealed partial class AccordScatterMain : Form
    {
        private readonly AccordPcaScatterAnalyzer accordAnalyzer;
        private readonly IPcaScatterPopupDataProvider popupDataProvider;
        private readonly ConvExperimentQueryOptions dataTableOptions;
        private readonly Font gridFont;
        private readonly Timer busyProgressTimer;

        private PcaScatterChart pcaChart;
        private PcaExadataSnapshot currentSnapshot;
        private PcaExadataAnalysisResult currentAnalysis;
        private List<PcaExperimentRecord> currentRecords;
        private bool parameterChangeEnabled;
        private bool nearestNeighborGridBinding;
        private DataTable pendingSourceTable;
        private bool pendingSourceTableLoadStarted;
        private DataTable receivedSourceTable;
        private string currentSourceDescription;
        private string lastFeatureAuditLogPath;

        public AccordScatterMain()
            : this((IPcaScatterPopupDataProvider)null)
        {
        }

        public AccordScatterMain(DataTable sourceTable)
            : this((IPcaScatterPopupDataProvider)null)
        {
            pendingSourceTable = sourceTable;
        }

        public AccordScatterMain(IPcaScatterPopupDataProvider popupDataProvider)
        {
            accordAnalyzer = new AccordPcaScatterAnalyzer();
            this.popupDataProvider = popupDataProvider;
            dataTableOptions = ConvExperimentQueryOptions.FromConfiguration();
            currentRecords = new List<PcaExperimentRecord>();
            gridFont = new Font("Malgun Gothic", 10f, FontStyle.Regular);
            busyProgressTimer = new Timer { Interval = 120 };
            busyProgressTimer.Tick += BusyProgressTimer_Tick;

            InitializeComponent();
            InitializeRuntimeControlValues();

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            InitializeChart();
            InitializeBusyOverlay();
            BindNearestNeighborTable(CreateNearestNeighborTable(null, null));

            Shown += AccordScatterMain_Shown;
        }

        public async Task LoadConvExperimentDataTableAsync(DataTable sourceTable)
        {
            if (sourceTable == null)
            {
                throw new ArgumentNullException("sourceTable");
            }

            if (!Visible)
            {
                pendingSourceTable = sourceTable;
                pendingSourceTableLoadStarted = false;
                return;
            }

            await LoadConvExperimentDataTableCoreAsync(sourceTable);
        }

        private async Task LoadConvExperimentDataTableCoreAsync(DataTable sourceTable)
        {
            BeginBusy("Loading DataTable...");
            try
            {
                receivedSourceTable = sourceTable;
                currentSourceDescription = "Injected DataTable";
                await AllowBusyOverlayToPaintAsync();
                currentSnapshot = await Task.Run(delegate
                {
                    IList<PcaExadataSourceRow> rows = ConvExperimentRepository.LoadFromDataTable(
                        sourceTable,
                        dataTableOptions);
                    return new PcaExadataSnapshot(rows, DateTime.UtcNow);
                });

                preferMemoryCheckBox.Checked = true;
                currentAnalysis = null;
                currentRecords.Clear();
                pcaChart.Clear();
                BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
                summaryLabel.Text = "DataTable loaded. Click Chart Draw to run PCA.";
            }
            finally
            {
                EndBusy();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (pcaChart != null)
                {
                    pcaChart.Dispose();
                    pcaChart = null;
                }

                if (gridFont != null)
                {
                    gridFont.Dispose();
                }

                if (busyProgressTimer != null)
                {
                    busyProgressTimer.Dispose();
                }

                if (components != null)
                {
                    components.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private async void AccordScatterMain_Shown(object sender, EventArgs e)
        {
            await LoadPendingSourceTableAfterShownAsync();
        }

        private async Task LoadPendingSourceTableAfterShownAsync()
        {
            if (pendingSourceTable == null || pendingSourceTableLoadStarted)
            {
                return;
            }

            pendingSourceTableLoadStarted = true;
            DataTable source = pendingSourceTable;
            pendingSourceTable = null;
            await LoadConvExperimentDataTableCoreAsync(source);
        }

        private void InitializeRuntimeControlValues()
        {
            if (knnAlgorithmComboBox.Items.Count == 0)
            {
                knnAlgorithmComboBox.Items.Add(KnnSearchAlgorithm.Auto);
                knnAlgorithmComboBox.Items.Add(KnnSearchAlgorithm.BruteForce);
                knnAlgorithmComboBox.Items.Add(KnnSearchAlgorithm.KdTree);
                knnAlgorithmComboBox.Items.Add(KnnSearchAlgorithm.BallTree);
            }

            knnAlgorithmComboBox.SelectedItem = KnnSearchAlgorithm.Auto;
        }

        private void InitializeChart()
        {
            pcaChart = PcaScatterChart.Create(chartHost, CreateChartOptions());
            pcaChart.SampleClicked += PcaChart_SampleClicked;
            pcaChart.AnalysisCompleted += PcaChart_AnalysisCompleted;
            pcaChart.AnalysisFailed += PcaChart_AnalysisFailed;
        }

        private void InitializeBusyOverlay()
        {
            busyOverlayPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Visible = false
            };

            busyOverlayLabel = new Label
            {
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(70, 70, 70),
                Font = new Font("Malgun Gothic", 10f, FontStyle.Regular)
            };

            busyProgressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            busyOverlayPanel.Controls.Add(busyOverlayLabel);
            busyOverlayPanel.Controls.Add(busyProgressBar);
            busyOverlayPanel.Resize += BusyOverlayPanel_Resize;
            chartHost.Controls.Add(busyOverlayPanel);
            busyOverlayPanel.BringToFront();
            UpdateBusyOverlayLayout();
        }

        private async void SearchButton_Click(object sender, EventArgs e)
        {
            await QueryDraftAsync();
        }

        private async void RefreshAllButton_Click(object sender, EventArgs e)
        {
            await RefreshCurrentDataAsync();
        }

        private async void ChartDrawButton_Click(object sender, EventArgs e)
        {
            await DrawCurrentChartAsync();
        }

        private async void SampleDataButton_Click(object sender, EventArgs e)
        {
            await LoadVirtualSampleDataAsync();
        }

        private async void ParameterRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (!parameterChangeEnabled
                || !((RadioButton)sender).Checked
                || currentSnapshot == null
                || currentAnalysis == null)
            {
                return;
            }

            await AnalyzeCurrentSnapshotAsync("Memory Snapshot", true);
        }

        private async Task<bool> ReloadFromPopupDataProviderAsync()
        {
            if (popupDataProvider == null)
            {
                pcaChart.Clear();
                currentRecords.Clear();
                BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
                summaryLabel.Text = "Data provider is not configured. Use Virtual Data for a sample.";
                MessageBox.Show(
                    this,
                    "Data provider is not configured. Pass a DataTable, inject a provider, or use Virtual Data for a sample.",
                    "Data Provider",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }

            DataTable table = await popupDataProvider.LoadAllAsync();
            receivedSourceTable = table;
            currentSourceDescription = popupDataProvider.SourceDescription;
            IList<PcaExadataSourceRow> rows = await Task.Run(delegate
            {
                return ConvExperimentRepository.LoadFromDataTable(table, dataTableOptions);
            });
            currentSnapshot = new PcaExadataSnapshot(rows, DateTime.UtcNow);
            return true;
        }

        private async Task LoadVirtualSampleDataAsync()
        {
            BeginBusy("Creating virtual data and running Accord.NET PCA...");
            try
            {
                receivedSourceTable = null;
                currentSourceDescription = "Virtual Data";
                await AllowBusyOverlayToPaintAsync();
                currentSnapshot = await Task.Run(delegate
                {
                    return new PcaExadataSampleDataFactory(20260629).CreateDefaultSnapshot();
                });
                preferMemoryCheckBox.Checked = true;
                await AnalyzeCurrentSnapshotAsync(currentSourceDescription, false);
            }
            finally
            {
                EndBusy();
            }
        }

        private async Task DrawCurrentChartAsync()
        {
            BeginBusy("Drawing PCA chart...");
            try
            {
                await AllowBusyOverlayToPaintAsync();
                if (currentSnapshot == null)
                {
                    bool loaded = await ReloadFromPopupDataProviderAsync();
                    if (!loaded)
                    {
                        return;
                    }
                }

                await AnalyzeCurrentSnapshotForRefreshAsync();
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, "Accord.NET PCA Chart Draw Failed");
            }
            finally
            {
                EndBusy();
            }
        }

        private async Task RefreshCurrentDataAsync()
        {
            BeginBusy("Refreshing current data and running Accord.NET PCA...");
            try
            {
                await AllowBusyOverlayToPaintAsync();
                if (receivedSourceTable != null)
                {
                    IList<PcaExadataSourceRow> rows = await Task.Run(delegate
                    {
                        return ConvExperimentRepository.LoadFromDataTable(
                            receivedSourceTable,
                            dataTableOptions);
                    });
                    currentSnapshot = new PcaExadataSnapshot(rows, DateTime.UtcNow);
                    currentSourceDescription = "Injected DataTable";
                }

                if (currentSnapshot == null)
                {
                    pcaChart.Clear();
                    currentRecords.Clear();
                    BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
                    summaryLabel.Text = "No DataTable is loaded. Use Virtual Data for a sample.";
                    MessageBox.Show(
                        this,
                        "No DataTable is loaded. Use Virtual Data for a sample, or pass a DataTable first.",
                        "Refresh",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                await AnalyzeCurrentSnapshotForRefreshAsync();
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, "Accord.NET PCA Refresh Failed");
            }
            finally
            {
                EndBusy();
            }
        }

        private async Task AnalyzeCurrentSnapshotForRefreshAsync()
        {
            PcaScatterOptions chartOptions = CreateChartOptions();
            string draftNo = (draftNoTextBox.Text ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(draftNo))
            {
                chartOptions.Series.HighlightDraftNo = draftNo;
            }

            PcaExadataAnalysisResult result = await AnalyzeSnapshotWithAccordAsync(
                currentSnapshot,
                GetSelectedParameterType(),
                chartOptions);

            PcaExperimentRecord target = null;
            IList<KnnNeighbor> neighbors = null;
            if (!string.IsNullOrWhiteSpace(draftNo))
            {
                target = result.Records.FirstOrDefault(record =>
                    string.Equals(record.DraftNo, draftNo, StringComparison.OrdinalIgnoreCase));
                if (target != null)
                {
                    neighbors = result.AnalysisResult.FindNearest(
                        target.DraftNo,
                        GetNeighborSearchCount(chartOptions));
                }
                else
                {
                    chartOptions.Series.HighlightDraftNo = string.Empty;
                }
            }

            ApplyAnalysis(result, chartOptions);
            BindNearestNeighborTable(CreateNearestNeighborTable(target, neighbors));
            UpdateSummary(
                result,
                string.IsNullOrWhiteSpace(currentSourceDescription)
                    ? "Memory Snapshot"
                    : currentSourceDescription);
        }

        private async Task QueryDraftAsync()
        {
            string draftNo = (draftNoTextBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(draftNo))
            {
                MessageBox.Show(
                    this,
                    "Enter DRAFT_NO.",
                    "Draft Search",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            BeginBusy("Searching Draft and running Accord.NET PCA...");
            try
            {
                await AllowBusyOverlayToPaintAsync();
                if (!preferMemoryCheckBox.Checked)
                {
                    bool loaded = await ReloadFromPopupDataProviderAsync();
                    if (!loaded)
                    {
                        return;
                    }
                }

                if (currentSnapshot == null)
                {
                    pcaChart.Clear();
                    currentRecords.Clear();
                    BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
                    summaryLabel.Text = "No DataTable is loaded. Use Virtual Data for a sample.";
                    MessageBox.Show(
                        this,
                        "No DataTable is loaded. Use Virtual Data for a sample, or pass a DataTable first.",
                        "Draft Search",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                PcaScatterOptions chartOptions = CreateChartOptions();
                chartOptions.Series.HighlightDraftNo = draftNo;
                PcaExadataAnalysisResult result = await AnalyzeSnapshotWithAccordAsync(
                    currentSnapshot,
                    GetSelectedParameterType(),
                    chartOptions);

                PcaExperimentRecord target = result.Records.FirstOrDefault(record =>
                    string.Equals(record.DraftNo, draftNo, StringComparison.OrdinalIgnoreCase));
                if (target == null)
                {
                    pcaChart.Clear();
                    currentRecords.Clear();
                    BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
                    summaryLabel.Text = "The selected PARAM_TYP does not contain DRAFT_NO.";
                    MessageBox.Show(
                        this,
                        "The selected PARAM_TYP does not contain DRAFT_NO.",
                        "Draft Search",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                ApplyAnalysis(result, chartOptions);
                IList<KnnNeighbor> neighbors = result.AnalysisResult.FindNearest(
                    target.DraftNo,
                    GetNeighborSearchCount(chartOptions));
                BindNearestNeighborTable(CreateNearestNeighborTable(target, neighbors));
                UpdateSummary(result, "Accord.NET Draft Search");
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, "Accord.NET Draft Search Failed");
            }
            finally
            {
                EndBusy();
            }
        }

        private async Task AnalyzeCurrentSnapshotAsync(string sourceDescription, bool manageBusy)
        {
            if (currentSnapshot == null)
            {
                return;
            }

            if (manageBusy)
            {
                BeginBusy("Filtering memory data and running Accord.NET PCA...");
                await AllowBusyOverlayToPaintAsync();
            }

            try
            {
                PcaScatterOptions chartOptions = CreateChartOptions();
                PcaExadataAnalysisResult result = await AnalyzeSnapshotWithAccordAsync(
                    currentSnapshot,
                    GetSelectedParameterType(),
                    chartOptions);
                ApplyAnalysis(result, chartOptions);
                BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
                UpdateSummary(result, sourceDescription);
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, "Accord.NET PCA Analysis Failed");
            }
            finally
            {
                if (manageBusy)
                {
                    EndBusy();
                }
            }
        }

        private Task<PcaExadataAnalysisResult> AnalyzeSnapshotWithAccordAsync(
            PcaExadataSnapshot snapshot,
            PcaParameterType parameterType,
            PcaScatterOptions chartOptions)
        {
            PcaScatterAnalysisOptions analysisOptions = chartOptions == null || chartOptions.Analysis == null
                ? new PcaScatterAnalysisOptions()
                : chartOptions.Analysis.Clone();
            return Task.Run(delegate
            {
                return accordAnalyzer.AnalyzeSnapshot(snapshot, parameterType, analysisOptions);
            });
        }

        private void ApplyAnalysis(
            PcaExadataAnalysisResult result,
            PcaScatterOptions chartOptions)
        {
            if (result == null || result.AnalysisResult == null)
            {
                throw new ArgumentNullException("result");
            }

            VerifyAccordAnalysis(result);
            currentAnalysis = result;
            currentRecords = result.Records.ToList();
            pcaChart.Bind(result.AnalysisResult, chartOptions);
            WriteFeatureSelectionAuditLog(result, chartOptions);
        }

        private static void VerifyAccordAnalysis(PcaExadataAnalysisResult result)
        {
            if (result.AnalysisResult == null || result.AnalysisResult.Verification == null)
            {
                throw new InvalidOperationException("Accord PCA verification report is missing.");
            }

            if (!result.AnalysisResult.Verification.SharedScalerInstance)
            {
                throw new InvalidOperationException(
                    "PCA and KNN must share the same StandardScalerModel instance.");
            }
        }

        private PcaScatterOptions CreateChartOptions()
        {
            PcaScatterOptions options = PcaScatterOptions.CreateDefault600x400();
            options.Analysis.KnnSearchAlgorithm = GetSelectedKnnAlgorithm();
            options.Display.ShowTitle = false;
            options.Display.XAxisTitle = "X1";
            options.Display.YAxisTitle = "X2";
            options.Display.MajorDivCount = 8;
            options.Display.AxisLabelFormat = "0.##";
            options.Display.GridLinesVisible = true;
            options.Display.GridColor = Color.FromArgb(232, 234, 238);
            options.Series.PassResultName = "PASS";
            options.Series.ReviewResultName = "FAIL";
            options.Series.SeriesOrder = new[] { "PASS", "FAIL", "Review", "Pass" };
            options.Legend.Position = LightningScatterLegendPosition.TopCenter;
            options.Legend.ShowCheckboxes = true;
            options.Legend.BackgroundColor = Color.White;
            options.Legend.BorderColor = Color.FromArgb(220, 220, 220);
            options.Tooltip.Enabled = true;
            options.Tooltip.HitPixelTolerance = 14;
            options.Tooltip.Format = "{5}\r\nX1:{1:0.###}, X2:{2:0.###}";
            options.NoData.Text = "PCA Scatter data is empty.";
            options.NoData.ShowWhenDataMissing = true;
            options.NoData.ShowWhenAllValuesZero = false;
            return options;
        }

        private KnnSearchAlgorithm GetSelectedKnnAlgorithm()
        {
            object selected = knnAlgorithmComboBox.SelectedItem;
            if (selected is KnnSearchAlgorithm)
            {
                return (KnnSearchAlgorithm)selected;
            }

            return KnnSearchAlgorithm.Auto;
        }

        private PcaParameterType GetSelectedParameterType()
        {
            return defectRadioButton.Checked
                ? PcaParameterType.Defect
                : PcaParameterType.Response;
        }

        private void PcaChart_SampleClicked(object sender, PcaScatterSampleClickedEventArgs e)
        {
            if (e.Sample == null)
            {
                return;
            }

            PcaExperimentRecord target = e.Sample.UserData as PcaExperimentRecord;
            if (target == null && e.Sample.SourceIndex >= 0 && e.Sample.SourceIndex < currentRecords.Count)
            {
                target = currentRecords[e.Sample.SourceIndex];
            }

            if (target == null)
            {
                return;
            }

            draftNoTextBox.Text = target.DraftNo;
            IList<KnnNeighbor> neighbors = currentAnalysis == null || currentAnalysis.AnalysisResult == null
                ? e.Neighbors
                : currentAnalysis.AnalysisResult.FindNearest(
                    target.DraftNo,
                    GetNeighborSearchCount(CreateChartOptions()));
            BindNearestNeighborTable(CreateNearestNeighborTable(target, neighbors));
        }

        private void PcaChart_AnalysisCompleted(object sender, PcaScatterAnalysisCompletedEventArgs e)
        {
            if (currentAnalysis == null && e.AnalysisResult != null)
            {
                summaryLabel.Text = "Accord.NET PCA analysis completed.";
            }
        }

        private void PcaChart_AnalysisFailed(object sender, PcaScatterAnalysisFailedEventArgs e)
        {
            summaryLabel.Text = e.Exception == null
                ? "Accord.NET PCA failed."
                : "Accord.NET PCA failed: " + e.Exception.Message;
        }

        private void NearestNeighborGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (nearestNeighborGridBinding)
            {
                return;
            }

            UpdateSelectedNeighborHighlight();
        }

        private void UpdateSelectedNeighborHighlight()
        {
            if (pcaChart == null || nearestNeighborGrid.SelectedRows.Count == 0)
            {
                if (pcaChart != null)
                {
                    pcaChart.ClearSelectedDraftHighlight();
                }

                return;
            }

            string draftNo = ResolveSelectedNeighborDraftNo();
            if (string.IsNullOrWhiteSpace(draftNo))
            {
                pcaChart.ClearSelectedDraftHighlight();
                return;
            }

            pcaChart.HighlightSelectedDraft(draftNo);
        }

        private string ResolveSelectedNeighborDraftNo()
        {
            DataGridViewRow row = nearestNeighborGrid.SelectedRows
                .Cast<DataGridViewRow>()
                .FirstOrDefault(item => item != null && !item.IsNewRow);
            if (row == null || !nearestNeighborGrid.Columns.Contains("DRAFT_NO"))
            {
                return string.Empty;
            }

            object value = row.Cells["DRAFT_NO"].Value;
            return value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private void ConfigureNearestNeighborGrid()
        {
            if (nearestNeighborGrid == null)
            {
                return;
            }

            nearestNeighborGrid.AllowUserToResizeRows = false;
            nearestNeighborGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            nearestNeighborGrid.RowTemplate.Height = 28;
            nearestNeighborGrid.RowTemplate.Resizable = DataGridViewTriState.False;
            nearestNeighborGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            nearestNeighborGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nearestNeighborGrid.ColumnHeadersDefaultCellStyle.Font = gridFont;
            nearestNeighborGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            nearestNeighborGrid.DefaultCellStyle.Font = gridFont;
            nearestNeighborGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 242, 128);
            nearestNeighborGrid.DefaultCellStyle.SelectionForeColor = Color.Black;
            nearestNeighborGrid.RowTemplate.DefaultCellStyle.Font = gridFont;
            nearestNeighborGrid.RowTemplate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            foreach (DataGridViewRow row in nearestNeighborGrid.Rows)
            {
                row.Height = 28;
                row.Resizable = DataGridViewTriState.False;
            }
        }

        private void BindNearestNeighborTable(DataTable table)
        {
            nearestNeighborGridBinding = true;
            try
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

                ConfigureNearestNeighborGrid();
                nearestNeighborGrid.ClearSelection();
            }
            finally
            {
                nearestNeighborGridBinding = false;
            }

            UpdateSelectedNeighborHighlight();
        }

        private DataTable CreateNearestNeighborTable(
            PcaExperimentRecord target,
            IEnumerable<KnnNeighbor> neighbors)
        {
            DataTable table = new DataTable();
            table.Columns.Add("DRAFT_NO", typeof(string));
            table.Columns.Add("PARAM_TYP", typeof(string));
            table.Columns.Add("LABEL(Y)", typeof(string));
            table.Columns.Add("X1", typeof(double));
            table.Columns.Add("X2", typeof(double));
            table.Columns.Add("Rank", typeof(int));
            table.Columns.Add("Target_Draft", typeof(string));
            table.Columns.Add("Distance", typeof(double));

            if (target == null || neighbors == null)
            {
                return table;
            }

            int maxRows = Math.Max(1, CreateChartOptions().Analysis.NeighborCount);
            foreach (KnnNeighbor neighbor in neighbors)
            {
                if (neighbor.SourceIndex < 0 || neighbor.SourceIndex >= currentRecords.Count)
                {
                    continue;
                }

                PcaExperimentRecord similar = currentRecords[neighbor.SourceIndex];
                if (string.Equals(similar.DraftNo, target.DraftNo, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                DataRow row = table.NewRow();
                row["DRAFT_NO"] = similar.DraftNo;
                row["PARAM_TYP"] = PcaParameterTypeParser.ToDatabaseValue(similar.ParameterType);
                row["LABEL(Y)"] = similar.LabelY;
                row["X1"] = similar.X1;
                row["X2"] = similar.X2;
                row["Rank"] = table.Rows.Count + 1;
                row["Target_Draft"] = target.DraftNo;
                row["Distance"] = neighbor.Distance;
                table.Rows.Add(row);
                if (table.Rows.Count >= maxRows)
                {
                    break;
                }
            }

            return table;
        }

        private static int GetNeighborSearchCount(PcaScatterOptions chartOptions)
        {
            int neighborCount = chartOptions == null || chartOptions.Analysis == null
                ? 3
                : Math.Max(1, chartOptions.Analysis.NeighborCount);
            return neighborCount + 1;
        }

        private void UpdateSummary(
            PcaExadataAnalysisResult result,
            string sourceDescription)
        {
            if (result == null || result.AnalysisResult == null)
            {
                summaryLabel.Text = "No analysis result.";
                return;
            }

            PcaAnalysisDiagnosticReport diagnostic = result.Diagnostic
                ?? PcaAnalysisDiagnosticReport.Create(
                    result.AnalysisResult,
                    result.Records == null ? 0 : result.Records.Count,
                    result.MissingExperimentCount);
            summaryLabel.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0} | TYPE={1} SRC={2} LOG={3}",
                diagnostic.CompactText,
                PcaParameterTypeParser.ToDatabaseValue(result.ParameterType),
                sourceDescription,
                string.IsNullOrWhiteSpace(lastFeatureAuditLogPath) ? "-" : lastFeatureAuditLogPath);
        }

        private void ShowOperationError(Exception exception, string title)
        {
            summaryLabel.Text = title;
            MessageBox.Show(
                this,
                exception == null ? title : exception.Message,
                title,
                MessageBoxButtons.OK,
                exception is KeyNotFoundException || exception is PcaExperimentDataMissingException
                    ? MessageBoxIcon.Warning
                    : MessageBoxIcon.Error);
        }

        private void BeginBusy(string message)
        {
            parameterChangeEnabled = false;
            SetInputEnabled(false);
            ShowBusyOverlay(message);
        }

        private void EndBusy()
        {
            HideBusyOverlay();
            SetInputEnabled(true);
            parameterChangeEnabled = true;
        }

        private void SetInputEnabled(bool enabled)
        {
            responseRadioButton.Enabled = enabled;
            defectRadioButton.Enabled = enabled;
            draftNoTextBox.Enabled = enabled;
            searchButton.Enabled = enabled;
            chartDrawButton.Enabled = enabled;
            refreshAllButton.Enabled = enabled;
            sampleDataButton.Enabled = enabled;
            preferMemoryCheckBox.Enabled = enabled;
            knnAlgorithmComboBox.Enabled = enabled;
            nearestNeighborGrid.Enabled = enabled;
            UseWaitCursor = !enabled;
        }

        private void ShowBusyOverlay(string message)
        {
            if (busyOverlayPanel == null)
            {
                return;
            }

            busyOverlayLabel.Text = string.IsNullOrWhiteSpace(message)
                ? "Processing. Please wait..."
                : message.Trim();
            busyProgressBar.Value = 8;
            busyOverlayPanel.Visible = true;
            busyOverlayPanel.Enabled = true;
            busyOverlayPanel.BringToFront();
            busyProgressTimer.Start();
            UpdateBusyOverlayLayout();
            Application.DoEvents();
        }

        private static Task AllowBusyOverlayToPaintAsync()
        {
            return Task.Delay(50);
        }

        private void HideBusyOverlay()
        {
            if (busyOverlayPanel == null)
            {
                return;
            }

            busyProgressTimer.Stop();
            busyProgressBar.Value = 0;
            busyOverlayPanel.Visible = false;
        }

        private void BusyProgressTimer_Tick(object sender, EventArgs e)
        {
            if (busyProgressBar == null || !busyOverlayPanel.Visible)
            {
                return;
            }

            busyProgressBar.Value = busyProgressBar.Value >= 95
                ? 12
                : Math.Min(95, busyProgressBar.Value + 4);
        }

        private void BusyOverlayPanel_Resize(object sender, EventArgs e)
        {
            UpdateBusyOverlayLayout();
        }

        private void UpdateBusyOverlayLayout()
        {
            if (busyOverlayPanel == null
                || busyOverlayLabel == null
                || busyProgressBar == null)
            {
                return;
            }

            int contentWidth = Math.Min(460, Math.Max(260, busyOverlayPanel.ClientSize.Width - 80));
            int progressWidth = Math.Min(380, contentWidth);
            int centerX = busyOverlayPanel.ClientSize.Width / 2;
            int centerY = busyOverlayPanel.ClientSize.Height / 2;

            busyOverlayLabel.SetBounds(
                Math.Max(8, centerX - (contentWidth / 2)),
                Math.Max(8, centerY - 34),
                contentWidth,
                24);
            busyProgressBar.SetBounds(
                Math.Max(8, centerX - (progressWidth / 2)),
                busyOverlayLabel.Bottom + 10,
                progressWidth,
                18);
        }

        private void WriteFeatureSelectionAuditLog(
            PcaExadataAnalysisResult result,
            PcaScatterOptions chartOptions)
        {
            if (result == null || result.FeatureSelectionReport == null)
            {
                return;
            }

            string detailedLog = BuildFeatureSelectionAuditText(result, chartOptions, true);
            lastFeatureAuditLogPath = SaveFeatureSelectionAuditLog(result, detailedLog);
            Debug.WriteLine("Accord PCA Feature Audit Log: " + lastFeatureAuditLogPath);

#if DEBUG
            string popupLog = BuildFeatureSelectionAuditText(result, chartOptions, false);
            if (!string.IsNullOrWhiteSpace(lastFeatureAuditLogPath))
            {
                popupLog += Environment.NewLine
                    + "Developer log file:"
                    + Environment.NewLine
                    + lastFeatureAuditLogPath;
            }

            MessageBox.Show(
                this,
                popupLog,
                "Accord PCA Feature Audit (Developer)",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
#endif
        }

        private static string BuildFeatureSelectionAuditText(
            PcaExadataAnalysisResult result,
            PcaScatterOptions chartOptions,
            bool includeFullDetails)
        {
            PcaFeatureSelectionReport report = result.FeatureSelectionReport;
            var builder = new StringBuilder();
            builder.AppendLine("Accord PCA Feature Selection Audit");
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "CreatedAt: {0:yyyy-MM-dd HH:mm:ss.fff}",
                DateTime.Now));
            builder.AppendLine("PCA Engine: Accord.NET PrincipalComponentAnalysis");
            builder.AppendLine("Manual PCA pipeline: Not used by AccordScatterMain");
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "ParameterType: {0}",
                PcaParameterTypeParser.ToDatabaseValue(result.ParameterType)));
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "LogMode: {0}",
                includeFullDetails ? "Detailed" : "Summary"));
            builder.AppendLine();

            if (result.Diagnostic != null)
            {
                builder.AppendLine(result.Diagnostic.CompactText);
                builder.AppendLine("KNN algorithm: " + result.Diagnostic.KnnAlgorithm);
                if (!string.IsNullOrWhiteSpace(result.Diagnostic.KnnAlgorithmReason))
                {
                    builder.AppendLine("KNN reason: " + result.Diagnostic.KnnAlgorithmReason);
                }
            }

            if (result.AnalysisResult != null && result.AnalysisResult.Verification != null)
            {
                builder.AppendLine("Verification: " + result.AnalysisResult.Verification.Message);
                builder.AppendLine("Shared scaler: " + result.AnalysisResult.Verification.SharedScalerInstance);
            }

            builder.AppendLine(report.ToSummaryText());
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "Numeric coverage threshold: {0:P1}",
                chartOptions == null || chartOptions.Analysis == null
                    ? 1d
                    : chartOptions.Analysis.MinimumNumericFeatureCoverageRatio));
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "Mean imputation: {0}",
                chartOptions != null
                    && chartOptions.Analysis != null
                    && chartOptions.Analysis.MeanImputationEnabled
                        ? "Enabled"
                        : "Disabled"));
            builder.AppendLine();
            AppendExcludedReasonSummary(builder, report);
            AppendFeatureNames(
                builder,
                "Included features",
                report.IncludedFeatureNames,
                includeFullDetails ? int.MaxValue : 20);
            AppendExcludedFeatureSamples(
                builder,
                report,
                includeFullDetails ? int.MaxValue : 15);

            if (includeFullDetails)
            {
                AppendFeatureDetails(
                    builder,
                    "Included feature details",
                    report.Details.Where(detail => detail.Included));
                AppendFeatureDetails(
                    builder,
                    "Excluded feature details",
                    report.Details.Where(detail => !detail.Included));
            }

            return builder.ToString();
        }

        private static void AppendExcludedReasonSummary(
            StringBuilder builder,
            PcaFeatureSelectionReport report)
        {
            builder.AppendLine("Excluded reason summary:");
            var groups = report.Details
                .Where(detail => !detail.Included)
                .GroupBy(detail => detail.Reason)
                .OrderByDescending(group => group.Count())
                .ToList();
            if (groups.Count == 0)
            {
                builder.AppendLine("- None");
                builder.AppendLine();
                return;
            }

            foreach (var group in groups)
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "- {0}: {1}",
                    group.Key,
                    group.Count()));
            }

            builder.AppendLine();
        }

        private static void AppendFeatureNames(
            StringBuilder builder,
            string title,
            IEnumerable<string> names,
            int maxCount)
        {
            builder.AppendLine(title + ":");
            string[] items = (names ?? Enumerable.Empty<string>()).ToArray();
            if (items.Length == 0)
            {
                builder.AppendLine("- None");
                builder.AppendLine();
                return;
            }

            foreach (string name in items.Take(maxCount))
            {
                builder.AppendLine("- " + name);
            }

            if (items.Length > maxCount)
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "- ... {0} more",
                    items.Length - maxCount));
            }

            builder.AppendLine();
        }

        private static void AppendExcludedFeatureSamples(
            StringBuilder builder,
            PcaFeatureSelectionReport report,
            int maxCount)
        {
            builder.AppendLine("Excluded feature samples:");
            PcaFeatureSelectionDetail[] details = report.Details
                .Where(detail => !detail.Included)
                .Take(maxCount)
                .ToArray();
            if (details.Length == 0)
            {
                builder.AppendLine("- None");
                builder.AppendLine();
                return;
            }

            foreach (PcaFeatureSelectionDetail detail in details)
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "- {0}: {1}, Present={2}, Numeric={3}, Missing={4}, NonNumeric={5}, Var={6:0.##########}",
                    detail.FeatureName,
                    detail.Reason,
                    detail.PresentCount,
                    detail.NumericCount,
                    detail.MissingCount,
                    detail.NonNumericCount,
                    detail.HasStatistics ? detail.Variance : 0d));
            }

            builder.AppendLine();
        }

        private static void AppendFeatureDetails(
            StringBuilder builder,
            string title,
            IEnumerable<PcaFeatureSelectionDetail> details)
        {
            builder.AppendLine(title + ":");
            foreach (PcaFeatureSelectionDetail detail in details ?? Enumerable.Empty<PcaFeatureSelectionDetail>())
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}, Included={1}, Reason={2}, Present={3}, Numeric={4}, Missing={5}, NonNumeric={6}, Mean={7:0.##########}, StdDev={8:0.##########}, Var={9:0.##########}, Min={10:0.##########}, Max={11:0.##########}, SampleDraft={12}",
                    detail.FeatureName,
                    detail.Included,
                    detail.Reason,
                    detail.PresentCount,
                    detail.NumericCount,
                    detail.MissingCount,
                    detail.NonNumericCount,
                    detail.HasStatistics ? detail.Mean : 0d,
                    detail.HasStatistics ? detail.StandardDeviation : 0d,
                    detail.HasStatistics ? detail.Variance : 0d,
                    detail.HasStatistics ? detail.Minimum : 0d,
                    detail.HasStatistics ? detail.Maximum : 0d,
                    detail.SampleDraftNo ?? string.Empty));
            }

            builder.AppendLine();
        }

        private static string SaveFeatureSelectionAuditLog(
            PcaExadataAnalysisResult result,
            string logText)
        {
            try
            {
                string root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SKhynix",
                    "TAS",
                    "AccordPcaScatter",
                    "AnalysisLogs",
                    DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
                Directory.CreateDirectory(root);
                string parameterType = result == null
                    ? "UNKNOWN"
                    : PcaParameterTypeParser.ToDatabaseValue(result.ParameterType);
                string fileName = string.Format(
                    CultureInfo.InvariantCulture,
                    "accord_pca_feature_audit_{0}_{1:yyyyMMdd_HHmmss_fff}_{2}.log",
                    SanitizeFileName(parameterType),
                    DateTime.Now,
                    Guid.NewGuid().ToString("N").Substring(0, 8));
                string path = Path.Combine(root, fileName);
                File.WriteAllText(path, logText ?? string.Empty, Encoding.UTF8);
                return path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Accord PCA Feature Audit log save failed: " + ex.Message);
                return string.Empty;
            }
        }

        private static string SanitizeFileName(string value)
        {
            string safeValue = string.IsNullOrWhiteSpace(value) ? "UNKNOWN" : value.Trim();
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                safeValue = safeValue.Replace(invalidChar, '_');
            }

            return safeValue;
        }
    }
}
