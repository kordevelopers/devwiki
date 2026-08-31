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
            Application.Run(new TSNEChartForm());
        }
    }
}
