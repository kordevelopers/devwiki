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
        private IList<ScatterSampleData> currentSamples;
        private IList<PcaExperimentRecord> currentRecords;
        private bool parameterChangeEnabled;
        private bool nearestNeighborGridBinding;
        private string lastFeatureAuditLogPath;
        private Panel busyOverlayPanel;
        private Label busyOverlayLabel;
        private ProgressBar busyOverlayProgressBar;
        private Font nearestNeighborGridFont;

        public ManualPcaScatterMain()
            : this(new PcaScatterVirtualDatabaseDataProvider())
        {
        }

        public ManualPcaScatterMain(IPcaScatterPopupDataProvider popupDataProvider)
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

            ConfigureNearestNeighborGrid();
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
            Debug.WriteLine("PCA Feature Audit Log: " + lastFeatureAuditLogPath);

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

            AppendPreprocessingAndScalingExplanation(builder, result, chartOptions, includeFullDetails);
            AppendPcaProjectionExplanation(builder, result, chartOptions, includeFullDetails);
            AppendDistanceExplanation(builder, result, chartOptions, includeFullDetails);
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
            PcaFeatureSelectionReport report = result == null ? null : result.FeatureSelectionReport;
            PcaScatterAnalysisOptions analysisOptions = chartOptions == null ? null : chartOptions.Analysis;
            double coverageRatio = analysisOptions == null ? 1d : analysisOptions.MinimumNumericFeatureCoverageRatio;
            bool meanImputationEnabled = analysisOptions != null && analysisOptions.MeanImputationEnabled;

            builder.AppendLine("Preprocessing and normalization explanation:");
            builder.AppendLine("- JSON 내부 값을 펼친 뒤 PUB_NO, _VERSION_NM, Draft_NO, AI_RSLT_Val 같은 식별/라벨 컬럼은 PCA feature에서 제외합니다.");
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "- 수치형 feature 선정 기준: 전체 분석 row 중 숫자로 읽힌 비율이 {0:P1} 이상이면 포함합니다.", coverageRatio));
            builder.AppendLine(meanImputationEnabled
                ? "- 숫자가 일부 row에서 빠진 feature는 포함 기준을 통과한 경우, 해당 feature의 평균값으로 빠진 값을 채웁니다."
                : "- 평균 보정이 꺼져 있으므로 모든 row에 숫자가 있는 feature만 포함합니다.");
            builder.AppendLine("- 분산이 거의 0인 feature는 모든 row에서 값이 거의 같아서 거리/PCA에 의미가 작으므로 제외합니다.");
            builder.AppendLine("- 정규화는 feature마다 전체 모집단의 평균과 표준편차를 먼저 계산합니다.");
            builder.AppendLine("- 각 row의 원래 값에서 그 feature의 평균을 뺀 뒤, 그 feature의 표준편차로 나눈 값이 표준화값입니다.");
            builder.AppendLine("- 말로 풀면: 표준화값 = (현재 row의 원래 값 - 전체 row 평균) / 전체 row 표준편차 입니다.");
            builder.AppendLine("- PCA와 KNN distance는 원본값이 아니라 이 표준화값을 기준으로 계산됩니다.");

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
            builder.AppendLine("- X1과 X2는 특정 feature 하나가 선택된 값이 아닙니다.");
            builder.AppendLine("- 각 축은 살아남은 모든 수치 feature의 표준화값에 PCA 가중치(loadings)를 곱해서 모두 더한 값입니다.");
            builder.AppendLine("- 즉 X1은 PC1 가중합이고, X2는 PC2 가중합입니다.");
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
            builder.AppendLine("- 현재 distance는 표준화된 전체 feature 공간에서 계산한 Euclidean distance입니다.");
            builder.AppendLine("- 각 feature마다 대상 Draft의 표준화값과 비교 Draft의 표준화값 차이를 구합니다.");
            builder.AppendLine("- 그 차이를 제곱하고, 모든 feature의 제곱값을 더한 뒤, 마지막에 제곱근을 씌운 값이 distance입니다.");
            builder.AppendLine("- 말로 풀면: distance = 모든 수치 feature의 표준화 차이를 누적한 전체 거리입니다.");
            builder.AppendLine("- feature가 80개처럼 많으면 제곱합이 누적되므로 값이 32처럼 커질 수 있습니다.");
            builder.AppendLine("- 다른 시스템의 0.0079 같은 값은 원본값 거리, 2D PCA 좌표 거리, feature 수로 나눈 거리, min-max 정규화 거리 등 다른 정의일 가능성이 큽니다.");
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
                double targetOriginal = 0d;
                double similarOriginal = 0d;
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
