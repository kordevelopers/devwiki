namespace LightingChartSamples
{
    partial class TextDocument
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panelRoot = new System.Windows.Forms.Panel();
            this.panelCard = new System.Windows.Forms.Panel();
            this.richTextBoxDocument = new System.Windows.Forms.RichTextBox();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.btnExportExcel = new System.Windows.Forms.Button();
            this.lblProgressValue = new System.Windows.Forms.Label();
            this.progressImpact = new System.Windows.Forms.ProgressBar();
            this.lblProgressTitle = new System.Windows.Forms.Label();
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblTag = new System.Windows.Forms.Label();
            this.lblHeroSub = new System.Windows.Forms.Label();
            this.lblHeroTitle = new System.Windows.Forms.Label();
            this.panelRoot.SuspendLayout();
            this.panelCard.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.panelHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelRoot
            // 
            this.panelRoot.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(251)))));
            this.panelRoot.Controls.Add(this.panelCard);
            this.panelRoot.Controls.Add(this.panelBottom);
            this.panelRoot.Controls.Add(this.panelHeader);
            this.panelRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRoot.Location = new System.Drawing.Point(0, 0);
            this.panelRoot.Name = "panelRoot";
            this.panelRoot.Padding = new System.Windows.Forms.Padding(24, 18, 24, 18);
            this.panelRoot.Size = new System.Drawing.Size(900, 620);
            this.panelRoot.TabIndex = 0;
            // 
            // panelCard
            // 
            this.panelCard.BackColor = System.Drawing.Color.White;
            this.panelCard.Controls.Add(this.richTextBoxDocument);
            this.panelCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelCard.Location = new System.Drawing.Point(24, 146);
            this.panelCard.Name = "panelCard";
            this.panelCard.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
            this.panelCard.Size = new System.Drawing.Size(852, 384);
            this.panelCard.TabIndex = 2;
            // 
            // richTextBoxDocument
            // 
            this.richTextBoxDocument.BackColor = System.Drawing.Color.White;
            this.richTextBoxDocument.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBoxDocument.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxDocument.Location = new System.Drawing.Point(20, 18);
            this.richTextBoxDocument.Name = "richTextBoxDocument";
            this.richTextBoxDocument.ReadOnly = true;
            this.richTextBoxDocument.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.richTextBoxDocument.Size = new System.Drawing.Size(812, 348);
            this.richTextBoxDocument.TabIndex = 0;
            this.richTextBoxDocument.Text = "";
            // 
            // panelBottom
            // 
            this.panelBottom.BackColor = System.Drawing.Color.White;
            this.panelBottom.Controls.Add(this.btnExportExcel);
            this.panelBottom.Controls.Add(this.lblProgressValue);
            this.panelBottom.Controls.Add(this.progressImpact);
            this.panelBottom.Controls.Add(this.lblProgressTitle);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(24, 530);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Padding = new System.Windows.Forms.Padding(18, 10, 18, 10);
            this.panelBottom.Size = new System.Drawing.Size(852, 72);
            this.panelBottom.TabIndex = 1;
            // 
            // btnExportExcel
            // 
            this.btnExportExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExportExcel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(58)))), ((int)(((byte)(123)))), ((int)(((byte)(213)))));
            this.btnExportExcel.FlatAppearance.BorderSize = 0;
            this.btnExportExcel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportExcel.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnExportExcel.ForeColor = System.Drawing.Color.White;
            this.btnExportExcel.Location = new System.Drawing.Point(686, 20);
            this.btnExportExcel.Name = "btnExportExcel";
            this.btnExportExcel.Size = new System.Drawing.Size(145, 32);
            this.btnExportExcel.TabIndex = 3;
            this.btnExportExcel.Text = "Excel 내보내기";
            this.btnExportExcel.UseVisualStyleBackColor = false;
            // 
            // lblProgressValue
            // 
            this.lblProgressValue.AutoSize = true;
            this.lblProgressValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblProgressValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(87)))), ((int)(((byte)(184)))));
            this.lblProgressValue.Location = new System.Drawing.Point(560, 26);
            this.lblProgressValue.Name = "lblProgressValue";
            this.lblProgressValue.Size = new System.Drawing.Size(37, 19);
            this.lblProgressValue.TabIndex = 2;
            this.lblProgressValue.Text = "78%";
            // 
            // progressImpact
            // 
            this.progressImpact.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(58)))), ((int)(((byte)(123)))), ((int)(((byte)(213)))));
            this.progressImpact.Location = new System.Drawing.Point(92, 35);
            this.progressImpact.Name = "progressImpact";
            this.progressImpact.Size = new System.Drawing.Size(462, 10);
            this.progressImpact.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressImpact.TabIndex = 1;
            this.progressImpact.Value = 78;
            // 
            // lblProgressTitle
            // 
            this.lblProgressTitle.AutoSize = true;
            this.lblProgressTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblProgressTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(57)))), ((int)(((byte)(80)))));
            this.lblProgressTitle.Location = new System.Drawing.Point(21, 26);
            this.lblProgressTitle.Name = "lblProgressTitle";
            this.lblProgressTitle.Size = new System.Drawing.Size(65, 19);
            this.lblProgressTitle.TabIndex = 0;
            this.lblProgressTitle.Text = "추론결과";
            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(129)))), ((int)(((byte)(226)))));
            this.panelHeader.Controls.Add(this.lblTag);
            this.panelHeader.Controls.Add(this.lblHeroSub);
            this.panelHeader.Controls.Add(this.lblHeroTitle);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(24, 18);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Padding = new System.Windows.Forms.Padding(24, 16, 24, 16);
            this.panelHeader.Size = new System.Drawing.Size(852, 128);
            this.panelHeader.TabIndex = 0;
            // 
            // lblTag
            // 
            this.lblTag.AutoSize = true;
            this.lblTag.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(228)))), ((int)(((byte)(236)))), ((int)(((byte)(255)))));
            this.lblTag.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblTag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(73)))), ((int)(((byte)(160)))));
            this.lblTag.Location = new System.Drawing.Point(27, 87);
            this.lblTag.Name = "lblTag";
            this.lblTag.Padding = new System.Windows.Forms.Padding(10, 4, 10, 4);
            this.lblTag.Size = new System.Drawing.Size(158, 23);
            this.lblTag.TabIndex = 2;
            this.lblTag.Text = "Executive AI Infernece \r\n";
            // 
            // lblHeroSub
            // 
            this.lblHeroSub.AutoSize = true;
            this.lblHeroSub.Font = new System.Drawing.Font("맑은 고딕", 10.5F);
            this.lblHeroSub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(239)))), ((int)(((byte)(255)))));
            this.lblHeroSub.Location = new System.Drawing.Point(27, 51);
            this.lblHeroSub.Name = "lblHeroSub";
            this.lblHeroSub.Size = new System.Drawing.Size(365, 19);
            this.lblHeroSub.TabIndex = 1;
            this.lblHeroSub.Text = "실행 중심 요약 · AI Inferenee 기반 요약 데이터 시나리오";
            // 
            // lblHeroTitle
            // 
            this.lblHeroTitle.AutoSize = true;
            this.lblHeroTitle.Font = new System.Drawing.Font("맑은 고딕", 17F, System.Drawing.FontStyle.Bold);
            this.lblHeroTitle.ForeColor = System.Drawing.Color.White;
            this.lblHeroTitle.Location = new System.Drawing.Point(25, 16);
            this.lblHeroTitle.Name = "lblHeroTitle";
            this.lblHeroTitle.Size = new System.Drawing.Size(296, 31);
            this.lblHeroTitle.TabIndex = 0;
            this.lblHeroTitle.Text = "📊 AI Summary 대시보드 ";
            // 
            // TextDocument
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 620);
            this.Controls.Add(this.panelRoot);
            this.MinimumSize = new System.Drawing.Size(900, 620);
            this.Name = "TextDocument";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Styled Report";
            this.panelRoot.ResumeLayout(false);
            this.panelCard.ResumeLayout(false);
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelRoot;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblHeroTitle;
        private System.Windows.Forms.Label lblHeroSub;
        private System.Windows.Forms.Label lblTag;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Label lblProgressTitle;
        private System.Windows.Forms.ProgressBar progressImpact;
        private System.Windows.Forms.Label lblProgressValue;
        private System.Windows.Forms.Button btnExportExcel;
        private System.Windows.Forms.Panel panelCard;
        private System.Windows.Forms.RichTextBox richTextBoxDocument;
    }
}