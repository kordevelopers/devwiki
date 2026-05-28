using System;
using System.Drawing;
using System.Windows.Forms;

namespace LightingChartSamples
{
    public partial class MainWindowForm : Form
    {
        public MainWindowForm()
        {
            InitializeComponent();
        }

        private void btnTextDocument_Click(object sender, EventArgs e)
        {
            using (var form = new TextDocument())
            {
                form.ShowDialog(this);
            }
        }

        private void btnLightningBar_Click(object sender, EventArgs e)
        {
            using (var form = new LightningBarSample())
            {
                form.ShowDialog(this);
            }
        }

        private void btnLightningRadar_Click(object sender, EventArgs e)
        {
            using (var form = new LightningRadarSample())
            {
                form.ShowDialog(this);
            }
        }

        private void btnRadarChartSample_Click(object sender, EventArgs e)
        {
            using (var form = new Form1())
            {
                form.ShowDialog(this);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
