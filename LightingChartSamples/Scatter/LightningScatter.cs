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

namespace LightingChartSamples.Scatter
{
    public enum LightningScatterLegendPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    public enum LightningScatterImageFileFormat
    {
        Png,
        Jpeg
    }

    public enum LightningScatterImageSaveFolder
    {
        LocalApplicationData,
        RoamingApplicationData,
        MyDocuments,
        Temp
    }

    public class LightningScatterPoint
    {
        public LightningScatterPoint()
        {
            Tag = null;
        }

        public LightningScatterPoint(double x, double y)
            : this(x, y, null)
        {
        }

        public LightningScatterPoint(double x, double y, object tag)
        {
            X = x;
            Y = y;
            Tag = tag;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public object Tag { get; set; }

        public LightningScatterPoint Clone()
        {
            return new LightningScatterPoint(X, Y, Tag);
        }
    }

    public class LightningScatterSeries
    {
        public LightningScatterSeries()
        {
            Name = string.Empty;
            LegendLabel = string.Empty;
            Points = new List<LightningScatterPoint>();
            PointColor = Color.FromArgb(76, 132, 210);
            LineColor = Color.FromArgb(76, 132, 210);
            PointSize = 8f;
            LineWidth = 1.5f;
            ShowLine = false;
            ShowPoints = true;
        }

        public string Name { get; set; }
        public string LegendLabel { get; set; }
        public IList<LightningScatterPoint> Points { get; set; }
        public Color PointColor { get; set; }
        public Color LineColor { get; set; }
        public float PointSize { get; set; }
        public float LineWidth { get; set; }
        public bool ShowLine { get; set; }
        public bool ShowPoints { get; set; }

        public LightningScatterSeries Clone()
        {
            return new LightningScatterSeries
            {
                Name = Name,
                LegendLabel = LegendLabel,
                Points = Points == null
                    ? new List<LightningScatterPoint>()
                    : Points.Select(point => point == null ? new LightningScatterPoint() : point.Clone()).ToList(),
                PointColor = PointColor,
                LineColor = LineColor,
                PointSize = PointSize,
                LineWidth = LineWidth,
                ShowLine = ShowLine,
                ShowPoints = ShowPoints
            };
        }
    }

    public class LightningScatterAxisOptions
    {
        public LightningScatterAxisOptions()
        {
            Title = string.Empty;
            Minimum = 0d;
            Maximum = 100d;
            AutoFit = true;
            MajorDivCount = 5;
            LabelFormat = "0.##";
            LabelColor = Color.FromArgb(95, 95, 95);
            AxisColor = Color.FromArgb(170, 170, 170);
            GridColor = Color.FromArgb(225, 225, 225);
            FontSize = 8f;
        }

        public string Title { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public bool AutoFit { get; set; }
        public int MajorDivCount { get; set; }
        public string LabelFormat { get; set; }
        public Color LabelColor { get; set; }
        public Color AxisColor { get; set; }
        public Color GridColor { get; set; }
        public float FontSize { get; set; }

        public LightningScatterAxisOptions Clone()
        {
            return (LightningScatterAxisOptions)MemberwiseClone();
        }
    }

    public class LightningScatterLegendOptions
    {
        public LightningScatterLegendOptions()
        {
            Visible = true;
            Position = LightningScatterLegendPosition.TopRight;
            FontSize = 8f;
            TextColor = Color.FromArgb(90, 90, 90);
            ShowCheckboxes = false;
            ShowIcons = true;
        }

        public bool Visible { get; set; }
        public LightningScatterLegendPosition Position { get; set; }
        public float FontSize { get; set; }
        public Color TextColor { get; set; }
        public bool ShowCheckboxes { get; set; }
        public bool ShowIcons { get; set; }

        public LightningScatterLegendOptions Clone()
        {
            return (LightningScatterLegendOptions)MemberwiseClone();
        }
    }

    public class LightningScatterTooltipOptions
    {
        public LightningScatterTooltipOptions()
        {
            Enabled = true;
            Format = "{0}\r\nX:{1:0.###}, Y:{2:0.###}";
            HitPixelTolerance = 12;
        }

        public bool Enabled { get; set; }
        public string Format { get; set; }
        public int HitPixelTolerance { get; set; }

        public LightningScatterTooltipOptions Clone()
        {
            return (LightningScatterTooltipOptions)MemberwiseClone();
        }
    }

    public class LightningScatterNoDataOptions
    {
        public LightningScatterNoDataOptions()
        {
            Text = "데이터가 없습니다.";
            ShowWhenDataMissing = true;
            ShowWhenAllValuesZero = false;
            FontSize = 11f;
            TextColor = Color.FromArgb(138, 118, 30);
            BadgeBackColor = Color.FromArgb(255, 249, 196);
            BadgeBorderColor = Color.FromArgb(240, 206, 84);
            BadgeWidthRatio = 0.8f;
            BadgeHeight = 58f;
        }

        public string Text { get; set; }
        public bool ShowWhenDataMissing { get; set; }
        public bool ShowWhenAllValuesZero { get; set; }
        public float FontSize { get; set; }
        public Color TextColor { get; set; }
        public Color BadgeBackColor { get; set; }
        public Color BadgeBorderColor { get; set; }
        public float BadgeWidthRatio { get; set; }
        public float BadgeHeight { get; set; }

        public LightningScatterNoDataOptions Clone()
        {
            return (LightningScatterNoDataOptions)MemberwiseClone();
        }
    }

    public class LightningScatterImageOptions
    {
        public LightningScatterImageOptions()
        {
            Width = 600;
            Height = 400;
            FileFormat = LightningScatterImageFileFormat.Png;
            SaveFolder = LightningScatterImageSaveFolder.LocalApplicationData;
            SaveDirectory = string.Empty;
            SubDirectoryName = "LightningScatterImages";
            UseDateFolder = true;
            UseGuidFileName = true;
            FileName = string.Empty;
        }

        public int Width { get; set; }
        public int Height { get; set; }
        public LightningScatterImageFileFormat FileFormat { get; set; }
        public LightningScatterImageSaveFolder SaveFolder { get; set; }
        public string SaveDirectory { get; set; }
        public string SubDirectoryName { get; set; }
        public bool UseDateFolder { get; set; }
        public bool UseGuidFileName { get; set; }
        public string FileName { get; set; }

        public LightningScatterImageOptions Clone()
        {
            return (LightningScatterImageOptions)MemberwiseClone();
        }
    }

    public class LightningScatterOptions
    {
        public LightningScatterOptions()
        {
            Title = string.Empty;
            ShowTitle = false;
            BackgroundColor = Color.White;
            GraphBackgroundColor = Color.White;
            XAxis = new LightningScatterAxisOptions { Title = "X" };
            YAxis = new LightningScatterAxisOptions { Title = "Y" };
            Legend = new LightningScatterLegendOptions();
            Tooltip = new LightningScatterTooltipOptions();
            NoData = new LightningScatterNoDataOptions();
            Image = new LightningScatterImageOptions();
        }

        public string Title { get; set; }
        public bool ShowTitle { get; set; }
        public Color BackgroundColor { get; set; }
        public Color GraphBackgroundColor { get; set; }
        public LightningScatterAxisOptions XAxis { get; set; }
        public LightningScatterAxisOptions YAxis { get; set; }
        public LightningScatterLegendOptions Legend { get; set; }
        public LightningScatterTooltipOptions Tooltip { get; set; }
        public LightningScatterNoDataOptions NoData { get; set; }
        public LightningScatterImageOptions Image { get; set; }

        public LightningScatterOptions Clone()
        {
            return new LightningScatterOptions
            {
                Title = Title,
                ShowTitle = ShowTitle,
                BackgroundColor = BackgroundColor,
                GraphBackgroundColor = GraphBackgroundColor,
                XAxis = XAxis == null ? new LightningScatterAxisOptions() : XAxis.Clone(),
                YAxis = YAxis == null ? new LightningScatterAxisOptions() : YAxis.Clone(),
                Legend = Legend == null ? new LightningScatterLegendOptions() : Legend.Clone(),
                Tooltip = Tooltip == null ? new LightningScatterTooltipOptions() : Tooltip.Clone(),
                NoData = NoData == null ? new LightningScatterNoDataOptions() : NoData.Clone(),
                Image = Image == null ? new LightningScatterImageOptions() : Image.Clone()
            };
        }
    }

    public class LightningScatterPointClickEventArgs : EventArgs
    {
        public LightningScatterPointClickEventArgs(LightningScatterSeries series, int seriesIndex, LightningScatterPoint point, int pointIndex)
        {
            Series = series == null ? new LightningScatterSeries() : series.Clone();
            SeriesIndex = seriesIndex;
            Point = point == null ? new LightningScatterPoint() : point.Clone();
            PointIndex = pointIndex;
        }

        public LightningScatterSeries Series { get; private set; }
        public int SeriesIndex { get; private set; }
        public LightningScatterPoint Point { get; private set; }
        public int PointIndex { get; private set; }
    }

    public class LightningScatterLegendClickEventArgs : EventArgs
    {
        public LightningScatterLegendClickEventArgs(LightningScatterSeries series, int seriesIndex, string legendLabel)
        {
            Series = series == null ? new LightningScatterSeries() : series.Clone();
            SeriesIndex = seriesIndex;
            LegendLabel = legendLabel ?? string.Empty;
        }

        public LightningScatterSeries Series { get; private set; }
        public int SeriesIndex { get; private set; }
        public string LegendLabel { get; private set; }
    }

    public class LightningScatterImageSavingEventArgs : EventArgs
    {
        public LightningScatterImageSavingEventArgs(string imagePath, LightningScatterImageOptions imageOptions)
        {
            ImagePath = imagePath ?? string.Empty;
            ImageOptions = imageOptions == null ? new LightningScatterImageOptions() : imageOptions.Clone();
        }

        public string ImagePath { get; private set; }
        public LightningScatterImageOptions ImageOptions { get; private set; }
        public bool IsFileSave { get { return !string.IsNullOrWhiteSpace(ImagePath); } }
    }

    public class LightningScatterImageSavedEventArgs : EventArgs
    {
        public LightningScatterImageSavedEventArgs(string imagePath, LightningScatterImageOptions imageOptions)
        {
            ImagePath = imagePath ?? string.Empty;
            ImageOptions = imageOptions == null ? new LightningScatterImageOptions() : imageOptions.Clone();
        }

        public string ImagePath { get; private set; }
        public LightningScatterImageOptions ImageOptions { get; private set; }
        public bool IsFileSave { get { return !string.IsNullOrWhiteSpace(ImagePath); } }
    }

    internal sealed class ScatterSeriesBinding
    {
        public ScatterSeriesBinding(PointLineSeries chartSeries, LightningScatterSeries sourceSeries, int seriesIndex)
        {
            ChartSeries = chartSeries;
            SourceSeries = sourceSeries;
            SeriesIndex = seriesIndex;
        }

        public PointLineSeries ChartSeries { get; private set; }
        public LightningScatterSeries SourceSeries { get; private set; }
        public int SeriesIndex { get; private set; }
    }

    public class LightningScatter : UserControl
    {
        public const string DefaultChartFontName = "맑은 고딕";

        private readonly LightningChartUltimate chart;
        private readonly ToolTip pointToolTip = new ToolTip();
        private readonly Dictionary<PointLineSeries, ScatterSeriesBinding> seriesBindings =
            new Dictionary<PointLineSeries, ScatterSeriesBinding>();
        private readonly object syncRoot = new object();
        private LightningScatterOptions options = new LightningScatterOptions();
        private List<LightningScatterSeries> series = new List<LightningScatterSeries>();
        private AnnotationXY noDataAnnotation;
        private Image lastSavedImage;
        private string lastSavedImagePath = string.Empty;
        private string currentToolTipText = string.Empty;
        private bool legendClickAttached;
        private bool isCleared = true;

        public event EventHandler<LightningScatterPointClickEventArgs> PointClicked;
        public event EventHandler<LightningScatterLegendClickEventArgs> LegendClicked;
        public event EventHandler<LightningScatterImageSavingEventArgs> ImageSaving;
        public event EventHandler<LightningScatterImageSavedEventArgs> ImageSaved;

        public LightningScatter()
        {
            Font = new Font(DefaultChartFontName, 9F, FontStyle.Regular);
            BackColor = Color.White;
            Size = new Size(600, 400);

            chart = new LightningChartUltimate
            {
                Dock = DockStyle.Fill,
                Font = new Font(DefaultChartFontName, 9F, FontStyle.Regular)
            };
            chart.MouseMove += Chart_MouseMove;
            chart.MouseLeave += delegate { HidePointToolTip(); };

            pointToolTip.InitialDelay = 150;
            pointToolTip.ReshowDelay = 100;
            pointToolTip.AutoPopDelay = 5000;

            Controls.Add(chart);
            InitializeChart();
        }

        [Browsable(false)]
        public LightningChartUltimate Chart
        {
            get { return chart; }
        }

        [Browsable(false)]
        public LightningScatterOptions Options
        {
            get
            {
                lock (syncRoot)
                {
                    return options.Clone();
                }
            }
        }

        [Browsable(false)]
        public LightningScatterSeries[] Series
        {
            get
            {
                lock (syncRoot)
                {
                    return series.Select(item => item.Clone()).ToArray();
                }
            }
        }

        [Browsable(false)]
        public string LastSavedImagePath
        {
            get
            {
                lock (syncRoot)
                {
                    return lastSavedImagePath;
                }
            }
        }

        [Browsable(false)]
        public Image LastSavedImage
        {
            get { return GetLastSavedImage(); }
        }

        public static LightningScatter Create(Control parent, IEnumerable<LightningScatterSeries> newSeries, LightningScatterOptions scatterOptions)
        {
            LightningScatter scatter = new LightningScatter();
            if (scatterOptions != null)
            {
                scatter.SetOptions(scatterOptions);
            }

            scatter.SetData(newSeries);
            scatter.Dock = DockStyle.Fill;
            if (parent != null)
            {
                parent.Controls.Add(scatter);
                scatter.BringToFront();
            }

            return scatter;
        }

        public static LightningScatter Create(Control parent, IEnumerable<LightningScatterPoint> points, LightningScatterOptions scatterOptions)
        {
            return Create(parent, new[]
            {
                new LightningScatterSeries
                {
                    Name = "Series 1",
                    LegendLabel = "Series 1",
                    Points = points == null ? new List<LightningScatterPoint>() : points.ToList()
                }
            }, scatterOptions);
        }

        public void SetOptions(LightningScatterOptions scatterOptions)
        {
            lock (syncRoot)
            {
                options = scatterOptions == null ? new LightningScatterOptions() : scatterOptions.Clone();
            }

            RebuildChart();
        }

        public void SetData(IEnumerable<LightningScatterSeries> newSeries)
        {
            lock (syncRoot)
            {
                series = newSeries == null
                    ? new List<LightningScatterSeries>()
                    : newSeries.Select(item => item == null ? new LightningScatterSeries() : item.Clone()).ToList();
                isCleared = false;
            }

            RebuildChart();
        }

        public void UpdateData(IEnumerable<LightningScatterSeries> newSeries, LightningScatterOptions scatterOptions)
        {
            lock (syncRoot)
            {
                options = scatterOptions == null ? new LightningScatterOptions() : scatterOptions.Clone();
                series = newSeries == null
                    ? new List<LightningScatterSeries>()
                    : newSeries.Select(item => item == null ? new LightningScatterSeries() : item.Clone()).ToList();
                isCleared = false;
            }

            RebuildChart();
        }

        public void Clear()
        {
            lock (syncRoot)
            {
                series = new List<LightningScatterSeries>();
                isCleared = true;
                ClearSavedImage();
            }

            RebuildChart();
        }

        public string SaveImage()
        {
            return SaveImage(Options.Image);
        }

        public string SaveImage(LightningScatterImageOptions imageOptions)
        {
            LightningScatterImageOptions effectiveOptions = imageOptions == null
                ? new LightningScatterImageOptions()
                : imageOptions.Clone();
            string fullPath = ResolveImageFilePath(effectiveOptions);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            OnImageSaving(new LightningScatterImageSavingEventArgs(fullPath, effectiveOptions));
            bool saved = chart.SaveToFile(
                fullPath,
                Math.Max(1, effectiveOptions.Width),
                Math.Max(1, effectiveOptions.Height),
                true);
            if (!saved)
            {
                throw new InvalidOperationException("LightningChart image save failed.");
            }

            using (Image image = LoadImage(fullPath))
            {
                StoreSavedImage(fullPath, image);
            }

            OnImageSaved(new LightningScatterImageSavedEventArgs(fullPath, effectiveOptions));
            return fullPath;
        }

        public Image GetLastSavedImage()
        {
            lock (syncRoot)
            {
                return lastSavedImage == null ? null : CloneImage(lastSavedImage);
            }
        }

        public Image LoadLastSavedImage()
        {
            string path;
            lock (syncRoot)
            {
                path = lastSavedImagePath;
            }

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return GetLastSavedImage();
            }

            using (Image image = LoadImage(path))
            {
                StoreSavedImage(path, image);
            }

            return GetLastSavedImage();
        }

        public void ClearSavedImage()
        {
            lock (syncRoot)
            {
                if (lastSavedImage != null)
                {
                    lastSavedImage.Dispose();
                    lastSavedImage = null;
                }

                lastSavedImagePath = string.Empty;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pointToolTip.Dispose();
                ClearSavedImage();
                chart.MouseMove -= Chart_MouseMove;
                if (legendClickAttached && chart.ViewXY.LegendBoxes.Count > 0)
                {
                    chart.ViewXY.LegendBoxes[0].SeriesTitleMouseClick -= LegendBox_SeriesTitleMouseClick;
                    legendClickAttached = false;
                }

                chart.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeChart()
        {
            chart.BeginUpdate();
            try
            {
                chart.ActiveView = ActiveView.ViewXY;
                EnsureAxes();
                EnsureLegendBox();
                EnsureNoDataAnnotation();
            }
            finally
            {
                chart.EndUpdate();
            }
        }

        private void RebuildChart()
        {
            LightningScatterOptions snapshotOptions;
            List<LightningScatterSeries> snapshotSeries;
            bool snapshotIsCleared;

            lock (syncRoot)
            {
                snapshotOptions = options.Clone();
                snapshotSeries = series.Select(item => item.Clone()).ToList();
                snapshotIsCleared = isCleared;
            }

            chart.BeginUpdate();
            try
            {
                ApplyChartOptions(snapshotOptions);
                ApplySeries(snapshotSeries, snapshotOptions);
                ApplyAxesRange(snapshotSeries, snapshotOptions);
                ApplyNoDataState(snapshotSeries, snapshotOptions, snapshotIsCleared);
            }
            finally
            {
                chart.EndUpdate();
            }
        }

        private void ApplyChartOptions(LightningScatterOptions currentOptions)
        {
            chart.Font = CreateChartFont(9f, FontStyle.Regular);
            chart.Background.Color = currentOptions.BackgroundColor;
            chart.Title.Text = currentOptions.Title ?? string.Empty;
            chart.Title.Visible = currentOptions.ShowTitle && !string.IsNullOrWhiteSpace(currentOptions.Title);
            chart.Title.Font = CreateChartFont(12f, FontStyle.Bold);
            chart.Title.Color = Color.FromArgb(90, 90, 90);

            ViewXY view = chart.ViewXY;
            view.GraphBackground.Color = currentOptions.GraphBackgroundColor;
            view.GraphBackground.Style = RectFillStyle.ColorOnly;
            view.Border.Color = Color.FromArgb(210, 210, 210);
            view.Margins = new Padding(70, 48, 22, 48);

            ApplyAxisOptions(GetXAxis(), currentOptions.XAxis);
            ApplyAxisOptions(GetYAxis(), currentOptions.YAxis);
            ApplyLegendOptions(GetLegendBox(), currentOptions.Legend);
        }

        private void ApplyAxisOptions(AxisBase axis, LightningScatterAxisOptions axisOptions)
        {
            LightningScatterAxisOptions effectiveOptions = axisOptions ?? new LightningScatterAxisOptions();
            axis.AxisColor = effectiveOptions.AxisColor;
            axis.LabelsColor = effectiveOptions.LabelColor;
            axis.LabelsFont = CreateChartFont(effectiveOptions.FontSize, FontStyle.Regular);
            axis.LabelsNumberFormat = effectiveOptions.LabelFormat ?? "0.##";
            axis.MajorDivCount = Math.Max(1, effectiveOptions.MajorDivCount);
            axis.MajorGrid.Visible = true;
            axis.MajorGrid.Color = effectiveOptions.GridColor;
            axis.MinorGrid.Visible = false;
            axis.AutoDivSpacing = false;
        }

        private void ApplyAxisOptions(AxisX axis, LightningScatterAxisOptions axisOptions)
        {
            ApplyAxisOptions((AxisBase)axis, axisOptions);
            LightningScatterAxisOptions effectiveOptions = axisOptions ?? new LightningScatterAxisOptions();
            axis.Title.Text = effectiveOptions.Title ?? string.Empty;
            axis.Title.Visible = !string.IsNullOrWhiteSpace(effectiveOptions.Title);
            axis.Title.Font = CreateChartFont(9f, FontStyle.Bold);
        }

        private void ApplyAxisOptions(AxisY axis, LightningScatterAxisOptions axisOptions)
        {
            ApplyAxisOptions((AxisBase)axis, axisOptions);
            LightningScatterAxisOptions effectiveOptions = axisOptions ?? new LightningScatterAxisOptions();
            axis.Title.Text = effectiveOptions.Title ?? string.Empty;
            axis.Title.Visible = !string.IsNullOrWhiteSpace(effectiveOptions.Title);
            axis.Title.Font = CreateChartFont(9f, FontStyle.Bold);
        }

        private void ApplyLegendOptions(LegendBoxXY legendBox, LightningScatterLegendOptions legendOptions)
        {
            LightningScatterLegendOptions effectiveOptions = legendOptions ?? new LightningScatterLegendOptions();
            legendBox.Visible = effectiveOptions.Visible;
            legendBox.Position = ConvertLegendPosition(effectiveOptions.Position);
            legendBox.Layout = LegendBoxLayout.Horizontal;
            legendBox.AutoSize = true;
            legendBox.SeriesTitleFont = CreateChartFont(effectiveOptions.FontSize, FontStyle.Regular);
            legendBox.SeriesTitleColor = effectiveOptions.TextColor;
            legendBox.ShowCheckboxes = effectiveOptions.ShowCheckboxes;
            legendBox.ShowIcons = effectiveOptions.ShowIcons;
            legendBox.MouseInteraction = true;
            legendBox.Fill.Color = Color.FromArgb(245, 245, 245);
            legendBox.Fill.Style = RectFillStyle.ColorOnly;
            legendBox.BorderColor = Color.FromArgb(190, 190, 190);
        }

        private void ApplySeries(IList<LightningScatterSeries> currentSeries, LightningScatterOptions currentOptions)
        {
            ViewXY view = chart.ViewXY;
            foreach (PointLineSeries existingSeries in view.PointLineSeries.ToArray())
            {
                existingSeries.MouseClick -= PointSeries_MouseClick;
            }

            view.PointLineSeries.Clear();
            seriesBindings.Clear();

            AxisX xAxis = GetXAxis();
            AxisY yAxis = GetYAxis();
            for (int i = 0; i < currentSeries.Count; i++)
            {
                LightningScatterSeries sourceSeries = currentSeries[i] ?? new LightningScatterSeries();
                PointLineSeries chartSeries = new PointLineSeries(view, xAxis, yAxis);
                chartSeries.Title.Text = GetLegendLabel(sourceSeries, i);
                chartSeries.ShowInLegendBox = true;
                chartSeries.PointsVisible = sourceSeries.ShowPoints;
                chartSeries.LineVisible = sourceSeries.ShowLine;
                chartSeries.LineStyle.Color = sourceSeries.LineColor;
                chartSeries.LineStyle.Width = Math.Max(0.5f, sourceSeries.LineWidth);
                chartSeries.PointStyle.Shape = Shape.Circle;
                chartSeries.PointStyle.Width = Math.Max(1f, sourceSeries.PointSize);
                chartSeries.PointStyle.Height = Math.Max(1f, sourceSeries.PointSize);
                chartSeries.PointStyle.Color1 = sourceSeries.PointColor;
                chartSeries.PointStyle.Color2 = sourceSeries.PointColor;
                chartSeries.PointStyle.BorderColor = sourceSeries.LineColor;
                chartSeries.PointStyle.BorderWidth = 1f;
                chartSeries.MouseInteraction = true;
                chartSeries.CursorTrackEnabled = true;
                chartSeries.MouseClick += PointSeries_MouseClick;
                chartSeries.Points = ToSeriesPoints(sourceSeries).ToArray();
                view.PointLineSeries.Add(chartSeries);
                seriesBindings[chartSeries] = new ScatterSeriesBinding(chartSeries, sourceSeries.Clone(), i);
            }
        }

        private IEnumerable<SeriesPoint> ToSeriesPoints(LightningScatterSeries sourceSeries)
        {
            if (sourceSeries == null || sourceSeries.Points == null)
            {
                yield break;
            }

            foreach (LightningScatterPoint point in sourceSeries.Points)
            {
                if (point == null)
                {
                    continue;
                }

                yield return new SeriesPoint(point.X, point.Y, point.Tag);
            }
        }

        private void ApplyAxesRange(IList<LightningScatterSeries> currentSeries, LightningScatterOptions currentOptions)
        {
            AxisX xAxis = GetXAxis();
            AxisY yAxis = GetYAxis();

            double xMin;
            double xMax;
            ResolveRange(
                currentSeries.SelectMany(item => item.Points == null ? Enumerable.Empty<LightningScatterPoint>() : item.Points).Select(point => point.X),
                currentOptions.XAxis,
                out xMin,
                out xMax);

            double yMin;
            double yMax;
            ResolveRange(
                currentSeries.SelectMany(item => item.Points == null ? Enumerable.Empty<LightningScatterPoint>() : item.Points).Select(point => point.Y),
                currentOptions.YAxis,
                out yMin,
                out yMax);

            xAxis.SetRange(xMin, xMax);
            yAxis.SetRange(yMin, yMax);
        }

        private void ResolveRange(IEnumerable<double> values, LightningScatterAxisOptions axisOptions, out double minimum, out double maximum)
        {
            LightningScatterAxisOptions effectiveOptions = axisOptions ?? new LightningScatterAxisOptions();
            List<double> cleanValues = values.Where(value => !double.IsNaN(value) && !double.IsInfinity(value)).ToList();
            if (effectiveOptions.AutoFit && cleanValues.Count > 0)
            {
                minimum = cleanValues.Min();
                maximum = cleanValues.Max();
                if (Math.Abs(maximum - minimum) < 0.000001d)
                {
                    minimum -= 1d;
                    maximum += 1d;
                }

                double padding = (maximum - minimum) * 0.08d;
                minimum -= padding;
                maximum += padding;
                return;
            }

            minimum = effectiveOptions.Minimum;
            maximum = effectiveOptions.Maximum;
            if (maximum <= minimum)
            {
                maximum = minimum + 1d;
            }
        }

        private void ApplyNoDataState(IList<LightningScatterSeries> currentSeries, LightningScatterOptions currentOptions, bool chartCleared)
        {
            EnsureNoDataAnnotation();
            if (chartCleared)
            {
                noDataAnnotation.Visible = false;
                return;
            }

            LightningScatterNoDataOptions noDataOptions = currentOptions.NoData ?? new LightningScatterNoDataOptions();
            bool hasRenderableData = HasRenderableData(currentSeries);
            bool allValuesZero = hasRenderableData && AreAllValuesZero(currentSeries);
            bool showNoData = (!hasRenderableData && noDataOptions.ShowWhenDataMissing)
                || (allValuesZero && noDataOptions.ShowWhenAllValuesZero);

            noDataAnnotation.Visible = showNoData && !string.IsNullOrWhiteSpace(noDataOptions.Text);
            noDataAnnotation.Text = noDataOptions.Text ?? string.Empty;
            noDataAnnotation.TextStyle.Font = CreateChartFont(noDataOptions.FontSize, FontStyle.Regular);
            noDataAnnotation.TextStyle.Color = noDataOptions.TextColor;
            noDataAnnotation.Fill.Color = noDataOptions.BadgeBackColor;
            noDataAnnotation.Fill.Style = RectFillStyle.ColorOnly;
            noDataAnnotation.BorderLineStyle.Color = noDataOptions.BadgeBorderColor;
            noDataAnnotation.BorderVisible = true;
            noDataAnnotation.CornerRoundRadius = 8;
            noDataAnnotation.SizeScreenCoords = new SizeFloatXY(
                Math.Max(160f, Math.Min(520f, Math.Max(0.1f, Math.Min(1f, noDataOptions.BadgeWidthRatio)) * Math.Max(1, Width))),
                Math.Max(40f, noDataOptions.BadgeHeight));
            AxisX xAxis = GetXAxis();
            AxisY yAxis = GetYAxis();
            noDataAnnotation.LocationAxisValues = new PointDoubleXY(
                xAxis.Minimum + ((xAxis.Maximum - xAxis.Minimum) / 2d),
                yAxis.Minimum + ((yAxis.Maximum - yAxis.Minimum) / 2d));
        }

        private bool HasRenderableData(IEnumerable<LightningScatterSeries> currentSeries)
        {
            return currentSeries != null
                && currentSeries.Any(item => item != null && item.Points != null && item.Points.Count > 0);
        }

        private bool AreAllValuesZero(IEnumerable<LightningScatterSeries> currentSeries)
        {
            foreach (LightningScatterSeries scatterSeries in currentSeries)
            {
                if (scatterSeries == null || scatterSeries.Points == null)
                {
                    continue;
                }

                foreach (LightningScatterPoint point in scatterSeries.Points)
                {
                    if (point != null && Math.Abs(point.Y) > 0.000001d)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void EnsureAxes()
        {
            ViewXY view = chart.ViewXY;
            if (view.XAxes.Count == 0)
            {
                view.XAxes.Add(new AxisX(view));
            }

            if (view.YAxes.Count == 0)
            {
                view.YAxes.Add(new AxisY(view));
            }
        }

        private void EnsureLegendBox()
        {
            ViewXY view = chart.ViewXY;
            if (view.LegendBoxes.Count == 0)
            {
                view.LegendBoxes.Add(new LegendBoxXY());
            }

            if (!legendClickAttached)
            {
                view.LegendBoxes[0].SeriesTitleMouseClick += LegendBox_SeriesTitleMouseClick;
                legendClickAttached = true;
            }
        }

        private void EnsureNoDataAnnotation()
        {
            if (noDataAnnotation != null)
            {
                return;
            }

            ViewXY view = chart.ViewXY;
            noDataAnnotation = new AnnotationXY(view, GetXAxis(), GetYAxis());
            noDataAnnotation.Visible = false;
            noDataAnnotation.Style = AnnotationStyle.RoundedRectangle;
            noDataAnnotation.LocationCoordinateSystem = CoordinateSystem.AxisValues;
            noDataAnnotation.Sizing = AnnotationXYSizing.ScreenCoordinates;
            noDataAnnotation.TargetCoordinateSystem = AnnotationTargetCoordinates.AxisValues;
            noDataAnnotation.TextStyle.HorizAlign = AlignmentHorizontal.Center;
            noDataAnnotation.TextStyle.MultiLineTextHorizontalAlign = AlignmentHorizontal.Center;
            noDataAnnotation.TextStyle.VerticalAlign = AlignmentVertical.Center;
            noDataAnnotation.MouseInteraction = false;
            view.Annotations.Add(noDataAnnotation);
        }

        private AxisX GetXAxis()
        {
            EnsureAxes();
            return chart.ViewXY.XAxes[0];
        }

        private AxisY GetYAxis()
        {
            EnsureAxes();
            return chart.ViewXY.YAxes[0];
        }

        private LegendBoxXY GetLegendBox()
        {
            EnsureLegendBox();
            return chart.ViewXY.LegendBoxes[0];
        }

        private Font CreateChartFont(float size, FontStyle style)
        {
            return new Font(DefaultChartFontName, Math.Max(1f, size), style);
        }

        private LegendBoxPositionXY ConvertLegendPosition(LightningScatterLegendPosition position)
        {
            switch (position)
            {
                case LightningScatterLegendPosition.TopLeft:
                    return LegendBoxPositionXY.TopLeft;
                case LightningScatterLegendPosition.TopCenter:
                    return LegendBoxPositionXY.TopCenter;
                case LightningScatterLegendPosition.BottomLeft:
                    return LegendBoxPositionXY.BottomLeft;
                case LightningScatterLegendPosition.BottomCenter:
                    return LegendBoxPositionXY.BottomCenter;
                case LightningScatterLegendPosition.BottomRight:
                    return LegendBoxPositionXY.BottomRight;
                case LightningScatterLegendPosition.TopRight:
                default:
                    return LegendBoxPositionXY.TopRight;
            }
        }

        private string GetLegendLabel(LightningScatterSeries sourceSeries, int seriesIndex)
        {
            if (sourceSeries == null)
            {
                return string.Format("Series {0}", seriesIndex + 1);
            }

            if (!string.IsNullOrWhiteSpace(sourceSeries.LegendLabel))
            {
                return sourceSeries.LegendLabel;
            }

            return string.IsNullOrWhiteSpace(sourceSeries.Name)
                ? string.Format("Series {0}", seriesIndex + 1)
                : sourceSeries.Name;
        }

        private void PointSeries_MouseClick(object sender, MouseEventArgs e)
        {
            PointLineSeries chartSeries = sender as PointLineSeries;
            if (chartSeries == null || !seriesBindings.ContainsKey(chartSeries))
            {
                return;
            }

            NearestPointHit hit;
            if (!TrySolveNearestPoint(chartSeries, e.Location, Options.Tooltip.HitPixelTolerance, out hit))
            {
                return;
            }

            ScatterSeriesBinding binding = seriesBindings[chartSeries];
            OnPointClicked(new LightningScatterPointClickEventArgs(
                binding.SourceSeries,
                binding.SeriesIndex,
                CreateHitPoint(binding, hit),
                hit.PointIndex));
        }

        private void LegendBox_SeriesTitleMouseClick(object sender, Arction.WinForms.Charting.Views.SeriesTitleMouseActionEventArgs e)
        {
            PointLineSeries chartSeries = e.Series as PointLineSeries;
            if (chartSeries == null || !seriesBindings.ContainsKey(chartSeries))
            {
                return;
            }

            ScatterSeriesBinding binding = seriesBindings[chartSeries];
            OnLegendClicked(new LightningScatterLegendClickEventArgs(
                binding.SourceSeries,
                binding.SeriesIndex,
                GetLegendLabel(binding.SourceSeries, binding.SeriesIndex)));
        }

        private void Chart_MouseMove(object sender, MouseEventArgs e)
        {
            LightningScatterOptions currentOptions = Options;
            if (currentOptions.Tooltip == null || !currentOptions.Tooltip.Enabled)
            {
                HidePointToolTip();
                return;
            }

            NearestPointHit bestHit;
            ScatterSeriesBinding bestBinding;
            if (!TrySolveNearestPoint(e.Location, currentOptions.Tooltip.HitPixelTolerance, out bestHit, out bestBinding))
            {
                HidePointToolTip();
                return;
            }

            string text = FormatToolTip(bestBinding, bestHit, currentOptions.Tooltip);
            if (string.Equals(currentToolTipText, text, StringComparison.Ordinal))
            {
                return;
            }

            currentToolTipText = text;
            pointToolTip.Show(text, chart, e.X + 14, e.Y + 14);
        }

        private bool TrySolveNearestPoint(Point location, int tolerance, out NearestPointHit bestHit, out ScatterSeriesBinding bestBinding)
        {
            bestHit = null;
            bestBinding = null;
            double bestDistance = double.MaxValue;

            foreach (KeyValuePair<PointLineSeries, ScatterSeriesBinding> pair in seriesBindings)
            {
                NearestPointHit hit;
                if (!TrySolveNearestPoint(pair.Key, location, tolerance, out hit))
                {
                    continue;
                }

                if (hit.Distance < bestDistance)
                {
                    bestDistance = hit.Distance;
                    bestHit = hit;
                    bestBinding = pair.Value;
                }
            }

            return bestHit != null && bestBinding != null;
        }

        private bool TrySolveNearestPoint(PointLineSeries chartSeries, Point location, int tolerance, out NearestPointHit hit)
        {
            hit = null;
            double x;
            double y;
            int index;
            if (!chartSeries.SolveNearestDataPointByCoord(location.X, location.Y, out x, out y, out index))
            {
                return false;
            }

            if (index < 0)
            {
                return false;
            }

            AxisX xAxis = GetXAxis();
            AxisY yAxis = GetYAxis();
            float xCoord = xAxis.ValueToCoord(x, false);
            float yCoord = yAxis.ValueToCoord(y, false);
            double dx = xCoord - location.X;
            double dy = yCoord - location.Y;
            double distance = Math.Sqrt((dx * dx) + (dy * dy));
            if (distance > Math.Max(1, tolerance))
            {
                return false;
            }

            hit = new NearestPointHit
            {
                X = x,
                Y = y,
                PointIndex = index,
                Distance = distance
            };
            return true;
        }

        private string FormatToolTip(ScatterSeriesBinding binding, NearestPointHit hit, LightningScatterTooltipOptions tooltipOptions)
        {
            string format = string.IsNullOrWhiteSpace(tooltipOptions.Format)
                ? "{0}\r\nX:{1:0.###}, Y:{2:0.###}"
                : tooltipOptions.Format;

            try
            {
                object tag = GetHitTag(binding, hit);
                return string.Format(
                    format,
                    GetLegendLabel(binding.SourceSeries, binding.SeriesIndex),
                    hit.X,
                    hit.Y,
                    binding.SeriesIndex,
                    hit.PointIndex,
                    tag);
            }
            catch (FormatException)
            {
                return string.Format("X:{0:0.###}, Y:{1:0.###}", hit.X, hit.Y);
            }
        }

        private LightningScatterPoint CreateHitPoint(ScatterSeriesBinding binding, NearestPointHit hit)
        {
            if (binding != null
                && binding.SourceSeries != null
                && binding.SourceSeries.Points != null
                && hit.PointIndex >= 0
                && hit.PointIndex < binding.SourceSeries.Points.Count
                && binding.SourceSeries.Points[hit.PointIndex] != null)
            {
                return binding.SourceSeries.Points[hit.PointIndex].Clone();
            }

            return new LightningScatterPoint(hit.X, hit.Y);
        }

        private object GetHitTag(ScatterSeriesBinding binding, NearestPointHit hit)
        {
            LightningScatterPoint point = CreateHitPoint(binding, hit);
            return point == null ? null : point.Tag;
        }

        private void HidePointToolTip()
        {
            currentToolTipText = string.Empty;
            if (!IsDisposed)
            {
                pointToolTip.Hide(chart);
            }
        }

        private string ResolveImageFilePath(LightningScatterImageOptions imageOptions)
        {
            string directory = ResolveImageSaveDirectory(imageOptions);
            Directory.CreateDirectory(directory);

            string extension = imageOptions.FileFormat == LightningScatterImageFileFormat.Jpeg ? ".jpg" : ".png";
            string fileName = imageOptions.UseGuidFileName
                ? string.Format("{0}{1}", Guid.NewGuid().ToString("N"), extension)
                : Path.GetFileName((imageOptions.FileName ?? string.Empty).Trim());
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = string.Format("{0}{1}", Guid.NewGuid().ToString("N"), extension);
            }

            return Path.Combine(directory, Path.ChangeExtension(fileName, extension));
        }

        private string ResolveImageSaveDirectory(LightningScatterImageOptions imageOptions)
        {
            string baseDirectory = string.IsNullOrWhiteSpace(imageOptions.SaveDirectory)
                ? ResolveImageSaveFolder(imageOptions.SaveFolder)
                : imageOptions.SaveDirectory;

            if (!string.IsNullOrWhiteSpace(imageOptions.SubDirectoryName))
            {
                baseDirectory = Path.Combine(baseDirectory, SanitizePathSegment(imageOptions.SubDirectoryName.Trim()));
            }

            if (imageOptions.UseDateFolder)
            {
                baseDirectory = Path.Combine(baseDirectory, DateTime.Now.ToString("yyyyMMdd"));
            }

            return Path.GetFullPath(baseDirectory);
        }

        private string ResolveImageSaveFolder(LightningScatterImageSaveFolder saveFolder)
        {
            string resolvedPath;
            switch (saveFolder)
            {
                case LightningScatterImageSaveFolder.RoamingApplicationData:
                    resolvedPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    break;
                case LightningScatterImageSaveFolder.MyDocuments:
                    resolvedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    break;
                case LightningScatterImageSaveFolder.Temp:
                    resolvedPath = Path.GetTempPath();
                    break;
                case LightningScatterImageSaveFolder.LocalApplicationData:
                default:
                    resolvedPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    break;
            }

            return string.IsNullOrWhiteSpace(resolvedPath) ? Path.GetTempPath() : resolvedPath;
        }

        private string SanitizePathSegment(string pathSegment)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            char[] chars = (pathSegment ?? string.Empty).ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (invalidChars.Contains(chars[i]))
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }

        private static Image LoadImage(string imagePath)
        {
            using (Image image = Image.FromFile(imagePath))
            {
                return CloneImage(image);
            }
        }

        private static Image CloneImage(Image image)
        {
            if (image == null)
            {
                return null;
            }

            Bitmap clone = new Bitmap(image);
            clone.SetResolution(
                Math.Max(1f, image.HorizontalResolution),
                Math.Max(1f, image.VerticalResolution));
            return clone;
        }

        private void StoreSavedImage(string fullPath, Image image)
        {
            Image imageCopy = CloneImage(image);
            lock (syncRoot)
            {
                if (lastSavedImage != null)
                {
                    lastSavedImage.Dispose();
                    lastSavedImage = null;
                }

                lastSavedImage = imageCopy;
                lastSavedImagePath = fullPath ?? string.Empty;
            }
        }

        protected virtual void OnPointClicked(LightningScatterPointClickEventArgs e)
        {
            EventHandler<LightningScatterPointClickEventArgs> handler = PointClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnLegendClicked(LightningScatterLegendClickEventArgs e)
        {
            EventHandler<LightningScatterLegendClickEventArgs> handler = LegendClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnImageSaving(LightningScatterImageSavingEventArgs e)
        {
            EventHandler<LightningScatterImageSavingEventArgs> handler = ImageSaving;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnImageSaved(LightningScatterImageSavedEventArgs e)
        {
            EventHandler<LightningScatterImageSavedEventArgs> handler = ImageSaved;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private sealed class NearestPointHit
        {
            public double X { get; set; }
            public double Y { get; set; }
            public int PointIndex { get; set; }
            public double Distance { get; set; }
        }
    }
}
