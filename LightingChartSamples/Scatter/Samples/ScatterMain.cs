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
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.PcaScatter;

namespace LightingChartSamples.Scatter
{
    public partial class ScatterMain : Form
    {
        private readonly PcaScatterChart pcaChart;
        private readonly PcaExadataService exadataService;
        private readonly ConvExperimentRepository exadataRepository;
        private readonly IPcaScatterPopupDataProvider popupDataProvider;

        private PcaAnalysisResult analysisResult;
        private PcaExadataAnalysisResult exadataAnalysis;
        private IList<ScatterSampleData> currentSamples;
        private IList<PcaExperimentRecord> currentRecords;
        private bool parameterChangeEnabled;
        private bool nearestNeighborGridBinding;
        private string lastFeatureAuditLogPath;
        private Panel busyOverlayPanel;
        private Label busyOverlayLabel;
        private ProgressBar busyOverlayProgressBar;

        public ScatterMain()
            : this(new PcaScatterVirtualDatabaseDataProvider())
        {
        }

        public ScatterMain(IPcaScatterPopupDataProvider popupDataProvider)
        {
            InitializeComponent();

            this.popupDataProvider = popupDataProvider ?? new PcaScatterVirtualDatabaseDataProvider();
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

            BindNearestNeighborTable(CreateNearestNeighborTable(null, null));
            pcaChart = PcaScatterChart.Create(chartHost, CreateChartOptions());
            pcaChart.SampleClicked += PcaChart_SampleClicked;
            pcaChart.AnalysisCompleted += PcaChart_AnalysisCompleted;
            pcaChart.AnalysisFailed += PcaChart_AnalysisFailed;
            pcaChart.Clear();
            InitializeBusyOverlay();
            summaryLabel.Text = "서비스 DataTable을 전달한 뒤 PARAM_TYP과 DRAFT_NO를 선택해 분석하세요.";
            parameterChangeEnabled = true;
            SetToolbarEnabled(true);
        }

        public async Task LoadConvExperimentDataTableAsync(DataTable sourceTable)
        {
            if (sourceTable == null)
            {
                throw new ArgumentNullException("sourceTable");
            }

            SetToolbarEnabled(false);
            ShowBusyOverlay("전달받은 DataTable 메모리 적재 및 PCA 분석 중...");
            try
            {
                exadataRepository.SetSourceTable(sourceTable);
                PcaExadataSnapshot snapshot = await exadataService.LoadAllAsync();
                preferMemoryCheckBox.Checked = true;
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

        private async void RefreshAllButton_Click(object sender, EventArgs e)
        {
            await RefreshAllAsync();
        }

        private async void SampleDataButton_Click(object sender, EventArgs e)
        {
            await LoadSampleDataAsync();
        }

        private async void AccordPcaButton_Click(object sender, EventArgs e)
        {
            await RunAccordPcaAsync();
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
                MessageBox.Show(
                    this,
                    "조회할 DRAFT_NO를 입력하세요.",
                    "DRAFT_NO 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                draftNoTextBox.Focus();
                return;
            }

            SetToolbarEnabled(false);
            ShowBusyOverlay("Draft 조회 및 PCA 분석 중...");
            PcaParameterType parameterType = GetSelectedParameterType();
            PcaExadataRefreshMode refreshMode = preferMemoryCheckBox.Checked
                ? PcaExadataRefreshMode.PreferMemorySnapshot
                : PcaExadataRefreshMode.AlwaysReload;
            summaryLabel.Text = string.Format(
                "{0} 전체 데이터에서 DRAFT_NO {1} 조회 및 PCA 분석 중...",
                PcaParameterTypeParser.ToDatabaseValue(parameterType),
                draftNo);
            UpdateBusyMessage(summaryLabel.Text);

            try
            {
                if (exadataService.CurrentSnapshot == null)
                {
                    await LoadPopupDatabaseSnapshotAsync();
                }

                if (exadataService.CurrentSnapshot != null)
                {
                    preferMemoryCheckBox.Checked = true;
                    refreshMode = PcaExadataRefreshMode.PreferMemorySnapshot;
                }

                PcaScatterOptions chartOptions = CreateChartOptions();
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
            finally
            {
                SetToolbarEnabled(true);
            }
        }

        private async Task LoadSampleDataAsync()
        {
            SetToolbarEnabled(false);
            ShowBusyOverlay("가상 데이터 생성 및 PCA 분석 중...");
            summaryLabel.Text = "가상 CONV_EXPER_CTN 데이터 생성 및 PCA 분석 중...";

            try
            {
                PcaExadataSnapshot snapshot = new PcaExadataSampleDataFactory(20260626)
                    .CreateDefaultSnapshot();
                exadataService.SetSnapshot(snapshot);
                preferMemoryCheckBox.Checked = true;

                PcaParameterType parameterType = GetSelectedParameterType();
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

                PcaExperimentRecord firstRecord = result.Records.FirstOrDefault();
                draftNoTextBox.Text = firstRecord == null ? string.Empty : firstRecord.DraftNo;
                UpdateSummary(result, "가상 데이터");
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, "가상 데이터 생성 실패");
            }
            finally
            {
                SetToolbarEnabled(true);
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

        private async Task RefreshAllAsync()
        {
            SetToolbarEnabled(false);
            ShowBusyOverlay("전체 데이터 조회 및 PCA 분석 중...");
            PcaParameterType parameterType = GetSelectedParameterType();
            summaryLabel.Text = "서비스 DataTable 전체 데이터 PCA 분석 중...";

            try
            {
                PcaExadataSnapshot snapshot = await LoadPopupDatabaseSnapshotAsync();
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
                UpdateSummary(
                    result,
                    string.Format(
                        "서비스 DataTable ({0:N0}행)",
                        result.Snapshot.Rows.Count));
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

        private async Task AnalyzeCurrentSnapshotAsync(
            PcaExadataSnapshot snapshot,
            bool manageToolbar)
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
                    return exadataService.AnalyzeSnapshot(
                        snapshot,
                        parameterType,
                        chartOptions.Analysis);
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

        private async Task RunAccordPcaAsync()
        {
            PcaExadataSnapshot snapshot = exadataService.CurrentSnapshot;
            if (snapshot == null)
            {
                MessageBox.Show(
                    this,
                    "먼저 서비스 DataTable을 전달하거나 가상 데이터를 생성하세요.",
                    "Accord PCA",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SetToolbarEnabled(false);
            ShowBusyOverlay("Accord.NET PCA 분석 중...");
            PcaParameterType parameterType = GetSelectedParameterType();
            summaryLabel.Text = PcaParameterTypeParser.ToDatabaseValue(parameterType)
                + " Accord.NET PCA 분석 중...";

            try
            {
                PcaScatterOptions chartOptions = CreateChartOptions();
                UpdateBusyMessage("Accord.NET PCA 분석 중...");
                var analyzer = new AccordPcaScatterAnalyzer();
                PcaExadataAnalysisResult result = await Task.Run(delegate
                {
                    return analyzer.AnalyzeSnapshot(
                        snapshot,
                        parameterType,
                        chartOptions.Analysis);
                });

                PcaExperimentRecord target = null;
                IList<KnnNeighbor> neighbors = null;
                string draftNo = (draftNoTextBox.Text ?? string.Empty).Trim();
                if (draftNo.Length > 0)
                {
                    target = result.Records.FirstOrDefault(record =>
                        string.Equals(record.DraftNo, draftNo, StringComparison.OrdinalIgnoreCase));
                    if (target != null)
                    {
                        chartOptions.Series.HighlightDraftNo = target.DraftNo;
                        neighbors = result.AnalysisResult.FindNearest(
                            target.DraftNo,
                            Math.Max(1, chartOptions.Analysis.NeighborCount));
                    }
                }

                ApplyAnalysis(result, chartOptions);
                BindNearestNeighborTable(CreateNearestNeighborTable(target, neighbors));
                UpdateSummary(result, "Accord.NET PCA");
            }
            catch (Exception ex)
            {
                ShowOperationError(ex, "Accord.NET PCA 분석 실패");
            }
            finally
            {
                SetToolbarEnabled(true);
            }
        }

        private void ApplyAnalysis(
            PcaExadataAnalysisResult result,
            PcaScatterOptions chartOptions)
        {
            if (result == null || result.AnalysisResult == null)
            {
                throw new ArgumentNullException("result");
            }

            exadataAnalysis = result;
            currentRecords = result.Records.ToList();
            pcaChart.Bind(result.AnalysisResult, chartOptions);
            WriteFeatureSelectionAuditLog(result, chartOptions);
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
            Debug.WriteLine("PCA Feature Audit Log: " + lastFeatureAuditLogPath);

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
                "PCA Feature Audit (Developer)",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
#endif
        }

        private string BuildFeatureSelectionAuditText(
            PcaExadataAnalysisResult result,
            PcaScatterOptions chartOptions,
            bool includeFullDetails)
        {
            PcaFeatureSelectionReport report = result.FeatureSelectionReport;
            DataTable survivingPopulation = result.CreateSurvivingPopulationDataTable();
            var builder = new StringBuilder();
            builder.AppendLine("PCA Feature Selection Audit");
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "CreatedAt: {0:yyyy-MM-dd HH:mm:ss.fff}",
                DateTime.Now));
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

            builder.AppendLine(report.ToSummaryText());
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "Numeric coverage threshold: {0:P1}",
                (chartOptions == null || chartOptions.Analysis == null)
                    ? 1d
                    : chartOptions.Analysis.MinimumNumericCoverageRatio));
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

            AppendExcludedReasonSummary(builder, report);
            AppendFeatureNameSummary(
                builder,
                includeFullDetails ? "Included features" : "Included features",
                report.IncludedFeatureNames,
                includeFullDetails ? int.MaxValue : 20);
            AppendExcludedFeatureSamples(
                builder,
                report,
                includeFullDetails ? int.MaxValue : 15);

            if (includeFullDetails)
            {
                AppendFeatureDetailTable(
                    builder,
                    "Included feature details",
                    report.Details.Where(detail => detail.Included));
                AppendFeatureDetailTable(
                    builder,
                    "Excluded feature details",
                    report.Details.Where(detail => !detail.Included));
            }

            return builder.ToString();
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
                    "PcaScatter",
                    "AnalysisLogs",
                    DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
                Directory.CreateDirectory(root);
                string parameterType = result == null
                    ? "UNKNOWN"
                    : PcaParameterTypeParser.ToDatabaseValue(result.ParameterType);
                string fileName = string.Format(
                    CultureInfo.InvariantCulture,
                    "pca_feature_audit_{0}_{1:yyyyMMdd_HHmmss_fff}_{2}.log",
                    SanitizeFileName(parameterType),
                    DateTime.Now,
                    Guid.NewGuid().ToString("N").Substring(0, 8));
                string path = Path.Combine(root, fileName);
                File.WriteAllText(path, logText ?? string.Empty, Encoding.UTF8);
                return path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PCA Feature Audit log save failed: " + ex.Message);
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

        private static void AppendFeatureNameSummary(
            StringBuilder builder,
            string title,
            IEnumerable<string> featureNames,
            int maxCount)
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

        private static void AppendExcludedFeatureSamples(
            StringBuilder builder,
            PcaFeatureSelectionReport report,
            int maxCount)
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

        private static void AppendFeatureDetailTable(
            StringBuilder builder,
            string title,
            IEnumerable<PcaFeatureSelectionDetail> details)
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
            refreshAllButton.Enabled = enabled;
            sampleDataButton.Enabled = enabled;
            accordPcaButton.Enabled = enabled;
            preferMemoryCheckBox.Enabled = enabled;
            nearestNeighborGrid.Enabled = enabled;
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
            options.Tooltip.Format = "{5}\r\nX1:{1:0.###}, X2:{2:0.###}";
            options.NoData.Text = "PCA Scatter 데이터가 없습니다.";
            options.NoData.ShowWhenAllValuesZero = false;
            return options;
        }

        private void UpdateSummary(
            PcaExadataAnalysisResult result,
            string sourceDescription)
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

                nearestNeighborGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 242, 128);
                nearestNeighborGrid.DefaultCellStyle.SelectionForeColor = Color.Black;
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

            foreach (KnnNeighbor neighbor in neighbors)
            {
                if (neighbor.SourceIndex < 0 || neighbor.SourceIndex >= currentRecords.Count)
                {
                    continue;
                }

                PcaExperimentRecord similar = currentRecords[neighbor.SourceIndex];
                DataRow row = table.NewRow();
                row["DRAFT_NO"] = similar.DraftNo;
                row["PARAM_TYP"] = PcaParameterTypeParser.ToDatabaseValue(similar.ParameterType);
                row["LABEL(Y)"] = similar.LabelY;
                row["X1"] = similar.X1;
                row["X2"] = similar.X2;
                row["Rank"] = neighbor.Rank;
                row["Target_Draft"] = target.DraftNo;
                row["Distance"] = neighbor.Distance;
                table.Rows.Add(row);
            }

            return table;
        }
    }
}
