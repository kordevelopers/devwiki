namespace LightingChartSamples.Scatter
{
    partial class ScatterMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (pcaChart != null)
                {
                    pcaChart.Dispose();
                }

                if (components != null)
                {
                    components.Dispose();
                }

                if (nearestNeighborGridFont != null)
                {
                    nearestNeighborGridFont.Dispose();
                    nearestNeighborGridFont = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.toolbarLayout = new System.Windows.Forms.TableLayoutPanel();
            this.titleLabel = new System.Windows.Forms.Label();
            this.summaryLabel = new System.Windows.Forms.Label();
            this.commandPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.parameterTypeLabel = new System.Windows.Forms.Label();
            this.responseRadioButton = new System.Windows.Forms.RadioButton();
            this.defectRadioButton = new System.Windows.Forms.RadioButton();
            this.draftNoLabel = new System.Windows.Forms.Label();
            this.draftNoTextBox = new System.Windows.Forms.TextBox();
            this.searchButton = new System.Windows.Forms.Button();
            this.refreshAllButton = new System.Windows.Forms.Button();
            this.sampleDataButton = new System.Windows.Forms.Button();
            this.accordPcaButton = new System.Windows.Forms.Button();
            this.preferMemoryCheckBox = new System.Windows.Forms.CheckBox();
            this.chartHost = new System.Windows.Forms.Panel();
            this.nearestNeighborGrid = new System.Windows.Forms.DataGridView();
            this.rootLayout.SuspendLayout();
            this.toolbarLayout.SuspendLayout();
            this.commandPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nearestNeighborGrid)).BeginInit();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.toolbarLayout, 0, 0);
            this.rootLayout.Controls.Add(this.chartHost, 0, 1);
            this.rootLayout.Controls.Add(this.nearestNeighborGrid, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 88F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 172F));
            this.rootLayout.Size = new System.Drawing.Size(1180, 820);
            this.rootLayout.TabIndex = 0;
            //
            // toolbarLayout
            //
            this.toolbarLayout.BackColor = System.Drawing.Color.White;
            this.toolbarLayout.ColumnCount = 2;
            this.toolbarLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.toolbarLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.toolbarLayout.Controls.Add(this.titleLabel, 0, 0);
            this.toolbarLayout.Controls.Add(this.summaryLabel, 1, 0);
            this.toolbarLayout.Controls.Add(this.commandPanel, 0, 1);
            this.toolbarLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolbarLayout.Location = new System.Drawing.Point(0, 0);
            this.toolbarLayout.Margin = new System.Windows.Forms.Padding(0);
            this.toolbarLayout.Name = "toolbarLayout";
            this.toolbarLayout.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.toolbarLayout.RowCount = 2;
            this.toolbarLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.toolbarLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.toolbarLayout.Size = new System.Drawing.Size(1180, 88);
            this.toolbarLayout.TabIndex = 0;
            this.toolbarLayout.SetColumnSpan(this.commandPanel, 2);
            //
            // titleLabel
            //
            this.titleLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.titleLabel.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.titleLabel.Location = new System.Drawing.Point(12, 8);
            this.titleLabel.Margin = new System.Windows.Forms.Padding(0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(120, 30);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "PCA Scatter";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // summaryLabel
            //
            this.summaryLabel.AutoEllipsis = true;
            this.summaryLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.summaryLabel.ForeColor = System.Drawing.Color.FromArgb(95, 95, 95);
            this.summaryLabel.Location = new System.Drawing.Point(132, 8);
            this.summaryLabel.Margin = new System.Windows.Forms.Padding(0);
            this.summaryLabel.Name = "summaryLabel";
            this.summaryLabel.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.summaryLabel.Size = new System.Drawing.Size(1036, 30);
            this.summaryLabel.TabIndex = 1;
            this.summaryLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // commandPanel
            //
            this.commandPanel.Controls.Add(this.parameterTypeLabel);
            this.commandPanel.Controls.Add(this.responseRadioButton);
            this.commandPanel.Controls.Add(this.defectRadioButton);
            this.commandPanel.Controls.Add(this.draftNoLabel);
            this.commandPanel.Controls.Add(this.draftNoTextBox);
            this.commandPanel.Controls.Add(this.searchButton);
            this.commandPanel.Controls.Add(this.refreshAllButton);
            this.commandPanel.Controls.Add(this.sampleDataButton);
            this.commandPanel.Controls.Add(this.accordPcaButton);
            this.commandPanel.Controls.Add(this.preferMemoryCheckBox);
            this.commandPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.commandPanel.Location = new System.Drawing.Point(12, 38);
            this.commandPanel.Margin = new System.Windows.Forms.Padding(0);
            this.commandPanel.Name = "commandPanel";
            this.commandPanel.Size = new System.Drawing.Size(1156, 34);
            this.commandPanel.TabIndex = 2;
            this.commandPanel.WrapContents = false;
            //
            // parameterTypeLabel
            //
            this.parameterTypeLabel.AutoSize = true;
            this.parameterTypeLabel.Location = new System.Drawing.Point(0, 7);
            this.parameterTypeLabel.Margin = new System.Windows.Forms.Padding(0, 7, 6, 0);
            this.parameterTypeLabel.Name = "parameterTypeLabel";
            this.parameterTypeLabel.Size = new System.Drawing.Size(74, 15);
            this.parameterTypeLabel.TabIndex = 0;
            this.parameterTypeLabel.Text = "PARAM_TYP";
            //
            // responseRadioButton
            //
            this.responseRadioButton.AutoSize = true;
            this.responseRadioButton.Checked = true;
            this.responseRadioButton.Location = new System.Drawing.Point(80, 6);
            this.responseRadioButton.Margin = new System.Windows.Forms.Padding(0, 6, 10, 0);
            this.responseRadioButton.Name = "responseRadioButton";
            this.responseRadioButton.Size = new System.Drawing.Size(83, 19);
            this.responseRadioButton.TabIndex = 1;
            this.responseRadioButton.TabStop = true;
            this.responseRadioButton.Text = "RESPONSE";
            this.responseRadioButton.UseVisualStyleBackColor = true;
            this.responseRadioButton.CheckedChanged += new System.EventHandler(this.ParameterType_CheckedChanged);
            //
            // defectRadioButton
            //
            this.defectRadioButton.AutoSize = true;
            this.defectRadioButton.Location = new System.Drawing.Point(173, 6);
            this.defectRadioButton.Margin = new System.Windows.Forms.Padding(0, 6, 10, 0);
            this.defectRadioButton.Name = "defectRadioButton";
            this.defectRadioButton.Size = new System.Drawing.Size(67, 19);
            this.defectRadioButton.TabIndex = 2;
            this.defectRadioButton.Text = "DEFECT";
            this.defectRadioButton.UseVisualStyleBackColor = true;
            this.defectRadioButton.CheckedChanged += new System.EventHandler(this.ParameterType_CheckedChanged);
            //
            // draftNoLabel
            //
            this.draftNoLabel.AutoSize = true;
            this.draftNoLabel.Location = new System.Drawing.Point(250, 7);
            this.draftNoLabel.Margin = new System.Windows.Forms.Padding(0, 7, 6, 0);
            this.draftNoLabel.Name = "draftNoLabel";
            this.draftNoLabel.Size = new System.Drawing.Size(64, 15);
            this.draftNoLabel.TabIndex = 3;
            this.draftNoLabel.Text = "DRAFT_NO";
            //
            // draftNoTextBox
            //
            this.draftNoTextBox.Location = new System.Drawing.Point(324, 3);
            this.draftNoTextBox.Margin = new System.Windows.Forms.Padding(4, 3, 8, 3);
            this.draftNoTextBox.Name = "draftNoTextBox";
            this.draftNoTextBox.Size = new System.Drawing.Size(160, 23);
            this.draftNoTextBox.TabIndex = 4;
            this.draftNoTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DraftNoTextBox_KeyDown);
            //
            // searchButton
            //
            this.searchButton.Location = new System.Drawing.Point(492, 1);
            this.searchButton.Margin = new System.Windows.Forms.Padding(0, 1, 4, 1);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new System.Drawing.Size(92, 28);
            this.searchButton.TabIndex = 5;
            this.searchButton.Text = "Draft 조회";
            this.searchButton.UseVisualStyleBackColor = true;
            this.searchButton.Click += new System.EventHandler(this.SearchButton_Click);
            //
            // refreshAllButton
            //
            this.refreshAllButton.Location = new System.Drawing.Point(588, 1);
            this.refreshAllButton.Margin = new System.Windows.Forms.Padding(0, 1, 8, 1);
            this.refreshAllButton.Name = "refreshAllButton";
            this.refreshAllButton.Size = new System.Drawing.Size(120, 28);
            this.refreshAllButton.TabIndex = 6;
            this.refreshAllButton.Text = "전체 새로고침";
            this.refreshAllButton.UseVisualStyleBackColor = true;
            this.refreshAllButton.Click += new System.EventHandler(this.RefreshAllButton_Click);
            //
            // sampleDataButton
            //
            this.sampleDataButton.Location = new System.Drawing.Point(716, 1);
            this.sampleDataButton.Margin = new System.Windows.Forms.Padding(0, 1, 8, 1);
            this.sampleDataButton.Name = "sampleDataButton";
            this.sampleDataButton.Size = new System.Drawing.Size(104, 28);
            this.sampleDataButton.TabIndex = 7;
            this.sampleDataButton.Text = "가상 데이터";
            this.sampleDataButton.UseVisualStyleBackColor = true;
            this.sampleDataButton.Click += new System.EventHandler(this.SampleDataButton_Click);
            //
            // accordPcaButton
            //
            this.accordPcaButton.Location = new System.Drawing.Point(828, 1);
            this.accordPcaButton.Margin = new System.Windows.Forms.Padding(0, 1, 8, 1);
            this.accordPcaButton.Name = "accordPcaButton";
            this.accordPcaButton.Size = new System.Drawing.Size(104, 28);
            this.accordPcaButton.TabIndex = 8;
            this.accordPcaButton.Text = "Accord PCA";
            this.accordPcaButton.UseVisualStyleBackColor = true;
            this.accordPcaButton.Click += new System.EventHandler(this.AccordPcaButton_Click);
            //
            // preferMemoryCheckBox
            //
            this.preferMemoryCheckBox.AutoSize = true;
            this.preferMemoryCheckBox.Location = new System.Drawing.Point(944, 6);
            this.preferMemoryCheckBox.Margin = new System.Windows.Forms.Padding(4, 6, 0, 0);
            this.preferMemoryCheckBox.Name = "preferMemoryCheckBox";
            this.preferMemoryCheckBox.Size = new System.Drawing.Size(129, 19);
            this.preferMemoryCheckBox.TabIndex = 9;
            this.preferMemoryCheckBox.Text = "메모리 데이터 우선";
            this.preferMemoryCheckBox.UseVisualStyleBackColor = true;
            //
            // chartHost
            //
            this.chartHost.BackColor = System.Drawing.Color.White;
            this.chartHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartHost.Location = new System.Drawing.Point(0, 88);
            this.chartHost.Margin = new System.Windows.Forms.Padding(0);
            this.chartHost.Name = "chartHost";
            this.chartHost.Size = new System.Drawing.Size(1180, 560);
            this.chartHost.TabIndex = 1;
            //
            // nearestNeighborGrid
            //
            this.nearestNeighborGrid.AllowUserToAddRows = false;
            this.nearestNeighborGrid.AllowUserToDeleteRows = false;
            this.nearestNeighborGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.nearestNeighborGrid.BackgroundColor = System.Drawing.Color.White;
            this.nearestNeighborGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.nearestNeighborGrid.ColumnHeadersHeight = 30;
            this.nearestNeighborGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nearestNeighborGrid.EnableHeadersVisualStyles = false;
            this.nearestNeighborGrid.Location = new System.Drawing.Point(12, 656);
            this.nearestNeighborGrid.Margin = new System.Windows.Forms.Padding(12, 8, 12, 12);
            this.nearestNeighborGrid.MultiSelect = false;
            this.nearestNeighborGrid.Name = "nearestNeighborGrid";
            this.nearestNeighborGrid.ReadOnly = true;
            this.nearestNeighborGrid.RowHeadersVisible = false;
            this.nearestNeighborGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.nearestNeighborGrid.Size = new System.Drawing.Size(1156, 152);
            this.nearestNeighborGrid.TabIndex = 2;
            this.nearestNeighborGrid.SelectionChanged += new System.EventHandler(this.NearestNeighborGrid_SelectionChanged);
            //
            // ScatterMain
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1180, 820);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MinimumSize = new System.Drawing.Size(900, 650);
            this.Name = "ScatterMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ScatterMain - PCA Scatter";
            this.rootLayout.ResumeLayout(false);
            this.toolbarLayout.ResumeLayout(false);
            this.commandPanel.ResumeLayout(false);
            this.commandPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nearestNeighborGrid)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.TableLayoutPanel toolbarLayout;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label summaryLabel;
        private System.Windows.Forms.FlowLayoutPanel commandPanel;
        private System.Windows.Forms.Label parameterTypeLabel;
        private System.Windows.Forms.RadioButton responseRadioButton;
        private System.Windows.Forms.RadioButton defectRadioButton;
        private System.Windows.Forms.Label draftNoLabel;
        private System.Windows.Forms.TextBox draftNoTextBox;
        private System.Windows.Forms.Button searchButton;
        private System.Windows.Forms.Button refreshAllButton;
        private System.Windows.Forms.Button sampleDataButton;
        private System.Windows.Forms.Button accordPcaButton;
        private System.Windows.Forms.CheckBox preferMemoryCheckBox;
        private System.Windows.Forms.Panel chartHost;
        private System.Windows.Forms.DataGridView nearestNeighborGrid;
    }
}
