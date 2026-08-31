using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart;

namespace SKhynix.TAS.UI.Report.Pccb
{
    public sealed class TSNEChartForm : Form
    {
        private readonly Panel chartHost;
        private readonly Label statusLabel;
        private TSNEChart chart;

        public TSNEChartForm()
        {
            Text = "t-SNE Scatter";
            Width = 1200;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            chartHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            statusLabel = new Label { Dock = DockStyle.Bottom, Height = 28, Text = "샘플 데이터를 분석하는 중...", Padding = new Padding(8, 6, 0, 0) };
            Controls.Add(chartHost);
            Controls.Add(statusLabel);
            Shown += TSNEChartForm_Shown;
        }

        private async void TSNEChartForm_Shown(object sender, EventArgs e)
        {
            try
            {
                chart = TSNEChart.Create(chartHost);
                DataTable table = await Task.Run(delegate
                {
                    return new PcaExadataSampleDataFactory(20260831).CreateDefaultDataTable(120);
                });
                chart.BindFromExadata(table);
                statusLabel.Text = "Accord.NET t-SNE 분석 완료 (120 samples)";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "t-SNE 오류: " + ex.Message;
                MessageBox.Show(this, ex.ToString(), "t-SNE 차트 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && chart != null) chart.Dispose();
            base.Dispose(disposing);
        }
    }
}
