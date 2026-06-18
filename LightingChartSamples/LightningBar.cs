using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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

    public enum LightningBarCategoryLabelOrientation
    {
        Horizontal,
        Vertical
    }

    public enum LightningBarRawDataButtonMode
    {
        Hidden,
        Visible
    }

    public enum LightningBarNoDataDisplayMode
    {
        HideChartAndMessage,
        OverlayOnChartWatermark
    }

    public enum LightningBarImageFileFormat
    {
        Png,
        Jpeg
    }

    public enum LightningBarImageSaveFolder
    {
        LocalApplicationData,
        RoamingApplicationData,
        MyDocuments,
        Temp
    }

    public enum LightningBarImagePreset
    {
        Default = 0,
        ChartZoom = 1,
        ExcelOptimized = 1,
        Custom = 2
    }

    public enum LightningBarLegendReservedWidthMode
    {
        Always,
        CollapseForTopBottomLegend
    }

    public enum LightningBarNoDataBadgeWidthMode
    {
        Auto,
        Fixed,
        Percent
    }

    public enum LightningBarChartAreaOutlineMode
    {
        None,
        PlotArea,
        ControlBounds
    }

    public class LightningBarImageOptions
    {
        public const int DefaultWidth = 600;
        public const int DefaultHeight = 400;

        public LightningBarImageOptions()
        {
            Preset = LightningBarImagePreset.Default;
            Width = DefaultWidth;
            Height = DefaultHeight;
            DpiX = 96f;
            DpiY = 96f;
            FileFormat = LightningBarImageFileFormat.Png;
            SaveFolder = LightningBarImageSaveFolder.LocalApplicationData;
            SaveDirectory = string.Empty;
            SubDirectoryName = "LightningBarImages";
            UseDateFolder = true;
            UseGuidFileName = true;
            FileName = string.Empty;
            JpegQuality = 90L;
            OptimizeForExcel = false;
            ContentScale = 1f;
            ReduceOuterPadding = false;
            HideRawDataButtonOnImage = true;
        }

        public LightningBarImagePreset Preset { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float DpiX { get; set; }
        public float DpiY { get; set; }
        public LightningBarImageFileFormat FileFormat { get; set; }
        public LightningBarImageSaveFolder SaveFolder { get; set; }
        public string SaveDirectory { get; set; }
        public string SubDirectoryName { get; set; }
        public bool UseDateFolder { get; set; }
        public bool UseGuidFileName { get; set; }
        public string FileName { get; set; }
        public long JpegQuality { get; set; }
        public bool OptimizeForExcel { get; set; }
        public float ContentScale { get; set; }
        public bool ReduceOuterPadding { get; set; }
        public bool HideRawDataButtonOnImage { get; set; }

        public static LightningBarImageOptions CreateDefault()
        {
            return new LightningBarImageOptions();
        }

        public static LightningBarImageOptions CreateChartZoom()
        {
            return new LightningBarImageOptions
            {
                Preset = LightningBarImagePreset.ChartZoom,
                Width = 900,
                Height = 600,
                DpiX = 150f,
                DpiY = 150f,
                OptimizeForExcel = true,
                ContentScale = 1.2f,
                ReduceOuterPadding = true,
                HideRawDataButtonOnImage = true
            };
        }

        public static LightningBarImageOptions CreateCustom(int width, int height)
        {
            return new LightningBarImageOptions
            {
                Preset = LightningBarImagePreset.Custom,
                Width = width,
                Height = height
            };
        }

        public LightningBarImageOptions Clone()
        {
            return (LightningBarImageOptions)MemberwiseClone();
        }
    }

    public class LightningBarTitleOptions
    {
        public LightningBarTitleOptions()
        {
            Text = string.Empty;
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
            ChartPadding = 20;
            TopOffset = 72;
            LegendReservedWidth = 120;
            LegendReservedWidthMode = LightningBarLegendReservedWidthMode.CollapseForTopBottomLegend;
            CategoryLabelReservedWidth = 110f;
            AutoCategoryLabelReservedWidth = true;
            MinCategoryLabelReservedWidth = 78f;
            MaxCategoryLabelReservedWidth = 150f;
            BottomScaleAreaHeight = 30f;
        }

        public int ChartPadding { get; set; }
        public int TopOffset { get; set; }
        public int LegendReservedWidth { get; set; }
        public LightningBarLegendReservedWidthMode LegendReservedWidthMode { get; set; }
        public float CategoryLabelReservedWidth { get; set; }
        public bool AutoCategoryLabelReservedWidth { get; set; }
        public float MinCategoryLabelReservedWidth { get; set; }
        public float MaxCategoryLabelReservedWidth { get; set; }
        public float BottomScaleAreaHeight { get; set; }

        public LightningBarLayoutOptions Clone()
        {
            return (LightningBarLayoutOptions)MemberwiseClone();
        }
    }

    public class LightningBarChartAreaOutlineOptions
    {
        public LightningBarChartAreaOutlineOptions()
        {
            Mode = LightningBarChartAreaOutlineMode.None;
            BorderWidth = 1f;
            BorderColor = Color.FromArgb(90, 120, 170);
            BackColor = Color.Transparent;
            BackOpacity = 0;
        }

        public LightningBarChartAreaOutlineMode Mode { get; set; }
        public float BorderWidth { get; set; }
        public Color BorderColor { get; set; }
        public Color BackColor { get; set; }
        public int BackOpacity { get; set; }

        public LightningBarChartAreaOutlineOptions Clone()
        {
            return (LightningBarChartAreaOutlineOptions)MemberwiseClone();
        }
    }

    public class LightningBarLegendOptions
    {
        public LightningBarLegendOptions()
        {
            Visible = true;
            Position = LightningBarLegendPosition.Top;
            Alignment = LightningBarLegendAlignment.Center;
            MarginFromChart = 8f;
            FontSize = 7.5f;
            TextColor = Color.FromArgb(90, 90, 90);
            MarkerWidth = 22f;
            MarkerHeight = 14f;
            LabelMaxWidth = 110f;
            LabelMaxLines = 3;
            ItemSpacing = 6f;
            SectionSpacing = 20f;
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
            FontSize = 8f;
            Color = Color.FromArgb(95, 95, 95);
            MaxLines = 3;
            LineSpacing = 1.5f;
            Orientation = LightningBarCategoryLabelOrientation.Horizontal;
        }

        public float FontSize { get; set; }
        public Color Color { get; set; }
        public int MaxLines { get; set; }
        public float LineSpacing { get; set; }
        public LightningBarCategoryLabelOrientation Orientation { get; set; }

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
            FontSize = 8f;
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
            Gap = 5f;
            GroupPaddingRatio = 0.16f;
            HeightMode = LightningBarHeightMode.Manual;
            FixedHeight = 30f;
            ClampFixedHeightToGroup = true;
            ReferenceSeriesCount = 5;
            MinHeight = 1f;
        }

        public float BorderWidth { get; set; }
        public float Gap { get; set; }
        public float GroupPaddingRatio { get; set; }
        public LightningBarHeightMode HeightMode { get; set; }
        public float FixedHeight { get; set; }
        public bool ClampFixedHeightToGroup { get; set; }
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
        public const string DefaultText = "데이터가 없습니다.";

        public LightningBarNoDataOptions()
        {
            Text = DefaultText;
            TextColor = Color.FromArgb(138, 118, 30);
            FontName = "맑은 고딕";
            FontSize = 11f;
            ShowWhenDataMissing = true;
            ShowWhenAllValuesZero = false;
            IncludeTitle = false;
            DisplayMode = LightningBarNoDataDisplayMode.HideChartAndMessage;
            BadgeBackColor = Color.FromArgb(255, 249, 196);
            BadgeBackOpacity = 128;
            BadgeBorderColor = Color.FromArgb(240, 206, 84);
            BadgeWidthMode = LightningBarNoDataBadgeWidthMode.Auto;
            BadgeFixedWidth = 200f;
            BadgeWidthRatio = 0.8f;
            BadgeSingleLine = true;
            BadgeHorizontalPadding = 10f;
            BadgeVerticalPadding = 4f;
            BadgeMinWidth = 0f;
            BadgeMinHeight = 0f;
        }

        public string Text { get; set; }
        public Color TextColor { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public bool ShowWhenDataMissing { get; set; }
        public bool ShowWhenAllValuesZero { get; set; }
        public bool IncludeTitle { get; set; }
        public LightningBarNoDataDisplayMode DisplayMode { get; set; }
        public Color BadgeBackColor { get; set; }
        public int BadgeBackOpacity { get; set; }
        public Color BadgeBorderColor { get; set; }
        public LightningBarNoDataBadgeWidthMode BadgeWidthMode { get; set; }
        public float BadgeFixedWidth { get; set; }
        public float BadgeWidthRatio { get; set; }
        public bool BadgeSingleLine { get; set; }
        public float BadgeHorizontalPadding { get; set; }
        public float BadgeVerticalPadding { get; set; }
        public float BadgeMinWidth { get; set; }
        public float BadgeMinHeight { get; set; }

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

    public class LightningBarDataPoint
    {
        public LightningBarDataPoint()
        {
        }

        public LightningBarDataPoint(float value)
            : this(value, null)
        {
        }

        public LightningBarDataPoint(float value, object userData)
        {
            Value = value;
            UserData = userData;
        }

        public float Value { get; set; }
        public object UserData { get; set; }
        public object RawData { get { return UserData; } set { UserData = value; } }

        public static LightningBarDataPoint[] FromValues(IEnumerable<float> values)
        {
            return FromValues<object>(values, null);
        }

        public static LightningBarDataPoint[] FromValues(System.Collections.IEnumerable values)
        {
            return FromValues(values, null as System.Collections.IEnumerable);
        }

        public static LightningBarDataPoint[] FromValues<TUserData>(IEnumerable<float> values, IEnumerable<TUserData> userDataItems)
        {
            float[] valueArray = values == null ? new float[0] : values.ToArray();
            TUserData[] userDataArray = userDataItems == null ? new TUserData[0] : userDataItems.ToArray();
            LightningBarDataPoint[] dataPoints = new LightningBarDataPoint[valueArray.Length];

            for (int i = 0; i < valueArray.Length; i++)
            {
                object userData = i < userDataArray.Length ? (object)userDataArray[i] : null;
                dataPoints[i] = new LightningBarDataPoint(valueArray[i], userData);
            }

            return dataPoints;
        }

        public static LightningBarDataPoint[] FromValues(System.Collections.IEnumerable values, System.Collections.IEnumerable userDataItems)
        {
            object[] valueArray = values == null ? new object[0] : values.Cast<object>().ToArray();
            object[] userDataArray = userDataItems == null ? new object[0] : userDataItems.Cast<object>().ToArray();
            LightningBarDataPoint[] dataPoints = new LightningBarDataPoint[valueArray.Length];

            for (int i = 0; i < valueArray.Length; i++)
            {
                object userData = i < userDataArray.Length ? userDataArray[i] : null;
                dataPoints[i] = new LightningBarDataPoint(ConvertToSingleValue(valueArray[i]), userData);
            }

            return dataPoints;
        }

        public static LightningBarDataPoint[] FromValues(IEnumerable<float> values, System.Collections.IEnumerable userDataItems)
        {
            return FromValues(values, userDataItems == null ? null : userDataItems.Cast<object>());
        }

        public static LightningBarDataPoint[] FromValues(System.Collections.IEnumerable values, Func<int, float, object> userDataFactory)
        {
            object[] valueArray = values == null ? new object[0] : values.Cast<object>().ToArray();
            LightningBarDataPoint[] dataPoints = new LightningBarDataPoint[valueArray.Length];

            for (int i = 0; i < valueArray.Length; i++)
            {
                float value = ConvertToSingleValue(valueArray[i]);
                object userData = userDataFactory == null ? null : userDataFactory(i, value);
                dataPoints[i] = new LightningBarDataPoint(value, userData);
            }

            return dataPoints;
        }

        public static LightningBarDataPoint[] FromValues(IEnumerable<float> values, Func<int, float, object> userDataFactory)
        {
            float[] valueArray = values == null ? new float[0] : values.ToArray();
            LightningBarDataPoint[] dataPoints = new LightningBarDataPoint[valueArray.Length];

            for (int i = 0; i < valueArray.Length; i++)
            {
                float value = valueArray[i];
                object userData = userDataFactory == null ? null : userDataFactory(i, value);
                dataPoints[i] = new LightningBarDataPoint(value, userData);
            }

            return dataPoints;
        }

        public static LightningBarDataPoint[] FromDataTable(DataTable table, string valueColumnName)
        {
            return FromDataSource(table, valueColumnName);
        }

        public static LightningBarDataPoint[] FromDataSource(object valueSource, string valueColumnName)
        {
            ValidateValueColumnName(valueColumnName);

            object[] items = GetDataSourceItems(valueSource).ToArray();
            LightningBarDataPoint[] dataPoints = new LightningBarDataPoint[items.Length];

            for (int i = 0; i < items.Length; i++)
            {
                object item = items[i];
                dataPoints[i] = new LightningBarDataPoint(
                    GetValueFromDataItem(item, valueColumnName),
                    GetRawDataFromDataItem(item));
            }

            return dataPoints;
        }

        private static float ConvertToSingleValue(object value)
        {
            if (value == null || Convert.IsDBNull(value))
            {
                return 0f;
            }

            return Convert.ToSingle(value);
        }

        private static IEnumerable<object> GetDataSourceItems(object valueSource)
        {
            if (valueSource == null)
            {
                return Enumerable.Empty<object>();
            }

            DataTable table = valueSource as DataTable;
            if (table != null)
            {
                return table.Rows.Cast<DataRow>().Cast<object>();
            }

            DataView view = valueSource as DataView;
            if (view != null)
            {
                return view.Cast<DataRowView>().Cast<object>();
            }

            DataRowCollection rows = valueSource as DataRowCollection;
            if (rows != null)
            {
                return rows.Cast<DataRow>().Cast<object>();
            }

            System.Collections.IEnumerable enumerable = valueSource as System.Collections.IEnumerable;
            if (enumerable != null && !(valueSource is string))
            {
                return enumerable.Cast<object>();
            }

            return new[] { valueSource };
        }

        private static float GetValueFromDataItem(object item, string valueColumnName)
        {
            if (item == null)
            {
                return 0f;
            }

            DataRow row = item as DataRow;
            if (row != null)
            {
                if (!row.Table.Columns.Contains(valueColumnName))
                {
                    throw new ArgumentException(string.Format("ValueColumnName '{0}' 컬럼을 찾을 수 없습니다.", valueColumnName), "valueColumnName");
                }

                return ConvertToSingleValue(row[valueColumnName]);
            }

            DataRowView rowView = item as DataRowView;
            if (rowView != null)
            {
                return ConvertToSingleValue(rowView[valueColumnName]);
            }

            System.Collections.IDictionary dictionary = item as System.Collections.IDictionary;
            if (dictionary != null)
            {
                if (!dictionary.Contains(valueColumnName))
                {
                    throw new ArgumentException(string.Format("ValueColumnName '{0}' 키를 찾을 수 없습니다.", valueColumnName), "valueColumnName");
                }

                return ConvertToSingleValue(dictionary[valueColumnName]);
            }

            PropertyDescriptor property = TypeDescriptor.GetProperties(item).Find(valueColumnName, true);
            if (property == null)
            {
                throw new ArgumentException(string.Format("ValueColumnName '{0}' 속성을 찾을 수 없습니다.", valueColumnName), "valueColumnName");
            }

            return ConvertToSingleValue(property.GetValue(item));
        }

        private static object GetRawDataFromDataItem(object item)
        {
            DataRowView rowView = item as DataRowView;
            return rowView == null ? item : rowView.Row;
        }

        private static void ValidateValueColumnName(string valueColumnName)
        {
            if (string.IsNullOrWhiteSpace(valueColumnName))
            {
                throw new ArgumentException("ValueSource를 사용할 때는 ValueColumnName을 지정해야 합니다.", "valueColumnName");
            }
        }

        public LightningBarDataPoint Clone()
        {
            return (LightningBarDataPoint)MemberwiseClone();
        }
    }

    public class LightningBarSeriesClickEventArgs : EventArgs
    {
        public LightningBarSeriesClickEventArgs(string categoryName, int categoryIndex, LightningBarSeries series, int seriesIndex, float value)
            : this(categoryName, categoryIndex, series, seriesIndex, value, null)
        {
        }

        public LightningBarSeriesClickEventArgs(string categoryName, int categoryIndex, LightningBarSeries series, int seriesIndex, float value, LightningBarDataPoint dataPoint)
            : this(categoryName, categoryIndex, series, seriesIndex, value, dataPoint, null)
        {
        }

        public LightningBarSeriesClickEventArgs(string categoryName, int categoryIndex, LightningBarSeries series, int seriesIndex, float value, LightningBarDataPoint dataPoint, IEnumerable<object> seriesRawData)
        {
            CategoryName = categoryName;
            CategoryIndex = categoryIndex;
            Series = series;
            SeriesIndex = seriesIndex;
            Value = value;
            DataPoint = dataPoint == null ? new LightningBarDataPoint(value) : dataPoint.Clone();
            UserData = DataPoint.UserData;
            RawData = DataPoint.RawData;
            SeriesRawData = seriesRawData == null ? new object[0] : seriesRawData.ToArray();
        }

        public string CategoryName { get; private set; }
        public int CategoryIndex { get; private set; }
        public LightningBarSeries Series { get; private set; }
        public int SeriesIndex { get; private set; }
        public float Value { get; private set; }
        public LightningBarDataPoint DataPoint { get; private set; }
        public object UserData { get; private set; }
        public object RawData { get; private set; }
        public object[] SeriesRawData { get; private set; }
    }

    public class LightningBarLegendClickEventArgs : EventArgs
    {
        public LightningBarLegendClickEventArgs(LightningBarSeries series, int seriesIndex, string legendLabel)
        {
            Series = series == null ? new LightningBarSeries() : series.Clone();
            SeriesIndex = seriesIndex;
            LegendLabel = legendLabel ?? string.Empty;
        }

        public LightningBarSeries Series { get; private set; }
        public int SeriesIndex { get; private set; }
        public string LegendLabel { get; private set; }
    }

    public class LightningBarImageSavingEventArgs : EventArgs
    {
        public LightningBarImageSavingEventArgs(string imagePath, LightningBarImageOptions imageOptions)
        {
            ImagePath = imagePath ?? string.Empty;
            ImageOptions = imageOptions == null ? new LightningBarImageOptions() : imageOptions.Clone();
        }

        public string ImagePath { get; private set; }
        public LightningBarImageOptions ImageOptions { get; private set; }
        public bool IsFileSave { get { return !string.IsNullOrWhiteSpace(ImagePath); } }
    }

    public class LightningBarImageSavedEventArgs : EventArgs
    {
        public LightningBarImageSavedEventArgs(string imagePath, LightningBarImageOptions imageOptions)
        {
            ImagePath = imagePath ?? string.Empty;
            ImageOptions = imageOptions == null ? new LightningBarImageOptions() : imageOptions.Clone();
        }

        public string ImagePath { get; private set; }
        public LightningBarImageOptions ImageOptions { get; private set; }
        public bool IsFileSave { get { return !string.IsNullOrWhiteSpace(ImagePath); } }
    }

    public class LightningBarSeries
    {
        public LightningBarSeries()
        {
            Name = string.Empty;
            Values = new float[0];
            DataPoints = new LightningBarDataPoint[0];
            ValueColumnName = string.Empty;
            FillColor = Color.FromArgb(180, 74, 166, 224);
            BorderColor = Color.FromArgb(230, 54, 130, 188);
        }

        public string Name { get; set; }

        public string LegendLabel { get; set; }

        public float[] Values { get; set; }

        public LightningBarDataPoint[] DataPoints { get; set; }

        public object ValueSource { get; set; }

        public string ValueColumnName { get; set; }

        public object DataSource { get { return ValueSource; } set { ValueSource = value; } }

        public string ValueMember { get { return ValueColumnName; } set { ValueColumnName = value; } }

        public Color FillColor { get; set; }

        public Color BorderColor { get; set; }

        public LightningBarSeries Clone()
        {
            return new LightningBarSeries
            {
                Name = Name,
                LegendLabel = LegendLabel,
                Values = Values == null ? new float[0] : Values.ToArray(),
                DataPoints = DataPoints == null
                    ? new LightningBarDataPoint[0]
                    : DataPoints.Select(item => item == null ? new LightningBarDataPoint() : item.Clone()).ToArray(),
                ValueSource = ValueSource,
                ValueColumnName = ValueColumnName,
                FillColor = FillColor,
                BorderColor = BorderColor
            };
        }
    }

    public class LightningBarOptions
    {
        private LightningBarNoDataOptions noData;
        private bool noDataTextSetExplicitly;

        public LightningBarOptions()
        {
            TitleOptions = new LightningBarTitleOptions();
            Layout = new LightningBarLayoutOptions();
            ChartAreaOutline = new LightningBarChartAreaOutlineOptions();
            Legend = new LightningBarLegendOptions();
            CategoryLabels = new LightningBarCategoryLabelOptions();
            Scale = new LightningBarScaleOptions();
            SeriesLabels = new LightningBarSeriesLabelOptions();
            Bars = new LightningBarBarOptions();
            Tooltip = new LightningBarTooltipOptions();
            noData = new LightningBarNoDataOptions();
            RawData = new LightningBarRawDataOptions();
            Image = new LightningBarImageOptions();
            BackgroundColor = Color.White;
        }

        public static LightningBarOptions CreateDefault600x400()
        {
            return new LightningBarOptions();
        }

        public LightningBarTitleOptions TitleOptions { get; set; }
        public LightningBarLayoutOptions Layout { get; set; }
        public LightningBarChartAreaOutlineOptions ChartAreaOutline { get; set; }
        public LightningBarLegendOptions Legend { get; set; }
        public LightningBarCategoryLabelOptions CategoryLabels { get; set; }
        public LightningBarScaleOptions Scale { get; set; }
        public LightningBarSeriesLabelOptions SeriesLabels { get; set; }
        public LightningBarBarOptions Bars { get; set; }
        public LightningBarTooltipOptions Tooltip { get; set; }
        public LightningBarNoDataOptions NoData
        {
            get { return noData; }
            set
            {
                LightningBarNoDataOptions nextNoData = value ?? new LightningBarNoDataOptions();
                if (ShouldKeepLegacyNoDataText(nextNoData))
                {
                    nextNoData.Text = noData.Text;
                }

                noData = nextNoData;
            }
        }
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
        public string NoDataText
        {
            get { return NoData.Text; }
            set
            {
                NoData.Text = value;
                noDataTextSetExplicitly = true;
            }
        }
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
            clone.ChartAreaOutline = ChartAreaOutline == null ? new LightningBarChartAreaOutlineOptions() : ChartAreaOutline.Clone();
            clone.Legend = Legend == null ? new LightningBarLegendOptions() : Legend.Clone();
            clone.CategoryLabels = CategoryLabels == null ? new LightningBarCategoryLabelOptions() : CategoryLabels.Clone();
            clone.Scale = Scale == null ? new LightningBarScaleOptions() : Scale.Clone();
            clone.SeriesLabels = SeriesLabels == null ? new LightningBarSeriesLabelOptions() : SeriesLabels.Clone();
            clone.Bars = Bars == null ? new LightningBarBarOptions() : Bars.Clone();
            clone.Tooltip = Tooltip == null ? new LightningBarTooltipOptions() : Tooltip.Clone();
            clone.noData = NoData == null ? new LightningBarNoDataOptions() : NoData.Clone();
            clone.RawData = RawData == null ? new LightningBarRawDataOptions() : RawData.Clone();
            clone.Image = Image == null ? new LightningBarImageOptions() : Image.Clone();
            return clone;
        }

        private bool ShouldKeepLegacyNoDataText(LightningBarNoDataOptions nextNoData)
        {
            return noDataTextSetExplicitly
                && noData != null
                && !string.IsNullOrWhiteSpace(noData.Text)
                && nextNoData != null
                && string.Equals(nextNoData.Text, LightningBarNoDataOptions.DefaultText, StringComparison.Ordinal);
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

            if (ChartAreaOutline == null)
            {
                ChartAreaOutline = new LightningBarChartAreaOutlineOptions();
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

            if (noData == null)
            {
                noData = new LightningBarNoDataOptions();
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
        public const string DefaultChartFontName = "맑은 고딕";

        private readonly object syncRoot = new object();
        private string[] categories = new string[0];
        private List<LightningBarSeries> series = new List<LightningBarSeries>();
        private LightningBarOptions options = new LightningBarOptions();
        private readonly ToolTip seriesToolTip = new ToolTip();
        private readonly List<LightningBarHitInfo> barHitInfos = new List<LightningBarHitInfo>();
        private readonly List<LightningBarLegendHitInfo> legendHitInfos = new List<LightningBarLegendHitInfo>();
        private string currentToolTipText = string.Empty;
        private bool collectBarHits;
        private bool hasBoundData;
        private RectangleF rawDataButtonBounds = RectangleF.Empty;
        private Bitmap lastSavedImage;
        private string lastSavedImagePath = string.Empty;

        public event EventHandler<LightningBarSeriesClickEventArgs> SeriesClicked;
        public event EventHandler<LightningBarSeriesClickEventArgs> BarClicked;
        public event EventHandler<LightningBarLegendClickEventArgs> LegendClicked;
        public event EventHandler<LightningBarImageSavingEventArgs> ImageSaving;
        public event EventHandler<LightningBarImageSavedEventArgs> ImageSaved;

        public LightningBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            DoubleBuffered = true;
            BackColor = Color.White;
            Font = new Font(DefaultChartFontName, 9F, FontStyle.Regular);
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
                ClearSavedImage();
            }

            base.Dispose(disposing);
        }

        protected virtual Font CreateChartFont(float fontSize, FontStyle fontStyle)
        {
            return new Font(DefaultChartFontName, Math.Max(1f, fontSize), fontStyle);
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Image LastSavedImage
        {
            get { return GetLastSavedImage(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Bitmap LastSavedBitmap
        {
            get { return GetLastSavedBitmap(); }
        }

        [Browsable(false)]
        public bool HasLastSavedImage
        {
            get
            {
                lock (syncRoot)
                {
                    return lastSavedImage != null;
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

                if (lastSavedImage != null)
                {
                    lastSavedImage.Dispose();
                    lastSavedImage = null;
                }

                lastSavedImagePath = string.Empty;
            }

            barHitInfos.Clear();
            legendHitInfos.Clear();
            rawDataButtonBounds = RectangleF.Empty;
            currentToolTipText = string.Empty;
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

        public void SetNoDataText(string text)
        {
            UpdateOptions(delegate (LightningBarOptions mutableOptions)
            {
                mutableOptions.NoData.Text = text ?? string.Empty;
            });
        }

        public void SetNoDataMessage(string message)
        {
            SetNoDataText(message);
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

        public Bitmap RenderVisibleImage()
        {
            int width = Math.Max(1, ClientSize.Width);
            int height = Math.Max(1, ClientSize.Height);
            float dpiX = 96f;
            float dpiY = 96f;

            using (Graphics controlGraphics = CreateGraphics())
            {
                dpiX = Math.Max(1f, controlGraphics.DpiX);
                dpiY = Math.Max(1f, controlGraphics.DpiY);
            }

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
            bitmap.SetResolution(dpiX, dpiY);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
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

        public Bitmap CaptureVisibleImage()
        {
            LightningBarImageOptions imageOptions = Options.Image;
            OnImageSaving(new LightningBarImageSavingEventArgs(string.Empty, imageOptions));
            using (Bitmap bitmap = RenderVisibleImage())
            {
                StoreSavedImage(string.Empty, bitmap);
            }

            Bitmap savedBitmap = GetLastSavedBitmap();
            OnImageSaved(new LightningBarImageSavedEventArgs(string.Empty, imageOptions));
            return savedBitmap;
        }

        public Bitmap SaveImageToMemory()
        {
            return SaveImageToMemory(Options.Image);
        }

        public Bitmap SaveImageToMemory(LightningBarImageOptions imageOptions)
        {
            LightningBarImageOptions effectiveOptions = CreateEffectiveImageOptions(imageOptions);

            OnImageSaving(new LightningBarImageSavingEventArgs(string.Empty, effectiveOptions));
            using (Bitmap bitmap = RenderImage(effectiveOptions))
            {
                StoreSavedImage(string.Empty, bitmap);
            }

            Bitmap savedBitmap = GetLastSavedBitmap();
            OnImageSaved(new LightningBarImageSavedEventArgs(string.Empty, effectiveOptions));
            return savedBitmap;
        }

        public Bitmap RenderImage(LightningBarImageOptions imageOptions)
        {
            LightningBarImageOptions effectiveImageOptions = CreateEffectiveImageOptions(imageOptions);
            int width = Math.Max(1, effectiveImageOptions.Width);
            int height = Math.Max(1, effectiveImageOptions.Height);
            float dpiX = Math.Max(1f, effectiveImageOptions.DpiX);
            float dpiY = Math.Max(1f, effectiveImageOptions.DpiY);

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

            snapshotOptions.EnsureGroups();
            ApplyImageRenderOptions(snapshotOptions, effectiveImageOptions);

            Bitmap bitmap = new Bitmap(width, height);
            bitmap.SetResolution(dpiX, dpiY);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
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
            LightningBarImageOptions effectiveOptions = CreateEffectiveImageOptions(imageOptions);
            string fullPath = ResolveImageFilePath(effectiveOptions);

            OnImageSaving(new LightningBarImageSavingEventArgs(fullPath, effectiveOptions));
            using (Bitmap bitmap = RenderImage(effectiveOptions))
            {
                SaveBitmap(bitmap, fullPath, effectiveOptions);
                StoreSavedImage(fullPath, bitmap);
            }

            OnImageSaved(new LightningBarImageSavedEventArgs(fullPath, effectiveOptions));
            return fullPath;
        }

        public string SaveImage(string filePath)
        {
            return SaveImage(filePath, Options.Image);
        }

        public string SaveImage(string filePath, LightningBarImageOptions imageOptions)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("이미지 파일 경로가 필요합니다.", "filePath");
            }

            LightningBarImageOptions effectiveOptions = CreateEffectiveImageOptions(imageOptions);
            string fullPath = Path.GetFullPath(filePath);
            fullPath = Path.ChangeExtension(fullPath, GetImageFileExtension(effectiveOptions.FileFormat));
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            OnImageSaving(new LightningBarImageSavingEventArgs(fullPath, effectiveOptions));
            using (Bitmap bitmap = RenderImage(effectiveOptions))
            {
                SaveBitmap(bitmap, fullPath, effectiveOptions);
                StoreSavedImage(fullPath, bitmap);
            }

            OnImageSaved(new LightningBarImageSavedEventArgs(fullPath, effectiveOptions));
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

        public Image GetLastSavedImage()
        {
            lock (syncRoot)
            {
                return lastSavedImage == null ? null : CloneImagePreservingResolution(lastSavedImage);
            }
        }

        public Bitmap GetLastSavedBitmap()
        {
            lock (syncRoot)
            {
                return lastSavedImage == null ? null : CloneImagePreservingResolution(lastSavedImage);
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

            using (Image loadedImage = LoadImage(path))
            {
                StoreSavedImage(path, loadedImage);
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

        public static Image LoadImage(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new ArgumentException("이미지 파일 경로가 필요합니다.", "imagePath");
            }

            using (Image source = System.Drawing.Image.FromFile(imagePath))
            {
                return CloneImagePreservingResolution(source);
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
            DrawChartAreaOutlineBackground(graphics, currentOptions, renderSize, currentCategories);

            if (!currentHasBoundData)
            {
                if (enableInteraction)
                {
                    barHitInfos.Clear();
                    rawDataButtonBounds = RectangleF.Empty;
                }

                DrawChartAreaOutlineBorder(graphics, currentOptions, renderSize, currentCategories);
                return;
            }

            DrawTitle(graphics, currentOptions, renderSize);
            if (enableInteraction)
            {
                barHitInfos.Clear();
                legendHitInfos.Clear();
            }

            bool hasChartFrame = currentCategories != null
                && currentCategories.Length > 0
                && currentSeries != null
                && currentSeries.Length > 0;
            bool hasRenderableData = HasRenderableData(currentCategories, currentSeries);
            LightningBarNoDataOptions noDataOptions = currentOptions.NoData ?? new LightningBarNoDataOptions();
            bool noDataByMissing = !hasRenderableData && noDataOptions.ShowWhenDataMissing;
            bool noDataByAllZero = ShouldShowAllValuesZeroMessage(currentCategories, currentSeries, currentOptions);
            bool isNoDataState = noDataByMissing || noDataByAllZero;

            bool showChartFrameForNoData = noDataOptions.DisplayMode == LightningBarNoDataDisplayMode.OverlayOnChartWatermark;
            bool shouldDrawChartFrame = hasChartFrame && (!isNoDataState || showChartFrameForNoData);

            if (shouldDrawChartFrame)
            {
                collectBarHits = enableInteraction;
                try
                {
                    DrawBarChart(graphics, currentCategories, currentSeries, currentOptions, renderSize);
                }
                finally
                {
                    collectBarHits = false;
                }

                DrawLegend(graphics, currentSeries, currentOptions, renderSize, currentCategories);
            }

            bool showNoDataMessage = isNoDataState;
            DrawChartAreaOutlineBorder(graphics, currentOptions, renderSize, currentCategories);

            if (showNoDataMessage)
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
            if (string.IsNullOrWhiteSpace(titleText))
            {
                return;
            }

            using (var titleFont = CreateChartFont(titleOptions.FontSize, titleOptions.FontStyle))
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

            LightningBarLegendHitInfo legendHitInfo = FindLegendHit(e.Location);
            if (legendHitInfo != null)
            {
                OnLegendClicked(new LightningBarLegendClickEventArgs(
                    legendHitInfo.Series,
                    legendHitInfo.SeriesIndex,
                    legendHitInfo.LegendLabel));
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
                hitInfo.Value,
                hitInfo.DataPoint,
                hitInfo.SeriesRawData));
        }

        protected virtual void DrawBarChart(Graphics graphics, string[] currentCategories, LightningBarSeries[] currentSeries, LightningBarOptions currentOptions, Size renderSize)
        {
            if (currentCategories == null || currentCategories.Length == 0 || currentSeries == null || currentSeries.Length == 0)
            {
                return;
            }

            RectangleF plotRect = GetPlotRectangle(graphics, renderSize, currentOptions, currentCategories);
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

            return currentSeries.Any(item => GetSeriesValueCount(item) > 0);
        }

        protected virtual int GetSeriesValueCount(LightningBarSeries barSeries)
        {
            if (barSeries == null)
            {
                return 0;
            }

            if (barSeries.DataPoints != null && barSeries.DataPoints.Length > 0)
            {
                return barSeries.DataPoints.Length;
            }

            return barSeries.Values == null ? 0 : barSeries.Values.Length;
        }

        protected virtual float GetSeriesValue(LightningBarSeries barSeries, int categoryIndex)
        {
            if (barSeries == null || categoryIndex < 0)
            {
                return 0f;
            }

            LightningBarDataPoint dataPoint = GetSeriesDataPoint(barSeries, categoryIndex);
            if (dataPoint != null)
            {
                return dataPoint.Value;
            }

            if (barSeries.Values != null && categoryIndex < barSeries.Values.Length)
            {
                return barSeries.Values[categoryIndex];
            }

            return 0f;
        }

        protected virtual LightningBarDataPoint GetSeriesDataPoint(LightningBarSeries barSeries, int categoryIndex)
        {
            if (barSeries == null || categoryIndex < 0 || barSeries.DataPoints == null || categoryIndex >= barSeries.DataPoints.Length)
            {
                return null;
            }

            return barSeries.DataPoints[categoryIndex];
        }

        protected virtual LightningBarDataPoint CreateHitDataPoint(LightningBarSeries barSeries, int categoryIndex, float value)
        {
            LightningBarDataPoint dataPoint = GetSeriesDataPoint(barSeries, categoryIndex);
            return dataPoint == null ? new LightningBarDataPoint(value) : dataPoint.Clone();
        }

        protected virtual object[] GetSeriesRawData(LightningBarSeries barSeries)
        {
            if (barSeries == null || barSeries.DataPoints == null || barSeries.DataPoints.Length == 0)
            {
                return new object[0];
            }

            object[] rawDataItems = barSeries.DataPoints
                .Select(item => item == null ? null : item.RawData)
                .ToArray();

            return rawDataItems.Any(item => item != null) ? rawDataItems : new object[0];
        }

        protected virtual LightningBarSeries CreateEventSeries(LightningBarSeries barSeries, object[] seriesRawData)
        {
            LightningBarSeries eventSeries = barSeries == null ? new LightningBarSeries() : barSeries.Clone();
            object[] safeSeriesRawData = seriesRawData ?? new object[0];
            eventSeries.ValueSource = safeSeriesRawData.Length == 0 ? null : safeSeriesRawData;
            return eventSeries;
        }

        protected virtual void DrawChartAreaOutlineBackground(Graphics graphics, LightningBarOptions currentOptions, Size renderSize, string[] currentCategories)
        {
            LightningBarChartAreaOutlineOptions outlineOptions = currentOptions.ChartAreaOutline ?? new LightningBarChartAreaOutlineOptions();
            if (outlineOptions.Mode == LightningBarChartAreaOutlineMode.None)
            {
                return;
            }

            int opacity = Math.Max(0, Math.Min(255, outlineOptions.BackOpacity));
            if (opacity <= 0)
            {
                return;
            }

            RectangleF outlineRect = GetChartAreaOutlineRectangle(graphics, currentOptions, renderSize, currentCategories);
            if (outlineRect.Width <= 0f || outlineRect.Height <= 0f)
            {
                return;
            }

            using (var backBrush = new SolidBrush(Color.FromArgb(opacity, outlineOptions.BackColor)))
            {
                graphics.FillRectangle(backBrush, outlineRect);
            }
        }

        protected virtual void DrawChartAreaOutlineBorder(Graphics graphics, LightningBarOptions currentOptions, Size renderSize, string[] currentCategories)
        {
            LightningBarChartAreaOutlineOptions outlineOptions = currentOptions.ChartAreaOutline ?? new LightningBarChartAreaOutlineOptions();
            if (outlineOptions.Mode == LightningBarChartAreaOutlineMode.None)
            {
                return;
            }

            float borderWidth = Math.Max(0f, outlineOptions.BorderWidth);
            if (borderWidth <= 0f)
            {
                return;
            }

            RectangleF outlineRect = GetChartAreaOutlineRectangle(graphics, currentOptions, renderSize, currentCategories);
            if (outlineRect.Width <= 0f || outlineRect.Height <= 0f)
            {
                return;
            }

            float halfWidth = borderWidth / 2f;
            RectangleF borderRect = RectangleF.Inflate(outlineRect, -halfWidth, -halfWidth);
            if (borderRect.Width <= 0f || borderRect.Height <= 0f)
            {
                return;
            }

            using (var borderPen = new Pen(outlineOptions.BorderColor, borderWidth))
            {
                graphics.DrawRectangle(borderPen, borderRect.X, borderRect.Y, borderRect.Width, borderRect.Height);
            }
        }

        protected virtual RectangleF GetChartAreaOutlineRectangle(Graphics graphics, LightningBarOptions currentOptions, Size renderSize, string[] currentCategories)
        {
            if (renderSize.Width <= 0 || renderSize.Height <= 0)
            {
                return RectangleF.Empty;
            }

            LightningBarChartAreaOutlineOptions outlineOptions = currentOptions.ChartAreaOutline ?? new LightningBarChartAreaOutlineOptions();
            switch (outlineOptions.Mode)
            {
                case LightningBarChartAreaOutlineMode.PlotArea:
                    return GetPlotRectangle(graphics, renderSize, currentOptions, currentCategories);
                case LightningBarChartAreaOutlineMode.ControlBounds:
                    return new RectangleF(0f, 0f, renderSize.Width, renderSize.Height);
                case LightningBarChartAreaOutlineMode.None:
                default:
                    return RectangleF.Empty;
            }
        }

        protected virtual void DrawNoDataMessage(Graphics graphics, LightningBarOptions currentOptions, Size renderSize)
        {
            LightningBarNoDataOptions noDataOptions = currentOptions.NoData ?? new LightningBarNoDataOptions();
            if (string.IsNullOrWhiteSpace(noDataOptions.Text))
            {
                return;
            }

            RectangleF messageRect = new RectangleF(0f, 0f, renderSize.Width, renderSize.Height);
            if (messageRect.Width <= 1f || messageRect.Height <= 1f)
            {
                return;
            }

            string fontName = string.IsNullOrWhiteSpace(noDataOptions.FontName) ? "맑은 고딕" : noDataOptions.FontName;
            using (var messageFont = new Font(fontName, Math.Max(1f, noDataOptions.FontSize), FontStyle.Regular))
            using (var messageBrush = new SolidBrush(noDataOptions.TextColor))
            using (var badgeBrush = new SolidBrush(Color.FromArgb(
                Math.Max(0, Math.Min(255, noDataOptions.BadgeBackOpacity)),
                noDataOptions.BadgeBackColor)))
            using (var borderPen = new Pen(noDataOptions.BadgeBorderColor, 1.2f))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(0, 0, 0, 0)))
            using (var format = new StringFormat())
            {
                string messageText = noDataOptions.Text ?? string.Empty;
                string title = currentOptions.TitleOptions == null ? string.Empty : currentOptions.TitleOptions.Text;
                if (noDataOptions.IncludeTitle && !string.IsNullOrWhiteSpace(title))
                {
                    messageText = string.Format("{0}\n{1}", title, messageText);
                }

                if (noDataOptions.BadgeSingleLine)
                {
                    messageText = messageText
                        .Replace("\r\n", " ")
                        .Replace('\r', ' ')
                        .Replace('\n', ' ')
                        .Trim();
                }

                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.Word;
                if (noDataOptions.BadgeSingleLine)
                {
                    format.FormatFlags |= StringFormatFlags.NoWrap;
                }

                float maxBadgeWidth = Math.Max(1f, messageRect.Width * 0.98f);
                float minBadgeWidth = Math.Min(maxBadgeWidth, Math.Max(0f, noDataOptions.BadgeMinWidth));
                float horizontalPadding = Math.Max(0f, noDataOptions.BadgeHorizontalPadding);
                float verticalPadding = Math.Max(0f, noDataOptions.BadgeVerticalPadding);
                float fixedBadgeWidth = Math.Max(0f, noDataOptions.BadgeFixedWidth);
                bool useFixedBadgeWidth = noDataOptions.BadgeWidthMode == LightningBarNoDataBadgeWidthMode.Fixed && fixedBadgeWidth > 0f;
                bool usePercentBadgeWidth = noDataOptions.BadgeWidthMode == LightningBarNoDataBadgeWidthMode.Percent;
                float badgeWidth = 0f;
                float maxTextMeasureWidth;

                if (useFixedBadgeWidth)
                {
                    badgeWidth = Math.Min(Math.Max(1f, messageRect.Width), fixedBadgeWidth);
                    maxTextMeasureWidth = Math.Max(1f, badgeWidth - (horizontalPadding * 2f));
                }
                else if (usePercentBadgeWidth)
                {
                    float safeRatio = Math.Max(0.1f, Math.Min(1f, noDataOptions.BadgeWidthRatio));
                    badgeWidth = Math.Max(1f, messageRect.Width * safeRatio);
                    maxTextMeasureWidth = Math.Max(1f, badgeWidth - (horizontalPadding * 2f));
                }
                else
                {
                    maxTextMeasureWidth = Math.Max(1f, maxBadgeWidth - (horizontalPadding * 2f));
                }

                SizeF textSize = noDataOptions.BadgeSingleLine
                    ? graphics.MeasureString(messageText, messageFont)
                    : graphics.MeasureString(messageText, messageFont, new SizeF(maxTextMeasureWidth, messageRect.Height), format);

                if (badgeWidth <= 0f)
                {
                    badgeWidth = Math.Max(minBadgeWidth, textSize.Width + (horizontalPadding * 2f));
                    badgeWidth = Math.Min(maxBadgeWidth, badgeWidth);
                }

                float minBadgeHeight = Math.Max(0f, noDataOptions.BadgeMinHeight);
                float maxBadgeHeight = Math.Max(1f, messageRect.Height * 0.98f);
                float badgeHeight = Math.Max(minBadgeHeight, textSize.Height + (verticalPadding * 2f));
                badgeHeight = Math.Min(maxBadgeHeight, badgeHeight);

                float badgeX = messageRect.Left + (messageRect.Width - badgeWidth) / 2f;
                float badgeY = messageRect.Top + (messageRect.Height - badgeHeight) / 2f;
                RectangleF badgeRect = new RectangleF(badgeX, badgeY, badgeWidth, badgeHeight);
                RectangleF shadowRect = new RectangleF(badgeRect.X + 2f, badgeRect.Y + 2f, badgeRect.Width, badgeRect.Height);
                float cornerRadius = Math.Max(8f, Math.Min(16f, Math.Min(badgeRect.Width, badgeRect.Height) * 0.12f));

                using (GraphicsPath shadowPath = CreateRoundedRectanglePath(shadowRect, cornerRadius))
                using (GraphicsPath badgePath = CreateRoundedRectanglePath(badgeRect, cornerRadius))
                {
                    graphics.FillPath(shadowBrush, shadowPath);
                    graphics.FillPath(badgeBrush, badgePath);
                    graphics.DrawPath(borderPen, badgePath);
                }

                graphics.DrawString(messageText, messageFont, messageBrush, badgeRect, format);
            }
        }

        protected virtual GraphicsPath CreateRoundedRectanglePath(RectangleF rectangle, float radius)
        {
            float safeRadius = Math.Max(1f, Math.Min(radius, Math.Min(rectangle.Width, rectangle.Height) / 2f));
            float diameter = safeRadius * 2f;

            GraphicsPath path = new GraphicsPath();
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180f, 90f);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270f, 90f);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0f, 90f);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90f, 90f);
            path.CloseFigure();
            return path;
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
                if (barSeries == null)
                {
                    continue;
                }

                int valueCount = Math.Min(categoryCount, GetSeriesValueCount(barSeries));
                for (int valueIndex = 0; valueIndex < valueCount; valueIndex++)
                {
                    if (Math.Abs(GetSeriesValue(barSeries, valueIndex)) > 0.000001f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        protected virtual RectangleF GetPlotRectangle(Size renderSize, LightningBarOptions currentOptions)
        {
            return GetPlotRectangle(null, renderSize, currentOptions, null);
        }

        protected virtual RectangleF GetPlotRectangle(Graphics graphics, Size renderSize, LightningBarOptions currentOptions, string[] currentCategories)
        {
            LightningBarLayoutOptions layout = currentOptions.Layout ?? new LightningBarLayoutOptions();
            float categoryLabelReservedWidth = GetEffectiveCategoryLabelReservedWidth(graphics, currentOptions, currentCategories);
            float legendReservedWidth = ShouldReserveLegendWidth(currentOptions) ? layout.LegendReservedWidth : 0f;
            float left = layout.ChartPadding + categoryLabelReservedWidth;
            float top = layout.TopOffset;
            float right = renderSize.Width - layout.ChartPadding - legendReservedWidth;
            float bottom = renderSize.Height - layout.ChartPadding - layout.BottomScaleAreaHeight;
            return RectangleF.FromLTRB(left, top, right, bottom);
        }

        protected virtual bool ShouldReserveLegendWidth(LightningBarOptions currentOptions)
        {
            LightningBarLayoutOptions layout = currentOptions.Layout ?? new LightningBarLayoutOptions();
            if (layout.LegendReservedWidthMode != LightningBarLegendReservedWidthMode.CollapseForTopBottomLegend)
            {
                return true;
            }

            LightningBarLegendOptions legendOptions = currentOptions.Legend ?? new LightningBarLegendOptions();
            if (!legendOptions.Visible)
            {
                return false;
            }

            return legendOptions.Position != LightningBarLegendPosition.Top
                && legendOptions.Position != LightningBarLegendPosition.Bottom;
        }

        protected virtual float GetEffectiveCategoryLabelReservedWidth(Graphics graphics, LightningBarOptions currentOptions, string[] currentCategories)
        {
            LightningBarLayoutOptions layout = currentOptions.Layout ?? new LightningBarLayoutOptions();
            float configuredWidth = Math.Max(0f, layout.CategoryLabelReservedWidth);
            if (!layout.AutoCategoryLabelReservedWidth || graphics == null || currentCategories == null || currentCategories.Length == 0)
            {
                return configuredWidth;
            }

            LightningBarCategoryLabelOptions categoryOptions = currentOptions.CategoryLabels ?? new LightningBarCategoryLabelOptions();
            float measuredWidth = 0f;
            int maxLines = Math.Max(1, categoryOptions.MaxLines);
            using (var labelFont = CreateChartFont(categoryOptions.FontSize, FontStyle.Regular))
            {
                for (int i = 0; i < currentCategories.Length; i++)
                {
                    string[] lines = GetCategoryLabelLines(currentCategories[i], maxLines, categoryOptions);
                    for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                    {
                        SizeF lineSize = graphics.MeasureString(lines[lineIndex] ?? string.Empty, labelFont);
                        measuredWidth = Math.Max(measuredWidth, lineSize.Width);
                    }
                }
            }

            float minWidth = Math.Max(0f, layout.MinCategoryLabelReservedWidth);
            float requiredWidth = measuredWidth + 18f;
            return Math.Max(Math.Max(minWidth, configuredWidth), requiredWidth);
        }

        protected virtual float GetMaxValue(string[] currentCategories, LightningBarSeries[] currentSeries, LightningBarOptions currentOptions)
        {
            float valueFromData = 0f;
            for (int i = 0; i < currentSeries.Length; i++)
            {
                LightningBarSeries barSeries = currentSeries[i];
                if (barSeries == null)
                {
                    continue;
                }

                for (int j = 0; j < Math.Min(currentCategories.Length, GetSeriesValueCount(barSeries)); j++)
                {
                    valueFromData = Math.Max(valueFromData, GetSeriesValue(barSeries, j));
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
            using (var labelFont = CreateChartFont(scaleOptions.FontSize, FontStyle.Regular))
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

            if (useFixedBarHeight && barOptions.ClampFixedHeightToGroup)
            {
                float actualGapWidth = barGap * Math.Max(0, seriesCount - 1);
                float maxBarHeightInGroup = (usableGroupHeight - actualGapWidth) / Math.Max(1, seriesCount);
                if (maxBarHeightInGroup >= barOptions.MinHeight)
                {
                    barHeight = Math.Min(barHeight, maxBarHeightInGroup);
                }
            }

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

                    float value = Math.Max(0f, GetSeriesValue(barSeries, categoryIndex));

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
                        object[] seriesRawData = GetSeriesRawData(barSeries);
                        barHitInfos.Add(new LightningBarHitInfo
                        {
                            Bounds = barRect,
                            CategoryName = currentCategories[categoryIndex],
                            CategoryIndex = categoryIndex,
                            Series = CreateEventSeries(barSeries, seriesRawData),
                            SeriesIndex = seriesIndex,
                            Value = value,
                            DataPoint = CreateHitDataPoint(barSeries, categoryIndex, value),
                            SeriesRawData = seriesRawData
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

            using (var labelFont = CreateChartFont(seriesLabelOptions.FontSize, FontStyle.Regular))
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

            using (var labelFont = CreateChartFont(categoryOptions.FontSize, FontStyle.Regular))
            using (var labelBrush = new SolidBrush(categoryOptions.Color))
            using (var rightCenterFormat = new StringFormat(StringFormat.GenericTypographic))
            {
                int maxLines = Math.Max(1, categoryOptions.MaxLines);
                float lineHeight = labelFont.GetHeight(graphics) + Math.Max(0f, categoryOptions.LineSpacing);
                float groupHeight = plotRect.Height / currentCategories.Length;
                float textRight = plotRect.Left - 8f;

                rightCenterFormat.Alignment = StringAlignment.Far;
                rightCenterFormat.LineAlignment = StringAlignment.Center;
                rightCenterFormat.Trimming = StringTrimming.None;
                rightCenterFormat.FormatFlags |= StringFormatFlags.NoWrap;

                for (int i = 0; i < currentCategories.Length; i++)
                {
                    string[] labelLines = GetCategoryLabelLines(currentCategories[i], maxLines, categoryOptions);
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

        protected virtual string[] GetCategoryLabelLines(string text, int maxLines, LightningBarCategoryLabelOptions categoryOptions)
        {
            string sourceText = text ?? string.Empty;
            LightningBarCategoryLabelOrientation orientation = categoryOptions == null
                ? LightningBarCategoryLabelOrientation.Horizontal
                : categoryOptions.Orientation;

            if (orientation == LightningBarCategoryLabelOrientation.Horizontal)
            {
                string horizontalText = sourceText
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n')
                    .Trim();

                return GetLegendTextLines(horizontalText, maxLines);
            }

            if (!sourceText.Contains("\n") && !sourceText.Contains("\r") && sourceText.Length > 1)
            {
                sourceText = string.Join("\n", sourceText.ToCharArray());
            }

            return GetLegendTextLines(sourceText, maxLines);
        }

        protected virtual void DrawLegend(Graphics graphics, LightningBarSeries[] currentSeries, LightningBarOptions currentOptions, Size renderSize)
        {
            DrawLegend(graphics, currentSeries, currentOptions, renderSize, null);
        }

        protected virtual void DrawLegend(Graphics graphics, LightningBarSeries[] currentSeries, LightningBarOptions currentOptions, Size renderSize, string[] currentCategories)
        {
            LightningBarLegendOptions legendOptions = currentOptions.Legend ?? new LightningBarLegendOptions();
            if (!legendOptions.Visible || currentSeries == null || currentSeries.Length == 0)
            {
                return;
            }

            float labelSpacing = Math.Max(0f, legendOptions.ItemSpacing);
            float sectionSpacing = Math.Max(0f, legendOptions.SectionSpacing);
            float markerWidth = Math.Max(1f, legendOptions.MarkerWidth);
            RectangleF plotRect = GetPlotRectangle(graphics, renderSize, currentOptions, currentCategories);

            using (var legendFont = CreateChartFont(legendOptions.FontSize, FontStyle.Regular))
            using (var textBrush = new SolidBrush(legendOptions.TextColor))
            {
                float totalLegendWidth = 0f;
                foreach (LightningBarSeries barSeries in currentSeries)
                {
                    SizeF textSize = MeasureLegendText(graphics, GetLegendLabel(barSeries), legendFont, currentOptions);
                    float textWidth = Math.Max(1f, textSize.Width);
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

                for (int seriesIndex = 0; seriesIndex < currentSeries.Length; seriesIndex++)
                {
                    LightningBarSeries barSeries = currentSeries[seriesIndex];
                    if (barSeries == null)
                    {
                        continue;
                    }

                    string legendText = GetLegendLabel(barSeries);
                    SizeF textSize = MeasureLegendText(graphics, legendText, legendFont, currentOptions);
                    float textWidth = Math.Max(1f, textSize.Width);
                    DrawLegendItem(graphics, legendFont, textBrush, legendX, legendY, barSeries.FillColor, barSeries.BorderColor, legendText, currentOptions);

                    if (collectBarHits)
                    {
                        float itemHeight = Math.Max(legendOptions.MarkerHeight, textSize.Height);
                        legendHitInfos.Add(new LightningBarLegendHitInfo
                        {
                            Bounds = new RectangleF(legendX, legendY - 4f, markerWidth + labelSpacing + textWidth, itemHeight + 8f),
                            Series = barSeries.Clone(),
                            SeriesIndex = seriesIndex,
                            LegendLabel = legendText
                        });
                    }

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

                int maxLines = Math.Max(1, legendOptions.LabelMaxLines);
                float lineHeight = font.GetHeight(graphics);
                string[] lines = GetLegendTextLines(text, maxLines);
                float textWidth = Math.Max(1f, MeasureTextLinesWidth(graphics, lines, font));
                RectangleF textRect = new RectangleF(x + markerWidth + legendOptions.ItemSpacing, y - 3f, textWidth + 4f, (lineHeight * lines.Length) + 8f);

                format.Alignment = StringAlignment.Near;
                format.LineAlignment = StringAlignment.Near;
                format.Trimming = StringTrimming.None;
                format.FormatFlags = 0;

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
            int maxLines = Math.Max(1, legendOptions.LabelMaxLines);
            string[] lines = GetLegendTextLines(text, maxLines);
            float textWidth = Math.Max(1f, MeasureTextLinesWidth(graphics, lines, font));
            float textHeight = (font.GetHeight(graphics) * Math.Max(1, lines.Length)) + 8f;
            return new SizeF(textWidth, textHeight);
        }

        protected virtual string[] GetLegendTextLines(string text, int maxLines)
        {
            string normalizedText = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            return normalizedText.Split(new[] { '\n' }, StringSplitOptions.None);
        }

        protected virtual float MeasureTextLinesWidth(Graphics graphics, string[] lines, Font font)
        {
            if (graphics == null || font == null || lines == null || lines.Length == 0)
            {
                return 1f;
            }

            float maxWidth = 1f;
            foreach (string line in lines)
            {
                SizeF lineSize = graphics.MeasureString(line ?? string.Empty, font);
                maxWidth = Math.Max(maxWidth, lineSize.Width);
            }

            return maxWidth;
        }

        protected virtual bool IsRawDataButtonVisible(LightningBarOptions currentOptions)
        {
            return currentOptions != null
                && currentOptions.RawData != null
                && currentOptions.RawData.ButtonMode == LightningBarRawDataButtonMode.Visible;
        }

        protected virtual LightningBarImageOptions CreateEffectiveImageOptions(LightningBarImageOptions imageOptions)
        {
            LightningBarImageOptions effectiveOptions = imageOptions == null
                ? new LightningBarImageOptions()
                : imageOptions.Clone();

            if (effectiveOptions.Preset == LightningBarImagePreset.ChartZoom)
            {
                if (effectiveOptions.Width <= 400)
                {
                    effectiveOptions.Width = 900;
                }

                if (effectiveOptions.Height <= 400)
                {
                    effectiveOptions.Height = 600;
                }

                if (effectiveOptions.DpiX <= 96f)
                {
                    effectiveOptions.DpiX = 150f;
                }

                if (effectiveOptions.DpiY <= 96f)
                {
                    effectiveOptions.DpiY = 150f;
                }

                if (effectiveOptions.ContentScale <= 1f)
                {
                    effectiveOptions.ContentScale = 1.2f;
                }

                effectiveOptions.OptimizeForExcel = true;
                effectiveOptions.ReduceOuterPadding = true;
                effectiveOptions.HideRawDataButtonOnImage = true;
            }

            return effectiveOptions;
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
            using (var buttonFont = CreateChartFont(8.5f, FontStyle.Regular))
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                graphics.FillRectangle(fillBrush, buttonBounds);
                graphics.DrawRectangle(borderPen, buttonBounds.X, buttonBounds.Y, buttonBounds.Width, buttonBounds.Height);
                graphics.DrawString(rawDataOptions.ButtonText ?? "RawData", buttonFont, textBrush, buttonBounds, format);
            }
        }

        protected virtual void ApplyImageRenderOptions(LightningBarOptions renderOptions, LightningBarImageOptions imageOptions)
        {
            if (renderOptions == null || imageOptions == null)
            {
                return;
            }

            renderOptions.EnsureGroups();

            if (imageOptions.HideRawDataButtonOnImage && renderOptions.RawData != null)
            {
                renderOptions.RawData.ButtonMode = LightningBarRawDataButtonMode.Hidden;
            }

            bool reducePadding = imageOptions.OptimizeForExcel || imageOptions.ReduceOuterPadding;
            if (reducePadding && renderOptions.Layout != null)
            {
                renderOptions.Layout.ChartPadding = Math.Max(6, (int)Math.Round(renderOptions.Layout.ChartPadding * 0.55f));
                renderOptions.Layout.TopOffset = Math.Max(48, Math.Min(renderOptions.Layout.TopOffset, 68));
                renderOptions.Layout.BottomScaleAreaHeight = Math.Max(22f, renderOptions.Layout.BottomScaleAreaHeight * 0.72f);
                renderOptions.Layout.AutoCategoryLabelReservedWidth = true;
                renderOptions.Layout.LegendReservedWidthMode = LightningBarLegendReservedWidthMode.CollapseForTopBottomLegend;

                if (renderOptions.Legend == null || renderOptions.Legend.Position == LightningBarLegendPosition.Top || renderOptions.Legend.Position == LightningBarLegendPosition.Bottom)
                {
                    renderOptions.Layout.LegendReservedWidth = 0;
                }
            }

            float scale = Math.Max(0.1f, imageOptions.ContentScale);
            if (Math.Abs(scale - 1f) < 0.0001f)
            {
                return;
            }

            ScaleImageRenderText(renderOptions, scale);
        }

        protected virtual void ScaleImageRenderText(LightningBarOptions renderOptions, float scale)
        {
            if (renderOptions.TitleOptions != null)
            {
                renderOptions.TitleOptions.FontSize = ScaleValue(renderOptions.TitleOptions.FontSize, scale, 1f);
            }

            if (renderOptions.Legend != null)
            {
                renderOptions.Legend.FontSize = ScaleValue(renderOptions.Legend.FontSize, scale, 1f);
                renderOptions.Legend.MarkerWidth = ScaleValue(renderOptions.Legend.MarkerWidth, scale, 1f);
                renderOptions.Legend.MarkerHeight = ScaleValue(renderOptions.Legend.MarkerHeight, scale, 1f);
                renderOptions.Legend.LabelMaxWidth = ScaleValue(renderOptions.Legend.LabelMaxWidth, scale, 1f);
                renderOptions.Legend.ItemSpacing = ScaleValue(renderOptions.Legend.ItemSpacing, scale, 0f);
                renderOptions.Legend.SectionSpacing = ScaleValue(renderOptions.Legend.SectionSpacing, scale, 0f);
            }

            if (renderOptions.CategoryLabels != null)
            {
                renderOptions.CategoryLabels.FontSize = ScaleValue(renderOptions.CategoryLabels.FontSize, scale, 1f);
                renderOptions.CategoryLabels.LineSpacing = ScaleValue(renderOptions.CategoryLabels.LineSpacing, scale, 0f);
            }

            if (renderOptions.Scale != null)
            {
                renderOptions.Scale.FontSize = ScaleValue(renderOptions.Scale.FontSize, scale, 1f);
            }

            if (renderOptions.SeriesLabels != null)
            {
                renderOptions.SeriesLabels.FontSize = ScaleValue(renderOptions.SeriesLabels.FontSize, scale, 1f);
                renderOptions.SeriesLabels.MaxWidth = ScaleValue(renderOptions.SeriesLabels.MaxWidth, scale, 1f);
            }

            if (renderOptions.Bars != null)
            {
                renderOptions.Bars.BorderWidth = ScaleValue(renderOptions.Bars.BorderWidth, scale, 0.5f);
                renderOptions.Bars.Gap = ScaleValue(renderOptions.Bars.Gap, scale, 0f);
                renderOptions.Bars.FixedHeight = ScaleValue(renderOptions.Bars.FixedHeight, scale, renderOptions.Bars.MinHeight);
            }

            if (renderOptions.NoData != null)
            {
                renderOptions.NoData.FontSize = ScaleValue(renderOptions.NoData.FontSize, scale, 1f);
                renderOptions.NoData.BadgeFixedWidth = ScaleValue(renderOptions.NoData.BadgeFixedWidth, scale, 1f);
                renderOptions.NoData.BadgeHorizontalPadding = ScaleValue(renderOptions.NoData.BadgeHorizontalPadding, scale, 0f);
                renderOptions.NoData.BadgeVerticalPadding = ScaleValue(renderOptions.NoData.BadgeVerticalPadding, scale, 0f);
                renderOptions.NoData.BadgeMinWidth = ScaleValue(renderOptions.NoData.BadgeMinWidth, scale, 0f);
                renderOptions.NoData.BadgeMinHeight = ScaleValue(renderOptions.NoData.BadgeMinHeight, scale, 0f);
            }

            if (renderOptions.Layout != null)
            {
                renderOptions.Layout.CategoryLabelReservedWidth = ScaleValue(renderOptions.Layout.CategoryLabelReservedWidth, scale, 1f);
                renderOptions.Layout.MinCategoryLabelReservedWidth = ScaleValue(renderOptions.Layout.MinCategoryLabelReservedWidth, scale, 1f);
                renderOptions.Layout.MaxCategoryLabelReservedWidth = ScaleValue(renderOptions.Layout.MaxCategoryLabelReservedWidth, scale, renderOptions.Layout.MinCategoryLabelReservedWidth);
            }
        }

        protected virtual float ScaleValue(float value, float scale, float minimum)
        {
            return Math.Max(minimum, value * scale);
        }

        protected virtual string ResolveImageFilePath(LightningBarImageOptions imageOptions)
        {
            string directory = ResolveImageSaveDirectory(imageOptions);
            directory = Path.GetFullPath(directory);
            Directory.CreateDirectory(directory);

            string extension = GetImageFileExtension(imageOptions.FileFormat);
            string fileName = imageOptions.UseGuidFileName
                ? string.Format("{0}{1}", Guid.NewGuid().ToString("N"), extension)
                : Path.GetFileName((imageOptions.FileName ?? string.Empty).Trim());

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = string.Format("{0}{1}", Guid.NewGuid().ToString("N"), extension);
            }

            fileName = Path.ChangeExtension(fileName, extension);
            return Path.Combine(directory, fileName);
        }

        protected virtual string ResolveImageSaveDirectory(LightningBarImageOptions imageOptions)
        {
            string baseDirectory = string.IsNullOrWhiteSpace(imageOptions.SaveDirectory)
                ? ResolveImageSaveFolder(imageOptions.SaveFolder)
                : imageOptions.SaveDirectory;

            string subDirectoryName = imageOptions.SubDirectoryName ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(subDirectoryName))
            {
                baseDirectory = Path.Combine(baseDirectory, SanitizePathSegment(subDirectoryName.Trim()));
            }

            if (imageOptions.UseDateFolder)
            {
                baseDirectory = Path.Combine(baseDirectory, DateTime.Now.ToString("yyyyMMdd"));
            }

            return baseDirectory;
        }

        protected virtual string ResolveImageSaveFolder(LightningBarImageSaveFolder saveFolder)
        {
            string resolvedPath;
            switch (saveFolder)
            {
                case LightningBarImageSaveFolder.RoamingApplicationData:
                    resolvedPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    break;
                case LightningBarImageSaveFolder.MyDocuments:
                    resolvedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    break;
                case LightningBarImageSaveFolder.Temp:
                    resolvedPath = Path.GetTempPath();
                    break;
                case LightningBarImageSaveFolder.LocalApplicationData:
                default:
                    resolvedPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    break;
            }

            return string.IsNullOrWhiteSpace(resolvedPath) ? Path.GetTempPath() : resolvedPath;
        }

        protected virtual string SanitizePathSegment(string pathSegment)
        {
            if (string.IsNullOrWhiteSpace(pathSegment))
            {
                return string.Empty;
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            char[] chars = pathSegment.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (invalidChars.Contains(chars[i]))
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }

        protected virtual string GetImageFileExtension(LightningBarImageFileFormat fileFormat)
        {
            return fileFormat == LightningBarImageFileFormat.Jpeg ? ".jpg" : ".png";
        }

        protected virtual void StoreSavedImage(string fullPath, Image image)
        {
            Bitmap imageCopy = image == null ? null : CloneImagePreservingResolution(image);

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

        private static Bitmap CloneImagePreservingResolution(Image image)
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
                    float value = GetSeriesValue(barSeries, categoryIndex);
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

        protected virtual LightningBarLegendHitInfo FindLegendHit(Point location)
        {
            return legendHitInfos.FirstOrDefault(item => item.Bounds.Contains(location));
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
                    hitInfo.CategoryIndex,
                    hitInfo.DataPoint == null ? null : hitInfo.DataPoint.UserData);
            }
            catch (FormatException)
            {
                return string.Format("Value:{0:0.#}", hitInfo.Value);
            }
        }

        protected virtual void HideSeriesToolTip()
        {
            currentToolTipText = string.Empty;
            if (!IsDisposed)
            {
                seriesToolTip.Hide(this);
            }
        }

        protected virtual void OnSeriesClicked(LightningBarSeriesClickEventArgs e)
        {
            EventHandler<LightningBarSeriesClickEventArgs> barHandler = BarClicked;
            if (barHandler != null)
            {
                barHandler(this, e);
            }

            EventHandler<LightningBarSeriesClickEventArgs> handler = SeriesClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnLegendClicked(LightningBarLegendClickEventArgs e)
        {
            EventHandler<LightningBarLegendClickEventArgs> handler = LegendClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnImageSaving(LightningBarImageSavingEventArgs e)
        {
            EventHandler<LightningBarImageSavingEventArgs> handler = ImageSaving;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnImageSaved(LightningBarImageSavedEventArgs e)
        {
            EventHandler<LightningBarImageSavedEventArgs> handler = ImageSaved;
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

            public LightningBarDataPoint DataPoint { get; set; }

            public object[] SeriesRawData { get; set; }
        }

        protected class LightningBarLegendHitInfo
        {
            public RectangleF Bounds { get; set; }

            public LightningBarSeries Series { get; set; }

            public int SeriesIndex { get; set; }

            public string LegendLabel { get; set; }
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

                if (barSeries.ValueSource != null)
                {
                    LightningBarDataPoint[] sourceDataPoints = LightningBarDataPoint.FromDataSource(barSeries.ValueSource, barSeries.ValueColumnName);
                    if (sourceDataPoints.Length == 0)
                    {
                        barSeries.DataPoints = new LightningBarDataPoint[0];
                        barSeries.Values = new float[0];
                        continue;
                    }

                    NormalizeDataPoints(barSeries, sourceDataPoints, targetCount, new float[0]);
                    continue;
                }

                LightningBarDataPoint[] dataPoints = barSeries.DataPoints ?? new LightningBarDataPoint[0];
                if (dataPoints.Length > 0)
                {
                    NormalizeDataPoints(barSeries, dataPoints, targetCount, barSeries.Values ?? new float[0]);
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

        protected virtual void NormalizeDataPoints(LightningBarSeries barSeries, LightningBarDataPoint[] dataPoints, int targetCount, float[] fallbackValues)
        {
            LightningBarDataPoint[] normalizedPoints = new LightningBarDataPoint[targetCount];
            float[] safeFallbackValues = fallbackValues ?? new float[0];
            for (int i = 0; i < targetCount; i++)
            {
                if (i < dataPoints.Length && dataPoints[i] != null)
                {
                    normalizedPoints[i] = dataPoints[i].Clone();
                    continue;
                }

                float fallbackValue = i < safeFallbackValues.Length ? safeFallbackValues[i] : 0f;
                normalizedPoints[i] = new LightningBarDataPoint(fallbackValue);
            }

            barSeries.DataPoints = normalizedPoints;
            barSeries.Values = normalizedPoints.Select(item => item.Value).ToArray();
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
