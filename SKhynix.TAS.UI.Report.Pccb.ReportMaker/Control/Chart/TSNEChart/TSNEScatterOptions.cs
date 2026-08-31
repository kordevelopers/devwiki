using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.Common;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public sealed class TSNEScatterAnalysisOptions
    {
        public TSNEScatterAnalysisOptions()
        {
            ConstantVarianceThreshold = 1e-10d;
            MinimumNumericFeatureCoverageRatio = 0.90d;
            MeanImputationEnabled = true;
            ComponentCount = 2;
            MaxIterations = 2000;
            ConvergenceTolerance = 1e-10d;
            NeighborCount = 3;
            KnnSearchAlgorithm = KnnSearchAlgorithm.Auto;
            ProjectionMethod = DimensionalityReductionMethod.TSNE;
            TSNEPerplexity = 30d;
            TSNEIterations = 750;
            TSNELearningRate = 200d;
            TSNERandomSeed = 20260831;
        }

        public double ConstantVarianceThreshold { get; set; }
        public double MinimumNumericFeatureCoverageRatio { get; set; }
        public bool MeanImputationEnabled { get; set; }
        public int ComponentCount { get; set; }
        public int MaxIterations { get; set; }
        public double ConvergenceTolerance { get; set; }
        public int NeighborCount { get; set; }
        public KnnSearchAlgorithm KnnSearchAlgorithm { get; set; }
        public DimensionalityReductionMethod ProjectionMethod { get; set; }
        public double TSNEPerplexity { get; set; }
        public int TSNEIterations { get; set; }
        public double TSNELearningRate { get; set; }
        public int TSNERandomSeed { get; set; }

        internal TSNEAnalysisOptions ToPipelineOptions()
        {
            return new TSNEAnalysisOptions
            {
                ConstantVarianceThreshold = ConstantVarianceThreshold,
                MinimumNumericFeatureCoverageRatio = MinimumNumericFeatureCoverageRatio,
                MeanImputationEnabled = MeanImputationEnabled,
                ComponentCount = ComponentCount,
                MaxIterations = MaxIterations,
                ConvergenceTolerance = ConvergenceTolerance,
                NeighborCount = NeighborCount,
                KnnSearchAlgorithm = KnnSearchAlgorithm,
                ProjectionMethod = ProjectionMethod,
                TSNEPerplexity = TSNEPerplexity,
                TSNEIterations = TSNEIterations,
                TSNELearningRate = TSNELearningRate,
                TSNERandomSeed = TSNERandomSeed
            };
        }

        public TSNEScatterAnalysisOptions Clone()
        {
            return (TSNEScatterAnalysisOptions)MemberwiseClone();
        }
    }

    public sealed class TSNEScatterSeriesOptions
    {
        public TSNEScatterSeriesOptions()
        {
            PointSize = 7f;
            PointShape = LightningScatterPointShape.RoundedRectangle;
            ShowLine = false;
            ShowPoints = true;
            UsePaletteColors = true;
            RequireSeriesLabel = true;
            ApplyColorAlpha = true;
            ColorTransparencyPercent = 20f;
            ColorAlpha = ResolveAlphaFromTransparencyPercent(ColorTransparencyPercent, 190);
            ApplyBorderTransparency = false;
            BorderTransparencyPercent = 0f;
            NaSeriesName = string.Empty;
            NaSeriesColor = Color.Empty;
            PassResultName = "Pass";
            ReviewResultName = "Review";
            PassColor = Color.Red;
            ReviewColor = Color.Green;
            DefaultColor = Color.Red;
            HighlightColor = Color.Yellow;
            HighlightPointBorderColor = Color.Yellow;
            HighlightPointBorderWidth = 1f;
            HighlightPointSize = 0f;
            SelectedDraftNo = string.Empty;
            SelectedPointColor = Color.Empty;
            SelectedPointBorderColor = Color.Lime;
            SelectedPointBorderWidth = 2.2f;
            SelectedPointSize = 0f;
            SeriesOrder = new[] { PassResultName, ReviewResultName, "FAIL" };
            SeriesColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            PastelPalette = CreateCompanySeriesPalette();
            BorderPalette = CreateCompanySeriesBorderPalette();
        }

        public float PointSize { get; set; }
        public LightningScatterPointShape PointShape { get; set; }
        public bool ShowLine { get; set; }
        public bool ShowPoints { get; set; }
        public bool UsePaletteColors { get; set; }
        public bool RequireSeriesLabel { get; set; }
        public bool ApplyColorAlpha { get; set; }
        public int ColorAlpha { get; set; }
        public float ColorTransparencyPercent { get; set; }
        public bool ApplyBorderTransparency { get; set; }
        public float BorderTransparencyPercent { get; set; }
        public string NaSeriesName { get; set; }
        public Color NaSeriesColor { get; set; }
        public string PassResultName { get; set; }
        public string ReviewResultName { get; set; }
        public Color PassColor { get; set; }
        public Color ReviewColor { get; set; }
        public Color DefaultColor { get; set; }
        public string HighlightDraftNo { get; set; }
        public Color HighlightColor { get; set; }
        public Color HighlightPointBorderColor { get; set; }
        public float HighlightPointBorderWidth { get; set; }
        public float HighlightPointSize { get; set; }
        public string SelectedDraftNo { get; set; }
        public Color SelectedPointColor { get; set; }
        public Color SelectedPointBorderColor { get; set; }
        public float SelectedPointBorderWidth { get; set; }
        public float SelectedPointSize { get; set; }
        public string[] SeriesOrder { get; set; }
        public IDictionary<string, Color> SeriesColors { get; set; }
        public Color[] PastelPalette { get; set; }
        public Color[] BorderPalette { get; set; }
        public Func<ScatterSampleData, string> SeriesNameSelector { get; set; }
        public Func<string, string> LegendLabelFormatter { get; set; }

        public TSNEScatterSeriesOptions Clone()
        {
            return new TSNEScatterSeriesOptions
            {
                PointSize = PointSize,
                PointShape = PointShape,
                ShowLine = ShowLine,
                ShowPoints = ShowPoints,
                UsePaletteColors = UsePaletteColors,
                RequireSeriesLabel = RequireSeriesLabel,
                ApplyColorAlpha = ApplyColorAlpha,
                ColorAlpha = ColorAlpha,
                ColorTransparencyPercent = ColorTransparencyPercent,
                ApplyBorderTransparency = ApplyBorderTransparency,
                BorderTransparencyPercent = BorderTransparencyPercent,
                NaSeriesName = NaSeriesName,
                NaSeriesColor = NaSeriesColor,
                PassResultName = PassResultName,
                ReviewResultName = ReviewResultName,
                PassColor = PassColor,
                ReviewColor = ReviewColor,
                DefaultColor = DefaultColor,
                HighlightDraftNo = HighlightDraftNo,
                HighlightColor = HighlightColor,
                HighlightPointBorderColor = HighlightPointBorderColor,
                HighlightPointBorderWidth = HighlightPointBorderWidth,
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
                PastelPalette = PastelPalette == null ? CreateCompanySeriesPalette() : (Color[])PastelPalette.Clone(),
                BorderPalette = BorderPalette == null ? CreateCompanySeriesBorderPalette() : (Color[])BorderPalette.Clone(),
                SeriesNameSelector = SeriesNameSelector,
                LegendLabelFormatter = LegendLabelFormatter
            };
        }

        public static Color[] CreateCompanySeriesPalette()
        {
            return new[]
            {
                Color.DarkBlue,
                Color.Red,
                Color.Green,
                Color.Black,
                Color.Yellow,
                Color.Navy,
                Color.Orange,
                Color.OliveDrab,
                Color.Purple,
                Color.Lime,
                Color.Pink,
                Color.MistyRose,
                Color.LightCyan
            };
        }

        public static Color[] CreateCompanySeriesBorderPalette()
        {
            return new[]
            {
                Color.FromArgb(255, 0, 0, 96),
                Color.FromArgb(255, 160, 0, 0),
                Color.FromArgb(255, 0, 110, 0),
                Color.FromArgb(255, 0, 0, 0),
                Color.FromArgb(255, 180, 150, 0),
                Color.FromArgb(255, 0, 0, 100),
                Color.FromArgb(255, 190, 90, 0),
                Color.FromArgb(255, 75, 110, 25),
                Color.FromArgb(255, 90, 0, 120),
                Color.FromArgb(255, 0, 150, 0),
                Color.FromArgb(255, 190, 80, 120),
                Color.FromArgb(255, 190, 140, 140),
                Color.FromArgb(255, 120, 190, 200)
            };
        }

        public static int ResolveAlphaFromTransparencyPercent(float transparencyPercent, int fallbackAlpha)
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

    public sealed class TSNEScatterDisplayOptions
    {
        public TSNEScatterDisplayOptions()
        {
            FontName = "Segoe UI";
            ShowTitle = true;
            Title = "Distribution Chart";
            TitleColor = Color.Black;
            BackgroundColor = Color.White;
            GraphBackgroundColor = Color.FromArgb(230, 230, 230);
            ThemeMode = LightningScatterThemeMode.LightGray;
            XAxisTitle = string.Empty;
            YAxisTitle = string.Empty;
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
        public Color TitleColor { get; set; }
        public Color BackgroundColor { get; set; }
        public Color GraphBackgroundColor { get; set; }
        public LightningScatterThemeMode ThemeMode { get; set; }
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

        public TSNEScatterDisplayOptions Clone()
        {
            return (TSNEScatterDisplayOptions)MemberwiseClone();
        }
    }

    public sealed class TSNEScatterOptions
    {
        public TSNEScatterOptions()
        {
            Analysis = new TSNEScatterAnalysisOptions();
            Series = new TSNEScatterSeriesOptions();
            Display = new TSNEScatterDisplayOptions();
            Legend = new LightningScatterLegendOptions
            {
                Position = LightningScatterLegendPosition.BottomCenter,
                OffsetY = 0,
                ShowCheckboxes = false,
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent,
                TransparentBackground = true
            };
            Tooltip = new LightningScatterTooltipOptions
            {
                Enabled = true,
                HitPixelTolerance = 14,
                Format = "{5}\r\nX1:{1:0.###}, X2:{2:0.###}\r\nAI_RSLT_Val:{0}"
            };
            NoData = new LightningScatterNoDataOptions
            {
                Text = "t-SNE Scatter No data available.",
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
                SubDirectoryName = "TSNEScatterImages"
            };
            Interaction = new LightningScatterInteractionOptions
            {
                ZoomEnabled = true,
                PanEnabled = true,
                MouseWheelZoomEnabled = true,
                AllowInternalMouseCursorChange = true,
                OpenPropertyEditorOnRightClick = true
            };
        }

        public TSNEScatterAnalysisOptions Analysis { get; set; }
        public TSNEScatterSeriesOptions Series { get; set; }
        public TSNEScatterDisplayOptions Display { get; set; }
        public LightningScatterLegendOptions Legend { get; set; }
        public LightningScatterTooltipOptions Tooltip { get; set; }
        public LightningScatterNoDataOptions NoData { get; set; }
        public LightningScatterImageOptions Image { get; set; }
        public LightningScatterInteractionOptions Interaction { get; set; }
        public Action<LightningScatterOptions> CustomizeScatterOptions { get; set; }

        public static TSNEScatterOptions CreateDefault()
        {
            return new TSNEScatterOptions();
        }

        public static TSNEScatterOptions CreateDefault600x400()
        {
            return new TSNEScatterOptions
            {
                Image = new LightningScatterImageOptions
                {
                    Width = 600,
                    Height = 400,
                    SubDirectoryName = "TSNEScatterImages"
                }
            };
        }

        public static TSNEScatterOptions CreateExcelImageOptimized()
        {
            TSNEScatterOptions options = CreateDefault600x400();
            options.Image.Width = 900;
            options.Image.Height = 600;
            options.Image.SubDirectoryName = "TSNEScatterExcelImages";
            options.Display.AxisPaddingRatio = 0.05d;
            options.Display.MinimumAxisPadding = 0.1d;
            options.Legend.FontSize = 8f;
            options.Series.PointSize = 17f;
            return options;
        }

        public TSNEScatterOptions Clone()
        {
            return new TSNEScatterOptions
            {
                Analysis = Analysis == null ? new TSNEScatterAnalysisOptions() : Analysis.Clone(),
                Series = Series == null ? new TSNEScatterSeriesOptions() : Series.Clone(),
                Display = Display == null ? new TSNEScatterDisplayOptions() : Display.Clone(),
                Legend = Legend == null ? new LightningScatterLegendOptions() : Legend.Clone(),
                Tooltip = Tooltip == null ? new LightningScatterTooltipOptions() : Tooltip.Clone(),
                NoData = NoData == null ? new LightningScatterNoDataOptions() : NoData.Clone(),
                Image = Image == null ? new LightningScatterImageOptions() : Image.Clone(),
                Interaction = Interaction == null ? new LightningScatterInteractionOptions() : Interaction.Clone(),
                CustomizeScatterOptions = CustomizeScatterOptions
            };
        }

        internal LightningScatterOptions ToScatterOptions(TSNEAnalysisResult analysisResult)
        {
            TSNEScatterOptions snapshot = Clone();
            LightningScatterOptions scatterOptions = LightningScatterOptions.CreateDefaultBubble();
            TSNEScatterDisplayOptions display = snapshot.Display ?? new TSNEScatterDisplayOptions();

            scatterOptions.FontName = string.IsNullOrWhiteSpace(display.FontName) ? "Segoe UI" : display.FontName.Trim();
            scatterOptions.ShowTitle = display.ShowTitle;
            scatterOptions.Title = display.Title ?? string.Empty;
            scatterOptions.TitleColor = display.TitleColor.IsEmpty ? Color.Black : display.TitleColor;
            scatterOptions.BackgroundColor = display.BackgroundColor;
            scatterOptions.GraphBackgroundColor = display.GraphBackgroundColor;
            scatterOptions.ThemeMode = display.ThemeMode;
            scatterOptions.Legend = snapshot.Legend ?? new LightningScatterLegendOptions();
            scatterOptions.Tooltip = snapshot.Tooltip ?? new LightningScatterTooltipOptions();
            scatterOptions.NoData = snapshot.NoData ?? new LightningScatterNoDataOptions();
            scatterOptions.Image = snapshot.Image ?? new LightningScatterImageOptions();
            scatterOptions.Interaction = snapshot.Interaction ?? new LightningScatterInteractionOptions();
            scatterOptions.Style.UsePastelPalette = false;
            scatterOptions.Style.ForceBubbleStyle = true;
            TSNEScatterSeriesOptions series = snapshot.Series ?? new TSNEScatterSeriesOptions();
            scatterOptions.Style.BubbleSize = Math.Max(1f, series.PointSize);
            scatterOptions.Style.PointShape = series.PointShape;
            scatterOptions.Style.ApplyColorAlpha = series.ApplyColorAlpha;
            scatterOptions.Style.ColorTransparencyPercent = series.ColorTransparencyPercent;
            scatterOptions.Style.ColorAlpha = TSNEScatterSeriesOptions.ResolveAlphaFromTransparencyPercent(series.ColorTransparencyPercent, series.ColorAlpha);
            scatterOptions.Style.ApplyColorTransparencyBlend = true;
            scatterOptions.Style.ColorBlendBackground = display.GraphBackgroundColor.IsEmpty ? display.BackgroundColor : display.GraphBackgroundColor;
            scatterOptions.Style.ApplyBorderTransparency = series.ApplyBorderTransparency;
            scatterOptions.Style.BorderTransparencyPercent = series.BorderTransparencyPercent;
            scatterOptions.Style.BubbleBorderWidth = 1f;
            scatterOptions.Style.PointBodyThickness = 1f;

            ApplyAxisOptions(scatterOptions, analysisResult, display, series);

            if (snapshot.CustomizeScatterOptions != null)
            {
                snapshot.CustomizeScatterOptions(scatterOptions);
            }

            return scatterOptions;
        }

        private static void ApplyAxisOptions(LightningScatterOptions scatterOptions, TSNEAnalysisResult analysisResult, TSNEScatterDisplayOptions display, TSNEScatterSeriesOptions series)
        {
            IList<ScatterSampleData> axisSamples = ResolveAxisSamples(analysisResult, series);
            AxisRange xRange = CalculateRange(
                axisSamples.Select(item => item.X1),
                display);
            AxisRange yRange = CalculateRange(
                axisSamples.Select(item => item.X2),
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

        private static IList<ScatterSampleData> ResolveAxisSamples(TSNEAnalysisResult analysisResult, TSNEScatterSeriesOptions series)
        {
            IList<ScatterSampleData> samples = analysisResult == null || analysisResult.ScatterData == null
                ? new List<ScatterSampleData>()
                : analysisResult.ScatterData.Where(item => item != null).ToList();
            if (series == null || !series.RequireSeriesLabel)
            {
                return samples;
            }

            string highlightedDraftNo = (series.HighlightDraftNo ?? string.Empty).Trim();
            string selectedDraftNo = (series.SelectedDraftNo ?? string.Empty).Trim();
            return samples.Where(sample =>
                HasSeriesLabel(sample, series)
                || IsSameDraftNo(sample.DraftNo, highlightedDraftNo)
                || IsSameDraftNo(sample.DraftNo, selectedDraftNo))
                .ToList();
        }

        private static bool HasSeriesLabel(ScatterSampleData sample, TSNEScatterSeriesOptions series)
        {
            if (sample == null)
            {
                return false;
            }

            string label = series != null && series.SeriesNameSelector != null
                ? series.SeriesNameSelector(sample)
                : sample.AiResultValue;
            return !string.IsNullOrWhiteSpace(label);
        }

        private static bool IsSameDraftNo(string left, string right)
        {
            return !string.IsNullOrWhiteSpace(left)
                && !string.IsNullOrWhiteSpace(right)
                && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static AxisRange CalculateRange(IEnumerable<double> values, TSNEScatterDisplayOptions display)
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






