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
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.ManualPcaScatter;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.PCAChart.Common;

namespace LightingChartSamples.Scatter.ManualPcaScatter
{
    public partial class ManualPcaScatterMain : Form
    {
        private readonly PcaScatterChart pcaChart;
        private readonly PcaExadataService exadataService;
        private readonly ConvExperimentRepository exadataRepository;
        private readonly IPcaScatterPopupDataProvider popupDataProvider;

        private PcaAnalysisResult analysisResult;
        private PcaExadataAnalysisResult exadataAnalysis;
        private DataTable pendingSourceTable;
        private IList<ScatterSampleData> currentSamples;
        private IList<PcaExperimentRecord> currentRecords;
        private bool parameterChangeEnabled;
        private bool nearestNeighborGridBinding;
        private string lastFeatureAuditLogPath;
        private Panel busyOverlayPanel;
        private Label busyOverlayLabel;
        private ProgressBar busyOverlayProgressBar;
        private Font nearestNeighborGridFont;
        private bool showAnalysisSummaryText;
        private bool showRefreshAllButton;
        private bool showAnalysisLogButton;
        private bool showPreferMemoryOption;
        private bool showFeatureAuditMessageBox;

        public ManualPcaScatterMain()
            : this(null)
        {
        }

        public ManualPcaScatterMain(IPcaScatterPopupDataProvider popupDataProvider)
        {
            InitializeComponent();

            this.popupDataProvider = popupDataProvider;
            MinimumNumericCoveragePercent = 90d;
            SeriesPointSize = 7f;
            HighlightPointSize = 0f;
            SelectedPointSize = 0f;
            currentSamples = new List<ScatterSampleData>();
            currentRecords = new List<PcaExperimentRecord>();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                exadataRepository = null;
                exadataService = null;
                pcaChart = null;
                return;
            }

            exadataRepository = new ConvExperimentRepository(
                PcaScatterExadataOptions.CreateDefault().ToQueryOptions());
            exadataService = new PcaExadataService(exadataRepository);

            ConfigureNearestNeighborGrid();
            BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
            pcaChart = PcaScatterChart.Create(chartHost, CreateChartOptions());
            pcaChart.SampleClicked += PcaChart_SampleClicked;
            pcaChart.AnalysisCompleted += PcaChart_AnalysisCompleted;
            pcaChart.AnalysisFailed += PcaChart_AnalysisFailed;
            pcaChart.Clear();
            InitializeBusyOverlay();
            lastFeatureAuditLogPath = GetFeatureSelectionAuditLogPath();
            summaryLabel.Text = "서비스 DataTable을 전달한 뒤 PARAM_TYP과 DRAFT_NO를 선택해 분석하세요.";
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
        /// PCA 분석 로그 열기 버튼 표시 여부다. 기본값은 숨김이다.
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
        /// 차트 렌더링 후 개발자용 PCA 로그 메시지박스를 표시할지 결정한다. 기본값은 비활성이다.
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
                nearestNeighborGridFont = new Font("맑은 고딕", 10f, FontStyle.Regular);
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
            currentSamples = new List<ScatterSampleData>();
            currentRecords = new List<PcaExperimentRecord>();
            pcaChart.Clear();
            BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
            preferMemoryCheckBox.Checked = true;
            summaryLabel.Text = string.Format(
                CultureInfo.InvariantCulture,
                "DataTable {0:N0}행을 받았습니다. 차트 그리기 버튼을 눌러 PCA 분석을 실행하세요.",
                sourceTable.Rows.Count);
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
            ShowBusyOverlay("DataTable 변환 및 PCA 차트 그리기 중...");
            try
            {
                PcaExadataSnapshot snapshot = await LoadCurrentSourceSnapshotAsync();
                preferMemoryCheckBox.Checked = true;
                UpdateBusyMessage("메모리 데이터 PCA 분석 및 차트 렌더링 중...");
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
                ShowOperationError(ex, "PCA 차트 그리기 실패");
            }
        }

        private async void RefreshAllButton_Click(object sender, EventArgs e)
        {
            await RefreshAllAsync();
        }

        private async void SampleDataButton_Click(object sender, EventArgs e)
        {
            try
            {
                await LoadSampleDataAsync();
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, "샘플 데이터 생성 실패");
            }
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

            PcaExadataSnapshot snapshot = exadataService.CurrentSnapshot;
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
                MessageBox.Show(this, "조회할 DRAFT Number를 입력하세요.", "DRAFT Number 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                draftNoTextBox.Focus();
                return;
            }

            PcaParameterType parameterType = GetSelectedParameterType();
            PcaExadataRefreshMode refreshMode = preferMemoryCheckBox.Checked
                ? PcaExadataRefreshMode.PreferMemorySnapshot
                : PcaExadataRefreshMode.AlwaysReload;
            summaryLabel.Text = string.Format(
                "{0} 전체 데이터에서 DRAFT_NO {1} 조회 중...",
                PcaParameterTypeParser.ToDatabaseValue(parameterType),
                draftNo);

            try
            {
                if (exadataService.CurrentSnapshot == null)
                {
                    MessageBox.Show(
                        this,
                        "먼저 차트 그리기 버튼을 눌러 PCA 분석을 실행하세요.",
                        "PCA 분석 필요",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                if (exadataService.CurrentSnapshot != null)
                {
                    preferMemoryCheckBox.Checked = true;
                    refreshMode = PcaExadataRefreshMode.PreferMemorySnapshot;
                }

                PcaScatterOptions chartOptions = CreateChartOptions();
                PcaExperimentRecord currentTarget;
                IList<KnnNeighbor> currentNeighbors;
                if (TryQueryDraftFromCurrentAnalysis(draftNo, parameterType, chartOptions, out currentTarget, out currentNeighbors))
                {
                    pcaChart.HighlightDraft(currentTarget.DraftNo);
                    BindNearestNeighborTable(CreateNearestNeighborTable(currentTarget, currentNeighbors));
                    UpdateSummary(exadataAnalysis, "현재 차트");
                    return;
                }

                PcaDraftQueryResult result = await exadataService.QueryDraftAsync(
                    draftNo,
                    parameterType,
                    refreshMode,
                    chartOptions.Analysis);
                chartOptions.Series.HighlightDraftNo = result.Target.DraftNo;
                ApplyAnalysis(result.Analysis, chartOptions);
                BindNearestNeighborTable(
                    CreateNearestNeighborTable(result.Target, result.Neighbors));
                UpdateSummary(result.Analysis, result.UsedMemorySnapshot
                    ? "메모리 스냅샷"
                    : "서비스 DataTable");
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, "DRAFT_NO 조회 실패");
            }
        }

        private async Task<PcaExadataSnapshot> LoadPopupDatabaseSnapshotAsync()
        {
            if (popupDataProvider == null)
            {
                throw new InvalidOperationException("PCA popup data provider is not configured.");
            }

            summaryLabel.Text = popupDataProvider.SourceDescription + " 데이터 조회 중...";
            UpdateBusyMessage(summaryLabel.Text);
            DataTable sourceTable = await popupDataProvider.LoadAllAsync();
            UpdateBusyMessage("조회 데이터 변환 및 메모리 적재 중...");
            exadataRepository.SetSourceTable(sourceTable);
            PcaExadataSnapshot snapshot = await exadataService.LoadFromDataTableAsync(sourceTable);
            preferMemoryCheckBox.Checked = true;
            return snapshot;
        }

        private async Task<PcaExadataSnapshot> LoadCurrentSourceSnapshotAsync()
        {
            if (pendingSourceTable != null)
            {
                UpdateBusyMessage("전달받은 DataTable을 PCA 메모리 스냅샷으로 변환 중...");
                exadataRepository.SetSourceTable(pendingSourceTable);
                PcaExadataSnapshot snapshot = await exadataService.LoadFromDataTableAsync(pendingSourceTable);
                preferMemoryCheckBox.Checked = true;
                return snapshot;
            }

            return await LoadPopupDatabaseSnapshotAsync();
        }

        private async Task LoadSampleDataAsync()
        {
            SetToolbarEnabled(false);
            ShowBusyOverlay("샘플 DataTable 생성 중...");
            try
            {
                await Task.Delay(50);
                DataTable sampleTable = await Task.Run(delegate
                {
                    return new PcaExadataSampleDataFactory(20260629).CreateDefaultDataTable();
                });

                await LoadConvExperimentDataTableAsync(sampleTable);
                responseRadioButton.Checked = true;
                draftNoTextBox.Text = "SAMPLE-R-001";
                preferMemoryCheckBox.Checked = true;
                await DrawLoadedDataAsync();
            }
            finally
            {
                SetToolbarEnabled(true);
            }
        }

        private async Task RefreshAllAsync()
        {
            SetToolbarEnabled(false);
            ShowBusyOverlay("전체 데이터 조회 및 PCA 분석 중...");
            PcaParameterType parameterType = GetSelectedParameterType();
            summaryLabel.Text = "서비스 DataTable 전체 데이터 PCA 분석 중...";

            try
            {
                PcaExadataSnapshot snapshot = await LoadCurrentSourceSnapshotAsync();
                PcaScatterOptions chartOptions = CreateChartOptions();
                PcaExadataAnalysisResult result = await Task.Run(delegate
                {
                    return exadataService.AnalyzeSnapshot(
                        snapshot,
                        parameterType,
                        chartOptions.Analysis);
                });
                ApplyAnalysis(result, chartOptions);
                BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
                UpdateSummary(result, string.Format("서비스 DataTable ({0:N0}행)", result.Snapshot.Rows.Count));
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, "서비스 DataTable 분석 실패");
            }
            finally
            {
                SetToolbarEnabled(true);
            }
        }

        private async Task AnalyzeCurrentSnapshotAsync(PcaExadataSnapshot snapshot)
        {
            await AnalyzeCurrentSnapshotAsync(snapshot, true);
        }

        private async Task AnalyzeCurrentSnapshotAsync(PcaExadataSnapshot snapshot, bool manageToolbar)
        {
            if (manageToolbar)
            {
                SetToolbarEnabled(false);
                ShowBusyOverlay("메모리 데이터 필터링 및 PCA 분석 중...");
            }

            PcaParameterType parameterType = GetSelectedParameterType();
            summaryLabel.Text = PcaParameterTypeParser.ToDatabaseValue(parameterType)
                + " 메모리 데이터 PCA 분석 중...";
            try
            {
                PcaScatterOptions chartOptions = CreateChartOptions();
                PcaExadataAnalysisResult result = await Task.Run(delegate
                {
                    return exadataService.AnalyzeSnapshot(snapshot, parameterType, chartOptions.Analysis);
                });
                ApplyAnalysis(result, chartOptions);
                BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
                UpdateSummary(result, "메모리 스냅샷");
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, "PARAM_TYP 분석 실패");
            }
            finally
            {
                if (manageToolbar)
                {
                    SetToolbarEnabled(true);
                }
            }
        }

        private void ApplyAnalysis(PcaExadataAnalysisResult result, PcaScatterOptions chartOptions)
        {
            if (result == null || result.AnalysisResult == null)
            {
                throw new ArgumentNullException("result");
            }

            exadataAnalysis = result;
            currentRecords = result.Records.ToList();
            pcaChart.Bind(result.AnalysisResult, chartOptions);
            // 차트를 새로 그린 직후 같은 로그 파일을 덮어써서, 화면과 로그가 항상 같은 분석 결과를 가리키게 한다.
            WriteFeatureSelectionAuditLog(result, chartOptions);
        }

        private void WriteFeatureSelectionAuditLog(PcaExadataAnalysisResult result, PcaScatterOptions chartOptions)
        {
            if (result == null || result.FeatureSelectionReport == null)
            {
                return;
            }

            string detailedLog = BuildFeatureSelectionAuditText(result, chartOptions, true);
            lastFeatureAuditLogPath = SaveFeatureSelectionAuditLog(result, detailedLog);
            UpdateAnalysisLogButtonState();
            Debug.WriteLine("PCA Feature Audit Log: " + lastFeatureAuditLogPath);

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
                    "PCA Feature Audit (Developer)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
#endif
        }

        private string BuildFeatureSelectionAuditText(PcaExadataAnalysisResult result, PcaScatterOptions chartOptions, bool includeFullDetails)
        {
            PcaFeatureSelectionReport report = result.FeatureSelectionReport;
            DataTable survivingPopulation = result.CreateSurvivingPopulationDataTable();
            var builder = new StringBuilder();
            builder.AppendLine("PCA Feature Selection Audit");
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, "CreatedAt: {0:yyyy-MM-dd HH:mm:ss.fff}", DateTime.Now));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, "ParameterType: {0}", PcaParameterTypeParser.ToDatabaseValue(result.ParameterType)));
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
                AppendPcaProcessingOverview(builder, result, chartOptions);
                AppendPreprocessingAndScalingExplanation(builder, result, chartOptions, true);
                AppendPcaProjectionExplanation(builder, result, chartOptions, true);
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

        private static string SaveFeatureSelectionAuditLog(PcaExadataAnalysisResult result, string logText)
        {
            try
            {
                string root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SKhynix",
                    "TAS",
                    "PcaScatter",
                    "AnalysisLogs");
                Directory.CreateDirectory(root);
                string path = Path.Combine(root, "manual_pca_latest_analysis.log");
                File.WriteAllText(path, logText ?? string.Empty, Encoding.UTF8);
                return path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PCA Feature Audit log save failed: " + ex.Message);
                return string.Empty;
            }
        }

        private static string GetFeatureSelectionAuditLogPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SKhynix",
                "TAS",
                "PcaScatter",
                "AnalysisLogs",
                "manual_pca_latest_analysis.log");
        }

        private void AppendPcaProcessingOverview(StringBuilder builder, PcaExadataAnalysisResult result, PcaScatterOptions chartOptions)
        {
            builder.AppendLine("PCA processing overview:");
            builder.AppendLine("- 이 로그 파일은 수동 PCA Scatter가 차트를 그릴 때마다 같은 경로에 덮어쓰는 최신 분석 로그입니다.");
            builder.AppendLine("- 입력 데이터는 DataTable의 DRAFT_NO, PARAM_TYP, CONV_EXPER_CTN, AI_RSLT_VAL, ENGR_RSLT_VAL 컬럼입니다.");
            builder.AppendLine("- CONV_EXPER_CTN JSON 배열에서 실험 객체를 꺼내고, Dict/List 구조를 펼쳐 feature 후보를 만듭니다.");
            builder.AppendLine("- PUB_NO, _VERSION_NM, Draft_NO, AI_RSLT_Val 같은 식별/라벨 값은 PCA 수치 feature에서 제외합니다.");
            builder.AppendLine("- 남은 값 중 숫자로 안전하게 변환되는 값만 후보가 되고, 누락/문자열/상수 feature는 규칙에 따라 제외됩니다.");
            builder.AppendLine("- 살아남은 feature가 입력 수치행렬이 됩니다. 행은 Draft, 열은 feature입니다.");
            builder.AppendLine("- 입력 수치행렬은 StandardScaler로 평균 0, 표준편차 1이 되도록 정규화됩니다.");
            builder.AppendLine("- PCA는 정규화된 행렬의 공분산에서 PC1/PC2 방향을 찾고, 각 Draft를 그 방향으로 투영해 X1/X2를 만듭니다.");
            builder.AppendLine("- KNN Distance는 화면의 2D 좌표가 아니라 정규화된 전체 feature 벡터 기준의 유클리드 거리입니다.");
            builder.AppendLine();
        }

        private PcaExperimentRecord ResolveAuditTargetRecord(PcaExadataAnalysisResult result, PcaScatterOptions chartOptions)
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
                PcaExperimentRecord matched = result.Records.FirstOrDefault(record =>
                    record != null && string.Equals(record.DraftNo, draftNo, StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                {
                    return matched;
                }
            }

            return result.Records.FirstOrDefault();
        }

        private static bool TryGetOriginalValue(PcaExperimentRecord record, string featureName, out double value)
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

        private void AppendPreprocessingAndScalingExplanation(StringBuilder builder, PcaExadataAnalysisResult result, PcaScatterOptions chartOptions, bool includeFullDetails)
        {
            PcaAnalysisResult analysis = result == null ? null : result.AnalysisResult;
            PcaScatterAnalysisOptions analysisOptions = chartOptions == null ? null : chartOptions.Analysis;
            double coverageRatio = analysisOptions == null ? 1d : analysisOptions.MinimumNumericFeatureCoverageRatio;
            bool meanImputationEnabled = analysisOptions != null && analysisOptions.MeanImputationEnabled;

            builder.AppendLine("Preprocessing and normalization explanation:");
            builder.AppendLine("- JSON 파싱: CONV_EXPER_CTN 문자열을 JSON 배열/객체로 읽고, 내부 객체의 값을 key-value 형태로 펼칩니다.");
            builder.AppendLine("- 수치형 필터링: double로 변환할 수 있고 NaN/Infinity가 아닌 값만 수치 feature 후보가 됩니다.");
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "- 포함 기준: 전체 분석 row 중 숫자로 읽힌 비율이 {0:P1} 이상이면 feature로 포함할 수 있습니다.", coverageRatio));
            builder.AppendLine(meanImputationEnabled
                ? "- 평균 보정: 일부 row에서 값이 빠진 feature는 포함 기준을 통과한 경우 해당 feature 평균값으로 채웁니다."
                : "- 평균 보정 없음: 모든 row에 숫자가 있는 feature만 포함합니다.");
            builder.AppendLine("- 저분산 제거: 모든 row에서 거의 같은 값인 feature는 X1/X2와 Distance에 의미가 작아서 제외합니다.");
            builder.AppendLine("- 정규화 공식: 각 원래 값에서 그 feature의 전체 평균을 빼고, 그 feature의 표준편차로 나눕니다.");
            builder.AppendLine("- 말로 풀면: 정규화값 = (현재 Draft의 원래 값 - 전체 Draft 평균) / 전체 Draft 표준편차 입니다.");
            builder.AppendLine("- PCA 입력 수치행렬은 이 정규화값 행렬입니다. KNN Distance도 같은 정규화값 행렬을 사용합니다.");

            PcaExperimentRecord target = ResolveAuditTargetRecord(result, chartOptions);
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

        private void AppendPcaProjectionExplanation(StringBuilder builder, PcaExadataAnalysisResult result, PcaScatterOptions chartOptions, bool includeFullDetails)
        {
            PcaAnalysisResult analysis = result == null ? null : result.AnalysisResult;
            if (analysis == null || analysis.PcaModel == null || analysis.FeatureNames == null || analysis.PcaModel.Components == null)
            {
                return;
            }

            PcaExperimentRecord target = ResolveAuditTargetRecord(result, chartOptions);
            int componentCount = Math.Min(2, analysis.PcaModel.Components.Length);
            int featureCount = analysis.FeatureNames.Length;
            int maxRows = includeFullDetails ? 30 : 8;

            builder.AppendLine("PCA X1/X2 projection explanation:");
            builder.AppendLine("- X1과 X2는 특정 feature 하나를 고른 값이 아닙니다.");
            builder.AppendLine("- PC1/PC2는 전체 feature가 함께 움직이는 방향입니다. 각 feature마다 PCA 가중치가 있습니다.");
            builder.AppendLine("- X1은 각 feature의 정규화값에 PC1 가중치를 곱한 뒤 모두 더한 값입니다.");
            builder.AppendLine("- X2는 각 feature의 정규화값에 PC2 가중치를 곱한 뒤 모두 더한 값입니다.");
            if (target != null)
            {
                builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "- Target Draft_NO={0}, X1={1}, X2={2}",
                    target.DraftNo,
                    FormatNumber(target.X1),
                    FormatNumber(target.X2)));
            }

            for (int componentIndex = 0; componentIndex < componentCount; componentIndex++)
            {
                double explained = analysis.PcaModel.ExplainedVarianceRatios != null && analysis.PcaModel.ExplainedVarianceRatios.Length > componentIndex
                    ? analysis.PcaModel.ExplainedVarianceRatios[componentIndex] * 100d
                    : 0d;
                builder.AppendLine();
                builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "PC{0} top contributing features by absolute PCA weight. Explained variance={1:0.###}%",
                    componentIndex + 1,
                    explained));
                builder.AppendLine("Feature\tWeight\tTargetStandardized\tContributionToAxis\tTargetOriginal\tMean\tStdDev");

                double[] component = analysis.PcaModel.Components[componentIndex];
                int[] indexes = Enumerable.Range(0, Math.Min(featureCount, component.Length))
                    .OrderByDescending(index => Math.Abs(component[index]))
                    .Take(maxRows)
                    .ToArray();
                foreach (int featureIndex in indexes)
                {
                    string featureName = analysis.FeatureNames[featureIndex];
                    double targetStandardized = target != null && featureIndex < target.StandardizedVector.Length
                        ? target.StandardizedVector[featureIndex]
                        : 0d;
                    double contribution = targetStandardized * component[featureIndex];
                    double originalValue = 0d;
                    bool hasOriginalValue = target != null && TryGetOriginalValue(target, featureName, out originalValue);
                    builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                        featureName,
                        FormatNumber(component[featureIndex]),
                        target == null ? "(no target)" : FormatNumber(targetStandardized),
                        target == null ? "(no target)" : FormatNumber(contribution),
                        hasOriginalValue ? FormatNumber(originalValue) : "(mean imputed/no target)",
                        analysis.Scaler == null ? string.Empty : FormatNumber(analysis.Scaler.Means[featureIndex]),
                        analysis.Scaler == null ? string.Empty : FormatNumber(analysis.Scaler.StandardDeviations[featureIndex])));
                }
            }

            if (target != null && analysis.PcaModel.Components.Length >= 2)
            {
                double recalculatedX1 = Dot(target.StandardizedVector, analysis.PcaModel.Components[0]);
                double recalculatedX2 = Dot(target.StandardizedVector, analysis.PcaModel.Components[1]);
                builder.AppendLine();
                builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "Target axis check: X1 stored={0}, recalculated={1}; X2 stored={2}, recalculated={3}",
                    FormatNumber(target.X1),
                    FormatNumber(recalculatedX1),
                    FormatNumber(target.X2),
                    FormatNumber(recalculatedX2)));
            }

            builder.AppendLine();
        }

        private void AppendAxisRangeExplanation(StringBuilder builder, PcaExadataAnalysisResult result, PcaScatterOptions chartOptions)
        {
            PcaAnalysisResult analysis = result == null ? null : result.AnalysisResult;
            if (analysis == null || analysis.ScatterData == null || analysis.ScatterData.Count == 0)
            {
                return;
            }

            PcaScatterDisplayOptions display = chartOptions == null || chartOptions.Display == null
                ? new PcaScatterDisplayOptions()
                : chartOptions.Display;
            AxisRangeAudit xRange = CalculateAxisRangeAudit(analysis.ScatterData, true, display);
            AxisRangeAudit yRange = CalculateAxisRangeAudit(analysis.ScatterData, false, display);

            builder.AppendLine("Chart axis range explanation:");
            builder.AppendLine("- X축 범위는 모든 Draft의 X1 좌표 최소/최대값으로 계산합니다.");
            builder.AppendLine("- Y축 범위는 모든 Draft의 X2 좌표 최소/최대값으로 계산합니다.");
            builder.AppendLine("- IncludeZeroInAxisRange가 켜져 있으면 0도 범위에 포함한 뒤 padding을 더합니다.");
            builder.AppendLine("- 따라서 축 끝값은 특정 한두 Draft의 X1/X2 극단값과 padding 옵션에 의해 결정됩니다.");
            builder.AppendLine("Axis\tRawMin\tRawMinDraft\tRawMax\tRawMaxDraft\tRangeMinAfterZero\tRangeMaxAfterZero\tPadding\tFinalMin\tFinalMax");
            AppendAxisRangeRow(builder, "X1", xRange);
            AppendAxisRangeRow(builder, "X2", yRange);
            builder.AppendLine();
        }

        private static AxisRangeAudit CalculateAxisRangeAudit(IList<ScatterSampleData> samples, bool useX, PcaScatterDisplayOptions display)
        {
            List<ScatterSampleData> cleanSamples = (samples ?? new List<ScatterSampleData>())
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

            ScatterSampleData minSample = cleanSamples.OrderBy(sample => useX ? sample.X1 : sample.X2).First();
            ScatterSampleData maxSample = cleanSamples.OrderByDescending(sample => useX ? sample.X1 : sample.X2).First();
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

        private void AppendDistanceExplanation(StringBuilder builder, PcaExadataAnalysisResult result, PcaScatterOptions chartOptions, bool includeFullDetails)
        {
            PcaAnalysisResult analysis = result == null ? null : result.AnalysisResult;
            PcaExperimentRecord target = ResolveAuditTargetRecord(result, chartOptions);
            if (analysis == null || target == null || result == null || result.Records == null || result.Records.Count == 0)
            {
                return;
            }

            int featureCount = analysis.FeatureNames == null ? 0 : analysis.FeatureNames.Length;
            int neighborCount = chartOptions == null || chartOptions.Analysis == null
                ? 3
                : Math.Max(1, chartOptions.Analysis.NeighborCount);
            IList<KnnNeighbor> neighbors = analysis.FindNearest(target.DraftNo, neighborCount);

            builder.AppendLine("KNN distance explanation:");
            builder.AppendLine("- 그리드 Distance는 화면의 X1/X2 거리만으로 계산하지 않습니다.");
            builder.AppendLine("- Distance는 정규화된 전체 feature 공간에서 계산한 Euclidean distance입니다.");
            builder.AppendLine("- 각 feature마다 대상 Draft의 정규화값과 비교 Draft의 정규화값 차이를 구합니다.");
            builder.AppendLine("- 그 차이를 제곱하고 모든 feature의 제곱값을 더한 뒤, 마지막에 제곱근을 씌운 값입니다.");
            builder.AppendLine("- feature가 많으면 제곱합이 누적되므로 Distance가 30 이상처럼 커질 수 있습니다.");
            builder.AppendLine("- 다른 시스템의 작은 값은 원본값 거리, X1/X2 좌표 거리, feature 수로 나눈 거리 등 정의가 다를 수 있습니다.");
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "- 비교용 RMS distance = distance / sqrt(featureCount)도 함께 표시합니다. FeatureCount={0:N0}",
                featureCount));
            builder.AppendLine();
            builder.AppendLine("Rank\tSimilarDraft\tDistance\tDistanceSquared\tRmsPerFeature\tPca2DChartDistance");

            foreach (KnnNeighbor neighbor in neighbors)
            {
                if (neighbor.SourceIndex < 0 || neighbor.SourceIndex >= result.Records.Count)
                {
                    continue;
                }

                PcaExperimentRecord similar = result.Records[neighbor.SourceIndex];
                double rms = featureCount <= 0 ? 0d : neighbor.Distance / Math.Sqrt(featureCount);
                double pca2dDistance = Math.Sqrt(Math.Pow(target.X1 - similar.X1, 2d) + Math.Pow(target.X2 - similar.X2, 2d));
                builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                    neighbor.Rank,
                    similar.DraftNo,
                    FormatNumber(neighbor.Distance),
                    FormatNumber(neighbor.Distance * neighbor.Distance),
                    FormatNumber(rms),
                    FormatNumber(pca2dDistance)));
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

        private static void AppendDistanceContributionTable(StringBuilder builder, PcaAnalysisResult analysis, PcaExperimentRecord target, PcaExperimentRecord similar, int maxRows)
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
                double rawMin, double rawMax, ScatterSampleData minSample,
                double rangeMinAfterZero, double rangeMaxAfterZero,
                double padding, double finalMin, double finalMax, ScatterSampleData maxSample)
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
            public ScatterSampleData MinSample { get; private set; }
            public double RangeMinAfterZero { get; private set; }
            public double RangeMaxAfterZero { get; private set; }
            public double Padding { get; private set; }
            public double FinalMin { get; private set; }
            public double FinalMax { get; private set; }
            public ScatterSampleData MaxSample { get; private set; }
        }

        private static void AppendExcludedReasonSummary(StringBuilder builder, PcaFeatureSelectionReport report)
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
                foreach (IGrouping<PcaFeatureSelectionReason, PcaFeatureSelectionDetail> group in groups)
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

        private static void AppendExcludedFeatureSamples(StringBuilder builder, PcaFeatureSelectionReport report, int maxCount)
        {
            int safeMaxCount = maxCount <= 0 ? int.MaxValue : maxCount;
            builder.AppendLine(safeMaxCount == int.MaxValue
                ? "Excluded feature samples (all):"
                : string.Format(CultureInfo.InvariantCulture, "Excluded feature samples (max {0}):", safeMaxCount));
            PcaFeatureSelectionDetail[] details = report.Details
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

            foreach (PcaFeatureSelectionDetail detail in details)
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

        private static void AppendFeatureDetailTable(StringBuilder builder, string title, IEnumerable<PcaFeatureSelectionDetail> details)
        {
            builder.AppendLine();
            builder.AppendLine(title + ":");
            builder.AppendLine("FeatureName\tIncluded\tReason\tPresent\tNumeric\tMissing\tNonNumeric\tMean\tStdDev\tVariance\tMin\tMax\tSampleDraftNo");
            foreach (PcaFeatureSelectionDetail detail in (details ?? Enumerable.Empty<PcaFeatureSelectionDetail>())
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
            pcaChart.HighlightDraft(target.DraftNo);
            BindNearestNeighborTable(CreateNearestNeighborTable(target, e.Neighbors));
        }

        private void PcaChart_AnalysisCompleted(object sender, PcaScatterAnalysisCompletedEventArgs e)
        {
            analysisResult = e.AnalysisResult;
            currentSamples = analysisResult == null || analysisResult.ScatterData == null
                ? new List<ScatterSampleData>()
                : analysisResult.ScatterData.ToList();
        }

        private void PcaChart_AnalysisFailed(object sender, PcaScatterAnalysisFailedEventArgs e)
        {
            summaryLabel.Text = e.Exception == null
                ? "분석 실패"
                : "분석 실패: " + e.Exception.Message;
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
                exception is KeyNotFoundException || exception is PcaExperimentDataMissingException
                    ? MessageBoxIcon.Warning
                    : MessageBoxIcon.Error);
        }

        private PcaParameterType GetSelectedParameterType()
        {
            return defectRadioButton.Checked
                ? PcaParameterType.Defect
                : PcaParameterType.Response;
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
            sampleDataButton.Enabled = enabled;
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
                    ? "처리 중입니다. 잠시만 기다려 주세요."
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
                    "아직 생성된 PCA 분석 로그가 없습니다. 조회 또는 전체 새로고침 후 다시 확인하세요.",
                    "PCA 분석 로그",
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
                ShowOperationError(ex, "PCA 분석 로그 열기 실패");
            }
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
                Font = new Font(Font.FontFamily, 10f, FontStyle.Regular)
            };

            busyOverlayProgressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 32
            };

            busyOverlayPanel.Controls.Add(busyOverlayLabel);
            busyOverlayPanel.Controls.Add(busyOverlayProgressBar);
            busyOverlayPanel.Resize += BusyOverlayPanel_Resize;
            chartHost.Controls.Add(busyOverlayPanel);
            busyOverlayPanel.BringToFront();
            UpdateBusyOverlayLayout();
        }

        private void ShowBusyOverlay(string message)
        {
            if (busyOverlayPanel == null)
            {
                return;
            }

            busyOverlayLabel.Text = string.IsNullOrWhiteSpace(message)
                ? "처리 중입니다. 잠시만 기다려 주세요."
                : message.Trim();
            busyOverlayProgressBar.MarqueeAnimationSpeed = 32;
            busyOverlayPanel.Visible = true;
            busyOverlayPanel.Enabled = true;
            busyOverlayPanel.BringToFront();
            UpdateBusyOverlayLayout();
            Application.DoEvents();
        }

        private void HideBusyOverlay()
        {
            if (busyOverlayPanel == null)
            {
                return;
            }

            busyOverlayProgressBar.MarqueeAnimationSpeed = 0;
            busyOverlayPanel.Visible = false;
        }

        private void UpdateBusyMessage(string message)
        {
            if (busyOverlayPanel == null || !busyOverlayPanel.Visible)
            {
                return;
            }

            busyOverlayLabel.Text = string.IsNullOrWhiteSpace(message)
                ? "처리 중입니다. 잠시만 기다려 주세요."
                : message.Trim();
            Application.DoEvents();
        }

        private void BusyOverlayPanel_Resize(object sender, EventArgs e)
        {
            UpdateBusyOverlayLayout();
        }

        private void UpdateBusyOverlayLayout()
        {
            if (busyOverlayPanel == null
                || busyOverlayLabel == null
                || busyOverlayProgressBar == null)
            {
                return;
            }

            int contentWidth = Math.Min(420, Math.Max(260, busyOverlayPanel.ClientSize.Width - 80));
            int progressWidth = Math.Min(360, contentWidth);
            int centerX = busyOverlayPanel.ClientSize.Width / 2;
            int centerY = busyOverlayPanel.ClientSize.Height / 2;

            busyOverlayLabel.SetBounds(
                Math.Max(8, centerX - (contentWidth / 2)),
                Math.Max(8, centerY - 34),
                contentWidth,
                24);
            busyOverlayProgressBar.SetBounds(
                Math.Max(8, centerX - (progressWidth / 2)),
                busyOverlayLabel.Bottom + 10,
                progressWidth,
                18);
        }

        private PcaScatterOptions CreateChartOptions()
        {
            PcaScatterOptions options = PcaScatterOptions.CreateDefault600x400();
            options.Analysis.MinimumNumericFeatureCoverageRatio =
                ConvertCoveragePercentToRatio(MinimumNumericCoveragePercent);
            options.Series.PassColor = Color.Red;
            options.Series.ReviewColor = Color.Green;
            options.Series.UsePaletteColors = true;
            options.Series.ColorTransparencyPercent = 20f;
            options.Series.ColorAlpha = PcaScatterSeriesOptions.ResolveAlphaFromTransparencyPercent(options.Series.ColorTransparencyPercent, options.Series.ColorAlpha);
            options.Series.ApplyBorderTransparency = false;
            options.Series.BorderTransparencyPercent = 0f;
            options.Series.NaSeriesName = string.Empty;
            options.Series.NaSeriesColor = Color.Empty;
            options.Series.SeriesOrder = new[] { "Pass", "Review", "FAIL" };
            options.Series.PastelPalette = PcaScatterSeriesOptions.CreateCompanySeriesPalette();
            options.Series.BorderPalette = PcaScatterSeriesOptions.CreateCompanySeriesBorderPalette();
            options.Series.PointSize = NormalizePointSize(SeriesPointSize, 7f);
            options.Series.HighlightColor = Color.Yellow;
            options.Series.HighlightPointBorderColor = Color.Yellow;
            options.Series.HighlightPointBorderWidth = 1f;
            options.Series.HighlightPointSize = ResolveHighlightedPointSize(options.Series.PointSize, HighlightPointSize);
            options.Series.SelectedPointSize = ResolveSelectedPointSize(options.Series.PointSize, SelectedPointSize);
            options.Series.SelectedPointColor = Color.Yellow;
            options.Series.SelectedPointBorderColor = Color.Red;
            options.Series.SelectedPointBorderWidth = 2.8f;
            options.Display.FontName = "맑은 고딕";
            options.Display.ShowTitle = true;
            options.Display.Title = "Distribution Chart";
            options.Display.TitleColor = Color.Black;
            options.Display.BackgroundColor = Color.White;
            options.Display.GraphBackgroundColor = Color.FromArgb(230, 230, 230);
            options.Display.ThemeMode = LightningScatterThemeMode.DarkGray;
            options.Display.XAxisTitle = string.Empty;
            options.Display.YAxisTitle = string.Empty;
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
            options.Tooltip.Format = "{5}\r\nX1:{1:0.###}, X2:{2:0.###}";
            options.NoData.Text = "PCA Scatter 데이터가 없습니다.";
            options.NoData.ShowWhenAllValuesZero = false;
            options.Interaction.ZoomEnabled = true;
            options.Interaction.PanEnabled = true;
            options.Interaction.MouseWheelZoomEnabled = true;
            options.Interaction.AllowInternalMouseCursorChange = true;
            options.Interaction.OpenPropertyEditorOnRightClick = true;
            return options;
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

        private void UpdateSummary(PcaExadataAnalysisResult result, string sourceDescription)
        {
            if (result == null || result.AnalysisResult == null)
            {
                summaryLabel.Text = "분석 결과 없음";
                return;
            }

            PcaAnalysisDiagnosticReport diagnostic = result.Diagnostic
                ?? PcaAnalysisDiagnosticReport.Create(
                    result.AnalysisResult,
                    result.Records == null ? 0 : result.Records.Count,
                    result.MissingExperimentCount);
            summaryLabel.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0} | TYPE={1} SRC={2}",
                diagnostic.CompactText,
                PcaParameterTypeParser.ToDatabaseValue(result.ParameterType),
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

        private DataTable CreateNearestNeighborTable(PcaExperimentRecord target, IEnumerable<KnnNeighbor> neighbors)
        {
            DataTable table = new DataTable();
            table.Columns.Add("DRAFT_NO", typeof(string));
            table.Columns.Add("PARAM_TYP", typeof(string));
            table.Columns.Add("LABEL(Y)", typeof(string));
            table.Columns.Add("X1", typeof(string));
            table.Columns.Add("X2", typeof(string));
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

                PcaExperimentRecord similar = currentRecords[neighbor.SourceIndex];
                AddNearestNeighborRow(table, similar, displayRank, target.DraftNo, neighbor.Distance);
                displayRank++;
            }

            return table;
        }

        private IEnumerable<KnnNeighbor> ResolveLabeledNearestNeighbors(PcaExperimentRecord target, IEnumerable<KnnNeighbor> neighbors, int desiredCount)
        {
            int safeDesiredCount = Math.Max(1, desiredCount);
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
                return pcaChart == null || pcaChart.Options == null || pcaChart.Options.Analysis == null
                    ? 3
                    : Math.Max(1, pcaChart.Options.Analysis.NeighborCount);
            }
            catch
            {
                return 3;
            }
        }

        private static void AddNearestNeighborRow(DataTable table, PcaExperimentRecord record, int rank, string targetDraftNo, double distance)
        {
            DataRow row = table.NewRow();
            row["DRAFT_NO"] = record.DraftNo;
            row["PARAM_TYP"] = PcaParameterTypeParser.ToDatabaseValue(record.ParameterType);
            row["LABEL(Y)"] = FormatGridText(record.LabelY);
            row["X1"] = FormatGridNumber(record.X1);
            row["X2"] = FormatGridNumber(record.X2);
            row["Rank"] = rank <= 0 ? "-" : rank.ToString(CultureInfo.InvariantCulture);
            row["Target_Draft"] = targetDraftNo;
            row["Distance"] = rank <= 0 ? "-" : FormatGridNumber(distance);
            table.Rows.Add(row);
        }

        private bool TryQueryDraftFromCurrentAnalysis(
            string draftNo,
            PcaParameterType parameterType,
            PcaScatterOptions chartOptions,
            out PcaExperimentRecord target,
            out IList<KnnNeighbor> neighbors)
        {
            target = null;
            neighbors = new List<KnnNeighbor>();
            if (exadataAnalysis == null
                || exadataAnalysis.AnalysisResult == null
                || exadataAnalysis.ParameterType != parameterType
                || currentRecords == null
                || pcaChart == null)
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
            neighbors = pcaChart.FindNearest(target.DraftNo, neighborCount);
            return true;
        }

        private static string FormatGridText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private static bool HasEngineeringResultLabel(PcaExperimentRecord record)
        {
            return record != null && !string.IsNullOrWhiteSpace(record.LabelY);
        }

        private static string FormatGridNumber(double value)
        {
            return value.ToString("0.0000", CultureInfo.InvariantCulture);
        }
    }
}
