using System;
using System.Windows.Forms;

namespace SKhynix.TAS.UI.Report.Pccb
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var form = new TSNEChartForm
            {
                ShowVirtualDataButton = true,
                ShowAnalysisSummaryText = true,
                ShowAnalysisLogButton = true,
                ShowRefreshAllButton = true
            };
            form.Shown += async delegate
            {
                try
                {
                    await form.LoadVirtualDataAsync();
                    await form.DrawChartAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(form, ex.Message, "t-SNE test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            Application.Run(form);
        }
    }
}



