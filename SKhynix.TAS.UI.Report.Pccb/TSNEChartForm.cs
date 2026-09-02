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
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.Common;

namespace SKhynix.TAS.UI.Report.Pccb
{
    public partial class TSNEChartForm : Form
    {
        private readonly TSNEChart tsneChart;
        private readonly TSNEExadataService exadataService;
        private readonly ConvExperimentRepository exadataRepository;
        private readonly ITSNEScatterPopupDataProvider popupDataProvider;

        private TSNEAnalysisResult analysisResult;
        private TSNEExadataAnalysisResult exadataAnalysis;
        private DataTable pendingSourceTable;
        private IList<TSNEPointData> currentSamples;
        private IList<TSNEExperimentRecord> currentRecords;
        private bool parameterChangeEnabled;
        private bool nearestNeighborGridBinding;
        private string lastFeatureAuditLogPath;
        private Font nearestNeighborGridFont;
        private bool showAnalysisSummaryText;
        private bool showRefreshAllButton;
        private bool showAnalysisLogButton;
        private bool showPreferMemoryOption;
        private bool showFeatureAuditMessageBox;
        private DimensionalityReductionMethod projectionMethod;

        public TSNEChartForm()
            : this(null)
        {
        }

        public TSNEChartForm(ITSNEScatterPopupDataProvider popupDataProvider)
        {
            InitializeComponent();

            this.popupDataProvider = popupDataProvider;
            MinimumNumericCoveragePercent = 90d;
            SeriesPointSize = 7f;
            HighlightPointSize = 0f;
            SelectedPointSize = 0f;
            // The effective perplexity is resolved from the input row count.
            TSNEPerplexity = 30d;
            TSNEIterations = 1000;
            // Accord.NET 3.8 uses its built-in learning rate (eta=200).
            TSNELearningRate = 200d;
            TSNERandomSeed = 42;
            currentSamples = new List<TSNEPointData>();
            currentRecords = new List<TSNEExperimentRecord>();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                exadataRepository = null;
                exadataService = null;
                tsneChart = null;
                return;
            }

            exadataRepository = new ConvExperimentRepository(
                TSNEScatterExadataOptions.CreateDefault().ToQueryOptions());
            exadataService = new TSNEExadataService(exadataRepository);

            ConfigureNearestNeighborGrid();
            BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
            tsneChart = TSNEChart.Create(chartHost, CreateChartOptions());
            tsneChart.SampleClicked += TSNEChart_SampleClicked;
            tsneChart.AnalysisCompleted += TSNEChart_AnalysisCompleted;
            tsneChart.AnalysisFailed += TSNEChart_AnalysisFailed;
            tsneChart.Clear();
            projectionMethod = DimensionalityReductionMethod.TSNE;
            lastFeatureAuditLogPath = GetFeatureSelectionAuditLogPath();
            summaryLabel.Text = "Operation message.";
            ApplyOptionalUiVisibility();
            parameterChangeEnabled = true;
            SetToolbarEnabled(true);
        }

        /// <summary>
        /// 수치형 feature로 인정할 최소 데이터 보유율이다.
        /// 90을 넣으면 90%, 0.9를 넣으면 90%로 처리한다.
        /// </summary>
        public double MinimumNumericCoveragePercent { get; set; }

        /// <summary>
        /// 일반 시리즈 포인트 크기다. 기본값은 7이다.
        /// </summary>
        public float SeriesPointSize { get; set; }

        /// <summary>
        /// 조회한 DRAFT_NO 강조 포인트 크기다. 0 이하이면 일반 포인트보다 10% 크게 표시한다.
        /// </summary>
        public float HighlightPointSize { get; set; }

        /// <summary>
        /// 그리드에서 선택한 행의 포인트 크기다. 0 이하이면 일반 포인트보다 10% 크게 표시한다.
        /// </summary>
        public float SelectedPointSize { get; set; }

        /// <summary>
        /// 차트에 사용할 2차원 축소 방식이다. 기본값은 기존 호환을 위한 TSNE다.
        /// </summary>
        public DimensionalityReductionMethod ProjectionMethod
        {
            get { return projectionMethod; }
            set
            {
                projectionMethod = value;
                ApplyProjectionUiText();
            }
        }

        public double TSNEPerplexity { get; set; }
        public int TSNEIterations { get; set; }
        public double TSNELearningRate { get; set; }
        public int TSNERandomSeed { get; set; }

        /// <summary>
        /// 상단 분석 상태 텍스트 표시 여부다. 기본값은 숨김이다.
        /// </summary>
        public bool ShowAnalysisSummaryText
        {
            get { return showAnalysisSummaryText; }
            set
            {
                showAnalysisSummaryText = value;
                ApplyOptionalUiVisibility();
            }
        }

        /// <summary>
        /// 전체 새로고침 버튼 표시 여부다. 기본값은 숨김이다.
        /// </summary>
        public bool ShowRefreshAllButton
        {
            get { return showRefreshAllButton; }
            set
            {
                showRefreshAllButton = value;
                ApplyOptionalUiVisibility();
            }
        }

        /// <summary>
        /// TSNE 분석 로그 열기 버튼 표시 여부다. 기본값은 숨김이다.
        /// </summary>
        public bool ShowAnalysisLogButton
        {
            get { return showAnalysisLogButton; }
            set
            {
                showAnalysisLogButton = value;
                ApplyOptionalUiVisibility();
            }
        }

        /// <summary>
        /// 메모리 데이터 우선 체크박스 표시 여부다. 기본값은 숨김이다.
        /// </summary>
        public bool ShowPreferMemoryOption
        {
            get { return showPreferMemoryOption; }
            set
            {
                showPreferMemoryOption = value;
                ApplyOptionalUiVisibility();
            }
        }

        /// <summary>
        /// 차트 렌더링 후 개발자용 TSNE 로그 메시지박스를 표시할지 결정한다. 기본값은 비활성이다.
        /// </summary>
        public bool ShowFeatureAuditMessageBox
        {
            get { return showFeatureAuditMessageBox; }
            set { showFeatureAuditMessageBox = value; }
        }

        private void ConfigureNearestNeighborGrid()
        {
            if (nearestNeighborGridFont == null)
            {
                nearestNeighborGridFont = new Font("Segoe UI", 10f, FontStyle.Regular);
            }

            nearestNeighborGrid.AllowUserToResizeRows = false;
            nearestNeighborGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            nearestNeighborGrid.RowTemplate.Height = 28;
            nearestNeighborGrid.RowTemplate.Resizable = DataGridViewTriState.False;
            nearestNeighborGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            nearestNeighborGrid.ColumnHeadersDefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;
            nearestNeighborGrid.ColumnHeadersDefaultCellStyle.Font = nearestNeighborGridFont;

            nearestNeighborGrid.DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleLeft;
            nearestNeighborGrid.DefaultCellStyle.Font = nearestNeighborGridFont;
            nearestNeighborGrid.RowsDefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleLeft;
            nearestNeighborGrid.RowsDefaultCellStyle.Font = nearestNeighborGridFont;
            nearestNeighborGrid.AlternatingRowsDefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleLeft;
            nearestNeighborGrid.AlternatingRowsDefaultCellStyle.Font = nearestNeighborGridFont;
        }

        private void ApplyOptionalUiVisibility()
        {
            ApplyOptionalControlState(summaryLabel, showAnalysisSummaryText);
            ApplyOptionalControlState(refreshAllButton, showRefreshAllButton);
            ApplyOptionalControlState(preferMemoryCheckBox, showPreferMemoryOption);
            if (analysisLogButton != null)
            {
                analysisLogButton.Visible = showAnalysisLogButton;
                if (!showAnalysisLogButton)
                {
                    analysisLogButton.Enabled = false;
                }
                else
                {
                    UpdateAnalysisLogButtonState();
                }
            }
        }

        private static void ApplyOptionalControlState(Control control, bool visible)
        {
            if (control == null)
            {
                return;
            }

            control.Visible = visible;
            control.Enabled = visible;
        }

        public Task LoadConvExperimentDataTableAsync(DataTable sourceTable)
        {
            if (sourceTable == null)
            {
                throw new ArgumentNullException("sourceTable");
            }

            pendingSourceTable = sourceTable;
            exadataRepository.SetSourceTable(sourceTable);
            exadataService.ClearSnapshot();
            analysisResult = null;
            exadataAnalysis = null;
            currentSamples = new List<TSNEPointData>();
            currentRecords = new List<TSNEExperimentRecord>();
            tsneChart.Clear();
            BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
            preferMemoryCheckBox.Checked = true;
            summaryLabel.Text = string.Format(
                CultureInfo.InvariantCulture,
                "Operation message: {0}",
                sourceTable.Rows.Count,
                GetProjectionDisplayName());
            SetToolbarEnabled(true);
            UpdateAnalysisLogButtonState();
            return Task.FromResult(0);
        }

        public Task DrawChartAsync()
        {
            return DrawLoadedDataAsync();
        }

        private async Task DrawLoadedDataAsync()
        {
            SetToolbarEnabled(false);
            ShowBusyOverlay("Operation message." + GetProjectionDisplayName() + "Operation message.");
            try
            {
                TSNEExadataSnapshot snapshot = await LoadCurrentSourceSnapshotAsync();
                preferMemoryCheckBox.Checked = true;
                UpdateBusyMessage("Operation message." + GetProjectionDisplayName() + "Operation message.");
                await AnalyzeCurrentSnapshotAsync(snapshot, false);
            }
            finally
            {
                SetToolbarEnabled(true);
            }
        }

        private async void SearchButton_Click(object sender, EventArgs e)
        {
            await QueryDraftAsync();
        }

        private async void DrawChartButton_Click(object sender, EventArgs e)
        {
            try
            {
                await DrawLoadedDataAsync();
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, GetProjectionDisplayName() + "Operation message.");
            }
        }

        private async void RefreshAllButton_Click(object sender, EventArgs e)
        {
            await RefreshAllAsync();
        }

        private void AnalysisLogButton_Click(object sender, EventArgs e)
        {
            OpenLatestAnalysisLog();
        }

        private async void ParameterType_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (!parameterChangeEnabled || radioButton == null || !radioButton.Checked)
            {
                return;
            }

            TSNEExadataSnapshot snapshot = exadataService.CurrentSnapshot;
            if (snapshot == null)
            {
                return;
            }

            await AnalyzeCurrentSnapshotAsync(snapshot);
        }

        private async void DraftNoTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
            await QueryDraftAsync();
        }

        private async Task QueryDraftAsync()
        {
            string draftNo = (draftNoTextBox.Text ?? string.Empty).Trim();
            if (draftNo.Length == 0)
            {
                MessageBox.Show(this, "Operation message.", "Operation message.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                draftNoTextBox.Focus();
                return;
            }

            TSNEParameterType parameterType = GetSelectedParameterType();
            TSNEExadataRefreshMode refreshMode = preferMemoryCheckBox.Checked
                ? TSNEExadataRefreshMode.PreferMemorySnapshot
                : TSNEExadataRefreshMode.AlwaysReload;
            summaryLabel.Text = string.Format(
                "Operation message: {0}",
                TSNEParameterTypeParser.ToDatabaseValue(parameterType),
                draftNo);

            try
            {
                if (exadataService.CurrentSnapshot == null)
                {
                    MessageBox.Show(
                        this,
                        "Operation message." + GetProjectionDisplayName() + "Operation message.",
                        GetProjectionDisplayName() + "Operation message.",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                if (exadataService.CurrentSnapshot != null)
                {
                    preferMemoryCheckBox.Checked = true;
                    refreshMode = TSNEExadataRefreshMode.PreferMemorySnapshot;
                }

                TSNEScatterOptions chartOptions = CreateChartOptions();
                TSNEExperimentRecord currentTarget;
                IList<KnnNeighbor> currentNeighbors;
                if (TryQueryDraftFromCurrentAnalysis(draftNo, parameterType, chartOptions, out currentTarget, out currentNeighbors))
                {
                    tsneChart.HighlightDraft(currentTarget.DraftNo);
                    BindNearestNeighborTable(CreateNearestNeighborTable(currentTarget, currentNeighbors));
                    UpdateSummary(exadataAnalysis, "Operation message.");
                    return;
                }

                TSNEDraftQueryResult result = await exadataService.QueryDraftAsync(
                    draftNo,
                    parameterType,
                    refreshMode,
                    chartOptions.Analysis);
                chartOptions.Series.HighlightDraftNo = result.Target.DraftNo;
                ApplyAnalysis(result.Analysis, chartOptions);
                BindNearestNeighborTable(
                    CreateNearestNeighborTable(result.Target, result.Neighbors));
                UpdateSummary(result.Analysis, result.UsedMemorySnapshot
                    ? "Operation message."
                    : "Operation message.");
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, "Operation message.");
            }
        }

        private async Task<TSNEExadataSnapshot> LoadPopupDatabaseSnapshotAsync()
        {
            if (popupDataProvider == null)
            {
                throw new InvalidOperationException("TSNE popup data provider is not configured.");
            }

            summaryLabel.Text = popupDataProvider.SourceDescription + "Operation message.";
            UpdateBusyMessage(summaryLabel.Text);
            DataTable sourceTable = await popupDataProvider.LoadAllAsync();
            UpdateBusyMessage("Operation message.");
            exadataRepository.SetSourceTable(sourceTable);
            TSNEExadataSnapshot snapshot = await exadataService.LoadFromDataTableAsync(sourceTable);
            preferMemoryCheckBox.Checked = true;
            return snapshot;
        }

        private async Task<TSNEExadataSnapshot> LoadCurrentSourceSnapshotAsync()
        {
            if (pendingSourceTable != null)
            {
                UpdateBusyMessage("Operation message.");
                exadataRepository.SetSourceTable(pendingSourceTable);
                TSNEExadataSnapshot snapshot = await exadataService.LoadFromDataTableAsync(pendingSourceTable);
                preferMemoryCheckBox.Checked = true;
                return snapshot;
            }

            return await LoadPopupDatabaseSnapshotAsync();
        }

        private async Task RefreshAllAsync()
        {
            SetToolbarEnabled(false);
            ShowBusyOverlay("Operation message." + GetProjectionDisplayName() + "Operation message.");
            TSNEParameterType parameterType = GetSelectedParameterType();
            summaryLabel.Text = "Operation message." + GetProjectionDisplayName() + "Operation message.";

            try
            {
                TSNEExadataSnapshot snapshot = await LoadCurrentSourceSnapshotAsync();
                TSNEScatterOptions chartOptions = CreateChartOptions();
                TSNEExadataAnalysisResult result = await Task.Run(delegate
                {
                    return exadataService.AnalyzeSnapshot(
                        snapshot,
                        parameterType,
                        chartOptions.Analysis);
                });
                ApplyAnalysis(result, chartOptions);
                BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
                UpdateSummary(result, string.Format("Operation message: {0}", result.Snapshot.Rows.Count));
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, "Operation message.");
            }
            finally
            {
                SetToolbarEnabled(true);
            }
        }

        private async Task AnalyzeCurrentSnapshotAsync(TSNEExadataSnapshot snapshot)
        {
            await AnalyzeCurrentSnapshotAsync(snapshot, true);
        }

        private async Task AnalyzeCurrentSnapshotAsync(TSNEExadataSnapshot snapshot, bool manageToolbar)
        {
            if (manageToolbar)
            {
                SetToolbarEnabled(false);
                ShowBusyOverlay("Operation message." + GetProjectionDisplayName() + "Operation message.");
            }

            TSNEParameterType parameterType = GetSelectedParameterType();
            summaryLabel.Text = TSNEParameterTypeParser.ToDatabaseValue(parameterType)
                + "Operation message." + GetProjectionDisplayName() + "Operation message.";
            try
            {
                TSNEScatterOptions chartOptions = CreateChartOptions();
                TSNEExadataAnalysisResult result = await Task.Run(delegate
                {
                    return exadataService.AnalyzeSnapshot(snapshot, parameterType, chartOptions.Analysis);
                });
                ApplyAnalysis(result, chartOptions);
                BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
                UpdateSummary(result, "Operation message.");
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, "Operation message.");
            }
            finally
            {
                if (manageToolbar)
                {
                    SetToolbarEnabled(true);
                }
            }
        }

        private void ApplyAnalysis(TSNEExadataAnalysisResult result, TSNEScatterOptions chartOptions)
        {
            if (result == null || result.AnalysisResult == null)
            {
                throw new ArgumentNullException("result");
            }

            exadataAnalysis = result;
            currentRecords = result.Records.ToList();
            tsneChart.Bind(result, chartOptions);
            // 차트를 새로 그린 직후 같은 로그 파일을 덮어써서, 화면과 로그가 항상 같은 분석 결과를 가리키게 한다.
            WriteFeatureSelectionAuditLog(result, chartOptions);
        }

        private void WriteFeatureSelectionAuditLog(TSNEExadataAnalysisResult result, TSNEScatterOptions chartOptions)
        {
            if (result == null || result.FeatureSelectionReport == null)
            {
                return;
            }

            string detailedLog = BuildFeatureSelectionAuditText(result, chartOptions, true);
            lastFeatureAuditLogPath = SaveFeatureSelectionAuditLog(result, detailedLog);
            UpdateAnalysisLogButtonState();
            Debug.WriteLine("TSNE Feature Audit Log: " + lastFeatureAuditLogPath);

#if DEBUG
            if (ShowFeatureAuditMessageBox)
            {
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
                    "TSNE Feature Audit (Developer)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
#endif
        }

        private string BuildFeatureSelectionAuditText(TSNEExadataAnalysisResult result, TSNEScatterOptions chartOptions, bool includeFullDetails)
        {
            TSNEFeatureSelectionReport report = result.FeatureSelectionReport;
            DataTable survivingPopulation = result.CreateSurvivingPopulationDataTable();
            var builder = new StringBuilder();
            builder.AppendLine(GetProjectionDisplayName() + " Feature Selection Audit");
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, "CreatedAt: {0:yyyy-MM-dd HH:mm:ss.fff}", DateTime.Now));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, "ParameterType: {0}", TSNEParameterTypeParser.ToDatabaseValue(result.ParameterType)));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, "LogMode: {0}", includeFullDetails ? "Detailed" : "Summary"));
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

            builder.AppendLine(report.ToSummaryText());
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "Numeric coverage threshold: {0:P1}",
                (chartOptions == null || chartOptions.Analysis == null)
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
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "Surviving population rows: {0:N0}",
                survivingPopulation.Rows.Count));
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "Surviving feature columns: {0:N0}",
                report.IncludedFeatureCount));
            builder.AppendLine();

            if (includeFullDetails)
            {
                AppendTSNEProcessingOverview(builder, result, chartOptions);
                AppendPreprocessingAndScalingExplanation(builder, result, chartOptions, true);
                AppendTSNEProjectionExplanation(builder, result, chartOptions, true);
                AppendAxisRangeExplanation(builder, result, chartOptions);
                AppendDistanceExplanation(builder, result, chartOptions, true);
            }

            AppendExcludedReasonSummary(builder, report);
            AppendFeatureNameSummary(builder, includeFullDetails ? "Included features" : "Included features", report.IncludedFeatureNames, includeFullDetails ? int.MaxValue : 20);
            AppendExcludedFeatureSamples(builder, report, includeFullDetails ? int.MaxValue : 15);

            if (includeFullDetails)
            {
                AppendFeatureDetailTable(builder, "Included feature details", report.Details.Where(detail => detail.Included));
                AppendFeatureDetailTable(builder, "Excluded feature details", report.Details.Where(detail => !detail.Included));
            }

            return builder.ToString();
        }

        private static string SaveFeatureSelectionAuditLog(TSNEExadataAnalysisResult result, string logText)
        {
            try
            {
                bool isTSNE = result != null
                    && result.AnalysisResult != null
                    && result.AnalysisResult.ProjectionMethod == DimensionalityReductionMethod.TSNE;
                string root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SKhynix",
                    "TAS",
                    isTSNE ? "TSNEScatter" : "TSNEScatter",
                    "AnalysisLogs");
                Directory.CreateDirectory(root);
                string path = Path.Combine(root, isTSNE ? "manual_tsne_latest_analysis.log" : "manual_tsne_latest_analysis.log");
                File.WriteAllText(path, logText ?? string.Empty, Encoding.UTF8);
                return path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TSNE Feature Audit log save failed: " + ex.Message);
                return string.Empty;
            }
        }

        private static string GetFeatureSelectionAuditLogPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SKhynix",
                "TAS",
                "TSNEScatter",
                "AnalysisLogs",
                "manual_tsne_latest_analysis.log");
        }

        private void AppendTSNEProcessingOverview(StringBuilder builder, TSNEExadataAnalysisResult result, TSNEScatterOptions chartOptions)
        {
            builder.AppendLine("TSNE processing overview:");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine();
        }

        private TSNEExperimentRecord ResolveAuditTargetRecord(TSNEExadataAnalysisResult result, TSNEScatterOptions chartOptions)
        {
            if (result == null || result.Records == null || result.Records.Count == 0)
            {
                return null;
            }

            string draftNo = null;
            if (chartOptions != null && chartOptions.Series != null && !string.IsNullOrWhiteSpace(chartOptions.Series.HighlightDraftNo))
            {
                draftNo = chartOptions.Series.HighlightDraftNo.Trim();
            }
            else if (draftNoTextBox != null && !string.IsNullOrWhiteSpace(draftNoTextBox.Text))
            {
                draftNo = draftNoTextBox.Text.Trim();
            }

            if (!string.IsNullOrWhiteSpace(draftNo))
            {
                TSNEExperimentRecord matched = result.Records.FirstOrDefault(record =>
                    record != null && string.Equals(record.DraftNo, draftNo, StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                {
                    return matched;
                }
            }

            return result.Records.FirstOrDefault();
        }

        private static bool TryGetOriginalValue(TSNEExperimentRecord record, string featureName, out double value)
        {
            value = 0d;
            return record != null
                && record.NumericFeatures != null
                && !string.IsNullOrWhiteSpace(featureName)
                && record.NumericFeatures.TryGetValue(featureName, out value);
        }

        private static double Dot(double[] left, double[] right)
        {
            if (left == null || right == null)
            {
                return 0d;
            }

            int length = Math.Min(left.Length, right.Length);
            double sum = 0d;
            for (int index = 0; index < length; index++)
            {
                sum += left[index] * right[index];
            }

            return sum;
        }

        private static string FormatNumber(double value)
        {
            if (double.IsNaN(value))
            {
                return "NaN";
            }

            if (double.IsInfinity(value))
            {
                return value > 0d ? "Infinity" : "-Infinity";
            }

            return value.ToString("0.##########", CultureInfo.InvariantCulture);
        }

        private void AppendPreprocessingAndScalingExplanation(StringBuilder builder, TSNEExadataAnalysisResult result, TSNEScatterOptions chartOptions, bool includeFullDetails)
        {
            TSNEAnalysisResult analysis = result == null ? null : result.AnalysisResult;
            TSNEScatterAnalysisOptions analysisOptions = chartOptions == null ? null : chartOptions.Analysis;
            double coverageRatio = analysisOptions == null ? 1d : analysisOptions.MinimumNumericFeatureCoverageRatio;
            bool meanImputationEnabled = analysisOptions != null && analysisOptions.MeanImputationEnabled;

            builder.AppendLine("Preprocessing and normalization explanation:");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Operation message: {0}", coverageRatio));
            builder.AppendLine(meanImputationEnabled
                ? "Operation message."
                : "Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");

            TSNEExperimentRecord target = ResolveAuditTargetRecord(result, chartOptions);
            if (analysis == null || analysis.Scaler == null || analysis.FeatureNames == null)
            {
                builder.AppendLine();
                return;
            }

            int maxRows = includeFullDetails ? 30 : 8;
            builder.AppendLine();
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Selected numeric feature count: {0:N0}", analysis.FeatureNames.Length));
            if (target != null)
            {
                builder.AppendLine("Normalization sample target Draft_NO: " + target.DraftNo);
                builder.AppendLine("Feature\tOriginalValue\tMean\tStdDev\tStandardizedValue");
                int count = Math.Min(maxRows, analysis.FeatureNames.Length);
                for (int index = 0; index < count; index++)
                {
                    string featureName = analysis.FeatureNames[index];
                    double originalValue = 0d;
                    bool hasOriginalValue = TryGetOriginalValue(target, featureName, out originalValue);
                    double standardizedValue = index < target.StandardizedVector.Length ? target.StandardizedVector[index] : 0d;
                    builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "{0}\t{1}\t{2}\t{3}\t{4}",
                        featureName,
                        hasOriginalValue ? FormatNumber(originalValue) : "(mean imputed)",
                        FormatNumber(analysis.Scaler.Means[index]),
                        FormatNumber(analysis.Scaler.StandardDeviations[index]),
                        FormatNumber(standardizedValue)));
                }
            }

            builder.AppendLine();
        }

        private void AppendTSNEProjectionExplanation(StringBuilder builder, TSNEExadataAnalysisResult result, TSNEScatterOptions chartOptions, bool includeFullDetails)
        {
            TSNEAnalysisResult analysis = result == null ? null : result.AnalysisResult;
            if (analysis == null || analysis.TSNEModel == null)
            {
                return;
            }
            builder.AppendLine("t-SNE projection uses Accord.NET Barnes-Hut optimization; PCA component weights and explained variance are not applicable.");
            builder.AppendLine();
        }

        private void AppendAxisRangeExplanation(StringBuilder builder, TSNEExadataAnalysisResult result, TSNEScatterOptions chartOptions)
        {
            TSNEAnalysisResult analysis = result == null ? null : result.AnalysisResult;
            if (analysis == null || analysis.ScatterData == null || analysis.ScatterData.Count == 0)
            {
                return;
            }

            TSNEScatterDisplayOptions display = chartOptions == null || chartOptions.Display == null
                ? new TSNEScatterDisplayOptions()
                : chartOptions.Display;
            AxisRangeAudit xRange = CalculateAxisRangeAudit(analysis.ScatterData, true, display);
            AxisRangeAudit yRange = CalculateAxisRangeAudit(analysis.ScatterData, false, display);

            builder.AppendLine("Chart axis range explanation:");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Axis\tRawMin\tRawMinDraft\tRawMax\tRawMaxDraft\tRangeMinAfterZero\tRangeMaxAfterZero\tPadding\tFinalMin\tFinalMax");
            AppendAxisRangeRow(builder, "X1", xRange);
            AppendAxisRangeRow(builder, "X2", yRange);
            builder.AppendLine();
        }

        private static AxisRangeAudit CalculateAxisRangeAudit(IList<TSNEPointData> samples, bool useX, TSNEScatterDisplayOptions display)
        {
            List<TSNEPointData> cleanSamples = (samples ?? new List<TSNEPointData>())
                .Where(sample =>
                {
                    double value = useX ? sample.X1 : sample.X2;
                    return !double.IsNaN(value) && !double.IsInfinity(value);
                })
                .ToList();

            if (!display.AutoCalculateAxisRange || cleanSamples.Count == 0)
            {
                return new AxisRangeAudit(-1d, 1d, null, -1d, 1d, 0d, -1d, 1d, null);
            }

            TSNEPointData minSample = cleanSamples.OrderBy(sample => useX ? sample.X1 : sample.X2).First();
            TSNEPointData maxSample = cleanSamples.OrderByDescending(sample => useX ? sample.X1 : sample.X2).First();
            double rawMin = useX ? minSample.X1 : minSample.X2;
            double rawMax = useX ? maxSample.X1 : maxSample.X2;
            double minimum = rawMin;
            double maximum = rawMax;
            if (display.IncludeZeroInAxisRange)
            {
                minimum = Math.Min(0d, minimum);
                maximum = Math.Max(0d, maximum);
            }

            if (Math.Abs(maximum - minimum) < 0.000001d)
            {
                minimum -= 1d;
                maximum += 1d;
            }

            double padding = Math.Max(Math.Max(0d, display.MinimumAxisPadding), (maximum - minimum) * Math.Max(0d, display.AxisPaddingRatio));
            return new AxisRangeAudit(rawMin, rawMax, minSample, minimum, maximum, padding, minimum - padding, maximum + padding, maxSample);
        }

        private static void AppendAxisRangeRow(StringBuilder builder, string axisName, AxisRangeAudit range)
        {
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",
                axisName,
                FormatNumber(range.RawMin),
                range.MinSample == null ? string.Empty : range.MinSample.DraftNo,
                FormatNumber(range.RawMax),
                range.MaxSample == null ? string.Empty : range.MaxSample.DraftNo,
                FormatNumber(range.RangeMinAfterZero),
                FormatNumber(range.RangeMaxAfterZero),
                FormatNumber(range.Padding),
                FormatNumber(range.FinalMin),
                FormatNumber(range.FinalMax)));
        }

        private void AppendDistanceExplanation(StringBuilder builder, TSNEExadataAnalysisResult result, TSNEScatterOptions chartOptions, bool includeFullDetails)
        {
            TSNEAnalysisResult analysis = result == null ? null : result.AnalysisResult;
            TSNEExperimentRecord target = ResolveAuditTargetRecord(result, chartOptions);
            if (analysis == null || target == null || result == null || result.Records == null || result.Records.Count == 0)
            {
                return;
            }

            int featureCount = analysis.FeatureNames == null ? 0 : analysis.FeatureNames.Length;
            int neighborCount = chartOptions == null || chartOptions.Analysis == null
                ? 3
                : Math.Max(1, chartOptions.Analysis.NeighborCount);
            IList<KnnNeighbor> neighbors = result.FindNearestByChartDistance(target.DraftNo, neighborCount, true);

            builder.AppendLine("KNN distance explanation:");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine("Operation message.");
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Operation message: {0}",
                featureCount));
            builder.AppendLine();
            builder.AppendLine("Rank\tSimilarDraft\tChartDistance\tChartDistanceSquared\tFeatureDistance\tRmsPerFeature");

            foreach (KnnNeighbor neighbor in neighbors)
            {
                if (neighbor.SourceIndex < 0 || neighbor.SourceIndex >= result.Records.Count)
                {
                    continue;
                }

                TSNEExperimentRecord similar = result.Records[neighbor.SourceIndex];
                double featureDistance = CalculateStandardizedFeatureDistance(target, similar);
                double rms = featureCount <= 0 ? 0d : featureDistance / Math.Sqrt(featureCount);
                builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                    neighbor.Rank,
                    similar.DraftNo,
                    FormatNumber(neighbor.Distance),
                    FormatNumber(neighbor.Distance * neighbor.Distance),
                    FormatNumber(featureDistance),
                    FormatNumber(rms)));
            }

            KnnNeighbor firstNeighbor = neighbors.FirstOrDefault(item => item.SourceIndex >= 0 && item.SourceIndex < result.Records.Count);
            if (firstNeighbor != null)
            {
                AppendDistanceContributionTable(
                    builder,
                    analysis,
                    target,
                    result.Records[firstNeighbor.SourceIndex],
                    includeFullDetails ? 30 : 8);
            }

            builder.AppendLine();
        }

        private static void AppendDistanceContributionTable(StringBuilder builder, TSNEAnalysisResult analysis, TSNEExperimentRecord target, TSNEExperimentRecord similar, int maxRows)
        {
            if (analysis == null || analysis.FeatureNames == null || target == null || similar == null)
            {
                return;
            }

            double[] targetVector = target.StandardizedVector;
            double[] similarVector = similar.StandardizedVector;
            int count = Math.Min(analysis.FeatureNames.Length, Math.Min(targetVector.Length, similarVector.Length));
            int[] indexes = Enumerable.Range(0, count)
                .OrderByDescending(index =>
                {
                    double diff = targetVector[index] - similarVector[index];
                    return diff * diff;
                })
                .Take(Math.Max(1, maxRows))
                .ToArray();

            builder.AppendLine();
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Distance contribution detail for nearest draft. Target={0}, Similar={1}",
                target.DraftNo,
                similar.DraftNo));
            builder.AppendLine("Feature\tTargetOriginal\tSimilarOriginal\tTargetStandardized\tSimilarStandardized\tStdDiff\tSquaredContribution");
            foreach (int index in indexes)
            {
                string featureName = analysis.FeatureNames[index];
                double targetOriginal;
                double similarOriginal;
                bool hasTargetOriginal = TryGetOriginalValue(target, featureName, out targetOriginal);
                bool hasSimilarOriginal = TryGetOriginalValue(similar, featureName, out similarOriginal);
                double diff = targetVector[index] - similarVector[index];
                builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                    featureName,
                    hasTargetOriginal ? FormatNumber(targetOriginal) : "(mean imputed)",
                    hasSimilarOriginal ? FormatNumber(similarOriginal) : "(mean imputed)",
                    FormatNumber(targetVector[index]),
                    FormatNumber(similarVector[index]),
                    FormatNumber(diff),
                    FormatNumber(diff * diff)));
            }
        }

        private sealed class AxisRangeAudit
        {
            public AxisRangeAudit(
                double rawMin, double rawMax, TSNEPointData minSample,
                double rangeMinAfterZero, double rangeMaxAfterZero,
                double padding, double finalMin, double finalMax, TSNEPointData maxSample)
            {
                RawMin = rawMin;
                RawMax = rawMax;
                MinSample = minSample;
                RangeMinAfterZero = rangeMinAfterZero;
                RangeMaxAfterZero = rangeMaxAfterZero;
                Padding = padding;
                FinalMin = finalMin;
                FinalMax = finalMax;
                MaxSample = maxSample;
            }

            public double RawMin { get; private set; }
            public double RawMax { get; private set; }
            public TSNEPointData MinSample { get; private set; }
            public double RangeMinAfterZero { get; private set; }
            public double RangeMaxAfterZero { get; private set; }
            public double Padding { get; private set; }
            public double FinalMin { get; private set; }
            public double FinalMax { get; private set; }
            public TSNEPointData MaxSample { get; private set; }
        }

        private static void AppendExcludedReasonSummary(StringBuilder builder, TSNEFeatureSelectionReport report)
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
            }
            else
            {
                foreach (IGrouping<TSNEFeatureSelectionReason, TSNEFeatureSelectionDetail> group in groups)
                {
                    builder.AppendLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "- {0}: {1:N0}",
                        group.Key,
                        group.Count()));
                }
            }

            builder.AppendLine();
        }

        private static void AppendFeatureNameSummary(StringBuilder builder, string title, IEnumerable<string> featureNames, int maxCount)
        {
            int safeMaxCount = maxCount <= 0 ? int.MaxValue : maxCount;
            string[] names = (featureNames ?? Enumerable.Empty<string>()).Take(safeMaxCount).ToArray();
            builder.AppendLine(safeMaxCount == int.MaxValue
                ? title + " (all):"
                : title + string.Format(CultureInfo.InvariantCulture, " (max {0}):", safeMaxCount));
            if (names.Length == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                foreach (string name in names)
                {
                    builder.AppendLine("- " + name);
                }
            }

            builder.AppendLine();
        }

        private static void AppendExcludedFeatureSamples(StringBuilder builder, TSNEFeatureSelectionReport report, int maxCount)
        {
            int safeMaxCount = maxCount <= 0 ? int.MaxValue : maxCount;
            builder.AppendLine(safeMaxCount == int.MaxValue
                ? "Excluded feature samples (all):"
                : string.Format(CultureInfo.InvariantCulture, "Excluded feature samples (max {0}):", safeMaxCount));
            TSNEFeatureSelectionDetail[] details = report.Details
                .Where(detail => !detail.Included)
                .OrderByDescending(detail => detail.MissingCount)
                .ThenByDescending(detail => detail.NonNumericCount)
                .ThenBy(detail => detail.FeatureName, StringComparer.OrdinalIgnoreCase)
                .Take(safeMaxCount)
                .ToArray();
            if (details.Length == 0)
            {
                builder.AppendLine("- None");
                return;
            }

            foreach (TSNEFeatureSelectionDetail detail in details)
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "- {0}: {1}, Present={2:N0}, Numeric={3:N0}, Missing={4:N0}, NonNumeric={5:N0}, Var={6:0.##########}",
                    detail.FeatureName,
                    detail.Reason,
                    detail.PresentCount,
                    detail.NumericCount,
                    detail.MissingCount,
                    detail.NonNumericCount,
                    detail.HasStatistics ? detail.Variance : 0d));
            }
        }

        private static void AppendFeatureDetailTable(StringBuilder builder, string title, IEnumerable<TSNEFeatureSelectionDetail> details)
        {
            builder.AppendLine();
            builder.AppendLine(title + ":");
            builder.AppendLine("FeatureName\tIncluded\tReason\tPresent\tNumeric\tMissing\tNonNumeric\tMean\tStdDev\tVariance\tMin\tMax\tSampleDraftNo");
            foreach (TSNEFeatureSelectionDetail detail in (details ?? Enumerable.Empty<TSNEFeatureSelectionDetail>())
                .OrderBy(detail => detail.FeatureName, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7:0.##########}\t{8:0.##########}\t{9:0.##########}\t{10:0.##########}\t{11:0.##########}\t{12}",
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
        }

        private void TSNEChart_SampleClicked(object sender, TSNESampleClickedEventArgs e)
        {
            if (e.Sample == null)
            {
                return;
            }

            TSNEExperimentRecord target = e.Sample.UserData as TSNEExperimentRecord;
            if (target == null && e.Sample.SourceIndex >= 0 && e.Sample.SourceIndex < currentRecords.Count)
            {
                target = currentRecords[e.Sample.SourceIndex];
            }

            if (target == null)
            {
                return;
            }

            draftNoTextBox.Text = target.DraftNo;
            tsneChart.HighlightDraft(target.DraftNo);
            BindNearestNeighborTable(CreateNearestNeighborTable(target, e.Neighbors));
        }

        private void TSNEChart_AnalysisCompleted(object sender, TSNEAnalysisCompletedEventArgs e)
        {
            analysisResult = e.AnalysisResult;
            currentSamples = analysisResult == null || analysisResult.ScatterData == null
                ? new List<TSNEPointData>()
                : analysisResult.ScatterData.ToList();
        }

        private void TSNEChart_AnalysisFailed(object sender, TSNEAnalysisFailedEventArgs e)
        {
            summaryLabel.Text = e.Exception == null
                ? "Operation message."
                : "Operation message." + e.Exception.Message;
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
            if (tsneChart == null || nearestNeighborGrid.SelectedRows.Count == 0)
            {
                if (tsneChart != null)
                {
                    tsneChart.ClearSelectedDraftHighlight();
                }

                return;
            }

            string draftNo = ResolveSelectedNeighborDraftNo();
            if (string.IsNullOrWhiteSpace(draftNo))
            {
                tsneChart.ClearSelectedDraftHighlight();
                return;
            }

            tsneChart.HighlightSelectedDraft(draftNo);
        }

        private string ResolveSelectedNeighborDraftNo()
        {
            DataGridViewRow row = nearestNeighborGrid.SelectedRows
                .Cast<DataGridViewRow>()
                .FirstOrDefault(item => item != null && !item.IsNewRow);
            if (row == null)
            {
                return string.Empty;
            }

            object value = null;
            if (nearestNeighborGrid.Columns.Contains("DRAFT_NO"))
            {
                value = row.Cells["DRAFT_NO"].Value;
            }
            else if (nearestNeighborGrid.Columns.Contains("Similar_Draft"))
            {
                value = row.Cells["Similar_Draft"].Value;
            }

            return value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private void ShowOperationError(Exception exception, string title)
        {
            summaryLabel.Text = title;
            MessageBox.Show(
                this,
                exception == null ? title : exception.Message,
                title,
                MessageBoxButtons.OK,
                exception is KeyNotFoundException || exception is TSNEExperimentDataMissingException
                    ? MessageBoxIcon.Warning
                    : MessageBoxIcon.Error);
        }

        private TSNEParameterType GetSelectedParameterType()
        {
            return defectRadioButton.Checked
                ? TSNEParameterType.Defect
                : TSNEParameterType.Response;
        }

        private void SetToolbarEnabled(bool enabled)
        {
            parameterChangeEnabled = enabled;
            responseRadioButton.Enabled = enabled;
            defectRadioButton.Enabled = enabled;
            draftNoTextBox.Enabled = enabled;
            searchButton.Enabled = enabled;
            drawChartButton.Enabled = enabled && HasRenderableDataSource();
            refreshAllButton.Enabled = enabled && showRefreshAllButton;
            preferMemoryCheckBox.Enabled = enabled && showPreferMemoryOption;
            summaryLabel.Enabled = enabled && showAnalysisSummaryText;
            nearestNeighborGrid.Enabled = enabled;
            UpdateAnalysisLogButtonState(enabled);
            UseWaitCursor = !enabled;
            if (enabled)
            {
                HideBusyOverlay();
            }
            else
            {
                ShowBusyOverlay(string.IsNullOrWhiteSpace(summaryLabel.Text)
                    ? "Operation message."
                    : summaryLabel.Text);
            }
        }

        private bool HasRenderableDataSource()
        {
            return pendingSourceTable != null
                || popupDataProvider != null
                || (exadataService != null && exadataService.CurrentSnapshot != null);
        }

        private void UpdateAnalysisLogButtonState()
        {
            UpdateAnalysisLogButtonState(true);
        }

        private void UpdateAnalysisLogButtonState(bool toolbarEnabled)
        {
            if (analysisLogButton == null)
            {
                return;
            }

            if (!showAnalysisLogButton)
            {
                analysisLogButton.Enabled = false;
                return;
            }

            string path = string.IsNullOrWhiteSpace(lastFeatureAuditLogPath)
                ? GetFeatureSelectionAuditLogPath()
                : lastFeatureAuditLogPath;
            analysisLogButton.Enabled = toolbarEnabled && File.Exists(path);
        }

        private void OpenLatestAnalysisLog()
        {
            string path = string.IsNullOrWhiteSpace(lastFeatureAuditLogPath)
                ? GetFeatureSelectionAuditLogPath()
                : lastFeatureAuditLogPath;
            if (!File.Exists(path))
            {
                MessageBox.Show(
                    this,
                    "Operation message.",
                    "Operation message.",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, "Operation message.");
            }
        }

        private void InitializeBusyOverlay()
        {
            // Busy overlays are intentionally not used in the t-SNE-only UI.
        }

        private void ShowBusyOverlay(string message)
        {
            // Intentionally disabled.
        }

        private void HideBusyOverlay()
        {
            // Intentionally disabled.
        }

        private void UpdateBusyMessage(string message)
        {
            // Intentionally disabled.
        }

        private void BusyOverlayPanel_Resize(object sender, EventArgs e)
        {
            // Intentionally disabled.
        }

        private void UpdateBusyOverlayLayout()
        {
            // Intentionally disabled.
        }

        private TSNEScatterOptions CreateChartOptions()
        {
            TSNEScatterOptions options = TSNEScatterOptions.CreateDefault600x400();
            options.Analysis.ProjectionMethod = projectionMethod;
            options.Analysis.TSNEPerplexity = TSNEPerplexity;
            options.Analysis.TSNEIterations = TSNEIterations;
            options.Analysis.TSNELearningRate = TSNELearningRate;
            options.Analysis.TSNERandomSeed = TSNERandomSeed;
            options.Analysis.MinimumNumericFeatureCoverageRatio =
                ConvertCoveragePercentToRatio(MinimumNumericCoveragePercent);
            options.Series.PassColor = projectionMethod == DimensionalityReductionMethod.TSNE
                ? Color.FromArgb(30, 64, 175)
                : Color.Red;
            options.Series.ReviewColor = projectionMethod == DimensionalityReductionMethod.TSNE
                ? Color.FromArgb(217, 119, 6)
                : Color.Green;
            options.Series.UsePaletteColors = true;
            options.Series.ColorTransparencyPercent = 20f;
            options.Series.ColorAlpha = TSNEScatterSeriesOptions.ResolveAlphaFromTransparencyPercent(options.Series.ColorTransparencyPercent, options.Series.ColorAlpha);
            options.Series.ApplyBorderTransparency = false;
            options.Series.BorderTransparencyPercent = 0f;
            options.Series.NaSeriesName = string.Empty;
            options.Series.NaSeriesColor = Color.Empty;
            options.Series.SeriesOrder = new[] { "Pass", "Review", "FAIL" };
            options.Series.PastelPalette = TSNEScatterSeriesOptions.CreateCompanySeriesPalette();
            options.Series.BorderPalette = TSNEScatterSeriesOptions.CreateCompanySeriesBorderPalette();
            if (projectionMethod == DimensionalityReductionMethod.TSNE)
            {
                options.Series.SeriesColors["Pass"] = Color.FromArgb(30, 64, 175);
                options.Series.SeriesColors["Review"] = Color.FromArgb(217, 119, 6);
                options.Series.SeriesColors["FAIL"] = Color.FromArgb(220, 38, 38);
            }
            options.Series.PointSize = NormalizePointSize(SeriesPointSize, 7f);
            options.Series.HighlightColor = Color.Yellow;
            options.Series.HighlightPointBorderColor = Color.Yellow;
            options.Series.HighlightPointBorderWidth = 1f;
            options.Series.HighlightPointSize = ResolveHighlightedPointSize(options.Series.PointSize, HighlightPointSize);
            options.Series.SelectedPointSize = ResolveSelectedPointSize(options.Series.PointSize, SelectedPointSize);
            options.Series.SelectedPointColor = Color.Yellow;
            options.Series.SelectedPointBorderColor = Color.Red;
            options.Series.SelectedPointBorderWidth = 2.8f;
            options.Display.FontName = "Segoe UI";
            options.Display.ShowTitle = true;
            options.Display.Title = projectionMethod == DimensionalityReductionMethod.TSNE
                ? "t-SNE Distribution Chart"
                : "TSNE Distribution Chart";
            options.Display.TitleColor = Color.Black;
            options.Display.BackgroundColor = Color.White;
            options.Display.GraphBackgroundColor = Color.FromArgb(230, 230, 230);
            options.Display.ThemeMode = LightningScatterThemeMode.DarkGray;
            options.Display.XAxisTitle = projectionMethod == DimensionalityReductionMethod.TSNE ? "t-SNE 1" : string.Empty;
            options.Display.YAxisTitle = projectionMethod == DimensionalityReductionMethod.TSNE ? "t-SNE 2" : string.Empty;
            options.Display.MajorDivCount = 8;
            options.Display.AxisLabelFormat = "0.##";
            options.Display.GridLinesVisible = true;
            options.Display.GridColor = Color.FromArgb(232, 234, 238);
            options.Legend.Position = LightningScatterLegendPosition.BottomCenter;
            options.Legend.OffsetY = 0;
            options.Legend.ShowCheckboxes = false;
            options.Legend.BackgroundColor = Color.Transparent;
            options.Legend.BorderColor = Color.Transparent;
            options.Legend.TransparentBackground = true;
            options.Tooltip.Enabled = true;
            options.Tooltip.HitPixelTolerance = 14;
            options.Tooltip.Format = projectionMethod == DimensionalityReductionMethod.TSNE
                ? "{5}\r\nt-SNE 1:{1:0.###}, t-SNE 2:{2:0.###}"
                : "{5}\r\nX1:{1:0.###}, X2:{2:0.###}";
            options.NoData.Text = GetProjectionDisplayName() + "Operation message.";
            options.NoData.ShowWhenAllValuesZero = false;
            options.Interaction.ZoomEnabled = true;
            options.Interaction.PanEnabled = true;
            options.Interaction.MouseWheelZoomEnabled = true;
            options.Interaction.AllowInternalMouseCursorChange = true;
            options.Interaction.OpenPropertyEditorOnRightClick = true;
            return options;
        }

        private string GetProjectionDisplayName()
        {
            return projectionMethod == DimensionalityReductionMethod.TSNE ? "t-SNE" : "TSNE";
        }

        private void ApplyProjectionUiText()
        {
            if (titleLabel != null)
            {
                titleLabel.Text = GetProjectionDisplayName() + " Scatter";
            }

            Text = projectionMethod == DimensionalityReductionMethod.TSNE
                ? "Manual t-SNE Scatter"
                : "Manual TSNE Scatter";
        }

        private static float NormalizePointSize(float value, float defaultValue)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                return defaultValue;
            }

            return Math.Max(1f, value);
        }

        private static float ResolveSelectedPointSize(float pointSize, float selectedPointSize)
        {
            if (!float.IsNaN(selectedPointSize)
                && !float.IsInfinity(selectedPointSize)
                && selectedPointSize > 0f)
            {
                return Math.Max(1f, selectedPointSize);
            }

            return Math.Max(1f, pointSize * 1.1f);
        }

        private static float ResolveHighlightedPointSize(float pointSize, float highlightedPointSize)
        {
            if (!float.IsNaN(highlightedPointSize)
                && !float.IsInfinity(highlightedPointSize)
                && highlightedPointSize > 0f)
            {
                return Math.Max(1f, highlightedPointSize);
            }

            return Math.Max(1f, pointSize * 1.1f);
        }

        private static double ConvertCoveragePercentToRatio(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0.9d;
            }

            if (value > 1d)
            {
                return Math.Min(100d, Math.Max(0d, value)) / 100d;
            }

            return Math.Min(1d, Math.Max(0d, value));
        }

        private void UpdateSummary(TSNEExadataAnalysisResult result, string sourceDescription)
        {
            if (result == null || result.AnalysisResult == null)
            {
                summaryLabel.Text = "Operation message.";
                return;
            }

            TSNEAnalysisDiagnosticReport diagnostic = result.Diagnostic
                ?? TSNEAnalysisDiagnosticReport.Create(
                    result.AnalysisResult,
                    result.Records == null ? 0 : result.Records.Count,
                    result.MissingExperimentCount);
            summaryLabel.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0} | TYPE={1} SRC={2}",
                diagnostic.CompactText,
                TSNEParameterTypeParser.ToDatabaseValue(result.ParameterType),
                sourceDescription);
        }

        private void BindNearestNeighborTable(DataTable table)
        {
            nearestNeighborGridBinding = true;
            try
            {
                nearestNeighborGrid.DataSource = table;
                nearestNeighborGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 242, 128);
                nearestNeighborGrid.DefaultCellStyle.SelectionForeColor = Color.Black;
                ConfigureNearestNeighborGrid();
                nearestNeighborGrid.ClearSelection();
            }
            finally
            {
                nearestNeighborGridBinding = false;
            }

            UpdateSelectedNeighborHighlight();
        }

        private DataTable CreateNearestNeighborTable(TSNEExperimentRecord target, IEnumerable<KnnNeighbor> neighbors)
        {
            DataTable table = new DataTable();
            table.Columns.Add("DRAFT_NO", typeof(string));
            table.Columns.Add("PARAM_TYP", typeof(string));
            table.Columns.Add("LABEL(Y)", typeof(string));
            table.Columns.Add(projectionMethod == DimensionalityReductionMethod.TSNE ? "t-SNE 1" : "X1", typeof(string));
            table.Columns.Add(projectionMethod == DimensionalityReductionMethod.TSNE ? "t-SNE 2" : "X2", typeof(string));
            table.Columns.Add("Rank", typeof(string));
            table.Columns.Add("Target_Draft", typeof(string));
            table.Columns.Add("Distance", typeof(string));

            if (target == null || neighbors == null)
            {
                return table;
            }

            AddNearestNeighborRow(table, target, 0, target.DraftNo, 0d);

            int displayRank = 1;
            foreach (KnnNeighbor neighbor in ResolveLabeledNearestNeighbors(target, neighbors, GetNearestNeighborGridCount()))
            {
                if (neighbor.SourceIndex < 0 || neighbor.SourceIndex >= currentRecords.Count)
                {
                    continue;
                }

                TSNEExperimentRecord similar = currentRecords[neighbor.SourceIndex];
                AddNearestNeighborRow(table, similar, displayRank, target.DraftNo, neighbor.Distance);
                displayRank++;
            }

            return table;
        }

        private IEnumerable<KnnNeighbor> ResolveLabeledNearestNeighbors(TSNEExperimentRecord target, IEnumerable<KnnNeighbor> neighbors, int desiredCount)
        {
            int safeDesiredCount = Math.Max(1, desiredCount);
            if (target != null && exadataAnalysis != null)
            {
                IList<KnnNeighbor> chartDistanceNeighbors =
                    exadataAnalysis.FindNearestByChartDistance(target.DraftNo, safeDesiredCount, true);
                if (chartDistanceNeighbors.Count > 0)
                {
                    return chartDistanceNeighbors;
                }
            }

            IEnumerable<KnnNeighbor> source = neighbors ?? Enumerable.Empty<KnnNeighbor>();
            List<KnnNeighbor> labeledNeighbors = source
                .Where(IsLabeledNeighbor)
                .Take(safeDesiredCount)
                .ToList();
            if (labeledNeighbors.Count >= safeDesiredCount
                || target == null
                || exadataAnalysis == null
                || exadataAnalysis.AnalysisResult == null
                || currentRecords == null
                || currentRecords.Count == 0)
            {
                return labeledNeighbors;
            }

            int expandedCount = Math.Max(safeDesiredCount, currentRecords.Count - 1);
            return exadataAnalysis.AnalysisResult
                .FindNearest(target.DraftNo, expandedCount)
                .Where(IsLabeledNeighbor)
                .Take(safeDesiredCount)
                .ToList();
        }

        private bool IsLabeledNeighbor(KnnNeighbor neighbor)
        {
            if (neighbor == null
                || neighbor.SourceIndex < 0
                || currentRecords == null
                || neighbor.SourceIndex >= currentRecords.Count)
            {
                return false;
            }

            return HasEngineeringResultLabel(currentRecords[neighbor.SourceIndex]);
        }

        private int GetNearestNeighborGridCount()
        {
            try
            {
                return tsneChart == null || tsneChart.Options == null || tsneChart.Options.Analysis == null
                    ? 3
                    : Math.Max(1, tsneChart.Options.Analysis.NeighborCount);
            }
            catch
            {
                return 3;
            }
        }

        private void AddNearestNeighborRow(DataTable table, TSNEExperimentRecord record, int rank, string targetDraftNo, double distance)
        {
            DataRow row = table.NewRow();
            row["DRAFT_NO"] = record.DraftNo;
            row["PARAM_TYP"] = TSNEParameterTypeParser.ToDatabaseValue(record.ParameterType);
            row["LABEL(Y)"] = FormatGridText(record.LabelY);
            string xColumn = projectionMethod == DimensionalityReductionMethod.TSNE ? "t-SNE 1" : "X1";
            string yColumn = projectionMethod == DimensionalityReductionMethod.TSNE ? "t-SNE 2" : "X2";
            row[xColumn] = FormatGridNumber(record.X1);
            row[yColumn] = FormatGridNumber(record.X2);
            row["Rank"] = rank <= 0 ? "-" : rank.ToString(CultureInfo.InvariantCulture);
            row["Target_Draft"] = targetDraftNo;
            row["Distance"] = rank <= 0 ? "-" : FormatGridNumber(distance);
            table.Rows.Add(row);
        }

        private bool TryQueryDraftFromCurrentAnalysis(
            string draftNo,
            TSNEParameterType parameterType,
            TSNEScatterOptions chartOptions,
            out TSNEExperimentRecord target,
            out IList<KnnNeighbor> neighbors)
        {
            target = null;
            neighbors = new List<KnnNeighbor>();
            if (exadataAnalysis == null
                || exadataAnalysis.AnalysisResult == null
                || exadataAnalysis.ParameterType != parameterType
                || currentRecords == null
                || tsneChart == null)
            {
                return false;
            }

            target = currentRecords.FirstOrDefault(record =>
                record != null
                && string.Equals(record.DraftNo, draftNo, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                return false;
            }

            int neighborCount = chartOptions == null || chartOptions.Analysis == null
                ? 3
                : Math.Max(1, chartOptions.Analysis.NeighborCount);
            neighbors = exadataAnalysis.FindNearestByChartDistance(target.DraftNo, neighborCount, true);
            return true;
        }

        private static string FormatGridText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private static bool HasEngineeringResultLabel(TSNEExperimentRecord record)
        {
            return record != null && !string.IsNullOrWhiteSpace(record.LabelY);
        }

        private static double CalculateStandardizedFeatureDistance(TSNEExperimentRecord left, TSNEExperimentRecord right)
        {
            if (left == null || right == null)
            {
                return 0d;
            }

            double[] leftVector = left.StandardizedVector;
            double[] rightVector = right.StandardizedVector;
            int length = Math.Min(leftVector.Length, rightVector.Length);
            double squaredDistance = 0d;
            for (int index = 0; index < length; index++)
            {
                double difference = leftVector[index] - rightVector[index];
                squaredDistance += difference * difference;
            }

            return Math.Sqrt(squaredDistance);
        }

        private static string FormatGridNumber(double value)
        {
            return value.ToString("0.0000", CultureInfo.InvariantCulture);
        }
    }
}




