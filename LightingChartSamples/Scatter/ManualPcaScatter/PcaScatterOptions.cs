using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LightingChartSamples.Scatter;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.ManualPcaScatter
{
    public sealed class PcaScatterAnalysisOptions
    {
        public PcaScatterAnalysisOptions()
        {
            ConstantVarianceThreshold = 1e-10d;
            MinimumNumericFeatureCoverageRatio = 0.90d;
            MeanImputationEnabled = true;
            ComponentCount = 2;
            MaxIterations = 2000;
            ConvergenceTolerance = 1e-10d;
            NeighborCount = 3;
            KnnSearchAlgorithm = KnnSearchAlgorithm.Auto;
        }

        public double ConstantVarianceThreshold { get; set; }
        public double MinimumNumericFeatureCoverageRatio { get; set; }
        public bool MeanImputationEnabled { get; set; }
        public int ComponentCount { get; set; }
        public int MaxIterations { get; set; }
        public double ConvergenceTolerance { get; set; }
        public int NeighborCount { get; set; }
        public KnnSearchAlgorithm KnnSearchAlgorithm { get; set; }

        internal PcaAnalysisOptions ToPipelineOptions()
        {
            return new PcaAnalysisOptions
            {
                ConstantVarianceThreshold = ConstantVarianceThreshold,
                MinimumNumericFeatureCoverageRatio = MinimumNumericFeatureCoverageRatio,
                MeanImputationEnabled = MeanImputationEnabled,
                ComponentCount = ComponentCount,
                MaxIterations = MaxIterations,
                ConvergenceTolerance = ConvergenceTolerance,
                NeighborCount = NeighborCount,
                KnnSearchAlgorithm = KnnSearchAlgorithm
            };
        }

        public PcaScatterAnalysisOptions Clone()
        {
            return (PcaScatterAnalysisOptions)MemberwiseClone();
        }
    }

    public sealed class PcaScatterSeriesOptions
    {
        public PcaScatterSeriesOptions()
        {
            PointSize = 15f;
            ShowLine = false;
            ShowPoints = true;
            PassResultName = PcaJsonSampleDataFactory.PassResult;
            ReviewResultName = PcaJsonSampleDataFactory.ReviewResult;
            PassColor = Color.FromArgb(151, 211, 169);
            ReviewColor = Color.FromArgb(238, 171, 210);
            DefaultColor = Color.FromArgb(129, 178, 231);
            HighlightColor = Color.Black;
            HighlightPointSize = 19f;
            SelectedDraftNo = string.Empty;
            SelectedPointColor = Color.FromArgb(255, 242, 128);
            SelectedPointBorderColor = Color.Red;
            SelectedPointBorderWidth = 2.8f;
            SelectedPointSize = 24f;
            SeriesOrder = new[] { PassResultName, ReviewResultName };
            SeriesColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            PastelPalette = LightningScatterOptions.CreateDefaultPastelPalette();
        }

        public float PointSize { get; set; }
        public bool ShowLine { get; set; }
        public bool ShowPoints { get; set; }
        public string PassResultName { get; set; }
        public string ReviewResultName { get; set; }
        public Color PassColor { get; set; }
        public Color ReviewColor { get; set; }
        public Color DefaultColor { get; set; }
        public string HighlightDraftNo { get; set; }
        public Color HighlightColor { get; set; }
        public float HighlightPointSize { get; set; }
        public string SelectedDraftNo { get; set; }
        public Color SelectedPointColor { get; set; }
        public Color SelectedPointBorderColor { get; set; }
        public float SelectedPointBorderWidth { get; set; }
        public float SelectedPointSize { get; set; }
        public string[] SeriesOrder { get; set; }
        public IDictionary<string, Color> SeriesColors { get; set; }
        public Color[] PastelPalette { get; set; }
        public Func<ScatterSampleData, string> SeriesNameSelector { get; set; }
        public Func<string, string> LegendLabelFormatter { get; set; }

        public PcaScatterSeriesOptions Clone()
        {
            return new PcaScatterSeriesOptions
            {
                PointSize = PointSize,
                ShowLine = ShowLine,
                ShowPoints = ShowPoints,
                PassResultName = PassResultName,
                ReviewResultName = ReviewResultName,
                PassColor = PassColor,
                ReviewColor = ReviewColor,
                DefaultColor = DefaultColor,
                HighlightDraftNo = HighlightDraftNo,
                HighlightColor = HighlightColor,
                HighlightPointSize = HighlightPointSize,
                SelectedDraftNo = SelectedDraftNo,
                SelectedPointColor = SelectedPointColor,
                SelectedPointBorderColor = SelectedPointBorderColor,
                SelectedPointBorderWidth = SelectedPointBorderWidth,
                SelectedPointSize = SelectedPointSize,
                SeriesOrder = SeriesOrder == null ? new string[0] : SeriesOrder.ToArray(),
                SeriesColors = SeriesColors == null
                    ? new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, Color>(SeriesColors, StringComparer.OrdinalIgnoreCase),
                PastelPalette = PastelPalette == null ? LightningScatterOptions.CreateDefaultPastelPalette() : (Color[])PastelPalette.Clone(),
                SeriesNameSelector = SeriesNameSelector,
                LegendLabelFormatter = LegendLabelFormatter
            };
        }
    }

    public sealed class PcaScatterDisplayOptions
    {
        public PcaScatterDisplayOptions()
        {
            FontName = "留묒? 怨좊뵓";
            ShowTitle = false;
            Title = string.Empty;
            BackgroundColor = Color.White;
            GraphBackgroundColor = Color.White;
            XAxisTitle = "X1";
            YAxisTitle = "X2";
            AutoCalculateAxisRange = true;
            IncludeZeroInAxisRange = true;
            AxisPaddingRatio = 0.08d;
            MinimumAxisPadding = 0.2d;
            MajorDivCount = 8;
            AxisLabelFormat = "0.##";
            GridLinesVisible = false;
            MinorGridLinesVisible = false;
            GridColor = Color.FromArgb(232, 234, 238);
        }

        public string FontName { get; set; }
        public bool ShowTitle { get; set; }
        public string Title { get; set; }
        public Color BackgroundColor { get; set; }
        public Color GraphBackgroundColor { get; set; }
        public string XAxisTitle { get; set; }
        public string YAxisTitle { get; set; }
        public bool AutoCalculateAxisRange { get; set; }
        public bool IncludeZeroInAxisRange { get; set; }
        public double AxisPaddingRatio { get; set; }
        public double MinimumAxisPadding { get; set; }
        public int MajorDivCount { get; set; }
        public string AxisLabelFormat { get; set; }
        public bool GridLinesVisible { get; set; }
        public bool MinorGridLinesVisible { get; set; }
        public Color GridColor { get; set; }

        public PcaScatterDisplayOptions Clone()
        {
            return (PcaScatterDisplayOptions)MemberwiseClone();
        }
    }

    public sealed class PcaScatterOptions
    {
        public PcaScatterOptions()
        {
            Analysis = new PcaScatterAnalysisOptions();
            Series = new PcaScatterSeriesOptions();
            Display = new PcaScatterDisplayOptions();
            Legend = new LightningScatterLegendOptions
            {
                Position = LightningScatterLegendPosition.TopCenter,
                ShowCheckboxes = true,
                BackgroundColor = Color.White,
                BorderColor = Color.FromArgb(220, 220, 220)
            };
            Tooltip = new LightningScatterTooltipOptions
            {
                Enabled = true,
                HitPixelTolerance = 14,
                Format = "{5}\r\nX1:{1:0.###}, X2:{2:0.###}\r\nAI_RSLT_Val:{0}"
            };
            NoData = new LightningScatterNoDataOptions
            {
                Text = "PCA Scatter ?곗씠?곌? ?놁뒿?덈떎.",
                ShowWhenDataMissing = true,
                ShowWhenAllValuesZero = false,
                FontSize = 11f,
                TextAlignment = LightningScatterTextAlignment.Center,
                BadgeSingleLine = true,
                BadgeHorizontalPadding = 10f,
                BadgeVerticalPadding = 4f
            };
            Image = new LightningScatterImageOptions
            {
                Width = 600,
                Height = 400,
                SubDirectoryName = "PcaScatterImages"
            };
            Interaction = new LightningScatterInteractionOptions
            {
                ZoomEnabled = false,
                PanEnabled = false,
                MouseWheelZoomEnabled = false,
                AllowInternalMouseCursorChange = false
            };
        }

        public PcaScatterAnalysisOptions Analysis { get; set; }
        public PcaScatterSeriesOptions Series { get; set; }
        public PcaScatterDisplayOptions Display { get; set; }
        public LightningScatterLegendOptions Legend { get; set; }
        public LightningScatterTooltipOptions Tooltip { get; set; }
        public LightningScatterNoDataOptions NoData { get; set; }
        public LightningScatterImageOptions Image { get; set; }
        public LightningScatterInteractionOptions Interaction { get; set; }
        public Action<LightningScatterOptions> CustomizeScatterOptions { get; set; }

        public static PcaScatterOptions CreateDefault()
        {
            return new PcaScatterOptions();
        }

        public static PcaScatterOptions CreateDefault600x400()
        {
            return new PcaScatterOptions
            {
                Image = new LightningScatterImageOptions
                {
                    Width = 600,
                    Height = 400,
                    SubDirectoryName = "PcaScatterImages"
                }
            };
        }

        public static PcaScatterOptions CreateExcelImageOptimized()
        {
            PcaScatterOptions options = CreateDefault600x400();
            options.Image.Width = 900;
            options.Image.Height = 600;
            options.Image.SubDirectoryName = "PcaScatterExcelImages";
            options.Display.AxisPaddingRatio = 0.05d;
            options.Display.MinimumAxisPadding = 0.1d;
            options.Legend.FontSize = 8f;
            options.Series.PointSize = 17f;
            return options;
        }

        public PcaScatterOptions Clone()
        {
            return new PcaScatterOptions
            {
                Analysis = Analysis == null ? new PcaScatterAnalysisOptions() : Analysis.Clone(),
                Series = Series == null ? new PcaScatterSeriesOptions() : Series.Clone(),
                Display = Display == null ? new PcaScatterDisplayOptions() : Display.Clone(),
                Legend = Legend == null ? new LightningScatterLegendOptions() : Legend.Clone(),
                Tooltip = Tooltip == null ? new LightningScatterTooltipOptions() : Tooltip.Clone(),
                NoData = NoData == null ? new LightningScatterNoDataOptions() : NoData.Clone(),
                Image = Image == null ? new LightningScatterImageOptions() : Image.Clone(),
                Interaction = Interaction == null ? new LightningScatterInteractionOptions() : Interaction.Clone(),
                CustomizeScatterOptions = CustomizeScatterOptions
            };
        }

        internal LightningScatterOptions ToScatterOptions(PcaAnalysisResult analysisResult)
        {
            PcaScatterOptions snapshot = Clone();
            LightningScatterOptions scatterOptions = LightningScatterOptions.CreateDefaultBubble();
            PcaScatterDisplayOptions display = snapshot.Display ?? new PcaScatterDisplayOptions();

            scatterOptions.FontName = string.IsNullOrWhiteSpace(display.FontName) ? "留묒? 怨좊뵓" : display.FontName.Trim();
            scatterOptions.ShowTitle = display.ShowTitle;
            scatterOptions.Title = display.Title ?? string.Empty;
            scatterOptions.BackgroundColor = display.BackgroundColor;
            scatterOptions.GraphBackgroundColor = display.GraphBackgroundColor;
            scatterOptions.Legend = snapshot.Legend ?? new LightningScatterLegendOptions();
            scatterOptions.Tooltip = snapshot.Tooltip ?? new LightningScatterTooltipOptions();
            scatterOptions.NoData = snapshot.NoData ?? new LightningScatterNoDataOptions();
            scatterOptions.Image = snapshot.Image ?? new LightningScatterImageOptions();
            scatterOptions.Interaction = snapshot.Interaction ?? new LightningScatterInteractionOptions();
            scatterOptions.Style.UsePastelPalette = false;
            scatterOptions.Style.ForceBubbleStyle = true;
            scatterOptions.Style.BubbleSize = Math.Max(1f, (snapshot.Series ?? new PcaScatterSeriesOptions()).PointSize);

            ApplyAxisOptions(scatterOptions, analysisResult, display);

            if (snapshot.CustomizeScatterOptions != null)
            {
                snapshot.CustomizeScatterOptions(scatterOptions);
            }

            return scatterOptions;
        }

        private static void ApplyAxisOptions(LightningScatterOptions scatterOptions, PcaAnalysisResult analysisResult, PcaScatterDisplayOptions display)
        {
            AxisRange xRange = CalculateRange(
                analysisResult == null ? null : analysisResult.ScatterData.Select(item => item.X1),
                display);
            AxisRange yRange = CalculateRange(
                analysisResult == null ? null : analysisResult.ScatterData.Select(item => item.X2),
                display);

            scatterOptions.XAxis.Title = display.XAxisTitle ?? string.Empty;
            scatterOptions.XAxis.AutoFit = false;
            scatterOptions.XAxis.Minimum = xRange.Minimum;
            scatterOptions.XAxis.Maximum = xRange.Maximum;
            scatterOptions.XAxis.MajorDivCount = Math.Max(1, display.MajorDivCount);
            scatterOptions.XAxis.LabelFormat = string.IsNullOrWhiteSpace(display.AxisLabelFormat) ? "0.##" : display.AxisLabelFormat;
            scatterOptions.XAxis.GridLinesVisible = display.GridLinesVisible;
            scatterOptions.XAxis.MinorGridLinesVisible = display.MinorGridLinesVisible;
            scatterOptions.XAxis.GridColor = display.GridColor;

            scatterOptions.YAxis.Title = display.YAxisTitle ?? string.Empty;
            scatterOptions.YAxis.AutoFit = false;
            scatterOptions.YAxis.Minimum = yRange.Minimum;
            scatterOptions.YAxis.Maximum = yRange.Maximum;
            scatterOptions.YAxis.MajorDivCount = Math.Max(1, display.MajorDivCount);
            scatterOptions.YAxis.LabelFormat = string.IsNullOrWhiteSpace(display.AxisLabelFormat) ? "0.##" : display.AxisLabelFormat;
            scatterOptions.YAxis.GridLinesVisible = display.GridLinesVisible;
            scatterOptions.YAxis.MinorGridLinesVisible = display.MinorGridLinesVisible;
            scatterOptions.YAxis.GridColor = display.GridColor;
        }

        private static AxisRange CalculateRange(IEnumerable<double> values, PcaScatterDisplayOptions display)
        {
            List<double> cleanValues = values == null
                ? new List<double>()
                : values.Where(value => !double.IsNaN(value) && !double.IsInfinity(value)).ToList();

            if (!display.AutoCalculateAxisRange || cleanValues.Count == 0)
            {
                return new AxisRange(-1d, 1d);
            }

            double minimum = cleanValues.Min();
            double maximum = cleanValues.Max();
            if (display.IncludeZeroInAxisRange)
            {
                minimum = Math.Min(0d, minimum);
                maximum = Math.Max(0d, maximum);
            }

            if (Math.Abs(maximum - minimum) < 0.000001d)
            {
                minimum -= 1d;
                maximum += 1d;
            }

            double padding = Math.Max(Math.Max(0d, display.MinimumAxisPadding), (maximum - minimum) * Math.Max(0d, display.AxisPaddingRatio));
            return new AxisRange(minimum - padding, maximum + padding);
        }

        private sealed class AxisRange
        {
            public AxisRange(double minimum, double maximum)
            {
                Minimum = minimum;
                Maximum = maximum;
            }

            public double Minimum { get; private set; }
            public double Maximum { get; private set; }
        }
    }
}
