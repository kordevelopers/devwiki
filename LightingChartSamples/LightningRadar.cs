using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Arction.WinForms.Charting;
using Arction.WinForms.Charting.Annotations;
using Arction.WinForms.Charting.Axes;
using Arction.WinForms.Charting.SeriesXY;
using Arction.WinForms.Charting.Views.ViewXY;

namespace LightingChartSamples
{
    public enum LightningRadarLegendLabelLocation
    {
        TopLeft,
        TopCenter,
        TopRight
    }

    public enum LightningRadarScaleLabelDisplayMode
    {
        Auto,
        All,
        None
    }

    public enum LightningRadarScaleLabelValueMode
    {
        Percent0To100,
        RingIndex0ToGridRingCount
    }

    /// <summary>LightningChart의 선 패턴과 1:1로 대응하는 Radar 시리즈 선 모양입니다.</summary>
    public enum LightningRadarLinePattern
    {
        Solid,
        Dash,
        Dot,
        SmallDot,
        DashDot
    }

    /// <summary>Radar 시리즈 내부 영역 표시 방법입니다.</summary>
    public enum LightningRadarFillMode
    {
        Transparent,
        Color
    }

    public class LightningRadarSeries
    {
        public LightningRadarSeries()
        {
            Name = string.Empty;
            Values = new float[0];
            FillColor = Color.FromArgb(80, 91, 155, 213);
            LineColor = Color.FromArgb(230, 65, 105, 225);
            LinePattern = LightningRadarLinePattern.Solid;
            FillMode = LightningRadarFillMode.Transparent;
            LineWidth = 0f;
            ShowPoints = true;
        }

        public string Name { get; set; }
        public float[] Values { get; set; }
        public Color FillColor { get; set; }
        public Color LineColor { get; set; }

        /// <summary>기본값은 Solid입니다. 시리즈마다 Dash, Dot 등을 지정할 수 있습니다.</summary>
        public LightningRadarLinePattern LinePattern { get; set; }

        /// <summary>기본값은 Transparent입니다. Color이면 FillColor로 내부를 채웁니다.</summary>
        public LightningRadarFillMode FillMode { get; set; }

        /// <summary>0 이하면 LightningRadarOptions.SeriesLineWidth를 사용합니다.</summary>
        public float LineWidth { get; set; }

        public bool ShowPoints { get; set; }

        public LightningRadarSeries Clone()
        {
            return new LightningRadarSeries
            {
                Name = Name,
                Values = Values == null ? new float[0] : Values.ToArray(),
                FillColor = FillColor,
                LineColor = LineColor,
                LinePattern = LinePattern,
                FillMode = FillMode,
                LineWidth = LineWidth,
                ShowPoints = ShowPoints
            };
        }
    }

    public class LightningRadarOptions
    {
        public LightningRadarOptions()
        {
            Title = "Radar Chart Sample";
            TitleColor = Color.FromArgb(90, 90, 90);
            TitleFontSize = 12f;
            BackgroundColor = Color.White;
            ChartPadding = 8;
            LegendWidth = 180;
            TopOffset = 34;
            RadiusPadding = 2f;
            GridRingCount = 10;
            CategoryLabelOffset = 4f;
            TopCategoryLabelHorizontalOffset = 0f;
            TopCategoryLabelVerticalOffset = 0f;
            CategoryFontSize = 8.5f;
            CategoryLabelColor = Color.FromArgb(95, 95, 95);
            ScaleFontSize = 7f;
            ScaleLabelColor = Color.FromArgb(95, 95, 95);
            ScaleLabelDisplayMode = LightningRadarScaleLabelDisplayMode.Auto;
            ScaleLabelValueMode = LightningRadarScaleLabelValueMode.Percent0To100;
            MajorGridColor = Color.FromArgb(205, 205, 205);
            MinorGridColor = Color.FromArgb(230, 230, 230);
            SpokeColor = Color.FromArgb(225, 225, 225);
            LegendTextColor = Color.FromArgb(90, 90, 90);
            LegendItemSpacing = 18f;
            LegendLabelLocation = LightningRadarLegendLabelLocation.TopCenter;
            MarkerTooltipEnabled = true;
            MarkerTooltipHitRadius = 10f;
            MarkerTooltipFormat = "{0} / {1}: {2:0.#}";
            ShowTitle = false;
            ShowLegend = true;
            SeriesLineWidth = 2f;
            SeriesPointSize = 8f;
            ImageStorage = new LightningRadarImageStorageOptions();
        }

        public string Title { get; set; }
        public Color TitleColor { get; set; }
        public float TitleFontSize { get; set; }
        public Color BackgroundColor { get; set; }
        public int ChartPadding { get; set; }
        public int LegendWidth { get; set; }
        public int TopOffset { get; set; }
        public float RadiusPadding { get; set; }
        public int GridRingCount { get; set; }
        public float CategoryLabelOffset { get; set; }
        public float TopCategoryLabelHorizontalOffset { get; set; }
        public float TopCategoryLabelVerticalOffset { get; set; }
        public float CategoryFontSize { get; set; }
        public Color CategoryLabelColor { get; set; }
        public float ScaleFontSize { get; set; }
        public Color ScaleLabelColor { get; set; }
        public LightningRadarScaleLabelDisplayMode ScaleLabelDisplayMode { get; set; }
        public LightningRadarScaleLabelValueMode ScaleLabelValueMode { get; set; }
        public Color MajorGridColor { get; set; }
        public Color MinorGridColor { get; set; }
        public Color SpokeColor { get; set; }
        public Color LegendTextColor { get; set; }
        public float LegendItemSpacing { get; set; }
        public LightningRadarLegendLabelLocation LegendLabelLocation { get; set; }
        public bool MarkerTooltipEnabled { get; set; }
        public float MarkerTooltipHitRadius { get; set; }
        public string MarkerTooltipFormat { get; set; }
        public bool ShowTitle { get; set; }
        public bool ShowLegend { get; set; }
        public float SeriesLineWidth { get; set; }
        public float SeriesPointSize { get; set; }
        public LightningRadarImageStorageOptions ImageStorage { get; set; }

        public LightningRadarOptions Clone()
        {
            LightningRadarOptions clone = (LightningRadarOptions)MemberwiseClone();
            clone.ImageStorage = ImageStorage == null ? new LightningRadarImageStorageOptions() : ImageStorage.Clone();
            return clone;
        }
    }

    public enum LightningRadarStorageRootType
    {
        Documents,
        AppData,
        LocalAppData,
        Desktop,
        Custom
    }

    public enum LightningRadarImageFormat
    {
        Png,
        Jpeg,
        Bmp,
        Gif
    }

    public class LightningRadarImageStorageOptions
    {
        public LightningRadarImageStorageOptions()
        {
            RootType = LightningRadarStorageRootType.Documents;
            CustomRootPath = string.Empty;
            RootFolderName = "LightningRadarImages";
            ImageFormat = LightningRadarImageFormat.Png;
            ImageWidth = 1280;
            ImageHeight = 720;
            JpegQuality = 90L;
        }

        public LightningRadarStorageRootType RootType { get; set; }
        public string CustomRootPath { get; set; }
        public string RootFolderName { get; set; }
        public LightningRadarImageFormat ImageFormat { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public long JpegQuality { get; set; }

        public LightningRadarImageStorageOptions Clone()
        {
            return (LightningRadarImageStorageOptions)MemberwiseClone();
        }
    }

    public class LightningRadarImagePathInfo
    {
        public LightningRadarImagePathInfo()
        {
            FabId = string.Empty;
            LotCd = string.Empty;
            DraftNo = string.Empty;
            FileNamePrefix = "radar";
        }

        public string FabId { get; set; }
        public string LotCd { get; set; }
        public string DraftNo { get; set; }
        public string FileNamePrefix { get; set; }

        public LightningRadarImagePathInfo Clone()
        {
            return (LightningRadarImagePathInfo)MemberwiseClone();
        }
    }

    /// <summary>
    /// Arction LightningChart Ultimate 8.5 기반 Radar 차트입니다.
    /// 격자, 축, 데이터 외곽선, 마커 및 채움은 모두 LightningChart 시리즈로 렌더링합니다.
    /// </summary>
    public class LightningRadar : UserControl
    {
        private const double RadarRadius = 100d;
        private readonly object syncRoot = new object();
        private readonly LightningChartUltimate chart;
        private readonly ToolTip markerToolTip;
        private readonly List<RadarPointBinding> radarPointBindings = new List<RadarPointBinding>();
        private string[] categories = new string[0];
        private List<LightningRadarSeries> series = new List<LightningRadarSeries>();
        private LightningRadarOptions options = new LightningRadarOptions();
        private string currentToolTipText = string.Empty;
        private bool rebuildingChart;

        public LightningRadar()
        {
            Size = new Size(820, 540);
            BackColor = Color.White;
            chart = new LightningChartUltimate
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Font = new Font("맑은 고딕", 9f, FontStyle.Regular)
            };
            chart.ActiveView = ActiveView.ViewXY;
            chart.MouseMove += Chart_MouseMove;
            chart.MouseLeave += Chart_MouseLeave;
            chart.Resize += Chart_Resize;
            Controls.Add(chart);

            markerToolTip = new ToolTip
            {
                InitialDelay = 150,
                ReshowDelay = 100,
                AutoPopDelay = 5000
            };

            EnsureChartNodes();
            RebuildChart();
        }

        [Browsable(false)]
        public LightningChartUltimate Chart
        {
            get { return chart; }
        }

        [Browsable(false)]
        public string[] Categories
        {
            get { lock (syncRoot) { return categories.ToArray(); } }
        }

        [Browsable(false)]
        public LightningRadarSeries[] Series
        {
            get { lock (syncRoot) { return series.Select(item => item.Clone()).ToArray(); } }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public LightningRadarOptions Options
        {
            get { lock (syncRoot) { return options.Clone(); } }
        }

        public static T AttachTo<T>(Control parent, DockStyle dockStyle, Rectangle? bounds, LightningRadarOptions options)
            where T : LightningRadar, new()
        {
            T radar = new T();
            if (options != null) radar.SetOptions(options);
            if (bounds.HasValue) radar.Bounds = bounds.Value;
            radar.Dock = dockStyle;
            radar.AddTo(parent);
            return radar;
        }

        public static LightningRadar Create(Control parent, IEnumerable<string> newCategories, IEnumerable<LightningRadarSeries> newSeries, LightningRadarOptions options)
        {
            LightningRadar radar = AttachTo<LightningRadar>(parent, DockStyle.Fill, null, options);
            radar.SetData(newCategories, newSeries);
            return radar;
        }

        public void AddTo(Control parent)
        {
            if (parent == null) throw new ArgumentNullException("parent");
            ExecuteOnUiThread(parent, delegate
            {
                if (Parent != parent) parent.Controls.Add(this);
                BringToFront();
            }, false);
        }

        public void SetData(IEnumerable<string> newCategories, IEnumerable<LightningRadarSeries> newSeries)
        {
            string[] nextCategories = newCategories == null ? new string[0] : newCategories.ToArray();
            List<LightningRadarSeries> nextSeries = CloneSeries(newSeries);
            NormalizeSeries(nextCategories, nextSeries);
            lock (syncRoot)
            {
                categories = nextCategories;
                series = nextSeries;
            }
            RefreshSafe();
        }

        public void SetCategories(IEnumerable<string> newCategories) { SetData(newCategories, Series); }
        public void SetSeries(IEnumerable<LightningRadarSeries> newSeries) { SetData(Categories, newSeries); }

        public void UpdateData(IEnumerable<string> newCategories, IEnumerable<LightningRadarSeries> newSeries, LightningRadarOptions newOptions)
        {
            string[] nextCategories = newCategories == null ? new string[0] : newCategories.ToArray();
            List<LightningRadarSeries> nextSeries = CloneSeries(newSeries);
            NormalizeSeries(nextCategories, nextSeries);
            lock (syncRoot)
            {
                categories = nextCategories;
                series = nextSeries;
                options = newOptions == null ? new LightningRadarOptions() : newOptions.Clone();
            }
            RefreshSafe();
        }

        public void SetOptions(LightningRadarOptions newOptions)
        {
            if (newOptions == null) throw new ArgumentNullException("newOptions");
            lock (syncRoot) { options = newOptions.Clone(); }
            RefreshSafe();
        }

        public void Update(Action<LightningRadar> updateAction)
        {
            if (updateAction == null) throw new ArgumentNullException("updateAction");
            ExecuteOnUiThread(this, delegate { updateAction(this); }, false);
        }

        public virtual LightningRadarOptions CreateDefaultOptions() { return new LightningRadarOptions(); }

        public string SaveImage(LightningRadarImagePathInfo pathInfo)
        {
            return SaveImage(pathInfo, Options.ImageStorage);
        }

        public string SaveImage(LightningRadarImagePathInfo pathInfo, LightningRadarImageStorageOptions storageOptions)
        {
            if (pathInfo == null) throw new ArgumentNullException("pathInfo");
            LightningRadarImageStorageOptions effective = storageOptions == null ? new LightningRadarImageStorageOptions() : storageOptions.Clone();
            string folder = GetImageFolderPath(pathInfo, effective, true);
            string fileName = string.Format("{0}_{1:yyyyMMdd_HHmmssfff}{2}",
                SanitizePathSegment(pathInfo.FileNamePrefix, "radar"), DateTime.Now, GetFileExtension(effective.ImageFormat));
            string fullPath = Path.Combine(folder, fileName);
            ExecuteOnUiThread(this, delegate
            {
                bool saved = chart.SaveToFile(fullPath,
                    Math.Max(1, effective.ImageWidth > 0 ? effective.ImageWidth : Width),
                    Math.Max(1, effective.ImageHeight > 0 ? effective.ImageHeight : Height), true);
                if (!saved) throw new IOException("LightningChart 이미지 저장에 실패했습니다: " + fullPath);
            }, true);
            return fullPath;
        }

        public string GetImageFolderPath(LightningRadarImagePathInfo pathInfo)
        {
            return GetImageFolderPath(pathInfo, Options.ImageStorage, false);
        }

        public string[] FindImages(LightningRadarImagePathInfo pathInfo) { return FindImages(pathInfo, Options.ImageStorage); }

        public string[] FindImages(LightningRadarImagePathInfo pathInfo, LightningRadarImageStorageOptions storageOptions)
        {
            string folder = GetImageFolderPath(pathInfo, storageOptions, false);
            if (!Directory.Exists(folder)) return new string[0];
            string[] extensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
            return Directory.GetFiles(folder)
                .Where(file => extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(File.GetCreationTime).ToArray();
        }

        public string FindLatestImage(LightningRadarImagePathInfo pathInfo) { return FindLatestImage(pathInfo, Options.ImageStorage); }
        public string FindLatestImage(LightningRadarImagePathInfo pathInfo, LightningRadarImageStorageOptions storageOptions)
        {
            return FindImages(pathInfo, storageOptions).FirstOrDefault();
        }

        public int DeleteImagesCreatedAfter(LightningRadarImagePathInfo pathInfo, DateTime threshold)
        {
            return DeleteImagesCreatedAfter(pathInfo, threshold, Options.ImageStorage);
        }

        public int DeleteImagesCreatedAfter(LightningRadarImagePathInfo pathInfo, DateTime threshold, LightningRadarImageStorageOptions storageOptions)
        {
            int count = 0;
            foreach (string file in FindImages(pathInfo, storageOptions))
            {
                if (File.GetCreationTime(file) <= threshold) continue;
                File.Delete(file);
                count++;
            }
            return count;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                markerToolTip.Dispose();
                chart.MouseMove -= Chart_MouseMove;
                chart.MouseLeave -= Chart_MouseLeave;
                chart.Resize -= Chart_Resize;
                chart.Dispose();
            }
            base.Dispose(disposing);
        }

        private void EnsureChartNodes()
        {
            ViewXY view = chart.ViewXY;
            if (view.XAxes.Count == 0) view.XAxes.Add(new AxisX(view));
            if (view.YAxes.Count == 0) view.YAxes.Add(new AxisY(view));
            if (view.LegendBoxes.Count == 0) view.LegendBoxes.Add(new LegendBoxXY());
        }

        private void RebuildChart()
        {
            if (IsDisposed || chart.IsDisposed || rebuildingChart) return;
            rebuildingChart = true;
            string[] currentCategories;
            LightningRadarSeries[] currentSeries;
            LightningRadarOptions currentOptions;
            lock (syncRoot)
            {
                currentCategories = categories.ToArray();
                currentSeries = series.Select(item => item.Clone()).ToArray();
                currentOptions = options.Clone();
            }

            chart.BeginUpdate();
            try
            {
                EnsureChartNodes();
                ClearChartObjects();
                ApplyChartAppearance(currentOptions);
                ApplyAxisRange(currentOptions);
                AddGrid(currentCategories.Length, currentOptions);
                AddCategoryLabels(currentCategories, currentOptions);
                AddSeries(currentCategories, currentSeries, currentOptions);
                ApplyLegend(currentOptions);
            }
            finally
            {
                chart.EndUpdate();
                rebuildingChart = false;
            }
        }

        private void ClearChartObjects()
        {
            ViewXY view = chart.ViewXY;
            view.FreeformPointLineSeries.Clear();
            view.PolygonSeries.Clear();
            view.Annotations.Clear();
            radarPointBindings.Clear();
        }

        private void ApplyChartAppearance(LightningRadarOptions currentOptions)
        {
            Color background = currentOptions.BackgroundColor.IsEmpty ? Color.White : currentOptions.BackgroundColor;
            chart.ColorTheme = ColorTheme.LightGray;
            chart.BackColor = background;
            chart.Background.Color = background;
            chart.Background.GradientColor = background;
            chart.Background.GradientFill = GradientFill.Solid;
            chart.Background.Style = RectFillStyle.ColorOnly;
            chart.Title.Text = currentOptions.Title ?? string.Empty;
            chart.Title.Visible = currentOptions.ShowTitle && !string.IsNullOrWhiteSpace(currentOptions.Title);
            chart.Title.Font = new Font("맑은 고딕", Math.Max(1f, currentOptions.TitleFontSize), FontStyle.Bold);
            chart.Title.Color = currentOptions.TitleColor;

            ViewXY view = chart.ViewXY;
            view.GraphBackground.Color = background;
            view.GraphBackground.GradientColor = background;
            view.GraphBackground.GradientFill = GradientFill.Solid;
            view.GraphBackground.Style = RectFillStyle.ColorOnly;
            view.Border.Color = background;
            int padding = Math.Max(0, currentOptions.ChartPadding);
            view.Margins = new Padding(padding, Math.Max(padding, currentOptions.TopOffset), padding, padding);
            view.ZoomPanOptions.LeftMouseButtonAction = MouseButtonAction.None;
            view.ZoomPanOptions.RightMouseButtonAction = MouseButtonAction.None;
            view.ZoomPanOptions.MiddleMouseButtonAction = MouseButtonAction.None;
            view.ZoomPanOptions.MouseWheelZooming = MouseWheelZooming.Off;

            AxisX xAxis = view.XAxes[0];
            AxisY yAxis = view.YAxes[0];
            xAxis.Visible = false;
            yAxis.Visible = false;
            xAxis.LabelsVisible = false;
            yAxis.LabelsVisible = false;
            xAxis.MajorGrid.Visible = false;
            yAxis.MajorGrid.Visible = false;
            xAxis.MinorGrid.Visible = false;
            yAxis.MinorGrid.Visible = false;
        }

        private void ApplyAxisRange(LightningRadarOptions currentOptions)
        {
            double width = Math.Max(1d, chart.ClientSize.Width - (Math.Max(0, currentOptions.ChartPadding) * 2d));
            double height = Math.Max(1d, chart.ClientSize.Height - Math.Max(0, currentOptions.TopOffset) - Math.Max(0, currentOptions.ChartPadding));
            double aspect = width / height;
            double extent = RadarRadius + Math.Max(0d, currentOptions.CategoryLabelOffset) + 18d +
                Math.Max(0d, currentOptions.RadiusPadding);
            double xExtent = aspect >= 1d ? extent * aspect : extent;
            double yExtent = aspect >= 1d ? extent : extent / aspect;
            chart.ViewXY.XAxes[0].SetRange(-xExtent, xExtent);
            chart.ViewXY.YAxes[0].SetRange(-yExtent, yExtent);
        }

        private void AddGrid(int categoryCount, LightningRadarOptions currentOptions)
        {
            if (categoryCount < 3) return;
            int ringCount = Math.Max(1, currentOptions.GridRingCount);
            int skip = CalculateScaleLabelSkip(ringCount, currentOptions);

            if (currentOptions.ScaleLabelDisplayMode != LightningRadarScaleLabelDisplayMode.None)
            {
                AddTextAnnotation(FormatScaleLabel(0, ringCount, currentOptions), 0d, 0d,
                    currentOptions.ScaleFontSize, currentOptions.ScaleLabelColor,
                    AlignmentHorizontal.Center, AlignmentVertical.Center, FontStyle.Bold);
            }

            for (int ring = 1; ring <= ringCount; ring++)
            {
                double radius = RadarRadius * ring / ringCount;
                bool major = ring % 2 == 0 || ring == ringCount;
                Color color = major ? currentOptions.MajorGridColor : currentOptions.MinorGridColor;
                AddClosedLine(CreateCirclePoints(radius), color, major ? 1.4f : 1f, LinePattern.Solid, false, string.Empty);
                if (ShouldShowScaleLabel(ring, ringCount, skip, currentOptions))
                {
                    AddTextAnnotation(FormatScaleLabel(ring, ringCount, currentOptions), 0d, radius + 2d,
                        currentOptions.ScaleFontSize, currentOptions.ScaleLabelColor,
                        AlignmentHorizontal.Center, AlignmentVertical.Bottom, FontStyle.Bold);
                }
            }

            for (int i = 0; i < categoryCount; i++)
            {
                PointDouble2D point = GetRadarPoint(RadarRadius, i, categoryCount);
                AddOpenLine(new[] { new PointDouble2D(0d, 0d), point }, currentOptions.SpokeColor, 1f, LinePattern.Solid, false, string.Empty);
            }
        }

        private void AddCategoryLabels(string[] currentCategories, LightningRadarOptions currentOptions)
        {
            for (int i = 0; i < currentCategories.Length; i++)
            {
                PointDouble2D point = GetRadarPoint(RadarRadius + Math.Max(0f, currentOptions.CategoryLabelOffset), i, currentCategories.Length);
                if (i == 0)
                {
                    point.X += currentOptions.TopCategoryLabelHorizontalOffset;
                    point.Y += currentOptions.TopCategoryLabelVerticalOffset;
                }
                double angle = -Math.PI / 2d + (Math.PI * 2d * i / currentCategories.Length);
                double cos = Math.Cos(angle);
                double sin = Math.Sin(angle);
                AlignmentHorizontal horizontal = cos > 0.25d ? AlignmentHorizontal.Left :
                    (cos < -0.25d ? AlignmentHorizontal.Right : AlignmentHorizontal.Center);
                AlignmentVertical vertical = sin > 0.25d ? AlignmentVertical.Top :
                    (sin < -0.25d ? AlignmentVertical.Bottom : AlignmentVertical.Center);
                AddTextAnnotation(currentCategories[i] ?? string.Empty, point.X, point.Y,
                    currentOptions.CategoryFontSize, currentOptions.CategoryLabelColor,
                    horizontal, vertical, FontStyle.Regular);
            }
        }

        private void AddSeries(string[] currentCategories, LightningRadarSeries[] currentSeries, LightningRadarOptions currentOptions)
        {
            for (int seriesIndex = 0; seriesIndex < currentSeries.Length; seriesIndex++)
            {
                LightningRadarSeries source = currentSeries[seriesIndex] ?? new LightningRadarSeries();
                PointDouble2D[] points = new PointDouble2D[currentCategories.Length];
                for (int i = 0; i < currentCategories.Length; i++)
                {
                    double value = source.Values == null || i >= source.Values.Length ? 0d : source.Values[i];
                    value = Math.Max(0d, Math.Min(100d, value));
                    points[i] = GetRadarPoint(RadarRadius * value / 100d, i, currentCategories.Length);
                    radarPointBindings.Add(new RadarPointBinding(points[i], source.Name, currentCategories[i], (float)value, source.LineColor));
                }

                if (points.Length < 3) continue;
                if (source.FillMode == LightningRadarFillMode.Color)
                {
                    PolygonSeries polygon = new PolygonSeries(chart.ViewXY, chart.ViewXY.XAxes[0], chart.ViewXY.YAxes[0]);
                    polygon.Points = points;
                    polygon.Fill.Style = RectFillStyle.ColorOnly;
                    polygon.Fill.Color = source.FillColor;
                    polygon.BorderVisible = false;
                    polygon.ShowInLegendBox = false;
                    polygon.MouseInteraction = false;
                    chart.ViewXY.PolygonSeries.Add(polygon);
                }

                float lineWidth = source.LineWidth > 0f ? source.LineWidth : Math.Max(0.5f, currentOptions.SeriesLineWidth);
                FreeformPointLineSeries line = AddClosedLine(points, source.LineColor, lineWidth,
                    ConvertLinePattern(source.LinePattern), true, source.Name ?? string.Empty);
                line.PointsVisible = source.ShowPoints;
                line.PointStyle.Width = Math.Max(1f, currentOptions.SeriesPointSize);
                line.PointStyle.Height = Math.Max(1f, currentOptions.SeriesPointSize);
                line.PointStyle.Shape = Shape.Circle;
                line.PointStyle.Color1 = source.LineColor;
                line.PointStyle.Color2 = source.LineColor;
                line.PointStyle.BorderColor = source.LineColor;
                line.PointStyle.BorderWidth = 1f;
                line.PointStyle.Antialiasing = true;
            }
        }

        private FreeformPointLineSeries AddClosedLine(PointDouble2D[] points, Color color, float width, LinePattern pattern, bool showLegend, string title)
        {
            SeriesPoint[] closed = new SeriesPoint[points.Length + 1];
            for (int i = 0; i < points.Length; i++) closed[i] = new SeriesPoint(points[i].X, points[i].Y);
            closed[closed.Length - 1] = new SeriesPoint(points[0].X, points[0].Y);
            return AddLine(closed, color, width, pattern, showLegend, title);
        }

        private FreeformPointLineSeries AddOpenLine(PointDouble2D[] points, Color color, float width, LinePattern pattern, bool showLegend, string title)
        {
            return AddLine(points.Select(point => new SeriesPoint(point.X, point.Y)).ToArray(), color, width, pattern, showLegend, title);
        }

        private FreeformPointLineSeries AddLine(SeriesPoint[] points, Color color, float width, LinePattern pattern, bool showLegend, string title)
        {
            ViewXY view = chart.ViewXY;
            FreeformPointLineSeries line = new FreeformPointLineSeries(view, view.XAxes[0], view.YAxes[0]);
            line.Points = points;
            line.LineVisible = true;
            line.PointsVisible = false;
            line.LineStyle.Color = color;
            line.LineStyle.Width = Math.Max(0.5f, width);
            line.LineStyle.Pattern = pattern;
            line.LineStyle.PatternScale = 1;
            line.Title.Text = title;
            line.ShowInLegendBox = showLegend;
            line.MouseInteraction = false;
            view.FreeformPointLineSeries.Add(line);
            return line;
        }

        private void AddTextAnnotation(string text, double x, double y, float fontSize, Color color,
            AlignmentHorizontal horizontal, AlignmentVertical vertical, FontStyle fontStyle)
        {
            ViewXY view = chart.ViewXY;
            AnnotationXY annotation = new AnnotationXY(view, view.XAxes[0], view.YAxes[0]);
            annotation.Text = text;
            annotation.LocationCoordinateSystem = CoordinateSystem.AxisValues;
            annotation.LocationAxisValues = new PointDoubleXY(x, y);
            annotation.Sizing = AnnotationXYSizing.Automatic;
            annotation.Fill.Style = RectFillStyle.None;
            annotation.BorderVisible = false;
            annotation.TextStyle.Font = new Font("맑은 고딕", Math.Max(1f, fontSize), fontStyle);
            annotation.TextStyle.Color = color;
            annotation.TextStyle.HorizAlign = horizontal;
            annotation.TextStyle.MultiLineTextHorizontalAlign = horizontal;
            annotation.TextStyle.VerticalAlign = vertical;
            annotation.MouseInteraction = false;
            view.Annotations.Add(annotation);
        }

        private void ApplyLegend(LightningRadarOptions currentOptions)
        {
            LegendBoxXY legend = chart.ViewXY.LegendBoxes[0];
            legend.Visible = currentOptions.ShowLegend;
            legend.Position = ConvertLegendPosition(currentOptions.LegendLabelLocation);
            legend.Layout = LegendBoxLayout.Horizontal;
            legend.AutoSize = true;
            legend.SeriesTitleFont = new Font("맑은 고딕", 9f, FontStyle.Regular);
            legend.SeriesTitleColor = currentOptions.LegendTextColor;
            legend.UseSeriesTitlesColors = true;
            legend.Fill.Style = RectFillStyle.None;
            legend.BorderWidth = 0;
            legend.Shadow.Visible = false;
            legend.MoveByMouse = false;
            legend.MoveFromSeriesTitle = false;
            legend.AllowMouseResize = false;
        }

        private void Chart_MouseMove(object sender, MouseEventArgs e)
        {
            LightningRadarOptions currentOptions = Options;
            if (!currentOptions.MarkerTooltipEnabled || radarPointBindings.Count == 0) { HideToolTip(); return; }
            double bestDistance = double.MaxValue;
            RadarPointBinding best = null;
            foreach (RadarPointBinding binding in radarPointBindings)
            {
                float x = chart.ViewXY.XAxes[0].ValueToCoord(binding.Point.X, false);
                float y = chart.ViewXY.YAxes[0].ValueToCoord(binding.Point.Y, false);
                double dx = x - e.X;
                double dy = y - e.Y;
                double distance = (dx * dx) + (dy * dy);
                if (distance < bestDistance) { bestDistance = distance; best = binding; }
            }
            double hitRadius = Math.Max(2d, currentOptions.MarkerTooltipHitRadius);
            if (best == null || bestDistance > hitRadius * hitRadius) { HideToolTip(); return; }
            string format = string.IsNullOrWhiteSpace(currentOptions.MarkerTooltipFormat) ? "{0} / {1}: {2:0.#}" : currentOptions.MarkerTooltipFormat;
            string text;
            try { text = string.Format(format, best.SeriesName, best.CategoryName, best.Value); }
            catch (FormatException) { text = string.Format("{0} / {1}: {2:0.#}", best.SeriesName, best.CategoryName, best.Value); }
            if (text == currentToolTipText) return;
            currentToolTipText = text;
            markerToolTip.Show(text, chart, e.X + 14, e.Y + 14);
        }

        private void Chart_MouseLeave(object sender, EventArgs e) { HideToolTip(); }
        private void Chart_Resize(object sender, EventArgs e) { RebuildChart(); }
        private void HideToolTip()
        {
            if (string.IsNullOrEmpty(currentToolTipText)) return;
            currentToolTipText = string.Empty;
            markerToolTip.Hide(chart);
        }

        private static PointDouble2D[] CreateRadarPoints(double radius, int count)
        {
            PointDouble2D[] points = new PointDouble2D[count];
            for (int i = 0; i < count; i++) points[i] = GetRadarPoint(radius, i, count);
            return points;
        }

        private static PointDouble2D[] CreateCirclePoints(double radius)
        {
            const int SegmentCount = 120;
            PointDouble2D[] points = new PointDouble2D[SegmentCount];
            for (int i = 0; i < SegmentCount; i++)
            {
                double angle = Math.PI * 2d * i / SegmentCount;
                points[i] = new PointDouble2D(Math.Cos(angle) * radius, Math.Sin(angle) * radius);
            }
            return points;
        }

        private static PointDouble2D GetRadarPoint(double radius, int index, int count)
        {
            double angle = -Math.PI / 2d + (Math.PI * 2d * index / count);
            return new PointDouble2D(Math.Cos(angle) * radius, -Math.Sin(angle) * radius);
        }

        private int CalculateScaleLabelSkip(int ringCount, LightningRadarOptions currentOptions)
        {
            if (currentOptions.ScaleLabelDisplayMode == LightningRadarScaleLabelDisplayMode.All) return 1;
            using (Font font = new Font("맑은 고딕", Math.Max(1f, currentOptions.ScaleFontSize), FontStyle.Bold))
            {
                float labelHeight = font.GetHeight() + 4f;
                float radiusPixels = Math.Max(1f, Math.Min(chart.ClientSize.Width, chart.ClientSize.Height) * 0.38f);
                return Math.Max(1, (int)Math.Ceiling(labelHeight / Math.Max(0.5f, radiusPixels / ringCount)));
            }
        }

        private static bool ShouldShowScaleLabel(int ring, int ringCount, int skip, LightningRadarOptions currentOptions)
        {
            if (currentOptions.ScaleLabelDisplayMode == LightningRadarScaleLabelDisplayMode.None) return false;
            return ring == ringCount || ring % Math.Max(1, skip) == 0;
        }

        private static string FormatScaleLabel(int ring, int ringCount, LightningRadarOptions currentOptions)
        {
            if (currentOptions.ScaleLabelValueMode == LightningRadarScaleLabelValueMode.RingIndex0ToGridRingCount)
            {
                return ring.ToString();
            }

            return Math.Round(ring * (100d / Math.Max(1, ringCount))).ToString("0");
        }

        private static LinePattern ConvertLinePattern(LightningRadarLinePattern pattern)
        {
            switch (pattern)
            {
                case LightningRadarLinePattern.Dash: return LinePattern.Dash;
                case LightningRadarLinePattern.Dot: return LinePattern.Dot;
                case LightningRadarLinePattern.SmallDot: return LinePattern.SmallDot;
                case LightningRadarLinePattern.DashDot: return LinePattern.DashDot;
                default: return LinePattern.Solid;
            }
        }

        private static LegendBoxPositionXY ConvertLegendPosition(LightningRadarLegendLabelLocation location)
        {
            switch (location)
            {
                case LightningRadarLegendLabelLocation.TopLeft: return LegendBoxPositionXY.TopLeft;
                case LightningRadarLegendLabelLocation.TopRight: return LegendBoxPositionXY.TopRight;
                default: return LegendBoxPositionXY.TopCenter;
            }
        }

        private static List<LightningRadarSeries> CloneSeries(IEnumerable<LightningRadarSeries> source)
        {
            return source == null ? new List<LightningRadarSeries>() :
                source.Select(item => item == null ? new LightningRadarSeries() : item.Clone()).ToList();
        }

        private static void NormalizeSeries(string[] currentCategories, IList<LightningRadarSeries> currentSeries)
        {
            int count = currentCategories == null ? 0 : currentCategories.Length;
            foreach (LightningRadarSeries item in currentSeries)
            {
                float[] source = item.Values ?? new float[0];
                if (source.Length == count) continue;
                float[] normalized = new float[count];
                Array.Copy(source, normalized, Math.Min(source.Length, count));
                item.Values = normalized;
            }
        }

        private string GetImageFolderPath(LightningRadarImagePathInfo pathInfo, LightningRadarImageStorageOptions storageOptions, bool create)
        {
            if (pathInfo == null) throw new ArgumentNullException("pathInfo");
            LightningRadarImageStorageOptions effective = storageOptions == null ? new LightningRadarImageStorageOptions() : storageOptions.Clone();
            string folder = Path.Combine(ResolveRootPath(effective),
                SanitizePathSegment(effective.RootFolderName, "LightningRadarImages"),
                SanitizePathSegment(pathInfo.FabId, "FAB_UNKNOWN"),
                SanitizePathSegment(pathInfo.LotCd, "LOT_UNKNOWN"),
                SanitizePathSegment(pathInfo.DraftNo, "DRAFT_UNKNOWN"));
            if (create && !Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        private static string ResolveRootPath(LightningRadarImageStorageOptions storageOptions)
        {
            switch (storageOptions.RootType)
            {
                case LightningRadarStorageRootType.AppData: return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                case LightningRadarStorageRootType.LocalAppData: return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                case LightningRadarStorageRootType.Desktop: return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                case LightningRadarStorageRootType.Custom:
                    if (string.IsNullOrWhiteSpace(storageOptions.CustomRootPath)) throw new InvalidOperationException("CustomRootPath 값이 필요합니다.");
                    return storageOptions.CustomRootPath;
                default: return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
        }

        private static string GetFileExtension(LightningRadarImageFormat format)
        {
            switch (format)
            {
                case LightningRadarImageFormat.Jpeg: return ".jpg";
                case LightningRadarImageFormat.Bmp: return ".bmp";
                case LightningRadarImageFormat.Gif: return ".gif";
                default: return ".png";
            }
        }

        private static string SanitizePathSegment(string value, string fallback)
        {
            string text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars()) text = text.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }

        private void RefreshSafe()
        {
            ExecuteOnUiThread(this, RebuildChart, false);
        }

        private static void ExecuteOnUiThread(Control control, Action action, bool synchronous)
        {
            if (control == null || control.IsDisposed || action == null) return;
            if (!control.IsHandleCreated || !control.InvokeRequired) { action(); return; }
            if (synchronous) control.Invoke(action); else control.BeginInvoke(action);
        }

        private sealed class RadarPointBinding
        {
            public RadarPointBinding(PointDouble2D point, string seriesName, string categoryName, float value, Color color)
            {
                Point = point;
                SeriesName = seriesName ?? string.Empty;
                CategoryName = categoryName ?? string.Empty;
                Value = value;
                Color = color;
            }
            public PointDouble2D Point { get; private set; }
            public string SeriesName { get; private set; }
            public string CategoryName { get; private set; }
            public float Value { get; private set; }
            public Color Color { get; private set; }
        }
    }
}
