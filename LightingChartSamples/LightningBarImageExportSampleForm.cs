using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LightingChartSamples
{
    public class LightningBarImageExportSampleForm : Form
    {
        private readonly LightningBar barChart;
        private readonly PictureBox picturePreview;
        private readonly TextBox txtSaveDirectory;
        private readonly Label lblLastPath;

        public LightningBarImageExportSampleForm()
        {
            Text = "LightningBar Image Export Sample";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1180, 720);
            BackColor = Color.White;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = Color.White
            };

            var lblDirectory = new Label
            {
                AutoSize = true,
                Location = new Point(14, 16),
                Text = "Save directory",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            txtSaveDirectory = new TextBox
            {
                Location = new Point(118, 12),
                Size = new Size(620, 23),
                Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LightningBarExcelImages")
            };

            var btnSavePng = new Button
            {
                Location = new Point(752, 10),
                Size = new Size(112, 28),
                Text = "Save PNG"
            };
            btnSavePng.Click += delegate { SavePreview(LightningBarImageFileFormat.Png); };

            var btnSaveJpeg = new Button
            {
                Location = new Point(872, 10),
                Size = new Size(112, 28),
                Text = "Save JPEG"
            };
            btnSaveJpeg.Click += delegate { SavePreview(LightningBarImageFileFormat.Jpeg); };

            var btnLoadCached = new Button
            {
                Location = new Point(992, 10),
                Size = new Size(150, 28),
                Text = "Load cached image"
            };
            btnLoadCached.Click += delegate { LoadCachedImage(); };

            lblLastPath = new Label
            {
                AutoEllipsis = true,
                Location = new Point(14, 45),
                Size = new Size(1128, 20),
                Text = "Last saved path: -",
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            topPanel.Controls.Add(lblDirectory);
            topPanel.Controls.Add(txtSaveDirectory);
            topPanel.Controls.Add(btnSavePng);
            topPanel.Controls.Add(btnSaveJpeg);
            topPanel.Controls.Add(btnLoadCached);
            topPanel.Controls.Add(lblLastPath);

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 570,
                BackColor = Color.White
            };

            var chartHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                BackColor = Color.White
            };

            picturePreview = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            splitContainer.Panel1.Controls.Add(chartHost);
            splitContainer.Panel2.Controls.Add(picturePreview);

            Controls.Add(splitContainer);
            Controls.Add(topPanel);

            barChart = LightningBar.Create(chartHost, CreateCategories(), CreateSeries(), CreateChartOptions());
            FormClosed += delegate { DisposePreviewImage(); };
        }

        private void SavePreview(LightningBarImageFileFormat fileFormat)
        {
            try
            {
                LightningBarImageOptions imageOptions = CreateImageOptions(fileFormat);
                string imagePath = barChart.SaveImage(imageOptions);
                lblLastPath.Text = "Last saved path: " + imagePath;
                LoadCachedImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadCachedImage()
        {
            Image image = barChart.GetLastSavedImage();
            if (image == null)
            {
                image = barChart.LoadLastSavedImage();
            }

            if (image == null)
            {
                MessageBox.Show(this, "No saved image is cached yet.", "Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetPreviewImage(image);
        }

        private void SetPreviewImage(Image image)
        {
            Image oldImage = picturePreview.Image;
            picturePreview.Image = image;
            if (oldImage != null)
            {
                oldImage.Dispose();
            }
        }

        private void DisposePreviewImage()
        {
            if (picturePreview.Image != null)
            {
                picturePreview.Image.Dispose();
                picturePreview.Image = null;
            }
        }

        private LightningBarImageOptions CreateImageOptions(LightningBarImageFileFormat fileFormat)
        {
            return new LightningBarImageOptions
            {
                Preset = LightningBarImagePreset.ChartZoom,
                Width = 900,
                Height = 600,
                DpiX = 150f,
                DpiY = 150f,
                FileFormat = fileFormat,
                SaveDirectory = txtSaveDirectory.Text,
                FileName = fileFormat == LightningBarImageFileFormat.Jpeg
                    ? "bar_chart_excel.jpg"
                    : "bar_chart_excel.png",
                JpegQuality = 92L,
                HideRawDataButtonOnImage = true
            };
        }

        private static LightningBarOptions CreateChartOptions()
        {
            return new LightningBarOptions
            {
                BackgroundColor = Color.White,
                TitleOptions = new LightningBarTitleOptions
                {
                    Text = "Excel Export Chart",
                    Position = LightningBarTitlePosition.TopCenter,
                    FontSize = 12f,
                    MarginTop = 12f
                },
                Layout = new LightningBarLayoutOptions
                {
                    ChartPadding = 28,
                    TopOffset = 86,
                    LegendReservedWidth = 150,
                    CategoryLabelReservedWidth = 130f,
                    BottomScaleAreaHeight = 34f
                },
                Legend = new LightningBarLegendOptions
                {
                    Position = LightningBarLegendPosition.Top,
                    Alignment = LightningBarLegendAlignment.Center,
                    FontSize = 8f,
                    LabelMaxLines = 2,
                    LabelMaxWidth = 140f
                },
                CategoryLabels = new LightningBarCategoryLabelOptions
                {
                    FontSize = 8f,
                    MaxLines = 3,
                    LineSpacing = 2f
                },
                Bars = new LightningBarBarOptions
                {
                    HeightMode = LightningBarHeightMode.Manual,
                    FixedHeight = 18f,
                    Gap = 5f,
                    GroupPaddingRatio = 0.18f
                },
                RawData = new LightningBarRawDataOptions
                {
                    ButtonMode = LightningBarRawDataButtonMode.Hidden
                },
                Image = new LightningBarImageOptions
                {
                    Preset = LightningBarImagePreset.ChartZoom,
                    Width = 900,
                    Height = 600,
                    DpiX = 150f,
                    DpiY = 150f,
                    HideRawDataButtonOnImage = true
                }
            };
        }

        private static IEnumerable<string> CreateCategories()
        {
            return new[]
            {
                "Quality\nInspection\nScore",
                "Production\nOutput\nRate",
                "Safety\nRisk\nIndex",
                "Cost\nControl\nLevel",
                "Delivery\nSchedule\nScore"
            };
        }

        private static IEnumerable<LightningBarSeries> CreateSeries()
        {
            return new[]
            {
                new LightningBarSeries
                {
                    Name = "Current",
                    LegendLabel = "Current\nResult",
                    Values = new[] { 88f, 82f, 91f, 79f, 95f },
                    FillColor = Color.FromArgb(175, 76, 132, 210),
                    BorderColor = Color.FromArgb(76, 132, 210)
                },
                new LightningBarSeries
                {
                    Name = "Target",
                    LegendLabel = "Target\nLine",
                    Values = new[] { 90f, 85f, 90f, 82f, 93f },
                    FillColor = Color.FromArgb(155, 86, 166, 112),
                    BorderColor = Color.FromArgb(86, 166, 112)
                }
            };
        }
    }
}
