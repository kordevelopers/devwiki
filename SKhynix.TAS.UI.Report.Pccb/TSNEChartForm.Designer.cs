namespace SKhynix.TAS.UI.Report.Pccb
{
    partial class TSNEChartForm
    {
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.Panel chartHost;
        private System.Windows.Forms.Label statusLabel;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            chartHost = new System.Windows.Forms.Panel();
            statusLabel = new System.Windows.Forms.Label();
            SuspendLayout();
            chartHost.BackColor = System.Drawing.Color.White;
            chartHost.Dock = System.Windows.Forms.DockStyle.Fill;
            chartHost.Name = "chartHost";
            statusLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            statusLabel.Height = 28;
            statusLabel.Name = "statusLabel";
            statusLabel.Padding = new System.Windows.Forms.Padding(8, 6, 0, 0);
            statusLabel.Text = "샘플 데이터를 분석하는 중...";
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1200, 800);
            Controls.Add(chartHost);
            Controls.Add(statusLabel);
            Name = "TSNEChartForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "t-SNE Scatter";
            ResumeLayout(false);
        }
    }
}
