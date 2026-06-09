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
    public class LightningBarSeriesClickEventArgs : EventArgs
    {
        public LightningBarSeriesClickEventArgs(string categoryName, int categoryIndex, LightningBarSeries series, int seriesIndex, float value)
        {
            CategoryName = categoryName;
            CategoryIndex = categoryIndex;
            Series = series;
            SeriesIndex = seriesIndex;
            Value = value;
        }

        public string CategoryName { get; private set; }
        public int CategoryIndex { get; private set; }
        public LightningBarSeries Series { get; private set; }
        public int SeriesIndex { get; private set; }
        public float Value { get; private set; }
    }

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

        public string LegendLabel { get; set; }

        public float[] Values { get; set; }

        public Color FillColor { get; set; }

        public Color BorderColor { get; set; }

        public LightningBarSeries Clone()
        {
            return new LightningBarSeries
            {
                Name = Name,
                LegendLabel = LegendLabel,
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
            CategoryLabelMaxLines = 1;
            ScaleFontSize = 9f;
            ScaleLabelColor = Color.FromArgb(95, 95, 95);
            AxisColor = Color.FromArgb(170, 170, 170);
            GridColor = Color.FromArgb(225, 225, 225);
            LegendTextColor = Color.FromArgb(90, 90, 90);
            LegendFontSize = 8f;
            LegendMarkerWidth = 26f;
            LegendMarkerHeight = 18f;
            LegendTextMaxWidth = 120f;
            LegendTextMaxLines = 3;
            SeriesLabelEnabled = false;
            SeriesLabelFontSize = 8f;
            SeriesLabelColor = Color.FromArgb(95, 95, 95);
            SeriesLabelMaxWidth = 140f;
            SeriesLabelMaxLines = 3;
            SeriesTooltipEnabled = true;
            SeriesTooltipFormat = "Value:{2:0.#} (* 클릭할 경우 해당 계측 데이터 차트로 가 보입니다.)";
            NoDataText = "데이터가 없습니다.";
            NoDataTextColor = Color.FromArgb(120, 120, 120);
            NoDataFontSize = 11f;
            ShowNoDataMessage = true;
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
        public int CategoryLabelMaxLines { get; set; }
        public float ScaleFontSize { get; set; }
        public Color ScaleLabelColor { get; set; }
        public Color AxisColor { get; set; }
        public Color GridColor { get; set; }
        public Color LegendTextColor { get; set; }
        public float LegendFontSize { get; set; }
        public float LegendMarkerWidth { get; set; }
        public float LegendMarkerHeight { get; set; }
        public float LegendTextMaxWidth { get; set; }
        public int LegendTextMaxLines { get; set; }
        public bool SeriesLabelEnabled { get; set; }
        public float SeriesLabelFontSize { get; set; }
        public Color SeriesLabelColor { get; set; }
        public float SeriesLabelMaxWidth { get; set; }
        public int SeriesLabelMaxLines { get; set; }
        public bool SeriesTooltipEnabled { get; set; }
        public string SeriesTooltipFormat { get; set; }
        public string NoDataText { get; set; }
        public Color NoDataTextColor { get; set; }
        public float NoDataFontSize { get; set; }
        public bool ShowNoDataMessage { get; set; }
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
        private readonly ToolTip seriesToolTip = new ToolTip();
        private readonly List<LightningBarHitInfo> barHitInfos = new List<LightningBarHitInfo>();
        private string currentToolTipText = string.Empty;
        private bool collectBarHits;
        private bool hasBoundData;

        public event EventHandler<LightningBarSeriesClickEventArgs> SeriesClicked;

        public LightningBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            DoubleBuffered = true;
            BackColor = Color.White;
            Size = new Size(820, 540);
            seriesToolTip.InitialDelay = 150;
            seriesToolTip.ReshowDelay = 100;
            seriesToolTip.AutoPopDelay = 5000;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                seriesToolTip.Dispose();
            }

            base.Dispose(disposing);
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
                hasBoundData = true;
            }

            RefreshSafe();
        }

        public void Clear()
        {
            lock (syncRoot)
            {
                categories = new string[0];
                series = new List<LightningBarSeries>();
                hasBoundData = false;
            }

            barHitInfos.Clear();
            ExecuteOnUiThread(this, HideSeriesToolTip);
            RefreshSafe();
        }

        public void ClearData()
        {
            Clear();
        }

        public void Reset()
        {
            Clear();
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
                hasBoundData = true;
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
            bool snapshotHasBoundData;

            lock (syncRoot)
            {
                snapshotCategories = categories.ToArray();
                snapshotSeries = series.Select(item => item.Clone()).ToArray();
                snapshotOptions = options.Clone();
                snapshotHasBoundData = hasBoundData;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(snapshotOptions.BackgroundColor);

            if (!snapshotHasBoundData)
            {
                barHitInfos.Clear();
                return;
            }

            DrawTitle(e.Graphics, snapshotOptions);
            barHitInfos.Clear();
            if (!HasRenderableData(snapshotCategories, snapshotSeries))
            {
                DrawNoDataMessage(e.Graphics, snapshotOptions, ClientSize);
                return;
            }

            collectBarHits = true;
            try
            {
                DrawBarChart(e.Graphics, snapshotCategories, snapshotSeries, snapshotOptions, ClientSize);
            }
            finally
            {
                collectBarHits = false;
            }

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

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            UpdateSeriesToolTip(e.Location);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HideSeriesToolTip();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            LightningBarHitInfo hitInfo = FindBarHit(e.Location);
            if (hitInfo == null)
            {
                return;
            }

            OnSeriesClicked(new LightningBarSeriesClickEventArgs(
                hitInfo.CategoryName,
                hitInfo.CategoryIndex,
                hitInfo.Series.Clone(),
                hitInfo.SeriesIndex,
                hitInfo.Value));
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

        protected virtual bool HasRenderableData(string[] currentCategories, LightningBarSeries[] currentSeries)
        {
            if (currentCategories == null || currentCategories.Length == 0 || currentSeries == null || currentSeries.Length == 0)
            {
                return false;
            }

            return currentSeries.Any(item => item != null && item.Values != null && item.Values.Length > 0);
        }

        protected virtual void DrawNoDataMessage(Graphics graphics, LightningBarOptions currentOptions, Size renderSize)
        {
            if (!currentOptions.ShowNoDataMessage)
            {
                return;
            }

            RectangleF messageRect = GetPlotRectangle(renderSize, currentOptions);
            if (messageRect.Width <= 1f || messageRect.Height <= 1f)
            {
                messageRect = new RectangleF(0f, 0f, renderSize.Width, renderSize.Height);
            }

            using (var messageFont = new Font(Font.FontFamily, Math.Max(1f, currentOptions.NoDataFontSize), FontStyle.Regular))
            using (var messageBrush = new SolidBrush(currentOptions.NoDataTextColor))
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisWord;
                graphics.DrawString(currentOptions.NoDataText ?? string.Empty, messageFont, messageBrush, messageRect, format);
            }
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

                    DrawSeriesLabel(graphics, barSeries, barRect, currentOptions);

                    if (collectBarHits)
                    {
                        barHitInfos.Add(new LightningBarHitInfo
                        {
                            Bounds = barRect,
                            CategoryName = currentCategories[categoryIndex],
                            CategoryIndex = categoryIndex,
                            Series = barSeries.Clone(),
                            SeriesIndex = seriesIndex,
                            Value = value
                        });
                    }
                }
            }
        }

        protected virtual void DrawSeriesLabel(Graphics graphics, LightningBarSeries barSeries, RectangleF barRect, LightningBarOptions currentOptions)
        {
            if (!currentOptions.SeriesLabelEnabled || barSeries == null)
            {
                return;
            }

            string labelText = GetLegendLabel(barSeries);
            if (string.IsNullOrWhiteSpace(labelText))
            {
                return;
            }

            float maxTextWidth = Math.Max(1f, currentOptions.SeriesLabelMaxWidth);
            int maxLines = Math.Max(1, currentOptions.SeriesLabelMaxLines);

            using (var labelFont = new Font(Font.FontFamily, Math.Max(1f, currentOptions.SeriesLabelFontSize), FontStyle.Regular))
            using (var labelBrush = new SolidBrush(currentOptions.SeriesLabelColor))
            using (var format = new StringFormat())
            {
                float lineHeight = labelFont.GetHeight(graphics);
                float textHeight = lineHeight * maxLines;
                float textX = barRect.Right + 4f;
                float textY = barRect.Y + ((barRect.Height - textHeight) / 2f);
                RectangleF textRect = new RectangleF(textX, textY, maxTextWidth, textHeight);

                format.Alignment = StringAlignment.Near;
                format.LineAlignment = StringAlignment.Near;
                format.Trimming = StringTrimming.EllipsisWord;
                format.FormatFlags = StringFormatFlags.LineLimit;

                graphics.DrawString(labelText, labelFont, labelBrush, textRect, format);
            }
        }

        protected virtual void DrawCategoryLabels(Graphics graphics, RectangleF plotRect, string[] currentCategories, LightningBarOptions currentOptions)
        {
            using (var labelFont = new Font(Font.FontFamily, currentOptions.CategoryFontSize, FontStyle.Regular))
            using (var labelBrush = new SolidBrush(currentOptions.CategoryLabelColor))
            using (var rightTopFormat = new StringFormat())
            {
                int maxLines = Math.Max(1, currentOptions.CategoryLabelMaxLines);
                float lineHeight = labelFont.GetHeight(graphics);
                float groupHeight = plotRect.Height / currentCategories.Length;
                float textRight = plotRect.Left - 8f;

                rightTopFormat.Alignment = StringAlignment.Far;
                rightTopFormat.LineAlignment = StringAlignment.Near;
                rightTopFormat.Trimming = StringTrimming.EllipsisWord;
                rightTopFormat.FormatFlags = StringFormatFlags.LineLimit;

                for (int i = 0; i < currentCategories.Length; i++)
                {
                    string[] labelLines = GetCategoryLabelLines(currentCategories[i], maxLines);
                    float actualTextHeight = lineHeight * labelLines.Length;
                    float centerY = plotRect.Top + (groupHeight * i) + (groupHeight / 2f);
                    float textY = centerY - (actualTextHeight / 2f);

                    for (int lineIndex = 0; lineIndex < labelLines.Length; lineIndex++)
                    {
                        RectangleF lineRect = new RectangleF(0f, textY + (lineHeight * lineIndex), textRight, lineHeight + 2f);
                        graphics.DrawString(labelLines[lineIndex], labelFont, labelBrush, lineRect, rightTopFormat);
                    }
                }
            }
        }

        protected virtual string[] GetCategoryLabelLines(string text, int maxLines)
        {
            return GetLegendTextLines(text, maxLines);
        }

        protected virtual void DrawLegend(Graphics graphics, LightningBarSeries[] currentSeries, LightningBarOptions currentOptions, Size renderSize)
        {
            if (currentSeries == null || currentSeries.Length == 0)
            {
                return;
            }

            const float labelSpacing = 8f;
            const float sectionSpacing = 28f;
            float maxTextWidth = Math.Max(1f, currentOptions.LegendTextMaxWidth);
            float markerWidth = Math.Max(1f, currentOptions.LegendMarkerWidth);

            using (var legendFont = new Font(Font.FontFamily, Math.Max(1f, currentOptions.LegendFontSize), FontStyle.Regular))
            using (var textBrush = new SolidBrush(currentOptions.LegendTextColor))
            {
                float totalLegendWidth = 0f;
                foreach (LightningBarSeries barSeries in currentSeries)
                {
                    SizeF textSize = MeasureLegendText(graphics, GetLegendLabel(barSeries), legendFont, currentOptions);
                    float textWidth = Math.Min(maxTextWidth, textSize.Width);
                    totalLegendWidth += markerWidth + labelSpacing + textWidth + sectionSpacing;
                }

                totalLegendWidth = Math.Max(0f, totalLegendWidth - sectionSpacing);
                float legendX = (renderSize.Width - totalLegendWidth) / 2f;
                float legendY = 48f;

                foreach (LightningBarSeries barSeries in currentSeries)
                {
                    string legendText = GetLegendLabel(barSeries);
                    SizeF textSize = MeasureLegendText(graphics, legendText, legendFont, currentOptions);
                    float textWidth = Math.Min(maxTextWidth, textSize.Width);
                    DrawLegendItem(graphics, legendFont, textBrush, legendX, legendY, barSeries.FillColor, barSeries.BorderColor, legendText, currentOptions);
                    legendX += markerWidth + labelSpacing + textWidth + sectionSpacing;
                }
            }
        }

        protected virtual void DrawLegendItem(Graphics graphics, Font font, Brush textBrush, float x, float y, Color fillColor, Color borderColor, string text, LightningBarOptions currentOptions)
        {
            float markerWidth = Math.Max(1f, currentOptions.LegendMarkerWidth);
            float markerHeight = Math.Max(1f, currentOptions.LegendMarkerHeight);
            RectangleF markerRect = new RectangleF(x, y, markerWidth, markerHeight);

            using (var fillBrush = new SolidBrush(fillColor))
            using (var borderPen = new Pen(borderColor, 1.5f))
            using (var format = new StringFormat())
            {
                graphics.FillRectangle(fillBrush, markerRect);
                graphics.DrawRectangle(borderPen, markerRect.X, markerRect.Y, markerRect.Width, markerRect.Height);

                float maxTextWidth = Math.Max(1f, currentOptions.LegendTextMaxWidth);
                int maxLines = Math.Max(1, currentOptions.LegendTextMaxLines);
                float lineHeight = font.GetHeight(graphics);
                RectangleF textRect = new RectangleF(x + markerWidth + 8f, y - 3f, maxTextWidth, (lineHeight * maxLines) + 8f);

                format.Alignment = StringAlignment.Near;
                format.LineAlignment = StringAlignment.Near;
                format.Trimming = StringTrimming.EllipsisWord;
                format.FormatFlags = StringFormatFlags.LineLimit;

                string[] lines = GetLegendTextLines(text, maxLines);
                for (int i = 0; i < lines.Length; i++)
                {
                    RectangleF lineRect = new RectangleF(textRect.X, textRect.Y + (lineHeight * i), textRect.Width, lineHeight + 4f);
                    graphics.DrawString(lines[i], font, textBrush, lineRect, format);
                }
            }
        }

        protected virtual string GetLegendLabel(LightningBarSeries barSeries)
        {
            if (barSeries == null)
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(barSeries.LegendLabel)
                ? (barSeries.Name ?? string.Empty)
                : barSeries.LegendLabel;
        }

        protected virtual SizeF MeasureLegendText(Graphics graphics, string text, Font font, LightningBarOptions currentOptions)
        {
            float maxTextWidth = Math.Max(1f, currentOptions.LegendTextMaxWidth);
            int maxLines = Math.Max(1, currentOptions.LegendTextMaxLines);
            float maxTextHeight = (font.GetHeight(graphics) * maxLines) + 8f;
            string[] lines = GetLegendTextLines(text, maxLines);

            if (lines.Length > 1)
            {
                float maxWidth = 0f;
                foreach (string line in lines)
                {
                    SizeF lineSize = graphics.MeasureString(line ?? string.Empty, font);
                    maxWidth = Math.Max(maxWidth, lineSize.Width);
                }

                return new SizeF(Math.Min(maxTextWidth, maxWidth), maxTextHeight);
            }

            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Near;
                format.LineAlignment = StringAlignment.Near;
                format.Trimming = StringTrimming.EllipsisWord;
                format.FormatFlags = StringFormatFlags.LineLimit;
                return graphics.MeasureString(text ?? string.Empty, font, new SizeF(maxTextWidth, maxTextHeight), format);
            }
        }

        protected virtual string[] GetLegendTextLines(string text, int maxLines)
        {
            string normalizedText = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = normalizedText.Split(new[] { '\n' }, StringSplitOptions.None);
            int effectiveMaxLines = Math.Max(1, maxLines);

            if (lines.Length <= effectiveMaxLines)
            {
                return lines;
            }

            string[] limitedLines = lines.Take(effectiveMaxLines).ToArray();
            if (limitedLines.Length > 0 && !string.IsNullOrEmpty(limitedLines[limitedLines.Length - 1]))
            {
                limitedLines[limitedLines.Length - 1] = limitedLines[limitedLines.Length - 1] + "...";
            }

            return limitedLines;
        }

        protected virtual void UpdateSeriesToolTip(Point location)
        {
            LightningBarOptions currentOptions = Options;
            if (!currentOptions.SeriesTooltipEnabled)
            {
                HideSeriesToolTip();
                return;
            }

            LightningBarHitInfo hitInfo = FindBarHit(location);
            if (hitInfo == null)
            {
                HideSeriesToolTip();
                return;
            }

            string toolTipText = FormatSeriesToolTip(hitInfo, currentOptions);
            if (toolTipText == currentToolTipText)
            {
                return;
            }

            currentToolTipText = toolTipText;
            seriesToolTip.Show(toolTipText, this, location.X + 14, location.Y + 14);
        }

        protected virtual LightningBarHitInfo FindBarHit(Point location)
        {
            return barHitInfos.FirstOrDefault(item => item.Bounds.Contains(location));
        }

        protected virtual string FormatSeriesToolTip(LightningBarHitInfo hitInfo, LightningBarOptions currentOptions)
        {
            string format = string.IsNullOrWhiteSpace(currentOptions.SeriesTooltipFormat)
                ? "Value:{2:0.#} (* 클릭할 경우 해당 계측 데이터 차트로 가 보입니다.)"
                : currentOptions.SeriesTooltipFormat;

            try
            {
                return string.Format(
                    format,
                    GetLegendLabel(hitInfo.Series),
                    hitInfo.CategoryName,
                    hitInfo.Value,
                    hitInfo.SeriesIndex,
                    hitInfo.CategoryIndex);
            }
            catch (FormatException)
            {
                return string.Format("Value:{0:0.#}", hitInfo.Value);
            }
        }

        protected virtual void HideSeriesToolTip()
        {
            if (string.IsNullOrEmpty(currentToolTipText))
            {
                return;
            }

            currentToolTipText = string.Empty;
            seriesToolTip.Hide(this);
        }

        protected virtual void OnSeriesClicked(LightningBarSeriesClickEventArgs e)
        {
            EventHandler<LightningBarSeriesClickEventArgs> handler = SeriesClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected class LightningBarHitInfo
        {
            public RectangleF Bounds { get; set; }

            public string CategoryName { get; set; }

            public int CategoryIndex { get; set; }

            public LightningBarSeries Series { get; set; }

            public int SeriesIndex { get; set; }

            public float Value { get; set; }
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
            bool snapshotHasBoundData;

            lock (syncRoot)
            {
                snapshotCategories = categories.ToArray();
                snapshotSeries = series.Select(item => item.Clone()).ToArray();
                snapshotOptions = options.Clone();
                snapshotHasBoundData = hasBoundData;
            }

            Bitmap bitmap = new Bitmap(width, height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(snapshotOptions.BackgroundColor);

                if (!snapshotHasBoundData)
                {
                    return bitmap;
                }

                DrawTitle(graphics, snapshotOptions);
                if (!HasRenderableData(snapshotCategories, snapshotSeries))
                {
                    DrawNoDataMessage(graphics, snapshotOptions, new Size(width, height));
                }
                else
                {
                    DrawBarChart(graphics, snapshotCategories, snapshotSeries, snapshotOptions, new Size(width, height));
                    DrawLegend(graphics, snapshotSeries, snapshotOptions, new Size(width, height));
                }
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
