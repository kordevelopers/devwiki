using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LightingChartSamples
{
    public class LightningBarImageExportSampleForm : Form
    {
        private const string ColumnCategory = "CATEGORY";
        private const string ColumnValue = "VALUE";
        private const string ColumnEquipmentId = "EQUIPMENT_ID";
        private const string ColumnMetricCode = "METRIC_CODE";

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
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
            };

            txtSaveDirectory = new TextBox
            {
                Location = new Point(118, 12),
                Size = new Size(620, 23),
                Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LightningBarExcelImages")
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
                Preset = LightningBarImagePreset.Default,
                Width = 600,
                Height = 400,
                DpiX = 96f,
                DpiY = 96f,
                FileFormat = fileFormat,
                SaveDirectory = txtSaveDirectory.Text,
                SubDirectoryName = string.Empty,
                UseDateFolder = true,
                UseGuidFileName = true,
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
                    Text = string.Empty,
                    Position = LightningBarTitlePosition.TopCenter,
                    FontSize = 12f,
                    MarginTop = 12f
                },
                Layout = new LightningBarLayoutOptions
                {
                    ChartPadding = 20,
                    TopOffset = 72,
                    LegendReservedWidth = 120,
                    LegendReservedWidthMode = LightningBarLegendReservedWidthMode.CollapseForTopBottomLegend,
                    CategoryLabelReservedWidth = 110f,
                    AutoCategoryLabelReservedWidth = true,
                    MinCategoryLabelReservedWidth = 78f,
                    MaxCategoryLabelReservedWidth = 150f,
                    BottomScaleAreaHeight = 30f
                },
                Legend = new LightningBarLegendOptions
                {
                    Position = LightningBarLegendPosition.Top,
                    Alignment = LightningBarLegendAlignment.Center,
                    MarginFromChart = 8f,
                    FontSize = 7.5f,
                    MarkerWidth = 22f,
                    MarkerHeight = 14f,
                    LabelMaxLines = 3,
                    LabelMaxWidth = 110f,
                    ItemSpacing = 6f,
                    SectionSpacing = 20f
                },
                CategoryLabels = new LightningBarCategoryLabelOptions
                {
                    FontSize = 8f,
                    MaxLines = 3,
                    LineSpacing = 1.5f
                },
                Bars = new LightningBarBarOptions
                {
                    HeightMode = LightningBarHeightMode.Manual,
                    FixedHeight = 30f,
                    ClampFixedHeightToGroup = true,
                    Gap = 5f,
                    GroupPaddingRatio = 0.16f
                },
                RawData = new LightningBarRawDataOptions
                {
                    ButtonMode = LightningBarRawDataButtonMode.Hidden
                },
                Image = new LightningBarImageOptions
                {
                    Preset = LightningBarImagePreset.Default,
                    Width = 600,
                    Height = 400,
                    DpiX = 96f,
                    DpiY = 96f,
                    SaveFolder = LightningBarImageSaveFolder.LocalApplicationData,
                    SubDirectoryName = "LightningBarImageExportSample",
                    UseDateFolder = true,
                    UseGuidFileName = true,
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
                    ValueSource = CreateRawDataTable("EQ-EXPORT-CURRENT", new[] { 88f, 82f, 91f, 79f, 95f }),
                    ValueColumnName = ColumnValue,
                    FillColor = Color.FromArgb(175, 255, 196, 214),
                    BorderColor = Color.FromArgb(225, 104, 150)
                },
                new LightningBarSeries
                {
                    Name = "Target",
                    LegendLabel = "Target\nLine",
                    ValueSource = CreateRawDataTable("EQ-EXPORT-TARGET", new[] { 90f, 85f, 90f, 82f, 93f }),
                    ValueColumnName = ColumnValue,
                    FillColor = Color.FromArgb(155, 86, 166, 112),
                    BorderColor = Color.FromArgb(86, 166, 112)
                }
            };
        }

        private static DataTable CreateRawDataTable(string equipmentId, float[] values)
        {
            DataTable table = new DataTable();
            table.Columns.Add(ColumnCategory, typeof(string));
            table.Columns.Add(ColumnValue, typeof(float));
            table.Columns.Add(ColumnEquipmentId, typeof(string));
            table.Columns.Add(ColumnMetricCode, typeof(string));

            string[] categories = new List<string>(CreateCategories()).ToArray();
            for (int i = 0; i < values.Length; i++)
            {
                DataRow row = table.NewRow();
                row[ColumnCategory] = i < categories.Length ? categories[i] : string.Empty;
                row[ColumnValue] = values[i];
                row[ColumnEquipmentId] = equipmentId;
                row[ColumnMetricCode] = string.Format("EXPORT-{0:00}", i + 1);
                table.Rows.Add(row);
            }

            return table;
        }
    }
}
