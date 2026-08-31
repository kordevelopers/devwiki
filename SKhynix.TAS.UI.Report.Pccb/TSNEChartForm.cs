using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart;

namespace SKhynix.TAS.UI.Report.Pccb
{
    public sealed partial class TSNEChartForm : Form
    {
        private TSNEChart chart;

        public TSNEChartForm()
        {
            InitializeComponent();
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
                statusLabel.Text = "Accord.NET t-SNE analysis complete (120 samples)";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "t-SNE error: " + ex.Message;
                MessageBox.Show(this, ex.ToString(), "t-SNE chart error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && chart != null) chart.Dispose();
            base.Dispose(disposing);
        }
    }
}

