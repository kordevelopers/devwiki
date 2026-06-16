using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LightingChartSamples.Scatter
{
    public class LightningScatterSampleForm : Form
    {
        private readonly LightningScatter scatterChart;
        private readonly Panel pnlTop;
        private readonly Panel pnlChartHost;
        private readonly Button btnSaveImage;
        private readonly Button btnClear;
        private readonly Button btnReload;
        private readonly ComboBox cboSaveFolder;
        private readonly Label lblSavedPath;
        private readonly TextBox txtEventLog;

        public LightningScatterSampleForm()
        {
            BackColor = Color.White;
            Font = new Font(LightningScatter.DefaultChartFontName, 9F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "LightningChart Scatter Chart Sample";
            ClientSize = new Size(1120, 780);

            pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                BackColor = Color.White
            };

            var lblGuide = new Label
            {
                AutoSize = true,
                Font = new Font(LightningScatter.DefaultChartFontName, 9F),
                Location = new Point(12, 14),
                Text = "LightningChart API 기반 Scatter 샘플 - 데이터와 옵션만 넘겨 차트를 생성하고 이벤트/이미지 저장을 확인합니다."
            };

            var lblSaveFolder = new Label
            {
                AutoSize = true,
                Font = new Font(LightningScatter.DefaultChartFontName, 9F),
                Location = new Point(12, 53),
                Text = "저장 위치"
            };

            cboSaveFolder = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(78, 49),
                Size = new Size(210, 23)
            };

            foreach (LightningScatterImageSaveFolder saveFolder in Enum.GetValues(typeof(LightningScatterImageSaveFolder)))
            {
                cboSaveFolder.Items.Add(saveFolder);
            }

            cboSaveFolder.SelectedItem = LightningScatterImageSaveFolder.LocalApplicationData;

            btnSaveImage = new Button
            {
                Location = new Point(300, 47),
                Size = new Size(118, 28),
                Text = "이미지 저장"
            };
            btnSaveImage.Click += delegate { SaveChartImage(); };

            btnClear = new Button
            {
                Location = new Point(426, 47),
                Size = new Size(92, 28),
                Text = "Clear"
            };
            btnClear.Click += delegate
            {
                scatterChart.Clear();
                AppendEventLog("Clear: 조회 전 상태로 초기화");
            };

            btnReload = new Button
            {
                Location = new Point(526, 47),
                Size = new Size(112, 28),
                Text = "데이터 재조회"
            };
            btnReload.Click += delegate
            {
                scatterChart.UpdateData(CreateSeries(), CreateOptions());
                AppendEventLog("UpdateData: 데이터와 옵션을 다시 바인딩");
            };

            lblSavedPath = new Label
            {
                AutoEllipsis = true,
                Location = new Point(650, 52),
                Size = new Size(450, 20),
                Text = "저장 경로: -",
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            pnlTop.Controls.Add(lblGuide);
            pnlTop.Controls.Add(lblSaveFolder);
            pnlTop.Controls.Add(cboSaveFolder);
            pnlTop.Controls.Add(btnSaveImage);
            pnlTop.Controls.Add(btnClear);
            pnlTop.Controls.Add(btnReload);
            pnlTop.Controls.Add(lblSavedPath);

            txtEventLog = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 124,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White,
                Font = new Font(LightningScatter.DefaultChartFontName, 9F)
            };

            pnlChartHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            Controls.Add(pnlChartHost);
            Controls.Add(txtEventLog);
            Controls.Add(pnlTop);

            scatterChart = LightningScatter.Create(pnlChartHost, CreateSeries(), CreateOptions());
            scatterChart.PointClicked += ScatterChart_PointClicked;
            scatterChart.LegendClicked += ScatterChart_LegendClicked;
            scatterChart.ImageSaving += ScatterChart_ImageSaving;
            scatterChart.ImageSaved += ScatterChart_ImageSaved;
        }

        private static IEnumerable<LightningScatterSeries> CreateSeries()
        {
            return new[]
            {
                new LightningScatterSeries
                {
                    Name = "Temperature",
                    LegendLabel = "온도",
                    PointColor = Color.FromArgb(217, 94, 86),
                    LineColor = Color.FromArgb(217, 94, 86),
                    PointSize = 9f,
                    ShowLine = true,
                    Points = new List<LightningScatterPoint>
                    {
                        new LightningScatterPoint(0, 21, "T-001"),
                        new LightningScatterPoint(1, 26, "T-002"),
                        new LightningScatterPoint(2, 33, "T-003"),
                        new LightningScatterPoint(3, 37, "T-004"),
                        new LightningScatterPoint(4, 41, "T-005")
                    }
                },
                new LightningScatterSeries
                {
                    Name = "Pressure",
                    LegendLabel = "압력",
                    PointColor = Color.FromArgb(71, 126, 207),
                    LineColor = Color.FromArgb(71, 126, 207),
                    PointSize = 9f,
                    ShowLine = true,
                    Points = new List<LightningScatterPoint>
                    {
                        new LightningScatterPoint(0, 18, "P-001"),
                        new LightningScatterPoint(1, 22, "P-002"),
                        new LightningScatterPoint(2, 30, "P-003"),
                        new LightningScatterPoint(3, 34, "P-004"),
                        new LightningScatterPoint(4, 39, "P-005")
                    }
                },
                new LightningScatterSeries
                {
                    Name = "Vibration",
                    LegendLabel = "진동",
                    PointColor = Color.FromArgb(77, 158, 117),
                    LineColor = Color.FromArgb(77, 158, 117),
                    PointSize = 9f,
                    ShowLine = false,
                    Points = new List<LightningScatterPoint>
                    {
                        new LightningScatterPoint(0.2, 10, "V-001"),
                        new LightningScatterPoint(1.4, 14, "V-002"),
                        new LightningScatterPoint(2.2, 20, "V-003"),
                        new LightningScatterPoint(3.3, 25, "V-004"),
                        new LightningScatterPoint(4.1, 31, "V-005")
                    }
                }
            };
        }

        private LightningScatterOptions CreateOptions()
        {
            LightningScatterImageSaveFolder saveFolder = cboSaveFolder != null
                && cboSaveFolder.SelectedItem is LightningScatterImageSaveFolder
                    ? (LightningScatterImageSaveFolder)cboSaveFolder.SelectedItem
                    : LightningScatterImageSaveFolder.LocalApplicationData;

            return new LightningScatterOptions
            {
                Title = string.Empty,
                ShowTitle = false,
                BackgroundColor = Color.White,
                GraphBackgroundColor = Color.White,
                XAxis = new LightningScatterAxisOptions
                {
                    Title = "시간",
                    Minimum = 0,
                    Maximum = 5,
                    AutoFit = true,
                    MajorDivCount = 5,
                    LabelFormat = "0.#"
                },
                YAxis = new LightningScatterAxisOptions
                {
                    Title = "측정값",
                    Minimum = 0,
                    Maximum = 50,
                    AutoFit = true,
                    MajorDivCount = 5,
                    LabelFormat = "0.#"
                },
                Legend = new LightningScatterLegendOptions
                {
                    Visible = true,
                    Position = LightningScatterLegendPosition.TopCenter,
                    FontSize = 8f,
                    ShowCheckboxes = true,
                    ShowIcons = true
                },
                Tooltip = new LightningScatterTooltipOptions
                {
                    Enabled = true,
                    HitPixelTolerance = 14,
                    Format = "{0}\r\nX:{1:0.###}, Y:{2:0.###}\r\n* 클릭할 경우 해당 계측 데이터 차트로 이동합니다."
                },
                NoData = new LightningScatterNoDataOptions
                {
                    Text = "Scatter 조회 데이터가 없습니다.",
                    ShowWhenDataMissing = true,
                    ShowWhenAllValuesZero = true,
                    FontSize = 10f,
                    BadgeWidthRatio = 0.8f
                },
                Image = new LightningScatterImageOptions
                {
                    Width = 600,
                    Height = 400,
                    FileFormat = LightningScatterImageFileFormat.Png,
                    SaveFolder = saveFolder,
                    SubDirectoryName = "LightningScatterSample",
                    UseDateFolder = true,
                    UseGuidFileName = true
                }
            };
        }

        private void SaveChartImage()
        {
            try
            {
                string imagePath = scatterChart.SaveImage(CreateOptions().Image);
                lblSavedPath.Text = "저장 경로: " + imagePath;
            }
            catch (Exception ex)
            {
                AppendEventLog("이미지 저장 실패: " + ex.Message);
                MessageBox.Show(this, ex.Message, "이미지 저장 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ScatterChart_PointClicked(object sender, LightningScatterPointClickEventArgs e)
        {
            AppendEventLog(string.Format(
                "PointClicked: Series={0}, Index={1}, X={2:0.###}, Y={3:0.###}",
                e.Series.Name,
                e.PointIndex,
                e.Point.X,
                e.Point.Y));
        }

        private void ScatterChart_LegendClicked(object sender, LightningScatterLegendClickEventArgs e)
        {
            AppendEventLog(string.Format("LegendClicked: Series={0}, Label={1}", e.Series.Name, e.LegendLabel));
        }

        private void ScatterChart_ImageSaving(object sender, LightningScatterImageSavingEventArgs e)
        {
            AppendEventLog("ImageSaving: " + e.ImagePath);
        }

        private void ScatterChart_ImageSaved(object sender, LightningScatterImageSavedEventArgs e)
        {
            lblSavedPath.Text = "저장 경로: " + e.ImagePath;
            AppendEventLog("ImageSaved: " + e.ImagePath);
        }

        private void AppendEventLog(string message)
        {
            if (txtEventLog.IsDisposed)
            {
                return;
            }

            txtEventLog.AppendText(string.Format("[{0:HH:mm:ss}] {1}{2}", DateTime.Now, message, Environment.NewLine));
        }
    }
}
