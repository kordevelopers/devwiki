namespace LightingChartSamples
{
    partial class MainWindowForm
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다.
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlContainer = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnTextDocument = new System.Windows.Forms.Button();
            this.btnLightningBar = new System.Windows.Forms.Button();
            this.btnLightningRadar = new System.Windows.Forms.Button();
            this.btnRadarChartSample = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.pnlContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlContainer
            // 
            this.pnlContainer.BackColor = System.Drawing.Color.White;
            this.pnlContainer.Controls.Add(this.lblTitle);
            this.pnlContainer.Controls.Add(this.btnTextDocument);
            this.pnlContainer.Controls.Add(this.btnLightningBar);
            this.pnlContainer.Controls.Add(this.btnLightningRadar);
            this.pnlContainer.Controls.Add(this.btnRadarChartSample);
            this.pnlContainer.Controls.Add(this.btnClose);
            this.pnlContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContainer.Location = new System.Drawing.Point(0, 0);
            this.pnlContainer.Name = "pnlContainer";
            this.pnlContainer.Padding = new System.Windows.Forms.Padding(28, 24, 28, 24);
            this.pnlContainer.Size = new System.Drawing.Size(520, 360);
            this.pnlContainer.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(53)))), ((int)(((byte)(84)))));
            this.lblTitle.Location = new System.Drawing.Point(27, 24);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(291, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "샘플 실행 메뉴 (MainWindowForm)";
            // 
            // btnTextDocument
            // 
            this.btnTextDocument.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(75)))), ((int)(((byte)(123)))), ((int)(((byte)(213)))));
            this.btnTextDocument.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTextDocument.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnTextDocument.ForeColor = System.Drawing.Color.White;
            this.btnTextDocument.Location = new System.Drawing.Point(32, 74);
            this.btnTextDocument.Name = "btnTextDocument";
            this.btnTextDocument.Size = new System.Drawing.Size(456, 46);
            this.btnTextDocument.TabIndex = 1;
            this.btnTextDocument.Text = "리치 문서 샘플 열기 (TextDocument)";
            this.btnTextDocument.UseVisualStyleBackColor = false;
            this.btnTextDocument.Click += new System.EventHandler(this.btnTextDocument_Click);
            // 
            // btnLightningBar
            // 
            this.btnLightningBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(165)))), ((int)(((byte)(99)))));
            this.btnLightningBar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLightningBar.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnLightningBar.ForeColor = System.Drawing.Color.White;
            this.btnLightningBar.Location = new System.Drawing.Point(32, 132);
            this.btnLightningBar.Name = "btnLightningBar";
            this.btnLightningBar.Size = new System.Drawing.Size(456, 46);
            this.btnLightningBar.TabIndex = 2;
            this.btnLightningBar.Text = "바 차트 샘플 열기 (LightningBarSample)";
            this.btnLightningBar.UseVisualStyleBackColor = false;
            this.btnLightningBar.Click += new System.EventHandler(this.btnLightningBar_Click);
            // 
            // btnLightningRadar
            // 
            this.btnLightningRadar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(153)))), ((int)(((byte)(53)))));
            this.btnLightningRadar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLightningRadar.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnLightningRadar.ForeColor = System.Drawing.Color.White;
            this.btnLightningRadar.Location = new System.Drawing.Point(32, 190);
            this.btnLightningRadar.Name = "btnLightningRadar";
            this.btnLightningRadar.Size = new System.Drawing.Size(456, 46);
            this.btnLightningRadar.TabIndex = 3;
            this.btnLightningRadar.Text = "레이더 차트 샘플 열기 (LightningRadarSample)";
            this.btnLightningRadar.UseVisualStyleBackColor = false;
            this.btnLightningRadar.Click += new System.EventHandler(this.btnLightningRadar_Click);
            // 
            // btnRadarChartSample
            // 
            this.btnRadarChartSample.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(149)))), ((int)(((byte)(115)))), ((int)(((byte)(206)))));
            this.btnRadarChartSample.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRadarChartSample.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnRadarChartSample.ForeColor = System.Drawing.Color.White;
            this.btnRadarChartSample.Location = new System.Drawing.Point(32, 248);
            this.btnRadarChartSample.Name = "btnRadarChartSample";
            this.btnRadarChartSample.Size = new System.Drawing.Size(456, 46);
            this.btnRadarChartSample.TabIndex = 4;
            this.btnRadarChartSample.Text = "레이더 차트 샘플 열기 (Form1)";
            this.btnRadarChartSample.UseVisualStyleBackColor = false;
            this.btnRadarChartSample.Click += new System.EventHandler(this.btnRadarChartSample_Click);
            // 
            // btnClose
            // 
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnClose.Location = new System.Drawing.Point(392, 311);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(96, 30);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "닫기";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // MainWindowForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 360);
            this.Controls.Add(this.pnlContainer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainWindowForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Main Window";
            this.pnlContainer.ResumeLayout(false);
            this.pnlContainer.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlContainer;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Button btnTextDocument;
        private System.Windows.Forms.Button btnLightningBar;
        private System.Windows.Forms.Button btnLightningRadar;
        private System.Windows.Forms.Button btnRadarChartSample;
        private System.Windows.Forms.Button btnClose;
    }
}
