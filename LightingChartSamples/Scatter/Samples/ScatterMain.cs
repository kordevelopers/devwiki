using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LightingChartSamples.Scatter
{
    public partial class ScatterMain : Form
    {
        private readonly PcaScatterChart pcaChart;
        private readonly PcaExadataService exadataService;
        private readonly ConvExperimentRepository exadataRepository;

        private PcaAnalysisResult analysisResult;
        private PcaExadataAnalysisResult exadataAnalysis;
        private IList<ScatterSampleData> currentSamples;
        private IList<PcaExperimentRecord> currentRecords;
        private bool parameterChangeEnabled;

        public ScatterMain()
        {
            InitializeComponent();

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

            exadataRepository.SetSourceTable(sourceTable);
            PcaExadataSnapshot snapshot = await exadataService.LoadAllAsync();
            preferMemoryCheckBox.Checked = true;
            await AnalyzeCurrentSnapshotAsync(snapshot);
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
            PcaParameterType parameterType = GetSelectedParameterType();
            PcaExadataRefreshMode refreshMode = preferMemoryCheckBox.Checked
                ? PcaExadataRefreshMode.PreferMemorySnapshot
                : PcaExadataRefreshMode.AlwaysReload;
            summaryLabel.Text = string.Format(
                "{0} 전체 데이터에서 DRAFT_NO {1} 조회 및 PCA 분석 중...",
                PcaParameterTypeParser.ToDatabaseValue(parameterType),
                draftNo);

            try
            {
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

        private async Task RefreshAllAsync()
        {
            SetToolbarEnabled(false);
            PcaParameterType parameterType = GetSelectedParameterType();
            summaryLabel.Text = "서비스 DataTable 전체 데이터 PCA 분석 중...";

            try
            {
                PcaScatterOptions chartOptions = CreateChartOptions();
                PcaExadataAnalysisResult result =
                    await exadataService.RefreshAndAnalyzeAsync(
                        parameterType,
                        chartOptions.Analysis);
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

            PcaAnalysisResult current = result.AnalysisResult;
            double pc1Ratio = current.PcaModel != null
                && current.PcaModel.ExplainedVarianceRatios.Length > 0
                    ? current.PcaModel.ExplainedVarianceRatios[0] * 100d
                    : 0d;
            double pc2Ratio = current.PcaModel != null
                && current.PcaModel.ExplainedVarianceRatios.Length > 1
                    ? current.PcaModel.ExplainedVarianceRatios[1] * 100d
                    : 0d;
            summaryLabel.Text = string.Format(
                "{0} | {1} {2:N0}건 | Feature {3:N0} | 누락 {4:N0} | PCA {5:0.0}% + {6:0.0}%",
                sourceDescription,
                PcaParameterTypeParser.ToDatabaseValue(result.ParameterType),
                result.Records.Count,
                current.FeatureNames == null ? 0 : current.FeatureNames.Length,
                result.MissingExperimentCount,
                pc1Ratio,
                pc2Ratio);
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
            table.Columns.Add("Similar_Draft", typeof(string));
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
                row["DRAFT_NO"] = target.DraftNo;
                row["PARAM_TYP"] = PcaParameterTypeParser.ToDatabaseValue(similar.ParameterType);
                row["LABEL(Y)"] = similar.LabelY;
                row["X1"] = similar.X1;
                row["X2"] = similar.X2;
                row["Rank"] = neighbor.Rank;
                row["Similar_Draft"] = similar.DraftNo;
                row["Distance"] = neighbor.Distance;
                table.Rows.Add(row);
            }

            return table;
        }
    }
}
