using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Arction.WinForms.Charting;
using Arction.WinForms.Charting.Annotations;
using Arction.WinForms.Charting.Axes;
using Arction.WinForms.Charting.SeriesXY;
using Arction.WinForms.Charting.Views.ViewXY;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.PCAChart.Common
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

    public enum LightningScatterPointShape
    {
        Circle,
        Rectangle,
        RoundedRectangle
    }

    public enum LightningScatterThemeMode
    {
        LightGray,
        DarkGray,
        Custom
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

    public enum LightningScatterTextAlignment
    {
        Left,
        Center,
        Right
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
            PointColor = Color.FromArgb(129, 178, 231);
            PointBorderColor = Color.Empty;
            PointBorderWidth = -1f;
            LineColor = Color.FromArgb(129, 178, 231);
            PointSize = 7f;
            PointShape = LightningScatterPointShape.Circle;
            LineWidth = 1.5f;
            ShowLine = false;
            ShowPoints = true;
            ShowInLegend = true;
        }

        public string Name { get; set; }
        public string LegendLabel { get; set; }
        public IList<LightningScatterPoint> Points { get; set; }
        public Color PointColor { get; set; }
        public Color PointBorderColor { get; set; }
        public float PointBorderWidth { get; set; }
        public Color LineColor { get; set; }
        public float PointSize { get; set; }
        public LightningScatterPointShape PointShape { get; set; }
        public float LineWidth { get; set; }
        public bool ShowLine { get; set; }
        public bool ShowPoints { get; set; }
        public bool ShowInLegend { get; set; }

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
                PointBorderColor = PointBorderColor,
                PointBorderWidth = PointBorderWidth,
                LineColor = LineColor,
                PointSize = PointSize,
                PointShape = PointShape,
                LineWidth = LineWidth,
                ShowLine = ShowLine,
                ShowPoints = ShowPoints,
                ShowInLegend = ShowInLegend
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
            GridLinesVisible = false;
            MinorGridLinesVisible = false;
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
        public bool GridLinesVisible { get; set; }
        public bool MinorGridLinesVisible { get; set; }
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
            Position = LightningScatterLegendPosition.TopCenter;
            FontSize = 7.5f;
            TextColor = Color.FromArgb(90, 90, 90);
            BackgroundColor = Color.White;
            BorderColor = Color.White;
            TransparentBackground = false;
            OffsetX = 0;
            OffsetY = 0;
            ShowCheckboxes = false;
            ShowIcons = true;
        }

        public bool Visible { get; set; }
        public LightningScatterLegendPosition Position { get; set; }
        public float FontSize { get; set; }
        public Color TextColor { get; set; }
        public Color BackgroundColor { get; set; }
        public Color BorderColor { get; set; }
        public bool TransparentBackground { get; set; }
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        public bool ShowCheckboxes { get; set; }
        public bool ShowIcons { get; set; }

        public LightningScatterLegendOptions Clone()
        {
            return (LightningScatterLegendOptions)MemberwiseClone();
        }
    }

    public class LightningScatterStyleOptions
    {
        public LightningScatterStyleOptions()
        {
            ForceBubbleStyle = true;
            UsePastelPalette = true;
            ApplyColorAlpha = true;
            ColorTransparencyPercent = 20f;
            ColorAlpha = ResolveAlphaFromTransparencyPercent(ColorTransparencyPercent, 190);
            ApplyColorTransparencyBlend = true;
            ColorBlendBackground = Color.White;
            ApplyBorderTransparency = true;
            BorderTransparencyPercent = 20f;
            BubbleSize = 7f;
            PointShape = LightningScatterPointShape.Circle;
            BubbleBorderWidth = 1f;
            PointBodyThickness = 1f;
            PastelPalette = LightningScatterOptions.CreateDefaultPastelPalette();
        }

        public bool ForceBubbleStyle { get; set; }
        public bool UsePastelPalette { get; set; }
        public bool ApplyColorAlpha { get; set; }
        public int ColorAlpha { get; set; }
        public float ColorTransparencyPercent { get; set; }
        public bool ApplyColorTransparencyBlend { get; set; }
        public Color ColorBlendBackground { get; set; }
        public bool ApplyBorderTransparency { get; set; }
        public float BorderTransparencyPercent { get; set; }
        public float BubbleSize { get; set; }
        public LightningScatterPointShape PointShape { get; set; }
        public float BubbleBorderWidth { get; set; }
        public float PointBodyThickness { get; set; }
        public Color[] PastelPalette { get; set; }

        public LightningScatterStyleOptions Clone()
        {
            return new LightningScatterStyleOptions
            {
                ForceBubbleStyle = ForceBubbleStyle,
                UsePastelPalette = UsePastelPalette,
                ApplyColorAlpha = ApplyColorAlpha,
                ColorAlpha = ColorAlpha,
                ColorTransparencyPercent = ColorTransparencyPercent,
                ApplyColorTransparencyBlend = ApplyColorTransparencyBlend,
                ColorBlendBackground = ColorBlendBackground,
                ApplyBorderTransparency = ApplyBorderTransparency,
                BorderTransparencyPercent = BorderTransparencyPercent,
                BubbleSize = BubbleSize,
                PointShape = PointShape,
                BubbleBorderWidth = BubbleBorderWidth,
                PointBodyThickness = PointBodyThickness,
                PastelPalette = PastelPalette == null ? LightningScatterOptions.CreateDefaultPastelPalette() : (Color[])PastelPalette.Clone()
            };
        }

        private static int ResolveAlphaFromTransparencyPercent(float transparencyPercent, int fallbackAlpha)
        {
            if (float.IsNaN(transparencyPercent) || float.IsInfinity(transparencyPercent))
            {
                return Math.Max(0, Math.Min(255, fallbackAlpha));
            }

            float transparency = Math.Max(0f, Math.Min(100f, transparencyPercent));
            int alpha = (int)Math.Round(255f * ((100f - transparency) / 100f));
            return Math.Max(0, Math.Min(255, alpha));
        }
    }

    public class LightningScatterInteractionOptions
    {
        public LightningScatterInteractionOptions()
        {
            ZoomEnabled = false;
            PanEnabled = false;
            MouseWheelZoomEnabled = false;
            AllowInternalMouseCursorChange = false;
            OpenPropertyEditorOnRightClick = false;
        }

        public bool ZoomEnabled { get; set; }
        public bool PanEnabled { get; set; }
        public bool MouseWheelZoomEnabled { get; set; }
        public bool AllowInternalMouseCursorChange { get; set; }
        public bool OpenPropertyEditorOnRightClick { get; set; }

        public LightningScatterInteractionOptions Clone()
        {
            return (LightningScatterInteractionOptions)MemberwiseClone();
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
            TextAlignment = LightningScatterTextAlignment.Center;
            BadgeBackColor = Color.FromArgb(255, 249, 196);
            BadgeBorderColor = Color.FromArgb(240, 206, 84);
            BadgeWidthRatio = 0.8f;
            BadgeHeight = 0f;
            BadgeSingleLine = true;
            BadgeHorizontalPadding = 10f;
            BadgeVerticalPadding = 4f;
            BadgeMinWidth = 0f;
            BadgeMinHeight = 0f;
        }

        public string Text { get; set; }
        public bool ShowWhenDataMissing { get; set; }
        public bool ShowWhenAllValuesZero { get; set; }
        public float FontSize { get; set; }
        public Color TextColor { get; set; }
        public LightningScatterTextAlignment TextAlignment { get; set; }
        public Color BadgeBackColor { get; set; }
        public Color BadgeBorderColor { get; set; }
        public float BadgeWidthRatio { get; set; }
        public float BadgeHeight { get; set; }
        public bool BadgeSingleLine { get; set; }
        public float BadgeHorizontalPadding { get; set; }
        public float BadgeVerticalPadding { get; set; }
        public float BadgeMinWidth { get; set; }
        public float BadgeMinHeight { get; set; }

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
            FontName = LightningScatter.DefaultChartFontName;
            Title = "Distribution Chart";
            ShowTitle = true;
            TitleColor = Color.Black;
            BackgroundColor = Color.White;
            GraphBackgroundColor = Color.FromArgb(245, 245, 245);
            ThemeMode = LightningScatterThemeMode.LightGray;
            XAxis = new LightningScatterAxisOptions { Title = "X" };
            YAxis = new LightningScatterAxisOptions { Title = "Y" };
            Legend = new LightningScatterLegendOptions();
            Style = new LightningScatterStyleOptions();
            Interaction = new LightningScatterInteractionOptions();
            Tooltip = new LightningScatterTooltipOptions();
            NoData = new LightningScatterNoDataOptions();
            Image = new LightningScatterImageOptions();
        }

        public string FontName { get; set; }
        public string Title { get; set; }
        public bool ShowTitle { get; set; }
        public Color TitleColor { get; set; }
        public Color BackgroundColor { get; set; }
        public Color GraphBackgroundColor { get; set; }
        public LightningScatterThemeMode ThemeMode { get; set; }
        public LightningScatterAxisOptions XAxis { get; set; }
        public LightningScatterAxisOptions YAxis { get; set; }
        public LightningScatterLegendOptions Legend { get; set; }
        public LightningScatterStyleOptions Style { get; set; }
        public LightningScatterInteractionOptions Interaction { get; set; }
        public LightningScatterTooltipOptions Tooltip { get; set; }
        public LightningScatterNoDataOptions NoData { get; set; }
        public LightningScatterImageOptions Image { get; set; }

        public static LightningScatterOptions CreateDefault()
        {
            return new LightningScatterOptions();
        }

        public static LightningScatterOptions CreateDefaultBubble()
        {
            return new LightningScatterOptions();
        }

        public static Color[] CreateDefaultPastelPalette()
        {
            return new[]
            {
                Color.FromArgb(129, 178, 231),
                Color.FromArgb(248, 180, 180),
                Color.FromArgb(151, 211, 169),
                Color.FromArgb(244, 205, 132),
                Color.FromArgb(190, 170, 230),
                Color.FromArgb(132, 204, 206),
                Color.FromArgb(238, 171, 210),
                Color.FromArgb(180, 205, 145)
            };
        }

        public LightningScatterOptions Clone()
        {
            return new LightningScatterOptions
            {
                FontName = FontName,
                Title = Title,
                ShowTitle = ShowTitle,
                TitleColor = TitleColor,
                BackgroundColor = BackgroundColor,
                GraphBackgroundColor = GraphBackgroundColor,
                ThemeMode = ThemeMode,
                XAxis = XAxis == null ? new LightningScatterAxisOptions() : XAxis.Clone(),
                YAxis = YAxis == null ? new LightningScatterAxisOptions() : YAxis.Clone(),
                Legend = Legend == null ? new LightningScatterLegendOptions() : Legend.Clone(),
                Style = Style == null ? new LightningScatterStyleOptions() : Style.Clone(),
                Interaction = Interaction == null ? new LightningScatterInteractionOptions() : Interaction.Clone(),
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

        private readonly TableLayoutPanel chartLayoutPanel;
        private readonly LightningChartUltimate chart;
        private readonly Panel legendStripPanel;
        private readonly FlowLayoutPanel legendItemsPanel;
        private readonly ContextMenuStrip chartContextMenu;
        private readonly ToolStripMenuItem openPropertiesWindowMenuItem;
        private readonly ToolTip pointToolTip = new ToolTip();
        private readonly Dictionary<PointLineSeries, ScatterSeriesBinding> seriesBindings =
            new Dictionary<PointLineSeries, ScatterSeriesBinding>();
        private readonly List<Image> generatedPointImages = new List<Image>();
        private readonly object syncRoot = new object();
        private LightningScatterOptions options = new LightningScatterOptions();
        private List<LightningScatterSeries> series = new List<LightningScatterSeries>();
        private AnnotationXY noDataAnnotation;
        private Image lastSavedImage;
        private string lastSavedImagePath = string.Empty;
        private string currentToolTipText = string.Empty;
        private Color currentToolTipMarkerColor = Color.Empty;
        private string currentFontName = DefaultChartFontName;
        private bool legendClickAttached;
        private bool isCleared = true;
        private bool rightMouseDown;
        private Point rightMouseDownLocation;
        private bool renderRefreshPending;

        public event EventHandler<LightningScatterPointClickEventArgs> PointClicked;
        public event EventHandler<LightningScatterLegendClickEventArgs> LegendClicked;
        public event EventHandler<LightningScatterImageSavingEventArgs> ImageSaving;
        public event EventHandler<LightningScatterImageSavedEventArgs> ImageSaved;

        public LightningScatter()
        {
            Font = new Font(DefaultChartFontName, 9F, FontStyle.Regular);
            BackColor = Color.White;
            Size = new Size(600, 400);

            chartLayoutPanel = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 2
            };
            chartLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            chartLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            chartLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));

            chart = new LightningChartUltimate
            {
                Dock = DockStyle.Fill,
                Font = new Font(DefaultChartFontName, 9F, FontStyle.Regular),
                Margin = new Padding(0)
            };
            chart.MouseDown += Chart_MouseDown;
            chart.MouseMove += Chart_MouseMove;
            chart.MouseUp += Chart_MouseUp;
            chart.MouseLeave += delegate { HidePointToolTip(); };
            chart.Resize += Chart_Resize;

            openPropertiesWindowMenuItem = new ToolStripMenuItem("Open Properties Window");
            openPropertiesWindowMenuItem.Click += OpenPropertiesWindowMenuItem_Click;
            chartContextMenu = new ContextMenuStrip();
            chartContextMenu.Items.Add(openPropertiesWindowMenuItem);

            legendStripPanel = new Panel
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Height = 0,
                Margin = new Padding(0),
                Padding = new Padding(0, 12, 0, 4),
                Visible = false
            };
            legendStripPanel.Resize += delegate { CenterLegendItems(); };

            legendItemsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                Location = new Point(0, 0),
                Margin = new Padding(0),
                Padding = new Padding(0),
                WrapContents = false
            };
            legendItemsPanel.SizeChanged += delegate { CenterLegendItems(); };
            legendStripPanel.Controls.Add(legendItemsPanel);

            pointToolTip.InitialDelay = 150;
            pointToolTip.ReshowDelay = 100;
            pointToolTip.AutoPopDelay = 5000;
            pointToolTip.OwnerDraw = true;
            pointToolTip.Popup += PointToolTip_Popup;
            pointToolTip.Draw += PointToolTip_Draw;

            chartLayoutPanel.Controls.Add(chart, 0, 0);
            chartLayoutPanel.Controls.Add(legendStripPanel, 0, 1);
            Controls.Add(chartLayoutPanel);
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
                openPropertiesWindowMenuItem.Click -= OpenPropertiesWindowMenuItem_Click;
                chartContextMenu.Dispose();
                ClearSavedImage();
                ClearGeneratedPointImages();
                chart.MouseDown -= Chart_MouseDown;
                chart.MouseMove -= Chart_MouseMove;
                chart.MouseUp -= Chart_MouseUp;
                chart.Resize -= Chart_Resize;
                chart.ViewXY.Panned -= ViewXY_Panned;
                chart.ViewXY.Zoomed -= ViewXY_Zoomed;
                pointToolTip.Popup -= PointToolTip_Popup;
                pointToolTip.Draw -= PointToolTip_Draw;
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
                chart.ViewXY.Panned += ViewXY_Panned;
                chart.ViewXY.Zoomed += ViewXY_Zoomed;
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
                ApplyLegendOptions(GetLegendBox(), snapshotOptions.Legend);
                ApplyNoDataState(snapshotSeries, snapshotOptions, snapshotIsCleared);
                UpdateLegendStrip(snapshotSeries, snapshotOptions);
            }
            finally
            {
                chart.EndUpdate();
            }
        }

        private void ApplyChartOptions(LightningScatterOptions currentOptions)
        {
            currentFontName = string.IsNullOrWhiteSpace(currentOptions.FontName)
                ? DefaultChartFontName
                : currentOptions.FontName.Trim();
            LightningScatterThemeColors themeColors = ResolveThemeColors(currentOptions);
            chart.Font = CreateChartFont(9f, FontStyle.Regular);
            chart.ColorTheme = themeColors.ColorTheme;
            chart.Options.AllowInternalMouseCursorChange = GetInteractionOptions(currentOptions).AllowInternalMouseCursorChange;
            chart.Options.MouseInteraction = true;
            chart.BackColor = themeColors.BackgroundColor;
            chart.Background.Color = themeColors.BackgroundColor;
            chart.Background.GradientColor = themeColors.BackgroundColor;
            chart.Background.GradientFill = GradientFill.Solid;
            chart.Background.Style = RectFillStyle.ColorOnly;
            chart.Title.Text = currentOptions.Title ?? string.Empty;
            chart.Title.Visible = currentOptions.ShowTitle && !string.IsNullOrWhiteSpace(currentOptions.Title);
            chart.Title.Font = CreateChartFont(12f, FontStyle.Bold);
            chart.Title.Color = currentOptions.TitleColor.IsEmpty ? Color.Black : currentOptions.TitleColor;
            chart.Title.MouseInteraction = false;
            chart.Title.MoveByMouse = false;

            ViewXY view = chart.ViewXY;
            view.GraphBackground.Color = themeColors.GraphBackgroundColor;
            view.GraphBackground.GradientColor = themeColors.GraphBackgroundColor;
            view.GraphBackground.GradientFill = GradientFill.Solid;
            view.GraphBackground.Style = RectFillStyle.ColorOnly;
            view.Border.Color = themeColors.BackgroundColor;
            view.Margins = ResolveViewMargins(currentOptions.Legend);
            if (currentOptions.Style != null)
            {
                currentOptions.Style.ColorBlendBackground = themeColors.GraphBackgroundColor;
            }

            ApplyInteractionOptions(view, currentOptions.Interaction);
            ApplyAxisOptions(GetXAxis(), currentOptions.XAxis);
            ApplyAxisOptions(GetYAxis(), currentOptions.YAxis);
            ApplyAxisInteraction(GetXAxis(), currentOptions.Interaction);
            ApplyAxisInteraction(GetYAxis(), currentOptions.Interaction);
            ApplyLegendOptions(GetLegendBox(), currentOptions.Legend);
        }

        private static LightningScatterThemeColors ResolveThemeColors(LightningScatterOptions options)
        {
            LightningScatterOptions effectiveOptions = options ?? new LightningScatterOptions();
            if (effectiveOptions.ThemeMode == LightningScatterThemeMode.DarkGray)
            {
                return new LightningScatterThemeColors(ColorTheme.LightGray, Color.White, Color.FromArgb(218, 218, 218));
            }

            Color backgroundColor = effectiveOptions.BackgroundColor.IsEmpty ? Color.White : effectiveOptions.BackgroundColor;
            Color graphBackgroundColor = effectiveOptions.GraphBackgroundColor.IsEmpty
                ? Color.FromArgb(245, 245, 245)
                : effectiveOptions.GraphBackgroundColor;
            return new LightningScatterThemeColors(ColorTheme.LightGray, backgroundColor, graphBackgroundColor);
        }

        private sealed class LightningScatterThemeColors
        {
            public LightningScatterThemeColors(ColorTheme colorTheme, Color backgroundColor, Color graphBackgroundColor)
            {
                ColorTheme = colorTheme;
                BackgroundColor = backgroundColor;
                GraphBackgroundColor = graphBackgroundColor;
            }

            public ColorTheme ColorTheme { get; private set; }
            public Color BackgroundColor { get; private set; }
            public Color GraphBackgroundColor { get; private set; }
        }

        private void ApplyInteractionOptions(ViewXY view, LightningScatterInteractionOptions interactionOptions)
        {
            LightningScatterInteractionOptions effectiveOptions = interactionOptions ?? new LightningScatterInteractionOptions();
            view.ZoomPanOptions.LeftMouseButtonAction = effectiveOptions.ZoomEnabled
                ? MouseButtonAction.Zoom
                : MouseButtonAction.None;
            view.ZoomPanOptions.RightMouseButtonAction = effectiveOptions.PanEnabled
                ? MouseButtonAction.Pan
                : MouseButtonAction.None;
            view.ZoomPanOptions.MiddleMouseButtonAction = effectiveOptions.PanEnabled
                ? MouseButtonAction.Pan
                : MouseButtonAction.None;
            view.ZoomPanOptions.MouseWheelZooming = effectiveOptions.ZoomEnabled && effectiveOptions.MouseWheelZoomEnabled
                ? MouseWheelZooming.HorizontalAndVertical
                : MouseWheelZooming.Off;
            view.ZoomPanOptions.RightToLeftZoomAction = effectiveOptions.ZoomEnabled
                ? RightToLeftZoomActionXY.ZoomOut
                : RightToLeftZoomActionXY.Off;
            view.ZoomPanOptions.MultiTouchZoomEnabled = effectiveOptions.ZoomEnabled;
            view.ZoomPanOptions.MultiTouchPanEnabled = effectiveOptions.PanEnabled;
        }

        private void Chart_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            rightMouseDown = true;
            rightMouseDownLocation = e.Location;
        }

        private void Chart_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                RequestRenderRefresh();
                return;
            }

            if (!rightMouseDown)
            {
                RequestRenderRefresh();
                return;
            }

            rightMouseDown = false;
            LightningScatterInteractionOptions interactionOptions = GetInteractionOptions(Options);
            if (!interactionOptions.OpenPropertyEditorOnRightClick)
            {
                RequestRenderRefresh();
                return;
            }

            int dx = Math.Abs(e.X - rightMouseDownLocation.X);
            int dy = Math.Abs(e.Y - rightMouseDownLocation.Y);
            if (dx > 4 || dy > 4)
            {
                RequestRenderRefresh();
                return;
            }

            chartContextMenu.Show(chart, e.Location);
        }

        private void ViewXY_Panned(object sender, PannedXYEventArgs e)
        {
            RequestRenderRefresh();
        }

        private void ViewXY_Zoomed(object sender, ZoomedXYEventArgs e)
        {
            RequestRenderRefresh();
        }

        private void RequestRenderRefresh()
        {
            if (IsDisposed || chart == null || chart.IsDisposed)
            {
                return;
            }

            if (renderRefreshPending)
            {
                return;
            }

            renderRefreshPending = true;
            BeginInvoke(new MethodInvoker(delegate
            {
                renderRefreshPending = false;
                ForceRenderRefresh();
            }));
        }

        private void ForceRenderRefresh()
        {
            if (IsDisposed || chart == null || chart.IsDisposed)
            {
                return;
            }

            chart.BeginUpdate();
            try
            {
                foreach (KeyValuePair<PointLineSeries, ScatterSeriesBinding> pair in seriesBindings.ToList())
                {
                    PointLineSeries chartSeries = pair.Key;
                    ScatterSeriesBinding binding = pair.Value;
                    if (chartSeries == null || binding == null || binding.SourceSeries == null)
                    {
                        continue;
                    }

                    chartSeries.Points = ToSeriesPoints(binding.SourceSeries).ToArray();
                    chartSeries.InvalidateData();
                }

                chart.UpdatePixelAlignment();
            }
            finally
            {
                chart.EndUpdate();
            }

            chart.Invalidate();
            chart.Refresh();
        }

        private void OpenPropertiesWindowMenuItem_Click(object sender, EventArgs e)
        {
            chart.ShowPropertiesEditor();
        }

        private void ApplyAxisInteraction(AxisXYBase axis, LightningScatterInteractionOptions interactionOptions)
        {
            LightningScatterInteractionOptions effectiveOptions = interactionOptions ?? new LightningScatterInteractionOptions();
            axis.ZoomingEnabled = effectiveOptions.ZoomEnabled;
            axis.PanningEnabled = effectiveOptions.PanEnabled;
        }

        private static Padding ResolveViewMargins(LightningScatterLegendOptions legendOptions)
        {
            LightningScatterLegendOptions effectiveOptions = legendOptions ?? new LightningScatterLegendOptions();
            bool legendAtBottom = effectiveOptions.Visible
                && (effectiveOptions.Position == LightningScatterLegendPosition.BottomLeft
                    || effectiveOptions.Position == LightningScatterLegendPosition.BottomCenter
                    || effectiveOptions.Position == LightningScatterLegendPosition.BottomRight);
            return new Padding(70, 68, 24, legendAtBottom ? 78 : 48);
        }

        private void ApplyAxisOptions(AxisBase axis, LightningScatterAxisOptions axisOptions)
        {
            LightningScatterAxisOptions effectiveOptions = axisOptions ?? new LightningScatterAxisOptions();
            axis.AxisColor = effectiveOptions.AxisColor;
            axis.LabelsColor = effectiveOptions.LabelColor;
            axis.LabelsFont = CreateChartFont(effectiveOptions.FontSize, FontStyle.Regular);
            axis.LabelsNumberFormat = effectiveOptions.LabelFormat ?? "0.##";
            axis.MajorDivCount = Math.Max(1, effectiveOptions.MajorDivCount);
            axis.MajorGrid.Visible = effectiveOptions.GridLinesVisible;
            axis.MajorGrid.Color = effectiveOptions.GridColor;
            axis.MinorGrid.Visible = effectiveOptions.MinorGridLinesVisible;
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
            legendBox.Visible = effectiveOptions.Visible && !ShouldUseLegendStrip(effectiveOptions);
            legendBox.Position = ConvertLegendPosition(effectiveOptions.Position);
            legendBox.Offset = new PointIntXY(effectiveOptions.OffsetX, effectiveOptions.OffsetY);
            legendBox.AlignmentInVerticalMargin = AlignmentInVerticalMargin.Center;
            legendBox.Layout = LegendBoxLayout.Horizontal;
            legendBox.AutoSize = true;
            legendBox.SeriesTitleFont = CreateChartFont(effectiveOptions.FontSize, FontStyle.Regular);
            legendBox.SeriesTitleColor = effectiveOptions.TextColor;
            legendBox.ShowCheckboxes = effectiveOptions.ShowCheckboxes;
            legendBox.ShowIcons = effectiveOptions.ShowIcons;
            legendBox.MouseInteraction = true;
            legendBox.MoveByMouse = false;
            legendBox.MoveFromSeriesTitle = false;
            legendBox.AllowMouseResize = false;
            if (effectiveOptions.TransparentBackground)
            {
                legendBox.Fill.Color = Color.Transparent;
                legendBox.Fill.Style = RectFillStyle.None;
                legendBox.BorderWidth = 0;
                legendBox.BorderColor = Color.Transparent;
                legendBox.Shadow.Visible = false;
            }
            else
            {
                legendBox.Fill.Color = effectiveOptions.BackgroundColor;
                legendBox.Fill.Style = RectFillStyle.ColorOnly;
                legendBox.BorderWidth = 1;
                legendBox.BorderColor = effectiveOptions.BorderColor;
                legendBox.Shadow.Visible = false;
            }
        }

        private void UpdateLegendStrip(IList<LightningScatterSeries> currentSeries, LightningScatterOptions currentOptions)
        {
            LightningScatterOptions effectiveChartOptions = currentOptions ?? new LightningScatterOptions();
            LightningScatterLegendOptions legendOptions = effectiveChartOptions.Legend;
            LightningScatterStyleOptions styleOptions = effectiveChartOptions.Style;
            LightningScatterLegendOptions effectiveOptions = legendOptions ?? new LightningScatterLegendOptions();
            ClearLegendStrip();

            if (!effectiveOptions.Visible || !ShouldUseLegendStrip(effectiveOptions))
            {
                HideLegendStrip();
                return;
            }

            IList<LightningScatterSeries> legendSeries = (currentSeries ?? new List<LightningScatterSeries>())
                .Where(item => item != null
                    && item.ShowInLegend
                    && item.Points != null
                    && item.Points.Count > 0)
                .ToList();
            if (legendSeries.Count == 0)
            {
                HideLegendStrip();
                return;
            }

            SetLegendStripVisible(true, 44);
            for (int index = 0; index < legendSeries.Count; index++)
            {
                LightningScatterSeries sourceSeries = legendSeries[index];
                string legendLabel = GetLegendLabel(sourceSeries, index);
                Color markerColor = ResolveSeriesColor(sourceSeries, index, styleOptions);
                legendItemsPanel.Controls.Add(CreateLegendItem(sourceSeries, index, legendLabel, markerColor, effectiveOptions));
            }

            CenterLegendItems();
        }

        private Control CreateLegendItem(
            LightningScatterSeries sourceSeries,
            int seriesIndex,
            string legendLabel,
            Color markerColor,
            LightningScatterLegendOptions legendOptions)
        {
            Font labelFont = CreateChartFont(legendOptions.FontSize, FontStyle.Regular);
            Size labelSize = TextRenderer.MeasureText(legendLabel, labelFont);
            int itemWidth = Math.Max(36, 18 + 6 + labelSize.Width);
            var itemPanel = new Panel
            {
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Height = 20,
                Margin = new Padding(10, 0, 10, 0),
                Width = itemWidth
            };

            var markerPanel = new Panel
            {
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Location = new Point(0, 4),
                Size = new Size(12, 12)
            };
            markerPanel.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (GraphicsPath path = CreateRoundedRectanglePath(new RectangleF(0, 0, 11, 11), 4f))
                using (SolidBrush brush = new SolidBrush(markerColor))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
            };

            var label = new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Font = labelFont,
                ForeColor = legendOptions.TextColor,
                Location = new Point(18, 2),
                Text = legendLabel
            };

            AttachLegendItemClick(itemPanel, sourceSeries, seriesIndex, legendLabel);
            AttachLegendItemClick(markerPanel, sourceSeries, seriesIndex, legendLabel);
            AttachLegendItemClick(label, sourceSeries, seriesIndex, legendLabel);

            itemPanel.Controls.Add(markerPanel);
            itemPanel.Controls.Add(label);
            return itemPanel;
        }

        private void AttachLegendItemClick(Control control, LightningScatterSeries sourceSeries, int seriesIndex, string legendLabel)
        {
            control.Click += delegate
            {
                OnLegendClicked(new LightningScatterLegendClickEventArgs(sourceSeries.Clone(), seriesIndex, legendLabel));
            };
        }

        private void ClearLegendStrip()
        {
            while (legendItemsPanel.Controls.Count > 0)
            {
                Control control = legendItemsPanel.Controls[0];
                legendItemsPanel.Controls.RemoveAt(0);
                control.Dispose();
            }
        }

        private void HideLegendStrip()
        {
            SetLegendStripVisible(false, 0);
        }

        private void SetLegendStripVisible(bool visible, int height)
        {
            legendStripPanel.Visible = visible;
            legendStripPanel.Height = visible ? height : 0;
            if (chartLayoutPanel.RowStyles.Count > 1)
            {
                chartLayoutPanel.RowStyles[1].Height = visible ? height : 0;
            }

            chartLayoutPanel.PerformLayout();
        }

        private void CenterLegendItems()
        {
            if (legendItemsPanel == null || legendStripPanel == null)
            {
                return;
            }

            int x = Math.Max(0, (legendStripPanel.ClientSize.Width - legendItemsPanel.Width) / 2);
            int y = Math.Max(0, (legendStripPanel.ClientSize.Height - legendItemsPanel.Height) / 2);
            legendItemsPanel.Location = new Point(x, y);
        }

        private static bool ShouldUseLegendStrip(LightningScatterLegendOptions legendOptions)
        {
            LightningScatterLegendOptions effectiveOptions = legendOptions ?? new LightningScatterLegendOptions();
            return effectiveOptions.Position == LightningScatterLegendPosition.BottomLeft
                || effectiveOptions.Position == LightningScatterLegendPosition.BottomCenter
                || effectiveOptions.Position == LightningScatterLegendPosition.BottomRight;
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
            ClearGeneratedPointImages();

            AxisX xAxis = GetXAxis();
            AxisY yAxis = GetYAxis();
            for (int i = 0; i < currentSeries.Count; i++)
            {
                LightningScatterSeries sourceSeries = currentSeries[i] ?? new LightningScatterSeries();
                LightningScatterSeries renderSeries = CreateRenderSeries(sourceSeries);
                PointLineSeries chartSeries = new PointLineSeries(view, xAxis, yAxis);
                LightningScatterStyleOptions styleOptions = GetStyleOptions(currentOptions);
                Color seriesColor = ResolveSeriesColor(sourceSeries, i, styleOptions);
                bool forceBubbleStyle = styleOptions.ForceBubbleStyle;
                float pointSize = ResolveBubbleSize(sourceSeries, styleOptions);
                Color pointBorderColor = ResolvePointBorderColor(sourceSeries, seriesColor, styleOptions);
                float pointBorderWidth = ResolvePointBorderWidth(sourceSeries, styleOptions);
                chartSeries.Title.Text = GetLegendLabel(sourceSeries, i);
                chartSeries.ShowInLegendBox = sourceSeries.ShowInLegend;
                chartSeries.PointsVisible = forceBubbleStyle || sourceSeries.ShowPoints;
                chartSeries.LineVisible = !forceBubbleStyle && sourceSeries.ShowLine;
                chartSeries.LineStyle.Color = seriesColor;
                chartSeries.LineStyle.Width = Math.Max(0.5f, sourceSeries.LineWidth);
                chartSeries.PointsOptimization = PointsRenderOptimization.None;
                ApplyPointStyle(
                    chartSeries.PointStyle,
                    ResolvePointShape(sourceSeries, styleOptions),
                    pointSize,
                    seriesColor,
                    pointBorderColor,
                    pointBorderWidth,
                    ResolvePointBodyThickness(styleOptions));
                chartSeries.MouseInteraction = true;
                chartSeries.CursorTrackEnabled = true;
                chartSeries.MouseClick += PointSeries_MouseClick;
                chartSeries.Points = ToSeriesPoints(renderSeries).ToArray();
                view.PointLineSeries.Add(chartSeries);
                seriesBindings[chartSeries] = new ScatterSeriesBinding(chartSeries, renderSeries, i);
            }
        }

        private static LightningScatterSeries CreateRenderSeries(LightningScatterSeries sourceSeries)
        {
            LightningScatterSeries renderSeries = sourceSeries == null
                ? new LightningScatterSeries()
                : sourceSeries.Clone();
            if (renderSeries.Points == null || renderSeries.Points.Count <= 1)
            {
                return renderSeries;
            }

            renderSeries.Points = renderSeries.Points
                .Where(point => point != null)
                .OrderBy(point => point.X)
                .ThenBy(point => point.Y)
                .ToList();
            return renderSeries;
        }

        private void ApplyPointStyle(
            PointShapeStyle pointStyle,
            LightningScatterPointShape pointShape,
            float pointSize,
            Color fillColor,
            Color borderColor,
            float borderWidth,
            float bodyThickness)
        {
            float safeSize = Math.Max(1f, pointSize);
            pointStyle.Width = safeSize;
            pointStyle.Height = safeSize;
            pointStyle.Color1 = fillColor;
            pointStyle.Color2 = fillColor;
            pointStyle.BorderColor = borderColor;
            pointStyle.BorderWidth = Math.Max(0f, borderWidth);
            pointStyle.BodyThickness = Math.Max(0f, bodyThickness);
            pointStyle.Antialiasing = true;

            switch (pointShape)
            {
                case LightningScatterPointShape.Rectangle:
                    pointStyle.Shape = Shape.Rectangle;
                    pointStyle.BitmapImage = null;
                    break;
                case LightningScatterPointShape.RoundedRectangle:
                    pointStyle.Shape = Shape.Bitmap;
                    pointStyle.BitmapImage = CreateRoundedRectanglePointImage(safeSize, fillColor, borderColor, borderWidth);
                    pointStyle.BitmapAlphaLevel = 255;
                    pointStyle.UseImageSize = false;
                    break;
                case LightningScatterPointShape.Circle:
                default:
                    pointStyle.Shape = Shape.Circle;
                    pointStyle.BitmapImage = null;
                    break;
            }
        }

        private Image CreateRoundedRectanglePointImage(float pointSize, Color fillColor, Color borderColor, float borderWidth)
        {
            int imageSize = Math.Max(4, (int)Math.Ceiling(pointSize) + 4);
            var image = new Bitmap(imageSize, imageSize);
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                float inset = Math.Max(1f, borderWidth) / 2f + 1f;
                var rect = new RectangleF(
                    inset,
                    inset,
                    imageSize - (inset * 2f),
                    imageSize - (inset * 2f));
                float radius = Math.Max(2f, rect.Height * 0.35f);

                using (GraphicsPath path = CreateRoundedRectanglePath(rect, radius))
                using (SolidBrush brush = new SolidBrush(fillColor))
                {
                    graphics.FillPath(brush, path);
                    if (borderWidth > 0f)
                    {
                        using (Pen pen = new Pen(borderColor, Math.Max(1f, borderWidth)))
                        {
                            graphics.DrawPath(pen, path);
                        }
                    }
                }
            }

            generatedPointImages.Add(image);
            return image;
        }

        private static GraphicsPath CreateRoundedRectanglePath(RectangleF rectangle, float radius)
        {
            float diameter = Math.Max(1f, radius * 2f);
            var path = new GraphicsPath();
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180f, 90f);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270f, 90f);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0f, 90f);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        private void ClearGeneratedPointImages()
        {
            foreach (Image image in generatedPointImages)
            {
                image.Dispose();
            }

            generatedPointImages.Clear();
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
            ResolveRange(currentSeries.SelectMany(item => item.Points == null ? Enumerable.Empty<LightningScatterPoint>() : item.Points).Select(point => point.X),
                currentOptions.XAxis, out xMin, out xMax);

            double yMin;
            double yMax;
            ResolveRange(currentSeries.SelectMany(item => item.Points == null ? Enumerable.Empty<LightningScatterPoint>() : item.Points).Select(point => point.Y),
                currentOptions.YAxis, out yMin, out yMax);

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

            string displayText = GetNoDataDisplayText(noDataOptions.Text, noDataOptions);
            noDataAnnotation.Visible = showNoData && !string.IsNullOrWhiteSpace(displayText);
            noDataAnnotation.Text = displayText;
            noDataAnnotation.TextStyle.Font = CreateChartFont(noDataOptions.FontSize, FontStyle.Regular);
            noDataAnnotation.TextStyle.Color = noDataOptions.TextColor;
            noDataAnnotation.TextStyle.HorizAlign = ConvertTextAlignment(noDataOptions.TextAlignment);
            noDataAnnotation.TextStyle.MultiLineTextHorizontalAlign = ConvertTextAlignment(noDataOptions.TextAlignment);
            noDataAnnotation.TextStyle.VerticalAlign = AlignmentVertical.Center;
            noDataAnnotation.Fill.Color = noDataOptions.BadgeBackColor;
            noDataAnnotation.Fill.Style = RectFillStyle.ColorOnly;
            noDataAnnotation.BorderLineStyle.Color = noDataOptions.BadgeBorderColor;
            noDataAnnotation.BorderVisible = true;
            noDataAnnotation.CornerRoundRadius = 8;
            UpdateNoDataAnnotationLayout(noDataOptions, displayText);
        }

        private void UpdateNoDataAnnotationLayout(LightningScatterNoDataOptions noDataOptions, string displayText)
        {
            if (noDataAnnotation == null)
            {
                return;
            }

            LightningScatterNoDataOptions effectiveOptions = noDataOptions ?? new LightningScatterNoDataOptions();
            int chartWidth = Math.Max(1, chart.ClientSize.Width);
            int chartHeight = Math.Max(1, chart.ClientSize.Height);
            float safeRatio = Math.Max(0.1f, Math.Min(1f, effectiveOptions.BadgeWidthRatio));
            float maxWidth = Math.Max(1f, Math.Min(chartWidth - 8f, chartWidth * safeRatio));
            float maxHeight = Math.Max(1f, chartHeight - 8f);
            float horizontalPadding = Math.Max(0f, effectiveOptions.BadgeHorizontalPadding);
            float verticalPadding = Math.Max(0f, effectiveOptions.BadgeVerticalPadding);
            Size textSize = MeasureNoDataText(displayText, effectiveOptions);
            float minWidth = Math.Min(maxWidth, Math.Max(0f, effectiveOptions.BadgeMinWidth));
            float minHeight = Math.Min(maxHeight, Math.Max(0f, effectiveOptions.BadgeMinHeight));
            float badgeWidth = Math.Min(maxWidth, Math.Max(minWidth, textSize.Width + (horizontalPadding * 2f)));
            float preferredHeight = effectiveOptions.BadgeHeight > 0f
                ? effectiveOptions.BadgeHeight
                : textSize.Height + (verticalPadding * 2f);
            float badgeHeight = Math.Min(maxHeight, Math.Max(minHeight, preferredHeight));

            noDataAnnotation.LocationCoordinateSystem = CoordinateSystem.ScreenCoordinates;
            noDataAnnotation.TargetCoordinateSystem = AnnotationTargetCoordinates.ScreenCoordinates;
            noDataAnnotation.Sizing = AnnotationXYSizing.ScreenCoordinates;
            noDataAnnotation.SizeScreenCoords = new SizeFloatXY(badgeWidth, badgeHeight);
            noDataAnnotation.LocationScreenCoords = new PointFloatXY(chartWidth / 2f, chartHeight / 2f);
            noDataAnnotation.TargetScreenCoords = new PointFloatXY(chartWidth / 2f, chartHeight / 2f);
        }

        private string GetNoDataDisplayText(string text, LightningScatterNoDataOptions noDataOptions)
        {
            string displayText = text ?? string.Empty;
            LightningScatterNoDataOptions effectiveOptions = noDataOptions ?? new LightningScatterNoDataOptions();
            if (!effectiveOptions.BadgeSingleLine)
            {
                return displayText;
            }

            return displayText
                .Replace("\r\n", " ")
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
        }

        private Size MeasureNoDataText(string displayText, LightningScatterNoDataOptions noDataOptions)
        {
            LightningScatterNoDataOptions effectiveOptions = noDataOptions ?? new LightningScatterNoDataOptions();
            using (Font measureFont = CreateChartFont(effectiveOptions.FontSize, FontStyle.Regular))
            {
                TextFormatFlags flags = TextFormatFlags.NoPadding;
                if (effectiveOptions.BadgeSingleLine)
                {
                    flags |= TextFormatFlags.SingleLine;
                }

                Size proposedSize = effectiveOptions.BadgeSingleLine
                    ? new Size(32767, 32767)
                    : new Size(Math.Max(1, chart.ClientSize.Width), 32767);
                return TextRenderer.MeasureText(displayText ?? string.Empty, measureFont, proposedSize, flags);
            }
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
            noDataAnnotation.LocationCoordinateSystem = CoordinateSystem.ScreenCoordinates;
            noDataAnnotation.Sizing = AnnotationXYSizing.ScreenCoordinates;
            noDataAnnotation.TargetCoordinateSystem = AnnotationTargetCoordinates.ScreenCoordinates;
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
            string fontName = string.IsNullOrWhiteSpace(currentFontName) ? DefaultChartFontName : currentFontName;
            return new Font(fontName, Math.Max(1f, size), style);
        }

        private LightningScatterStyleOptions GetStyleOptions(LightningScatterOptions currentOptions)
        {
            return currentOptions == null || currentOptions.Style == null
                ? new LightningScatterStyleOptions()
                : currentOptions.Style;
        }

        private LightningScatterInteractionOptions GetInteractionOptions(LightningScatterOptions currentOptions)
        {
            return currentOptions == null || currentOptions.Interaction == null
                ? new LightningScatterInteractionOptions()
                : currentOptions.Interaction;
        }

        private Color ResolveSeriesColor(LightningScatterSeries sourceSeries, int seriesIndex, LightningScatterStyleOptions styleOptions)
        {
            LightningScatterStyleOptions effectiveOptions = styleOptions ?? new LightningScatterStyleOptions();
            Color resolvedColor;
            if (effectiveOptions.UsePastelPalette
                && effectiveOptions.PastelPalette != null
                && effectiveOptions.PastelPalette.Length > 0)
            {
                resolvedColor = effectiveOptions.PastelPalette[Math.Abs(seriesIndex) % effectiveOptions.PastelPalette.Length];
                return ApplyColorAlpha(resolvedColor, effectiveOptions);
            }

            resolvedColor = sourceSeries == null || sourceSeries.PointColor.IsEmpty
                ? Color.FromArgb(129, 178, 231)
                : sourceSeries.PointColor;
            return ApplyColorAlpha(resolvedColor, effectiveOptions);
        }

        private float ResolveBubbleSize(LightningScatterSeries sourceSeries, LightningScatterStyleOptions styleOptions)
        {
            if (sourceSeries != null && sourceSeries.PointSize > 0f)
            {
                return Math.Max(1f, sourceSeries.PointSize);
            }

            LightningScatterStyleOptions effectiveOptions = styleOptions ?? new LightningScatterStyleOptions();
            if (effectiveOptions.ForceBubbleStyle)
            {
                return Math.Max(1f, effectiveOptions.BubbleSize);
            }

            return Math.Max(1f, effectiveOptions.BubbleSize);
        }

        private LightningScatterPointShape ResolvePointShape(LightningScatterSeries sourceSeries, LightningScatterStyleOptions styleOptions)
        {
            if (sourceSeries != null)
            {
                return sourceSeries.PointShape;
            }

            LightningScatterStyleOptions effectiveOptions = styleOptions ?? new LightningScatterStyleOptions();
            return effectiveOptions.PointShape;
        }

        private Color ResolvePointBorderColor(LightningScatterSeries sourceSeries, Color fallbackColor, LightningScatterStyleOptions styleOptions)
        {
            Color borderColor;
            if (sourceSeries == null || sourceSeries.PointBorderColor.IsEmpty)
            {
                borderColor = fallbackColor;
            }
            else
            {
                borderColor = sourceSeries.PointBorderColor;
            }

            return ApplyBorderTransparency(borderColor, styleOptions);
        }

        private float ResolvePointBorderWidth(LightningScatterSeries sourceSeries, LightningScatterStyleOptions styleOptions)
        {
            if (sourceSeries != null && sourceSeries.PointBorderWidth >= 0f)
            {
                return sourceSeries.PointBorderWidth;
            }

            LightningScatterStyleOptions effectiveOptions = styleOptions ?? new LightningScatterStyleOptions();
            return Math.Max(0f, effectiveOptions.BubbleBorderWidth);
        }

        private float ResolvePointBodyThickness(LightningScatterStyleOptions styleOptions)
        {
            LightningScatterStyleOptions effectiveOptions = styleOptions ?? new LightningScatterStyleOptions();
            return Math.Max(0f, effectiveOptions.PointBodyThickness);
        }

        private static Color ApplyColorAlpha(Color color, LightningScatterStyleOptions styleOptions)
        {
            LightningScatterStyleOptions effectiveOptions = styleOptions ?? new LightningScatterStyleOptions();
            if (!effectiveOptions.ApplyColorAlpha || color.IsEmpty)
            {
                return color;
            }

            int alpha = ResolveAlphaFromTransparencyPercent(effectiveOptions.ColorTransparencyPercent, effectiveOptions.ColorAlpha);
            if (effectiveOptions.ApplyColorTransparencyBlend)
            {
                return BlendWithBackground(color, effectiveOptions.ColorBlendBackground, alpha);
            }

            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        private static Color ApplyBorderTransparency(Color color, LightningScatterStyleOptions styleOptions)
        {
            LightningScatterStyleOptions effectiveOptions = styleOptions ?? new LightningScatterStyleOptions();
            if (!effectiveOptions.ApplyBorderTransparency || color.IsEmpty)
            {
                return color;
            }

            float transparency = Math.Max(0f, Math.Min(100f, effectiveOptions.BorderTransparencyPercent));
            int alpha = (int)Math.Round(255f * ((100f - transparency) / 100f));
            alpha = Math.Max(0, Math.Min(255, alpha));
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        private static int ResolveAlphaFromTransparencyPercent(float transparencyPercent, int fallbackAlpha)
        {
            if (float.IsNaN(transparencyPercent) || float.IsInfinity(transparencyPercent))
            {
                return Math.Max(0, Math.Min(255, fallbackAlpha));
            }

            float transparency = Math.Max(0f, Math.Min(100f, transparencyPercent));
            int alpha = (int)Math.Round(255f * ((100f - transparency) / 100f));
            return Math.Max(0, Math.Min(255, alpha));
        }

        private static Color BlendWithBackground(Color color, Color backgroundColor, int alpha)
        {
            Color background = backgroundColor.IsEmpty ? Color.White : backgroundColor;
            float ratio = Math.Max(0f, Math.Min(255f, alpha)) / 255f;
            int red = BlendChannel(color.R, background.R, ratio);
            int green = BlendChannel(color.G, background.G, ratio);
            int blue = BlendChannel(color.B, background.B, ratio);
            return Color.FromArgb(255, red, green, blue);
        }

        private static int BlendChannel(int foreground, int background, float foregroundRatio)
        {
            int value = (int)Math.Round((foreground * foregroundRatio) + (background * (1f - foregroundRatio)));
            return Math.Max(0, Math.Min(255, value));
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
            currentToolTipMarkerColor = ResolveToolTipMarkerColor(bestBinding);
            pointToolTip.Show(text, chart, e.X + 14, e.Y + 14);
        }

        private Color ResolveToolTipMarkerColor(ScatterSeriesBinding binding)
        {
            if (binding != null && binding.ChartSeries != null)
            {
                return binding.ChartSeries.PointStyle.Color1;
            }

            return Color.Empty;
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
            // 라이트닝차트 화면에서 마우스와 가장 가까운 점을 찾는 처리다.
            // 표준화된 다차원 특징 벡터의 유클리드 거리를 계산하는 KNN과는 관계가 없다.
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
            currentToolTipMarkerColor = Color.Empty;
            if (!IsDisposed)
            {
                pointToolTip.Hide(chart);
            }
        }

        private void PointToolTip_Popup(object sender, PopupEventArgs e)
        {
            if (currentToolTipMarkerColor == Color.Empty)
            {
                return;
            }

            e.ToolTipSize = new Size(e.ToolTipSize.Width + 20, Math.Max(22, e.ToolTipSize.Height));
        }

        private void PointToolTip_Draw(object sender, DrawToolTipEventArgs e)
        {
            e.DrawBackground();
            e.DrawBorder();

            if (currentToolTipMarkerColor == Color.Empty)
            {
                e.DrawText();
                return;
            }

            const int markerDiameter = 10;
            int markerLeft = e.Bounds.Left + 6;
            int markerTop = e.Bounds.Top + ((e.Bounds.Height - markerDiameter) / 2);
            Rectangle markerRect = new Rectangle(markerLeft, markerTop, markerDiameter, markerDiameter);
            using (SolidBrush markerBrush = new SolidBrush(currentToolTipMarkerColor))
            using (Pen markerBorder = new Pen(Color.FromArgb(120, 120, 120), 1f))
            {
                e.Graphics.FillEllipse(markerBrush, markerRect);
                e.Graphics.DrawEllipse(markerBorder, markerRect);
            }

            Rectangle textRect = new Rectangle(markerRect.Right + 6, e.Bounds.Top + 2, e.Bounds.Width - markerRect.Right - 10, e.Bounds.Height - 4);
            TextRenderer.DrawText(e.Graphics, e.ToolTipText ?? string.Empty, e.Font ?? SystemFonts.DefaultFont, textRect,
                SystemColors.InfoText, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix);
        }

        private void Chart_Resize(object sender, EventArgs e)
        {
            if (IsDisposed || noDataAnnotation == null || !noDataAnnotation.Visible)
            {
                return;
            }

            LightningScatterOptions currentOptions = Options;
            chart.BeginUpdate();
            try
            {
                LightningScatterNoDataOptions noDataOptions = currentOptions.NoData ?? new LightningScatterNoDataOptions();
                UpdateNoDataAnnotationLayout(noDataOptions, GetNoDataDisplayText(noDataOptions.Text, noDataOptions));
            }
            finally
            {
                chart.EndUpdate();
            }
        }

        private AlignmentHorizontal ConvertTextAlignment(LightningScatterTextAlignment textAlignment)
        {
            switch (textAlignment)
            {
                case LightningScatterTextAlignment.Left:
                    return AlignmentHorizontal.Left;
                case LightningScatterTextAlignment.Right:
                    return AlignmentHorizontal.Right;
                case LightningScatterTextAlignment.Center:
                default:
                    return AlignmentHorizontal.Center;
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
            clone.SetResolution(Math.Max(1f, image.HorizontalResolution), Math.Max(1f, image.VerticalResolution));
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
