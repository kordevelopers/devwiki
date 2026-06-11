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
    public enum LightningBarHeightMode
    {
        Auto,
        Manual
    }

    public enum LightningBarTitlePosition
    {
        TopLeft,
        TopCenter,
        TopRight
    }

    public enum LightningBarLegendPosition
    {
        Top,
        Bottom
    }

    public enum LightningBarLegendAlignment
    {
        Left,
        Center,
        Right
    }

    public enum LightningBarRawDataButtonMode
    {
        Hidden,
        Visible
    }

    public enum LightningBarImageFileFormat
    {
        Png,
        Jpeg
    }

    public class LightningBarImageOptions
    {
        public LightningBarImageOptions()
        {
            Width = 400;
            Height = 400;
            FileFormat = LightningBarImageFileFormat.Png;
            SaveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            FileName = string.Empty;
            JpegQuality = 90L;
        }

        public int Width { get; set; }
        public int Height { get; set; }
        public LightningBarImageFileFormat FileFormat { get; set; }
        public string SaveDirectory { get; set; }
        public string FileName { get; set; }
        public long JpegQuality { get; set; }

        public LightningBarImageOptions Clone()
        {
            return (LightningBarImageOptions)MemberwiseClone();
        }
    }

    public class LightningBarTitleOptions
    {
        public LightningBarTitleOptions()
        {
            Text = "Bar Chart Sample";
            Color = Color.FromArgb(90, 90, 90);
            FontSize = 12f;
            MarginTop = 15f;
            MarginHorizontal = 20f;
            Position = LightningBarTitlePosition.TopLeft;
            FontStyle = FontStyle.Bold;
            Visible = true;
        }

        public string Text { get; set; }
        public Color Color { get; set; }
        public float FontSize { get; set; }
        public float MarginTop { get; set; }
        public float MarginHorizontal { get; set; }
        public LightningBarTitlePosition Position { get; set; }
        public FontStyle FontStyle { get; set; }
        public bool Visible { get; set; }

        public LightningBarTitleOptions Clone()
        {
            return (LightningBarTitleOptions)MemberwiseClone();
        }
    }

    public class LightningBarLayoutOptions
    {
        public LightningBarLayoutOptions()
        {
            ChartPadding = 32;
            TopOffset = 90;
            LegendReservedWidth = 180;
            CategoryLabelReservedWidth = 90f;
            BottomScaleAreaHeight = 34f;
        }

        public int ChartPadding { get; set; }
        public int TopOffset { get; set; }
        public int LegendReservedWidth { get; set; }
        public float CategoryLabelReservedWidth { get; set; }
        public float BottomScaleAreaHeight { get; set; }

        public LightningBarLayoutOptions Clone()
        {
            return (LightningBarLayoutOptions)MemberwiseClone();
        }
    }

    public class LightningBarLegendOptions
    {
        public LightningBarLegendOptions()
        {
            Visible = true;
            Position = LightningBarLegendPosition.Top;
            Alignment = LightningBarLegendAlignment.Center;
            MarginFromChart = 12f;
            FontSize = 8f;
            TextColor = Color.FromArgb(90, 90, 90);
            MarkerWidth = 26f;
            MarkerHeight = 18f;
            LabelMaxWidth = 120f;
            LabelMaxLines = 3;
            ItemSpacing = 8f;
            SectionSpacing = 28f;
        }

        public bool Visible { get; set; }
        public LightningBarLegendPosition Position { get; set; }
        public LightningBarLegendAlignment Alignment { get; set; }
        public float MarginFromChart { get; set; }
        public float FontSize { get; set; }
        public Color TextColor { get; set; }
        public float MarkerWidth { get; set; }
        public float MarkerHeight { get; set; }
        public float LabelMaxWidth { get; set; }
        public int LabelMaxLines { get; set; }
        public float ItemSpacing { get; set; }
        public float SectionSpacing { get; set; }

        public LightningBarLegendOptions Clone()
        {
            return (LightningBarLegendOptions)MemberwiseClone();
        }
    }

    public class LightningBarCategoryLabelOptions
    {
        public LightningBarCategoryLabelOptions()
        {
            FontSize = 8.5f;
            Color = Color.FromArgb(95, 95, 95);
            MaxLines = 3;
            LineSpacing = 2f;
        }

        public float FontSize { get; set; }
        public Color Color { get; set; }
        public int MaxLines { get; set; }
        public float LineSpacing { get; set; }

        public LightningBarCategoryLabelOptions Clone()
        {
            return (LightningBarCategoryLabelOptions)MemberwiseClone();
        }
    }

    public class LightningBarScaleOptions
    {
        public LightningBarScaleOptions()
        {
            GridLineCount = 5;
            FontSize = 9f;
            LabelColor = Color.FromArgb(95, 95, 95);
            AxisColor = Color.FromArgb(170, 170, 170);
            GridColor = Color.FromArgb(225, 225, 225);
            MaxValue = 100f;
        }

        public int GridLineCount { get; set; }
        public float FontSize { get; set; }
        public Color LabelColor { get; set; }
        public Color AxisColor { get; set; }
        public Color GridColor { get; set; }
        public float MaxValue { get; set; }

        public LightningBarScaleOptions Clone()
        {
            return (LightningBarScaleOptions)MemberwiseClone();
        }
    }

    public class LightningBarSeriesLabelOptions
    {
        public LightningBarSeriesLabelOptions()
        {
            Enabled = false;
            FontSize = 8f;
            Color = Color.FromArgb(95, 95, 95);
            MaxWidth = 140f;
            MaxLines = 3;
        }

        public bool Enabled { get; set; }
        public float FontSize { get; set; }
        public Color Color { get; set; }
        public float MaxWidth { get; set; }
        public int MaxLines { get; set; }

        public LightningBarSeriesLabelOptions Clone()
        {
            return (LightningBarSeriesLabelOptions)MemberwiseClone();
        }
    }

    public class LightningBarBarOptions
    {
        public LightningBarBarOptions()
        {
            BorderWidth = 1.2f;
            Gap = 8f;
            GroupPaddingRatio = 0.18f;
            HeightMode = LightningBarHeightMode.Manual;
            FixedHeight = 18f;
            ReferenceSeriesCount = 5;
            MinHeight = 1f;
        }

        public float BorderWidth { get; set; }
        public float Gap { get; set; }
        public float GroupPaddingRatio { get; set; }
        public LightningBarHeightMode HeightMode { get; set; }
        public float FixedHeight { get; set; }
        public int ReferenceSeriesCount { get; set; }
        public float MinHeight { get; set; }

        public LightningBarBarOptions Clone()
        {
            return (LightningBarBarOptions)MemberwiseClone();
        }
    }

    public class LightningBarTooltipOptions
    {
        public LightningBarTooltipOptions()
        {
            Enabled = true;
            Format = "Value:{2:0.#} (* 클릭할 경우 해당 계측 데이터 차트로 가 보입니다.)";
        }

        public bool Enabled { get; set; }
        public string Format { get; set; }

        public LightningBarTooltipOptions Clone()
        {
            return (LightningBarTooltipOptions)MemberwiseClone();
        }
    }

    public class LightningBarNoDataOptions
    {
        public LightningBarNoDataOptions()
        {
            Text = "데이터가 없습니다.";
            TextColor = Color.FromArgb(120, 120, 120);
            FontName = "맑은 고딕";
            FontSize = 11f;
            ShowWhenDataMissing = true;
            ShowWhenAllValuesZero = false;
            IncludeTitle = false;
        }

        public string Text { get; set; }
        public Color TextColor { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public bool ShowWhenDataMissing { get; set; }
        public bool ShowWhenAllValuesZero { get; set; }
        public bool IncludeTitle { get; set; }

        public bool ShowMessage
        {
            get { return ShowWhenDataMissing; }
            set { ShowWhenDataMissing = value; }
        }

        public LightningBarNoDataOptions Clone()
        {
            return (LightningBarNoDataOptions)MemberwiseClone();
        }
    }

    public class LightningBarRawDataOptions
    {
        public LightningBarRawDataOptions()
        {
            ButtonMode = LightningBarRawDataButtonMode.Hidden;
            ButtonText = "RawData";
            ButtonWidth = 88f;
            ButtonHeight = 28f;
            MarginTop = 8f;
            MarginRight = 10f;
        }

        public LightningBarRawDataButtonMode ButtonMode { get; set; }
        public string ButtonText { get; set; }
        public float ButtonWidth { get; set; }
        public float ButtonHeight { get; set; }
        public float MarginTop { get; set; }
        public float MarginRight { get; set; }

        public LightningBarRawDataOptions Clone()
        {
            return (LightningBarRawDataOptions)MemberwiseClone();
        }
    }

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
            TitleOptions = new LightningBarTitleOptions();
            Layout = new LightningBarLayoutOptions();
            Legend = new LightningBarLegendOptions();
            CategoryLabels = new LightningBarCategoryLabelOptions();
            Scale = new LightningBarScaleOptions();
            SeriesLabels = new LightningBarSeriesLabelOptions();
            Bars = new LightningBarBarOptions();
            Tooltip = new LightningBarTooltipOptions();
            NoData = new LightningBarNoDataOptions();
            RawData = new LightningBarRawDataOptions();
            Image = new LightningBarImageOptions();
            BackgroundColor = Color.White;
        }

        public LightningBarTitleOptions TitleOptions { get; set; }
        public LightningBarLayoutOptions Layout { get; set; }
        public LightningBarLegendOptions Legend { get; set; }
        public LightningBarCategoryLabelOptions CategoryLabels { get; set; }
        public LightningBarScaleOptions Scale { get; set; }
        public LightningBarSeriesLabelOptions SeriesLabels { get; set; }
        public LightningBarBarOptions Bars { get; set; }
        public LightningBarTooltipOptions Tooltip { get; set; }
        public LightningBarNoDataOptions NoData { get; set; }
        public LightningBarRawDataOptions RawData { get; set; }
        public LightningBarImageOptions Image { get; set; }
        public Color BackgroundColor { get; set; }
        public string Title { get { return TitleOptions.Text; } set { TitleOptions.Text = value; } }
        public Color TitleColor { get { return TitleOptions.Color; } set { TitleOptions.Color = value; } }
        public float TitleFontSize { get { return TitleOptions.FontSize; } set { TitleOptions.FontSize = value; } }
        public int ChartPadding { get { return Layout.ChartPadding; } set { Layout.ChartPadding = value; } }
        public int TopOffset { get { return Layout.TopOffset; } set { Layout.TopOffset = value; } }
        public int LegendWidth { get { return Layout.LegendReservedWidth; } set { Layout.LegendReservedWidth = value; } }
        public int GridLineCount { get { return Scale.GridLineCount; } set { Scale.GridLineCount = value; } }
        public float CategoryFontSize { get { return CategoryLabels.FontSize; } set { CategoryLabels.FontSize = value; } }
        public Color CategoryLabelColor { get { return CategoryLabels.Color; } set { CategoryLabels.Color = value; } }
        public int CategoryLabelMaxLines { get { return CategoryLabels.MaxLines; } set { CategoryLabels.MaxLines = value; } }
        public float ScaleFontSize { get { return Scale.FontSize; } set { Scale.FontSize = value; } }
        public Color ScaleLabelColor { get { return Scale.LabelColor; } set { Scale.LabelColor = value; } }
        public Color AxisColor { get { return Scale.AxisColor; } set { Scale.AxisColor = value; } }
        public Color GridColor { get { return Scale.GridColor; } set { Scale.GridColor = value; } }
        public Color LegendTextColor { get { return Legend.TextColor; } set { Legend.TextColor = value; } }
        public float LegendFontSize { get { return Legend.FontSize; } set { Legend.FontSize = value; } }
        public float LegendMarkerWidth { get { return Legend.MarkerWidth; } set { Legend.MarkerWidth = value; } }
        public float LegendMarkerHeight { get { return Legend.MarkerHeight; } set { Legend.MarkerHeight = value; } }
        public float LegendTextMaxWidth { get { return Legend.LabelMaxWidth; } set { Legend.LabelMaxWidth = value; } }
        public int LegendTextMaxLines { get { return Legend.LabelMaxLines; } set { Legend.LabelMaxLines = value; } }
        public bool SeriesLabelEnabled { get { return SeriesLabels.Enabled; } set { SeriesLabels.Enabled = value; } }
        public float SeriesLabelFontSize { get { return SeriesLabels.FontSize; } set { SeriesLabels.FontSize = value; } }
        public Color SeriesLabelColor { get { return SeriesLabels.Color; } set { SeriesLabels.Color = value; } }
        public float SeriesLabelMaxWidth { get { return SeriesLabels.MaxWidth; } set { SeriesLabels.MaxWidth = value; } }
        public int SeriesLabelMaxLines { get { return SeriesLabels.MaxLines; } set { SeriesLabels.MaxLines = value; } }
        public bool SeriesTooltipEnabled { get { return Tooltip.Enabled; } set { Tooltip.Enabled = value; } }
        public string SeriesTooltipFormat { get { return Tooltip.Format; } set { Tooltip.Format = value; } }
        public string NoDataText { get { return NoData.Text; } set { NoData.Text = value; } }
        public Color NoDataTextColor { get { return NoData.TextColor; } set { NoData.TextColor = value; } }
        public float NoDataFontSize { get { return NoData.FontSize; } set { NoData.FontSize = value; } }
        public bool ShowNoDataMessage { get { return NoData.ShowMessage; } set { NoData.ShowMessage = value; } }
        public float BarBorderWidth { get { return Bars.BorderWidth; } set { Bars.BorderWidth = value; } }
        public float BarGap { get { return Bars.Gap; } set { Bars.Gap = value; } }
        public float GroupPaddingRatio { get { return Bars.GroupPaddingRatio; } set { Bars.GroupPaddingRatio = value; } }
        public LightningBarHeightMode BarHeightMode { get { return Bars.HeightMode; } set { Bars.HeightMode = value; } }
        public float FixedBarHeight { get { return Bars.FixedHeight; } set { Bars.FixedHeight = value; } }
        public float MaxValue { get { return Scale.MaxValue; } set { Scale.MaxValue = value; } }

        public LightningBarOptions Clone()
        {
            LightningBarOptions clone = (LightningBarOptions)MemberwiseClone();
            clone.TitleOptions = TitleOptions == null ? new LightningBarTitleOptions() : TitleOptions.Clone();
            clone.Layout = Layout == null ? new LightningBarLayoutOptions() : Layout.Clone();
            clone.Legend = Legend == null ? new LightningBarLegendOptions() : Legend.Clone();
            clone.CategoryLabels = CategoryLabels == null ? new LightningBarCategoryLabelOptions() : CategoryLabels.Clone();
            clone.Scale = Scale == null ? new LightningBarScaleOptions() : Scale.Clone();
            clone.SeriesLabels = SeriesLabels == null ? new LightningBarSeriesLabelOptions() : SeriesLabels.Clone();
            clone.Bars = Bars == null ? new LightningBarBarOptions() : Bars.Clone();
            clone.Tooltip = Tooltip == null ? new LightningBarTooltipOptions() : Tooltip.Clone();
            clone.NoData = NoData == null ? new LightningBarNoDataOptions() : NoData.Clone();
            clone.RawData = RawData == null ? new LightningBarRawDataOptions() : RawData.Clone();
            clone.Image = Image == null ? new LightningBarImageOptions() : Image.Clone();
            return clone;
        }

        public void EnsureGroups()
        {
            if (TitleOptions == null)
            {
                TitleOptions = new LightningBarTitleOptions();
            }

            if (Layout == null)
            {
                Layout = new LightningBarLayoutOptions();
            }

            if (Legend == null)
            {
                Legend = new LightningBarLegendOptions();
            }

            if (CategoryLabels == null)
            {
                CategoryLabels = new LightningBarCategoryLabelOptions();
            }

            if (Scale == null)
            {
                Scale = new LightningBarScaleOptions();
            }

            if (SeriesLabels == null)
            {
                SeriesLabels = new LightningBarSeriesLabelOptions();
            }

            if (Bars == null)
            {
                Bars = new LightningBarBarOptions();
            }

            if (Tooltip == null)
            {
                Tooltip = new LightningBarTooltipOptions();
            }

            if (NoData == null)
            {
                NoData = new LightningBarNoDataOptions();
            }

            if (RawData == null)
            {
                RawData = new LightningBarRawDataOptions();
            }

            if (Image == null)
            {
                Image = new LightningBarImageOptions();
            }
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
        private RectangleF rawDataButtonBounds = RectangleF.Empty;

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

            newOptions.EnsureGroups();

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
            nextOptions.EnsureGroups();

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

        public void UpdateOptions(Action<LightningBarOptions> updateOptionsAction)
        {
            if (updateOptionsAction == null)
            {
                throw new ArgumentNullException("updateOptionsAction");
            }

            lock (syncRoot)
            {
                LightningBarOptions mutableOptions = options.Clone();
                mutableOptions.EnsureGroups();
                updateOptionsAction(mutableOptions);
                mutableOptions.EnsureGroups();
                options = mutableOptions;
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

        public Bitmap RenderImage()
        {
            return RenderImage(Options.Image);
        }

        public Bitmap RenderImage(LightningBarImageOptions imageOptions)
        {
            LightningBarImageOptions effectiveImageOptions = imageOptions == null
                ? new LightningBarImageOptions()
                : imageOptions.Clone();
            int width = Math.Max(1, effectiveImageOptions.Width);
            int height = Math.Max(1, effectiveImageOptions.Height);

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
                RenderChart(
                    graphics,
                    snapshotCategories,
                    snapshotSeries,
                    snapshotOptions,
                    snapshotHasBoundData,
                    new Size(width, height),
                    false);
            }

            return bitmap;
        }

        public string SaveImage()
        {
            return SaveImage(Options.Image);
        }

        public string SaveImage(LightningBarImageOptions imageOptions)
        {
            LightningBarImageOptions effectiveOptions = imageOptions == null
                ? new LightningBarImageOptions()
                : imageOptions.Clone();
            string fullPath = ResolveImageFilePath(effectiveOptions);

            using (Bitmap bitmap = RenderImage(effectiveOptions))
            {
                SaveBitmap(bitmap, fullPath, effectiveOptions);
            }

            return fullPath;
        }

        public string SaveImage(string filePath, LightningBarImageOptions imageOptions)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("이미지 파일 경로가 필요합니다.", "filePath");
            }

            LightningBarImageOptions effectiveOptions = imageOptions == null
                ? new LightningBarImageOptions()
                : imageOptions.Clone();
            string fullPath = Path.GetFullPath(filePath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (Bitmap bitmap = RenderImage(effectiveOptions))
            {
                SaveBitmap(bitmap, fullPath, effectiveOptions);
            }

            return fullPath;
        }

        public static Bitmap RenderImage(
            IEnumerable<string> newCategories,
            IEnumerable<LightningBarSeries> newSeries,
            LightningBarOptions barOptions,
            LightningBarImageOptions imageOptions)
        {
            using (var chart = new LightningBar())
            {
                chart.UpdateData(newCategories, newSeries, barOptions);
                return chart.RenderImage(imageOptions);
            }
        }

        public static string SaveImage(
            IEnumerable<string> newCategories,
            IEnumerable<LightningBarSeries> newSeries,
            LightningBarOptions barOptions,
            LightningBarImageOptions imageOptions)
        {
            using (var chart = new LightningBar())
            {
                chart.UpdateData(newCategories, newSeries, barOptions);
                return chart.SaveImage(imageOptions);
            }
        }

        public static Image LoadImage(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new ArgumentException("이미지 파일 경로가 필요합니다.", "imagePath");
            }

            using (Image source = System.Drawing.Image.FromFile(imagePath))
            {
                return new Bitmap(source);
            }
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
            RenderChart(
                e.Graphics,
                snapshotCategories,
                snapshotSeries,
                snapshotOptions,
                snapshotHasBoundData,
                ClientSize,
                true);
        }

        protected virtual void RenderChart(
            Graphics graphics,
            string[] currentCategories,
            LightningBarSeries[] currentSeries,
            LightningBarOptions currentOptions,
            bool currentHasBoundData,
            Size renderSize,
            bool enableInteraction)
        {
            graphics.Clear(currentOptions.BackgroundColor);

            if (!currentHasBoundData)
            {
                if (enableInteraction)
                {
                    barHitInfos.Clear();
                    rawDataButtonBounds = RectangleF.Empty;
                }

                return;
            }

            DrawTitle(graphics, currentOptions, renderSize);
            if (enableInteraction)
            {
                barHitInfos.Clear();
            }

            if (!HasRenderableData(currentCategories, currentSeries))
            {
                LightningBarNoDataOptions noDataOptions = currentOptions.NoData ?? new LightningBarNoDataOptions();
                if (noDataOptions.ShowWhenDataMissing)
                {
                    DrawNoDataMessage(graphics, currentOptions, renderSize);
                }

                DrawRawDataButton(graphics, currentOptions, renderSize, enableInteraction);

                return;
            }

            collectBarHits = enableInteraction;
            try
            {
                DrawBarChart(graphics, currentCategories, currentSeries, currentOptions, renderSize);
            }
            finally
            {
                collectBarHits = false;
            }

            DrawLegend(graphics, currentSeries, currentOptions, renderSize);
            if (ShouldShowAllValuesZeroMessage(currentCategories, currentSeries, currentOptions))
            {
                DrawNoDataMessage(graphics, currentOptions, renderSize);
            }

            DrawRawDataButton(graphics, currentOptions, renderSize, enableInteraction);
        }

        protected virtual void DrawTitle(Graphics graphics, LightningBarOptions currentOptions, Size renderSize)
        {
            LightningBarTitleOptions titleOptions = currentOptions.TitleOptions ?? new LightningBarTitleOptions();
            if (!titleOptions.Visible)
            {
                return;
            }

            string titleText = titleOptions.Text ?? string.Empty;
            using (var titleFont = new Font(Font.FontFamily, Math.Max(1f, titleOptions.FontSize), titleOptions.FontStyle))
            using (var titleBrush = new SolidBrush(titleOptions.Color))
            using (var format = new StringFormat())
            {
                SizeF textSize = graphics.MeasureString(titleText, titleFont);
                float x;
                switch (titleOptions.Position)
                {
                    case LightningBarTitlePosition.TopCenter:
                        x = (renderSize.Width - textSize.Width) / 2f;
                        break;
                    case LightningBarTitlePosition.TopRight:
                        x = renderSize.Width - textSize.Width - Math.Max(0f, titleOptions.MarginHorizontal);
                        break;
                    case LightningBarTitlePosition.TopLeft:
                    default:
                        x = Math.Max(0f, titleOptions.MarginHorizontal);
                        break;
                }

                x = Math.Max(0f, x);
                float y = Math.Max(0f, titleOptions.MarginTop);
                format.Trimming = StringTrimming.EllipsisCharacter;
                graphics.DrawString(titleText, titleFont, titleBrush, new RectangleF(x, y, Math.Max(1f, renderSize.Width - x), textSize.Height + 4f), format);
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

            if (IsRawDataButtonVisible(Options) && rawDataButtonBounds.Contains(e.Location))
            {
                ShowRawDataPopup();
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
            LightningBarNoDataOptions noDataOptions = currentOptions.NoData ?? new LightningBarNoDataOptions();
            if (string.IsNullOrWhiteSpace(noDataOptions.Text))
            {
                return;
            }

            RectangleF messageRect = GetPlotRectangle(renderSize, currentOptions);
            if (messageRect.Width <= 1f || messageRect.Height <= 1f)
            {
                messageRect = new RectangleF(0f, 0f, renderSize.Width, renderSize.Height);
            }

            string fontName = string.IsNullOrWhiteSpace(noDataOptions.FontName) ? "맑은 고딕" : noDataOptions.FontName;
            using (var messageFont = new Font(fontName, Math.Max(1f, noDataOptions.FontSize), FontStyle.Regular))
            using (var messageBrush = new SolidBrush(noDataOptions.TextColor))
            using (var format = new StringFormat())
            {
                string messageText = noDataOptions.Text ?? string.Empty;
                string title = currentOptions.TitleOptions == null ? string.Empty : currentOptions.TitleOptions.Text;
                if (noDataOptions.IncludeTitle && !string.IsNullOrWhiteSpace(title))
                {
                    messageText = string.Format("{0}\n{1}", title, messageText);
                }

                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisWord;
                graphics.DrawString(messageText, messageFont, messageBrush, messageRect, format);
            }
        }

        protected virtual bool ShouldShowAllValuesZeroMessage(string[] currentCategories, LightningBarSeries[] currentSeries, LightningBarOptions currentOptions)
        {
            LightningBarNoDataOptions noDataOptions = currentOptions.NoData ?? new LightningBarNoDataOptions();
            return noDataOptions.ShowWhenAllValuesZero && AreAllSeriesValuesZero(currentCategories, currentSeries);
        }

        protected virtual bool AreAllSeriesValuesZero(string[] currentCategories, LightningBarSeries[] currentSeries)
        {
            if (!HasRenderableData(currentCategories, currentSeries))
            {
                return false;
            }

            int categoryCount = currentCategories.Length;
            for (int seriesIndex = 0; seriesIndex < currentSeries.Length; seriesIndex++)
            {
                LightningBarSeries barSeries = currentSeries[seriesIndex];
                if (barSeries == null || barSeries.Values == null)
                {
                    continue;
                }

                int valueCount = Math.Min(categoryCount, barSeries.Values.Length);
                for (int valueIndex = 0; valueIndex < valueCount; valueIndex++)
                {
                    if (Math.Abs(barSeries.Values[valueIndex]) > 0.000001f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        protected virtual RectangleF GetPlotRectangle(Size renderSize, LightningBarOptions currentOptions)
        {
            LightningBarLayoutOptions layout = currentOptions.Layout ?? new LightningBarLayoutOptions();
            float left = layout.ChartPadding + layout.CategoryLabelReservedWidth;
            float top = layout.TopOffset;
            float right = renderSize.Width - layout.ChartPadding - layout.LegendReservedWidth;
            float bottom = renderSize.Height - layout.ChartPadding - layout.BottomScaleAreaHeight;
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
            LightningBarScaleOptions scaleOptions = currentOptions.Scale ?? new LightningBarScaleOptions();
            int lineCount = Math.Max(1, scaleOptions.GridLineCount);

            using (var gridPen = new Pen(scaleOptions.GridColor, 1f))
            using (var axisPen = new Pen(scaleOptions.AxisColor, 1.4f))
            using (var labelFont = new Font(Font.FontFamily, scaleOptions.FontSize, FontStyle.Regular))
            using (var labelBrush = new SolidBrush(scaleOptions.LabelColor))
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
            LightningBarBarOptions barOptions = currentOptions.Bars ?? new LightningBarBarOptions();
            int categoryCount = currentCategories.Length;
            int seriesCount = currentSeries.Length;
            if (categoryCount <= 0 || seriesCount <= 0)
            {
                return;
            }

            float groupHeight = plotRect.Height / categoryCount;
            float safeRatio = Math.Max(0f, Math.Min(0.8f, barOptions.GroupPaddingRatio));
            float usableGroupHeight = groupHeight * (1f - safeRatio);
            int referenceSeriesCount = Math.Max(seriesCount, Math.Max(1, barOptions.ReferenceSeriesCount));
            float barGap = Math.Max(0f, barOptions.Gap);
            float totalGapWidth = barGap * Math.Max(0, referenceSeriesCount - 1);
            bool useFixedBarHeight = barOptions.HeightMode == LightningBarHeightMode.Manual;
            float barHeight = useFixedBarHeight
                ? Math.Max(barOptions.MinHeight, barOptions.FixedHeight)
                : (usableGroupHeight - totalGapWidth) / Math.Max(1, referenceSeriesCount);

            if (barHeight < barOptions.MinHeight)
            {
                barHeight = Math.Max(1f, barOptions.MinHeight);
                barGap = 0f;
                totalGapWidth = 0f;
            }

            float barsTotalHeight = (barHeight * seriesCount) + (barGap * Math.Max(0, seriesCount - 1));

            for (int categoryIndex = 0; categoryIndex < categoryCount; categoryIndex++)
            {
                float groupTop = plotRect.Top + (groupHeight * categoryIndex);
                float groupY = useFixedBarHeight
                    ? groupTop + ((groupHeight - barsTotalHeight) / 2f)
                    : groupTop + ((groupHeight - usableGroupHeight) / 2f);

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
                    using (var borderPen = new Pen(barSeries.BorderColor, barOptions.BorderWidth))
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
            LightningBarSeriesLabelOptions seriesLabelOptions = currentOptions.SeriesLabels ?? new LightningBarSeriesLabelOptions();
            if (!seriesLabelOptions.Enabled || barSeries == null)
            {
                return;
            }

            string labelText = GetLegendLabel(barSeries);
            if (string.IsNullOrWhiteSpace(labelText))
            {
                return;
            }

            float maxTextWidth = Math.Max(1f, seriesLabelOptions.MaxWidth);
            int maxLines = Math.Max(1, seriesLabelOptions.MaxLines);

            using (var labelFont = new Font(Font.FontFamily, Math.Max(1f, seriesLabelOptions.FontSize), FontStyle.Regular))
            using (var labelBrush = new SolidBrush(seriesLabelOptions.Color))
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
            LightningBarCategoryLabelOptions categoryOptions = currentOptions.CategoryLabels ?? new LightningBarCategoryLabelOptions();

            using (var labelFont = new Font(Font.FontFamily, categoryOptions.FontSize, FontStyle.Regular))
            using (var labelBrush = new SolidBrush(categoryOptions.Color))
            using (var rightCenterFormat = new StringFormat(StringFormat.GenericTypographic))
            {
                int maxLines = Math.Max(1, categoryOptions.MaxLines);
                float lineHeight = labelFont.GetHeight(graphics) + Math.Max(0f, categoryOptions.LineSpacing);
                float groupHeight = plotRect.Height / currentCategories.Length;
                float textRight = plotRect.Left - 8f;

                rightCenterFormat.Alignment = StringAlignment.Far;
                rightCenterFormat.LineAlignment = StringAlignment.Center;
                rightCenterFormat.Trimming = StringTrimming.EllipsisCharacter;
                rightCenterFormat.FormatFlags |= StringFormatFlags.NoWrap;

                for (int i = 0; i < currentCategories.Length; i++)
                {
                    string[] labelLines = GetCategoryLabelLines(currentCategories[i], maxLines);
                    float actualTextHeight = lineHeight * labelLines.Length;
                    float centerY = plotRect.Top + (groupHeight * i) + (groupHeight / 2f);
                    float textY = centerY - (actualTextHeight / 2f);

                    for (int lineIndex = 0; lineIndex < labelLines.Length; lineIndex++)
                    {
                        RectangleF lineRect = new RectangleF(0f, textY + (lineHeight * lineIndex), textRight, lineHeight);
                        graphics.DrawString(labelLines[lineIndex], labelFont, labelBrush, lineRect, rightCenterFormat);
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
            LightningBarLegendOptions legendOptions = currentOptions.Legend ?? new LightningBarLegendOptions();
            if (!legendOptions.Visible || currentSeries == null || currentSeries.Length == 0)
            {
                return;
            }

            float labelSpacing = Math.Max(0f, legendOptions.ItemSpacing);
            float sectionSpacing = Math.Max(0f, legendOptions.SectionSpacing);
            float maxTextWidth = Math.Max(1f, legendOptions.LabelMaxWidth);
            float markerWidth = Math.Max(1f, legendOptions.MarkerWidth);
            RectangleF plotRect = GetPlotRectangle(renderSize, currentOptions);

            using (var legendFont = new Font(Font.FontFamily, Math.Max(1f, legendOptions.FontSize), FontStyle.Regular))
            using (var textBrush = new SolidBrush(legendOptions.TextColor))
            {
                float totalLegendWidth = 0f;
                foreach (LightningBarSeries barSeries in currentSeries)
                {
                    SizeF textSize = MeasureLegendText(graphics, GetLegendLabel(barSeries), legendFont, currentOptions);
                    float textWidth = Math.Min(maxTextWidth, textSize.Width);
                    totalLegendWidth += markerWidth + labelSpacing + textWidth + sectionSpacing;
                }

                totalLegendWidth = Math.Max(0f, totalLegendWidth - sectionSpacing);
                float legendX;
                switch (legendOptions.Alignment)
                {
                    case LightningBarLegendAlignment.Left:
                        legendX = Math.Max(0f, plotRect.Left);
                        break;
                    case LightningBarLegendAlignment.Right:
                        legendX = Math.Max(0f, plotRect.Right - totalLegendWidth);
                        break;
                    case LightningBarLegendAlignment.Center:
                    default:
                        legendX = Math.Max(0f, (renderSize.Width - totalLegendWidth) / 2f);
                        break;
                }

                float legendY = legendOptions.Position == LightningBarLegendPosition.Bottom
                    ? plotRect.Bottom + legendOptions.MarginFromChart
                    : Math.Max(0f, plotRect.Top - legendOptions.MarginFromChart - legendOptions.MarkerHeight);

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
            LightningBarLegendOptions legendOptions = currentOptions.Legend ?? new LightningBarLegendOptions();
            float markerWidth = Math.Max(1f, legendOptions.MarkerWidth);
            float markerHeight = Math.Max(1f, legendOptions.MarkerHeight);
            RectangleF markerRect = new RectangleF(x, y, markerWidth, markerHeight);

            using (var fillBrush = new SolidBrush(fillColor))
            using (var borderPen = new Pen(borderColor, 1.5f))
            using (var format = new StringFormat())
            {
                graphics.FillRectangle(fillBrush, markerRect);
                graphics.DrawRectangle(borderPen, markerRect.X, markerRect.Y, markerRect.Width, markerRect.Height);

                float maxTextWidth = Math.Max(1f, legendOptions.LabelMaxWidth);
                int maxLines = Math.Max(1, legendOptions.LabelMaxLines);
                float lineHeight = font.GetHeight(graphics);
                RectangleF textRect = new RectangleF(x + markerWidth + legendOptions.ItemSpacing, y - 3f, maxTextWidth, (lineHeight * maxLines) + 8f);

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
            LightningBarLegendOptions legendOptions = currentOptions.Legend ?? new LightningBarLegendOptions();
            float maxTextWidth = Math.Max(1f, legendOptions.LabelMaxWidth);
            int maxLines = Math.Max(1, legendOptions.LabelMaxLines);
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

        protected virtual bool IsRawDataButtonVisible(LightningBarOptions currentOptions)
        {
            return currentOptions != null
                && currentOptions.RawData != null
                && currentOptions.RawData.ButtonMode == LightningBarRawDataButtonMode.Visible;
        }

        protected virtual void DrawRawDataButton(Graphics graphics, LightningBarOptions currentOptions, Size renderSize, bool updateHitBounds)
        {
            if (!IsRawDataButtonVisible(currentOptions))
            {
                if (updateHitBounds)
                {
                    rawDataButtonBounds = RectangleF.Empty;
                }

                return;
            }

            LightningBarRawDataOptions rawDataOptions = currentOptions.RawData;
            float width = Math.Max(50f, rawDataOptions.ButtonWidth);
            float height = Math.Max(20f, rawDataOptions.ButtonHeight);
            float x = renderSize.Width - width - Math.Max(0f, rawDataOptions.MarginRight);
            float y = Math.Max(0f, rawDataOptions.MarginTop);
            RectangleF buttonBounds = new RectangleF(x, y, width, height);
            if (updateHitBounds)
            {
                rawDataButtonBounds = buttonBounds;
            }

            using (var fillBrush = new SolidBrush(Color.FromArgb(240, 240, 240)))
            using (var borderPen = new Pen(Color.FromArgb(170, 170, 170), 1f))
            using (var textBrush = new SolidBrush(Color.FromArgb(75, 75, 75)))
            using (var buttonFont = new Font(Font.FontFamily, 8.5f, FontStyle.Regular))
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                graphics.FillRectangle(fillBrush, buttonBounds);
                graphics.DrawRectangle(borderPen, buttonBounds.X, buttonBounds.Y, buttonBounds.Width, buttonBounds.Height);
                graphics.DrawString(rawDataOptions.ButtonText ?? "RawData", buttonFont, textBrush, buttonBounds, format);
            }
        }

        protected virtual string ResolveImageFilePath(LightningBarImageOptions imageOptions)
        {
            string directory = string.IsNullOrWhiteSpace(imageOptions.SaveDirectory)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : imageOptions.SaveDirectory;
            directory = Path.GetFullPath(directory);
            Directory.CreateDirectory(directory);

            string extension = imageOptions.FileFormat == LightningBarImageFileFormat.Jpeg ? ".jpg" : ".png";
            string fileName = string.IsNullOrWhiteSpace(imageOptions.FileName)
                ? string.Format("LightningBar_{0:yyyyMMdd_HHmmssfff}{1}", DateTime.Now, extension)
                : Path.GetFileName(imageOptions.FileName.Trim());

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = string.Format("LightningBar_{0:yyyyMMdd_HHmmssfff}{1}", DateTime.Now, extension);
            }

            fileName = Path.ChangeExtension(fileName, extension);
            return Path.Combine(directory, fileName);
        }

        protected virtual void SaveBitmap(Bitmap bitmap, string fullPath, LightningBarImageOptions imageOptions)
        {
            if (imageOptions.FileFormat != LightningBarImageFileFormat.Jpeg)
            {
                bitmap.Save(fullPath, ImageFormat.Png);
                return;
            }

            ImageCodecInfo jpegCodec = ImageCodecInfo.GetImageEncoders()
                .FirstOrDefault(item => item.FormatID == ImageFormat.Jpeg.Guid);
            if (jpegCodec == null)
            {
                bitmap.Save(fullPath, ImageFormat.Jpeg);
                return;
            }

            using (var encoderParameters = new EncoderParameters(1))
            {
                long quality = Math.Max(1L, Math.Min(100L, imageOptions.JpegQuality));
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                bitmap.Save(fullPath, jpegCodec, encoderParameters);
            }
        }

        protected virtual void ShowRawDataPopup()
        {
            string[] snapshotCategories;
            LightningBarSeries[] snapshotSeries;
            LightningBarOptions snapshotOptions;

            lock (syncRoot)
            {
                snapshotCategories = categories.ToArray();
                snapshotSeries = series.Select(item => item.Clone()).ToArray();
                snapshotOptions = options.Clone();
            }

            string title = snapshotOptions.TitleOptions == null ? "RawData" : snapshotOptions.TitleOptions.Text;
            string message = BuildRawDataText(snapshotCategories, snapshotSeries);
            if (string.IsNullOrWhiteSpace(message))
            {
                message = "바인딩된 데이터가 없습니다.";
            }

            MessageBox.Show(this, message, string.Format("{0} - RawData", title), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected virtual string BuildRawDataText(string[] currentCategories, LightningBarSeries[] currentSeries)
        {
            if (currentCategories == null || currentCategories.Length == 0 || currentSeries == null || currentSeries.Length == 0)
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < currentSeries.Length; i++)
            {
                LightningBarSeries barSeries = currentSeries[i];
                if (barSeries == null)
                {
                    continue;
                }

                string seriesLabel = string.IsNullOrWhiteSpace(barSeries.Name) ? string.Format("Series {0}", i + 1) : barSeries.Name;
                builder.AppendLine(seriesLabel);
                for (int categoryIndex = 0; categoryIndex < currentCategories.Length; categoryIndex++)
                {
                    float value = (barSeries.Values != null && categoryIndex < barSeries.Values.Length)
                        ? barSeries.Values[categoryIndex]
                        : 0f;
                    builder.Append("  - ");
                    builder.Append(currentCategories[categoryIndex] ?? string.Empty);
                    builder.Append(": ");
                    builder.AppendLine(value.ToString("0.###"));
                }

                if (i < currentSeries.Length - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString().TrimEnd();
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
