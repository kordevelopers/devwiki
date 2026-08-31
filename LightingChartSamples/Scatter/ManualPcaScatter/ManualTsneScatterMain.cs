using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.ManualPcaScatter;

namespace LightingChartSamples.Scatter.ManualPcaScatter
{
    /// <summary>
    /// 기존 수동 PCA WinForms 기능을 유지하면서 2차원 분포만 t-SNE로 표시하는 실행 화면이다.
    /// 외부 차원축소 라이브러리를 사용하지 않는다.
    /// </summary>
    public sealed class ManualTsneScatterMain : ManualPcaScatterMain
    {
        private bool initialSampleLoaded;

        public ManualTsneScatterMain()
            : this(null)
        {
        }

        public ManualTsneScatterMain(IPcaScatterPopupDataProvider popupDataProvider)
            : base(popupDataProvider)
        {
            ProjectionMethod = DimensionalityReductionMethod.Tsne;
            ShowAnalysisSummaryText = true;
            ShowRefreshAllButton = false;
            ShowAnalysisLogButton = false;
            ShowPreferMemoryOption = false;
        }

        public bool AutoLoadSampleData { get; set; } = true;

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!AutoLoadSampleData || initialSampleLoaded)
            {
                return;
            }

            initialSampleLoaded = true;
            try
            {
                DataTable sample = await Task.Run(delegate
                {
                    return new PcaExadataSampleDataFactory(20260831).CreateDefaultDataTable(120);
                });
                await LoadConvExperimentDataTableAsync(sample);
                await DrawChartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "t-SNE 샘플 차트를 표시하지 못했습니다.\r\n" + ex.Message,
                    "t-SNE 차트 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
