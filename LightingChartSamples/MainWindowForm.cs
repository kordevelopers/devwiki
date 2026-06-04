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
            AddTabbedLayoutSampleButton();
            AddDynamicDocumentBarChartButton();
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

        private void btnTabbedLayoutSample_Click(object sender, EventArgs e)
        {
            using (var form = new TabbedLayoutSampleForm())
            {
                form.ShowDialog(this);
            }
        }

        private void btnDynamicDocumentBarChart_Click(object sender, EventArgs e)
        {
            using (var form = new DynamicDocumentBarChartForm())
            {
                form.ShowDialog(this);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AddTabbedLayoutSampleButton()
        {
            var button = new Button
            {
                BackColor = Color.FromArgb(60, 132, 150),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(32, 306),
                Name = "btnTabbedLayoutSample",
                Size = new Size(456, 46),
                TabIndex = 5,
                Text = "탭 레이아웃 샘플 열기 (TabbedLayoutSampleForm)",
                UseVisualStyleBackColor = false
            };

            button.Click += btnTabbedLayoutSample_Click;
            pnlContainer.Controls.Add(button);

            btnClose.Location = new Point(392, 365);
            btnClose.TabIndex = 6;
            ClientSize = new Size(520, 414);
        }

        private void AddDynamicDocumentBarChartButton()
        {
            var button = new Button
            {
                BackColor = Color.FromArgb(74, 118, 164),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(32, 364),
                Name = "btnDynamicDocumentBarChart",
                Size = new Size(456, 46),
                TabIndex = 6,
                Text = "동적 문서/바 차트 화면 열기",
                UseVisualStyleBackColor = false
            };

            button.Click += btnDynamicDocumentBarChart_Click;
            pnlContainer.Controls.Add(button);

            btnClose.Location = new Point(392, 423);
            btnClose.TabIndex = 7;
            ClientSize = new Size(520, 472);
        }
    }
}
