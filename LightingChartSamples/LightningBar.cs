using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LightingChartSamples
{
    public class LightningBarSeries
    {
        public LightningBarSeries()
        {
            Name = string.Empty;
            Values = new float[0];
            FillColor = Color.FromArgb(180, 74, 166, 224);
            BorderColor = Color.FromArgb(230, 54, 130, 188);
        }

        public string Name { get; set; }

        public float[] Values { get; set; }

        public Color FillColor { get; set; }

        public Color BorderColor { get; set; }

        public LightningBarSeries Clone()
        {
            return new LightningBarSeries
            {
                Name = Name,
                Values = Values == null ? new float[0] : Values.ToArray(),
                FillColor = FillColor,
                BorderColor = BorderColor
            };
        }
    }

    public class LightningBarOptions
    {
        public LightningBarOptions()
        {
            Title = "Bar Chart Sample";
            TitleColor = Color.FromArgb(90, 90, 90);
            TitleFontSize = 12f;
            BackgroundColor = Color.White;
            ChartPadding = 32;
            TopOffset = 90;
            LegendWidth = 180;
            GridLineCount = 5;
            CategoryFontSize = 8.5f;
            CategoryLabelColor = Color.FromArgb(95, 95, 95);
            ScaleFontSize = 9f;
            ScaleLabelColor = Color.FromArgb(95, 95, 95);
            AxisColor = Color.FromArgb(170, 170, 170);
            GridColor = Color.FromArgb(225, 225, 225);
            LegendTextColor = Color.FromArgb(90, 90, 90);
            BarBorderWidth = 1.2f;
            BarGap = 8f;
            GroupPaddingRatio = 0.18f;
            MaxValue = 100f;
            ImageStorage = new LightningRadarImageStorageOptions();
        }

        public string Title { get; set; }
        public Color TitleColor { get; set; }
        public float TitleFontSize { get; set; }
        public Color BackgroundColor { get; set; }
        public int ChartPadding { get; set; }
        public int TopOffset { get; set; }
        public int LegendWidth { get; set; }
        public int GridLineCount { get; set; }
        public float CategoryFontSize { get; set; }
        public Color CategoryLabelColor { get; set; }
        public float ScaleFontSize { get; set; }
        public Color ScaleLabelColor { get; set; }
        public Color AxisColor { get; set; }
        public Color GridColor { get; set; }
        public Color LegendTextColor { get; set; }
        public float BarBorderWidth { get; set; }
        public float BarGap { get; set; }
        public float GroupPaddingRatio { get; set; }
        public float MaxValue { get; set; }
        public LightningRadarImageStorageOptions ImageStorage { get; set; }

        public LightningBarOptions Clone()
        {
            LightningBarOptions clone = (LightningBarOptions)MemberwiseClone();
            clone.ImageStorage = ImageStorage == null ? new LightningRadarImageStorageOptions() : ImageStorage.Clone();
            return clone;
        }
    }

    public class LightningBarImagePathInfo
    {
        public LightningBarImagePathInfo()
        {
            FabId = string.Empty;
            LotCd = string.Empty;
            DraftNo = string.Empty;
            FileNamePrefix = "bar";
        }

        public string FabId { get; set; }
        public string LotCd { get; set; }
        public string DraftNo { get; set; }
        public string FileNamePrefix { get; set; }

        public LightningBarImagePathInfo Clone()
        {
            return (LightningBarImagePathInfo)MemberwiseClone();
        }
    }

    public class LightningBar : Control
    {
        private readonly object syncRoot = new object();
        private string[] categories = new string[0];
        private List<LightningBarSeries> series = new List<LightningBarSeries>();
        private LightningBarOptions options = new LightningBarOptions();

        public LightningBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            DoubleBuffered = true;
            BackColor = Color.White;
            Size = new Size(820, 540);
        }

        [Browsable(false)]
        public string[] Categories
        {
            get
            {
                lock (syncRoot)
                {
                    return categories.ToArray();
                }
            }
        }

        [Browsable(false)]
        public LightningBarSeries[] Series
        {
            get
            {
                lock (syncRoot)
                {
                    return series.Select(item => item.Clone()).ToArray();
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public LightningBarOptions Options
        {
            get
            {
                lock (syncRoot)
                {
                    return options.Clone();
                }
            }
        }

        public static T AttachTo<T>(Control parent, DockStyle dockStyle, Rectangle? bounds, LightningBarOptions barOptions)
            where T : LightningBar, new()
        {
            T chart = new T();

            if (barOptions != null)
            {
                chart.SetOptions(barOptions);
            }

            if (bounds.HasValue)
            {
                chart.Bounds = bounds.Value;
            }

            chart.Dock = dockStyle;
            chart.AddTo(parent);
            return chart;
        }

        public static LightningBar Create(Control parent, IEnumerable<string> newCategories, IEnumerable<LightningBarSeries> newSeries, LightningBarOptions barOptions)
        {
            LightningBar chart = AttachTo<LightningBar>(parent, DockStyle.Fill, null, barOptions);
            chart.SetData(newCategories, newSeries);
            return chart;
        }

        public void AddTo(Control parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            ExecuteOnUiThread(parent, delegate
            {
                if (Parent == parent)
                {
                    return;
                }

                parent.Controls.Add(this);
                BringToFront();
            });
        }

        public void SetData(IEnumerable<string> newCategories, IEnumerable<LightningBarSeries> newSeries)
        {
            string[] categoryArray = newCategories == null ? new string[0] : newCategories.ToArray();
            List<LightningBarSeries> seriesList = newSeries == null
                ? new List<LightningBarSeries>()
                : newSeries.Select(item => item == null ? new LightningBarSeries() : item.Clone()).ToList();

            NormalizeSeries(categoryArray, seriesList);

            lock (syncRoot)
            {
                categories = categoryArray;
                series = seriesList;
            }

            RefreshSafe();
        }

        public void SetCategories(IEnumerable<string> newCategories)
        {
            SetData(newCategories, Series);
        }

        public void SetSeries(IEnumerable<LightningBarSeries> newSeries)
        {
            SetData(Categories, newSeries);
        }

        public void SetOptions(LightningBarOptions newOptions)
        {
            if (newOptions == null)
            {
                throw new ArgumentNullException("newOptions");
            }

            lock (syncRoot)
            {
                options = newOptions.Clone();
            }

            RefreshSafe();
        }

        public void UpdateData(IEnumerable<string> newCategories, IEnumerable<LightningBarSeries> newSeries, LightningBarOptions newOptions)
        {
            string[] categoryArray = newCategories == null ? new string[0] : newCategories.ToArray();
            List<LightningBarSeries> seriesList = newSeries == null
                ? new List<LightningBarSeries>()
                : newSeries.Select(item => item == null ? new LightningBarSeries() : item.Clone()).ToList();
            LightningBarOptions nextOptions = newOptions == null ? new LightningBarOptions() : newOptions.Clone();

            NormalizeSeries(categoryArray, seriesList);

            lock (syncRoot)
            {
                categories = categoryArray;
                series = seriesList;
                options = nextOptions;
            }

            RefreshSafe();
        }

        public void Update(Action<LightningBar> updateAction)
        {
            if (updateAction == null)
            {
                throw new ArgumentNullException("updateAction");
            }

            ExecuteOnUiThread(this, delegate { updateAction(this); });
        }

        public string SaveImage(LightningBarImagePathInfo pathInfo)
        {
            return SaveImage(pathInfo, Options.ImageStorage);
        }

        public string SaveImage(LightningBarImagePathInfo pathInfo, LightningRadarImageStorageOptions storageOptions)
        {
            if (pathInfo == null)
            {
                throw new ArgumentNullException("pathInfo");
            }

            LightningRadarImageStorageOptions effectiveOptions = storageOptions == null ? new LightningRadarImageStorageOptions() : storageOptions.Clone();
            string folderPath = GetImageFolderPath(pathInfo, effectiveOptions, true);
            string fileExtension = GetFileExtension(effectiveOptions.ImageFormat);
            string safePrefix = SanitizePathSegment(pathInfo.FileNamePrefix, "bar");
            string fileName = string.Format("{0}_{1:yyyyMMdd_HHmmssfff}{2}", safePrefix, DateTime.Now, fileExtension);
            string fullPath = Path.Combine(folderPath, fileName);

            ExecuteOnUiThread(this, delegate
            {
                using (Bitmap bitmap = CreateChartBitmap(effectiveOptions))
                {
                    SaveBitmapToFile(bitmap, fullPath, effectiveOptions);
                }
            }, true);

            return fullPath;
        }

        public string GetImageFolderPath(LightningBarImagePathInfo pathInfo)
        {
            return GetImageFolderPath(pathInfo, Options.ImageStorage, false);
        }

        public string[] FindImages(LightningBarImagePathInfo pathInfo)
        {
            return FindImages(pathInfo, Options.ImageStorage);
        }

        public string[] FindImages(LightningBarImagePathInfo pathInfo, LightningRadarImageStorageOptions storageOptions)
        {
            string folderPath = GetImageFolderPath(pathInfo, storageOptions, false);
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return new string[0];
            }

            string[] allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
            return Directory.GetFiles(folderPath)
                .Where(file => allowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(File.GetCreationTime)
                .ToArray();
        }

        public string FindLatestImage(LightningBarImagePathInfo pathInfo)
        {
            return FindLatestImage(pathInfo, Options.ImageStorage);
        }

        public string FindLatestImage(LightningBarImagePathInfo pathInfo, LightningRadarImageStorageOptions storageOptions)
        {
            return FindImages(pathInfo, storageOptions).FirstOrDefault();
        }

        public int DeleteImagesCreatedAfter(LightningBarImagePathInfo pathInfo, DateTime threshold)
        {
            return DeleteImagesCreatedAfter(pathInfo, threshold, Options.ImageStorage);
        }

        public int DeleteImagesCreatedAfter(LightningBarImagePathInfo pathInfo, DateTime threshold, LightningRadarImageStorageOptions storageOptions)
        {
            string[] files = FindImages(pathInfo, storageOptions);
            int deletedCount = 0;

            foreach (string file in files)
            {
                DateTime createdTime = File.GetCreationTime(file);
                if (createdTime <= threshold)
                {
                    continue;
                }

                File.Delete(file);
                deletedCount++;
            }

            return deletedCount;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            string[] snapshotCategories;
            LightningBarSeries[] snapshotSeries;
            LightningBarOptions snapshotOptions;

            lock (syncRoot)
            {
                snapshotCategories = categories.ToArray();
                snapshotSeries = series.Select(item => item.Clone()).ToArray();
                snapshotOptions = options.Clone();
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(snapshotOptions.BackgroundColor);

            DrawTitle(e.Graphics, snapshotOptions);
            DrawBarChart(e.Graphics, snapshotCategories, snapshotSeries, snapshotOptions, ClientSize);
            DrawLegend(e.Graphics, snapshotSeries, snapshotOptions, ClientSize);
        }

        protected virtual void DrawTitle(Graphics graphics, LightningBarOptions currentOptions)
        {
            using (var titleFont = new Font(Font.FontFamily, currentOptions.TitleFontSize, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(currentOptions.TitleColor))
            {
                graphics.DrawString(currentOptions.Title ?? string.Empty, titleFont, titleBrush, 20f, 15f);
            }
        }

        protected virtual void DrawBarChart(Graphics graphics, string[] currentCategories, LightningBarSeries[] currentSeries, LightningBarOptions currentOptions, Size renderSize)
        {
            if (currentCategories == null || currentCategories.Length == 0 || currentSeries == null || currentSeries.Length == 0)
            {
                return;
            }

            RectangleF plotRect = GetPlotRectangle(renderSize, currentOptions);
            if (plotRect.Width <= 40f || plotRect.Height <= 40f)
            {
                return;
            }

            float maxValue = GetMaxValue(currentCategories, currentSeries, currentOptions);
            DrawGridAndAxis(graphics, plotRect, maxValue, currentOptions);
            DrawBars(graphics, plotRect, currentCategories, currentSeries, maxValue, currentOptions);
            DrawCategoryLabels(graphics, plotRect, currentCategories, currentOptions);
        }

        protected virtual RectangleF GetPlotRectangle(Size renderSize, LightningBarOptions currentOptions)
        {
            float left = currentOptions.ChartPadding + 90f;
            float top = currentOptions.TopOffset;
            float right = renderSize.Width - currentOptions.ChartPadding - currentOptions.LegendWidth;
            float bottom = renderSize.Height - currentOptions.ChartPadding - 34f;
            return RectangleF.FromLTRB(left, top, right, bottom);
        }

        protected virtual float GetMaxValue(string[] currentCategories, LightningBarSeries[] currentSeries, LightningBarOptions currentOptions)
        {
            float valueFromData = 0f;
            for (int i = 0; i < currentSeries.Length; i++)
            {
                LightningBarSeries barSeries = currentSeries[i];
                if (barSeries == null || barSeries.Values == null)
                {
                    continue;
                }

                for (int j = 0; j < Math.Min(currentCategories.Length, barSeries.Values.Length); j++)
                {
                    valueFromData = Math.Max(valueFromData, barSeries.Values[j]);
                }
            }

            float baseline = Math.Max(1f, currentOptions.MaxValue);
            return Math.Max(baseline, valueFromData);
        }

        protected virtual void DrawGridAndAxis(Graphics graphics, RectangleF plotRect, float maxValue, LightningBarOptions currentOptions)
        {
            int lineCount = Math.Max(1, currentOptions.GridLineCount);

            using (var gridPen = new Pen(currentOptions.GridColor, 1f))
            using (var axisPen = new Pen(currentOptions.AxisColor, 1.4f))
            using (var labelFont = new Font(Font.FontFamily, currentOptions.ScaleFontSize, FontStyle.Regular))
            using (var labelBrush = new SolidBrush(currentOptions.ScaleLabelColor))
            {
                for (int i = 0; i <= lineCount; i++)
                {
                    float ratio = (float)i / lineCount;
                    float x = plotRect.Left + (plotRect.Width * ratio);
                    graphics.DrawLine(gridPen, x, plotRect.Top, x, plotRect.Bottom);

                    string labelText = ((int)Math.Round(maxValue * ratio)).ToString();
                    SizeF size = graphics.MeasureString(labelText, labelFont);
                    graphics.DrawString(labelText, labelFont, labelBrush, x - (size.Width / 2f), plotRect.Bottom + 6f);
                }

                graphics.DrawLine(axisPen, plotRect.Left, plotRect.Top, plotRect.Left, plotRect.Bottom);
                graphics.DrawLine(axisPen, plotRect.Left, plotRect.Bottom, plotRect.Right, plotRect.Bottom);
            }
        }

        protected virtual void DrawBars(Graphics graphics, RectangleF plotRect, string[] currentCategories, LightningBarSeries[] currentSeries, float maxValue, LightningBarOptions currentOptions)
        {
            int categoryCount = currentCategories.Length;
            int seriesCount = currentSeries.Length;
            if (categoryCount <= 0 || seriesCount <= 0)
            {
                return;
            }

            float groupHeight = plotRect.Height / categoryCount;
            float safeRatio = Math.Max(0f, Math.Min(0.8f, currentOptions.GroupPaddingRatio));
            float usableGroupHeight = groupHeight * (1f - safeRatio);
            float barGap = Math.Max(0f, currentOptions.BarGap);
            float totalGapWidth = barGap * Math.Max(0, seriesCount - 1);
            float barHeight = (usableGroupHeight - totalGapWidth) / Math.Max(1, seriesCount);

            if (barHeight < 1f)
            {
                barHeight = 1f;
                barGap = 0f;
            }

            for (int categoryIndex = 0; categoryIndex < categoryCount; categoryIndex++)
            {
                float groupY = plotRect.Top + (groupHeight * categoryIndex) + ((groupHeight - usableGroupHeight) / 2f);

                for (int seriesIndex = 0; seriesIndex < seriesCount; seriesIndex++)
                {
                    LightningBarSeries barSeries = currentSeries[seriesIndex];
                    if (barSeries == null)
                    {
                        continue;
                    }

                    float value = 0f;
                    if (barSeries.Values != null && categoryIndex < barSeries.Values.Length)
                    {
                        value = Math.Max(0f, barSeries.Values[categoryIndex]);
                    }

                    float ratio = maxValue <= 0f ? 0f : Math.Min(1f, value / maxValue);
                    float barWidth = plotRect.Width * ratio;
                    float x = plotRect.Left;
                    float y = groupY + seriesIndex * (barHeight + barGap);
                    RectangleF barRect = new RectangleF(x, y, barWidth, barHeight);

                    using (var fillBrush = new SolidBrush(barSeries.FillColor))
                    using (var borderPen = new Pen(barSeries.BorderColor, currentOptions.BarBorderWidth))
                    {
                        graphics.FillRectangle(fillBrush, barRect);
                        graphics.DrawRectangle(borderPen, barRect.X, barRect.Y, barRect.Width, barRect.Height);
                    }
                }
            }
        }

        protected virtual void DrawCategoryLabels(Graphics graphics, RectangleF plotRect, string[] currentCategories, LightningBarOptions currentOptions)
        {
            using (var labelFont = new Font(Font.FontFamily, currentOptions.CategoryFontSize, FontStyle.Regular))
            using (var labelBrush = new SolidBrush(currentOptions.CategoryLabelColor))
            using (var rightCenterFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
            {
                float groupHeight = plotRect.Height / currentCategories.Length;
                float textRight = plotRect.Left - 8f;

                for (int i = 0; i < currentCategories.Length; i++)
                {
                    float centerY = plotRect.Top + (groupHeight * i) + (groupHeight / 2f);
                    RectangleF textRect = new RectangleF(0f, centerY - 12f, textRight, 24f);
                    graphics.DrawString(currentCategories[i], labelFont, labelBrush, textRect, rightCenterFormat);
                }
            }
        }

        protected virtual void DrawLegend(Graphics graphics, LightningBarSeries[] currentSeries, LightningBarOptions currentOptions, Size renderSize)
        {
            if (currentSeries == null || currentSeries.Length == 0)
            {
                return;
            }

            const float markerWidth = 20f;
            const float labelSpacing = 8f;
            const float sectionSpacing = 28f;

            using (var legendFont = new Font(Font.FontFamily, 9f, FontStyle.Regular))
            using (var textBrush = new SolidBrush(currentOptions.LegendTextColor))
            {
                float totalLegendWidth = 0f;
                foreach (LightningBarSeries barSeries in currentSeries)
                {
                    SizeF textSize = graphics.MeasureString(barSeries.Name ?? string.Empty, legendFont);
                    totalLegendWidth += markerWidth + labelSpacing + textSize.Width + sectionSpacing;
                }

                totalLegendWidth = Math.Max(0f, totalLegendWidth - sectionSpacing);
                float legendX = (renderSize.Width - totalLegendWidth) / 2f;
                float legendY = 48f;

                foreach (LightningBarSeries barSeries in currentSeries)
                {
                    SizeF textSize = graphics.MeasureString(barSeries.Name ?? string.Empty, legendFont);
                    DrawLegendItem(graphics, legendFont, textBrush, legendX, legendY, barSeries.FillColor, barSeries.BorderColor, barSeries.Name ?? string.Empty);
                    legendX += markerWidth + labelSpacing + textSize.Width + sectionSpacing;
                }
            }
        }

        protected virtual void DrawLegendItem(Graphics graphics, Font font, Brush textBrush, float x, float y, Color fillColor, Color borderColor, string text)
        {
            RectangleF markerRect = new RectangleF(x, y, 20f, 14f);

            using (var fillBrush = new SolidBrush(fillColor))
            using (var borderPen = new Pen(borderColor, 1.5f))
            {
                graphics.FillRectangle(fillBrush, markerRect);
                graphics.DrawRectangle(borderPen, markerRect.X, markerRect.Y, markerRect.Width, markerRect.Height);
            }

            graphics.DrawString(text, font, textBrush, x + 28f, y - 2f);
        }

        protected virtual void RefreshSafe()
        {
            ExecuteOnUiThread(this, Invalidate);
        }

        protected virtual void NormalizeSeries(string[] currentCategories, IList<LightningBarSeries> currentSeries)
        {
            int targetCount = currentCategories == null ? 0 : currentCategories.Length;

            if (targetCount <= 0 || currentSeries == null)
            {
                return;
            }

            foreach (LightningBarSeries barSeries in currentSeries)
            {
                if (barSeries == null)
                {
                    continue;
                }

                float[] values = barSeries.Values ?? new float[0];
                if (values.Length == targetCount)
                {
                    continue;
                }

                float[] normalized = new float[targetCount];
                Array.Copy(values, normalized, Math.Min(values.Length, targetCount));
                barSeries.Values = normalized;
            }
        }

        protected virtual Bitmap CreateChartBitmap(LightningRadarImageStorageOptions storageOptions)
        {
            int width = Math.Max(1, storageOptions.ImageWidth > 0 ? storageOptions.ImageWidth : Width);
            int height = Math.Max(1, storageOptions.ImageHeight > 0 ? storageOptions.ImageHeight : Height);

            string[] snapshotCategories;
            LightningBarSeries[] snapshotSeries;
            LightningBarOptions snapshotOptions;

            lock (syncRoot)
            {
                snapshotCategories = categories.ToArray();
                snapshotSeries = series.Select(item => item.Clone()).ToArray();
                snapshotOptions = options.Clone();
            }

            Bitmap bitmap = new Bitmap(width, height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(snapshotOptions.BackgroundColor);

                DrawTitle(graphics, snapshotOptions);
                DrawBarChart(graphics, snapshotCategories, snapshotSeries, snapshotOptions, new Size(width, height));
                DrawLegend(graphics, snapshotSeries, snapshotOptions, new Size(width, height));
            }

            return bitmap;
        }

        protected virtual void SaveBitmapToFile(Bitmap bitmap, string path, LightningRadarImageStorageOptions storageOptions)
        {
            ImageFormat imageFormat = GetDrawingImageFormat(storageOptions.ImageFormat);
            if (storageOptions.ImageFormat == LightningRadarImageFormat.Jpeg)
            {
                ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(item => item.FormatID == ImageFormat.Jpeg.Guid);
                if (codec != null)
                {
                    EncoderParameters parameters = new EncoderParameters(1);
                    long quality = Math.Max(1L, Math.Min(100L, storageOptions.JpegQuality));
                    parameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                    bitmap.Save(path, codec, parameters);
                    parameters.Dispose();
                    return;
                }
            }

            bitmap.Save(path, imageFormat);
        }

        protected virtual string GetImageFolderPath(LightningBarImagePathInfo pathInfo, LightningRadarImageStorageOptions storageOptions, bool createIfMissing)
        {
            if (pathInfo == null)
            {
                throw new ArgumentNullException("pathInfo");
            }

            LightningRadarImageStorageOptions effectiveOptions = storageOptions == null ? new LightningRadarImageStorageOptions() : storageOptions.Clone();
            string rootPath = ResolveRootPath(effectiveOptions);
            string folderPath = Path.Combine(
                rootPath,
                SanitizePathSegment(effectiveOptions.RootFolderName, "LightningBarImages"),
                SanitizePathSegment(pathInfo.FabId, "FAB_UNKNOWN"),
                SanitizePathSegment(pathInfo.LotCd, "LOT_UNKNOWN"),
                SanitizePathSegment(pathInfo.DraftNo, "DRAFT_UNKNOWN"));

            if (createIfMissing && !Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            return folderPath;
        }

        protected virtual string ResolveRootPath(LightningRadarImageStorageOptions storageOptions)
        {
            switch (storageOptions.RootType)
            {
                case LightningRadarStorageRootType.AppData:
                    return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                case LightningRadarStorageRootType.LocalAppData:
                    return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                case LightningRadarStorageRootType.Desktop:
                    return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                case LightningRadarStorageRootType.Custom:
                    if (string.IsNullOrWhiteSpace(storageOptions.CustomRootPath))
                    {
                        throw new InvalidOperationException("CustomRootPath 값이 필요합니다.");
                    }

                    return storageOptions.CustomRootPath;
                case LightningRadarStorageRootType.Documents:
                default:
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
        }

        protected virtual ImageFormat GetDrawingImageFormat(LightningRadarImageFormat imageFormat)
        {
            switch (imageFormat)
            {
                case LightningRadarImageFormat.Jpeg:
                    return ImageFormat.Jpeg;
                case LightningRadarImageFormat.Bmp:
                    return ImageFormat.Bmp;
                case LightningRadarImageFormat.Gif:
                    return ImageFormat.Gif;
                case LightningRadarImageFormat.Png:
                default:
                    return ImageFormat.Png;
            }
        }

        protected virtual string GetFileExtension(LightningRadarImageFormat imageFormat)
        {
            switch (imageFormat)
            {
                case LightningRadarImageFormat.Jpeg:
                    return ".jpg";
                case LightningRadarImageFormat.Bmp:
                    return ".bmp";
                case LightningRadarImageFormat.Gif:
                    return ".gif";
                case LightningRadarImageFormat.Png:
                default:
                    return ".png";
            }
        }

        protected virtual string SanitizePathSegment(string value, string fallback)
        {
            string text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                text = text.Replace(invalidChar, '_');
            }

            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }

        protected virtual void ExecuteOnUiThread(Control control, Action action)
        {
            ExecuteOnUiThread(control, action, false);
        }

        protected virtual void ExecuteOnUiThread(Control control, Action action, bool forceSynchronous)
        {
            if (action == null)
            {
                return;
            }

            if (control == null || control.IsDisposed)
            {
                return;
            }

            if (!control.IsHandleCreated)
            {
                action();
                return;
            }

            if (control.InvokeRequired)
            {
                if (forceSynchronous)
                {
                    control.Invoke(action);
                }
                else
                {
                    control.BeginInvoke(action);
                }

                return;
            }

            action();
        }
    }
}
