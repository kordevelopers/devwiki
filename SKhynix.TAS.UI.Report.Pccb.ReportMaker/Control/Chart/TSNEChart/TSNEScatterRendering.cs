using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsControl = System.Windows.Forms.Control;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.Common;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public sealed class TSNEPointData
    {
        public int SourceIndex { get; set; }
        public string DraftNo { get; set; }
        public double X1 { get; set; }
        public double X2 { get; set; }
        public string AiResultValue { get; set; }
        public double? Distance { get; set; }
        public string ParameterType { get; set; }
        public string TooltipText { get; set; }
        public object UserData { get; set; }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(TooltipText)
                ? DraftNo ?? string.Empty
                : TooltipText;
        }
    }

}






namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public enum TSNEScatterDataSourceKind
    {
        JsonSamples,
        ActDataJsonDocuments,
        ConvExperimentJsonDocuments,
        AnalysisResult
    }

    public sealed class TSNEScatterDataSource
    {
        private readonly IList<string> documents;
        private readonly TSNEAnalysisResult analysisResult;

        private TSNEScatterDataSource(TSNEScatterDataSourceKind kind, IEnumerable<string> documents, TSNEAnalysisResult analysisResult)
        {
            Kind = kind;
            this.documents = documents == null
                ? new List<string>()
                : documents.ToList();
            this.analysisResult = analysisResult;
        }

        public TSNEScatterDataSourceKind Kind { get; private set; }

        public static TSNEScatterDataSource FromJsonSamples(IEnumerable<string> jsonSamples)
        {
            return new TSNEScatterDataSource(TSNEScatterDataSourceKind.JsonSamples, jsonSamples, null);
        }

        public static TSNEScatterDataSource FromActDataJson(IEnumerable<string> actDataDocuments)
        {
            return new TSNEScatterDataSource(TSNEScatterDataSourceKind.ActDataJsonDocuments, actDataDocuments, null);
        }

        public static TSNEScatterDataSource FromActDataJson(string actDataDocument)
        {
            return FromActDataJson(new[] { actDataDocument });
        }

        public static TSNEScatterDataSource FromConvExperimentJson(IEnumerable<string> convExperimentDocuments)
        {
            return new TSNEScatterDataSource(
                TSNEScatterDataSourceKind.ConvExperimentJsonDocuments,
                convExperimentDocuments,
                null);
        }

        public static TSNEScatterDataSource FromAnalysisResult(TSNEAnalysisResult analysisResult)
        {
            if (analysisResult == null)
            {
                throw new ArgumentNullException("analysisResult");
            }

            return new TSNEScatterDataSource(TSNEScatterDataSourceKind.AnalysisResult, null, analysisResult);
        }

        public TSNEAnalysisResult Analyze(TSNEScatterAnalysisOptions analysisOptions)
        {
            if (Kind == TSNEScatterDataSourceKind.AnalysisResult)
            {
                return analysisResult;
            }

            TSNEAnalysisOptions pipelineOptions = (analysisOptions ?? new TSNEScatterAnalysisOptions()).ToPipelineOptions();
            // This assembly is intentionally t-SNE-only; callers cannot switch the projection back to TSNE.
            pipelineOptions.ProjectionMethod = DimensionalityReductionMethod.TSNE;
            TSNEAnalysisPipeline pipeline = new TSNEAnalysisPipeline(pipelineOptions);
            if (Kind == TSNEScatterDataSourceKind.ActDataJsonDocuments)
            {
                return pipeline.AnalyzeActDataDocuments(documents);
            }

            if (Kind == TSNEScatterDataSourceKind.ConvExperimentJsonDocuments)
            {
                return pipeline.AnalyzeConvExperimentDocuments(documents);
            }

            return pipeline.Analyze(documents);
        }
    }

    public sealed class TSNEScatterExadataOptions
    {
        public TSNEScatterExadataOptions()
        {
            JsonColumnName = "CONV_EXPER_CTN";
            DraftNoColumnName = "DRAFT_NO";
            ParameterTypeColumnName = "PARAM_TYP";
            AiResultColumnName = "AI_RSLT_VAL";
            LabelColumnName = "ENGR_RSLT_VAL";
            ParameterType = TSNEParameterType.Response;
        }

        public DataTable SourceTable { get; set; }
        public string JsonColumnName { get; set; }
        public string DraftNoColumnName { get; set; }
        public string ParameterTypeColumnName { get; set; }
        public string AiResultColumnName { get; set; }
        public string LabelColumnName { get; set; }
        public TSNEParameterType ParameterType { get; set; }

        public static TSNEScatterExadataOptions CreateDefault()
        {
            ConvExperimentQueryOptions configured = ConvExperimentQueryOptions.FromConfiguration();
            return new TSNEScatterExadataOptions
            {
                JsonColumnName = configured.JsonColumnName,
                DraftNoColumnName = configured.DraftNoColumnName,
                ParameterTypeColumnName = configured.ParameterTypeColumnName,
                AiResultColumnName = configured.AiResultColumnName,
                LabelColumnName = configured.LabelColumnName,
                ParameterType = TSNEParameterType.Response
            };
        }

        public static TSNEScatterExadataOptions FromDataTable(DataTable sourceTable)
        {
            return new TSNEScatterExadataOptions
            {
                SourceTable = sourceTable
            };
        }

        public ConvExperimentQueryOptions ToQueryOptions()
        {
            return new ConvExperimentQueryOptions
            {
                JsonColumnName = string.IsNullOrWhiteSpace(JsonColumnName)
                    ? "CONV_EXPER_CTN"
                    : JsonColumnName.Trim(),
                DraftNoColumnName = string.IsNullOrWhiteSpace(DraftNoColumnName)
                    ? "DRAFT_NO"
                    : DraftNoColumnName.Trim(),
                ParameterTypeColumnName = string.IsNullOrWhiteSpace(ParameterTypeColumnName)
                    ? "PARAM_TYP"
                    : ParameterTypeColumnName.Trim(),
                AiResultColumnName = string.IsNullOrWhiteSpace(AiResultColumnName)
                    ? "AI_RSLT_VAL"
                    : AiResultColumnName.Trim(),
                LabelColumnName = string.IsNullOrWhiteSpace(LabelColumnName)
                    ? "ENGR_RSLT_VAL"
                    : LabelColumnName.Trim()
            };
        }
    }

    public sealed class TSNEScatterDatabaseOptions
    {
        public TSNEScatterDatabaseOptions()
        {
            ActDataColumnName = "ACT_DATA";
        }

        public DataTable SourceTable { get; set; }
        public string ActDataColumnName { get; set; }

        public static TSNEScatterDatabaseOptions CreateDefault()
        {
            return new TSNEScatterDatabaseOptions();
        }

        public static TSNEScatterDatabaseOptions FromDataTable(DataTable sourceTable)
        {
            return new TSNEScatterDatabaseOptions
            {
                SourceTable = sourceTable
            };
        }

        internal ActDataQueryOptions ToActDataQueryOptions()
        {
            return new ActDataQueryOptions
            {
                ActDataColumnName = string.IsNullOrWhiteSpace(ActDataColumnName)
                    ? "ACT_DATA"
                    : ActDataColumnName.Trim()
            };
        }
    }
}






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
        public Func<TSNEPointData, string> SeriesNameSelector { get; set; }
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
            IList<TSNEPointData> axisSamples = ResolveAxisSamples(analysisResult, series);
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

        private static IList<TSNEPointData> ResolveAxisSamples(TSNEAnalysisResult analysisResult, TSNEScatterSeriesOptions series)
        {
            IList<TSNEPointData> samples = analysisResult == null || analysisResult.ScatterData == null
                ? new List<TSNEPointData>()
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

        private static bool HasSeriesLabel(TSNEPointData sample, TSNEScatterSeriesOptions series)
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







namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public interface ITSNEScatterPopupDataProvider
    {
        string SourceDescription { get; }
        Task<DataTable> LoadAllAsync();
    }
}






namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public sealed class TSNEScatterSeriesBuilder
    {
        public IEnumerable<LightningScatterSeries> Build(TSNEAnalysisResult analysisResult, TSNEScatterSeriesOptions seriesOptions)
        {
            TSNEScatterSeriesOptions options = seriesOptions == null
                ? new TSNEScatterSeriesOptions()
                : seriesOptions.Clone();
            IList<TSNEPointData> samples = analysisResult == null || analysisResult.ScatterData == null
                ? new List<TSNEPointData>()
                : analysisResult.ScatterData.Where(item => item != null).ToList();
            TSNEPointData highlightedSample = ResolveHighlightedSample(samples, options);
            TSNEPointData selectedSample = ResolveSelectedSample(samples, options);
            IList<TSNEPointData> regularSamples = samples
                .Where(item => ShouldIncludeInRegularSeries(item, options))
                .ToList();
            if (highlightedSample != null)
            {
                regularSamples = regularSamples
                    .Where(item => !object.ReferenceEquals(item, highlightedSample))
                    .ToList();
            }

            if (selectedSample != null)
            {
                regularSamples = regularSamples
                    .Where(item => !object.ReferenceEquals(item, selectedSample))
                    .ToList();
            }

            Dictionary<string, List<TSNEPointData>> allGroups = regularSamples
                .GroupBy(item => ResolveSeriesName(item, options), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<TSNEPointData>> groups = regularSamples
                .GroupBy(item => ResolveSeriesName(item, options), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            List<string> orderedNames = ResolveSeriesOrder(allGroups.Keys, options);
            Dictionary<string, Color> seriesColors = ResolveSeriesColors(orderedNames, options);
            Dictionary<string, Color> seriesBorderColors = ResolveSeriesBorderColors(orderedNames, options);
            var result = new List<LightningScatterSeries>();
            for (int index = 0; index < orderedNames.Count; index++)
            {
                string seriesName = orderedNames[index];
                if (!groups.ContainsKey(seriesName) || groups[seriesName].Count == 0)
                {
                    continue;
                }

                Color seriesColor = seriesColors[seriesName];
                Color seriesBorderColor = seriesBorderColors.ContainsKey(seriesName)
                    ? seriesBorderColors[seriesName]
                    : seriesColor;
                result.Add(new LightningScatterSeries
                {
                    Name = seriesName,
                    LegendLabel = ResolveLegendLabel(seriesName, options),
                    LineColor = seriesColor,
                    PointColor = seriesColor,
                    PointBorderColor = seriesBorderColor,
                    PointSize = Math.Max(1f, options.PointSize),
                    PointShape = options.PointShape,
                    ShowLine = options.ShowLine,
                    ShowPoints = options.ShowPoints,
                    Points = groups[seriesName]
                        .Select(item => new LightningScatterPoint(item.X1, item.X2, item))
                        .ToList()
                });
            }

            if (highlightedSample != null)
            {
                result.Add(CreateSinglePointSeries(highlightedSample, highlightedSample.DraftNo.Trim(), options.HighlightColor, options.HighlightPointBorderColor, options.PointShape, ResolveHighlightedPointSize(options), Math.Max(0f, options.HighlightPointBorderWidth), true));
            }

            if (selectedSample != null && !object.ReferenceEquals(selectedSample, highlightedSample))
            {
                string selectedSeriesName = ResolveSeriesName(selectedSample, options);
                Color selectedPointColor = ResolveSelectedPointColor(selectedSeriesName, seriesColors, options);
                result.Add(CreateSinglePointSeries(selectedSample, selectedSample.DraftNo.Trim(), selectedPointColor, options.SelectedPointBorderColor, options.PointShape, ResolveSelectedPointSize(options), Math.Max(0f, options.SelectedPointBorderWidth), false));
            }

            return result;
        }

        private static TSNEPointData ResolveHighlightedSample(IEnumerable<TSNEPointData> samples, TSNEScatterSeriesOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.HighlightDraftNo))
            {
                return null;
            }

            string draftNo = options.HighlightDraftNo.Trim();
            return (samples ?? Enumerable.Empty<TSNEPointData>()).FirstOrDefault(item =>
                item != null
                && string.Equals(item.DraftNo, draftNo, StringComparison.OrdinalIgnoreCase));
        }

        private static TSNEPointData ResolveSelectedSample(IEnumerable<TSNEPointData> samples, TSNEScatterSeriesOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.SelectedDraftNo))
            {
                return null;
            }

            string draftNo = options.SelectedDraftNo.Trim();
            return (samples ?? Enumerable.Empty<TSNEPointData>()).FirstOrDefault(item =>
                item != null
                && string.Equals(item.DraftNo, draftNo, StringComparison.OrdinalIgnoreCase));
        }

        private static LightningScatterSeries CreateSinglePointSeries(
            TSNEPointData sample, string seriesName, Color fillColor, Color borderColor,
            LightningScatterPointShape pointShape, float pointSize, float borderWidth, bool showInLegend)
        {
            return new LightningScatterSeries
            {
                Name = seriesName,
                LegendLabel = seriesName,
                LineColor = borderColor,
                PointColor = fillColor,
                PointBorderColor = borderColor,
                PointBorderWidth = borderWidth,
                PointSize = pointSize,
                PointShape = pointShape,
                ShowLine = false,
                ShowPoints = true,
                ShowInLegend = showInLegend,
                Points = new List<LightningScatterPoint>
                {
                    new LightningScatterPoint(sample.X1, sample.X2, sample)
                }
            };
        }

        private static string ResolveSeriesName(TSNEPointData sample, TSNEScatterSeriesOptions options)
        {
            string seriesName = ResolveRawSeriesName(sample, options);
            return string.IsNullOrWhiteSpace(seriesName) ? "Unknown" : seriesName.Trim();
        }

        private static string ResolveRawSeriesName(TSNEPointData sample, TSNEScatterSeriesOptions options)
        {
            if (sample == null)
            {
                return string.Empty;
            }

            return options != null && options.SeriesNameSelector != null
                ? options.SeriesNameSelector(sample)
                : sample.AiResultValue;
        }

        private static bool ShouldIncludeInRegularSeries(TSNEPointData sample, TSNEScatterSeriesOptions options)
        {
            if (sample == null)
            {
                return false;
            }

            if (options == null || !options.RequireSeriesLabel)
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(ResolveRawSeriesName(sample, options));
        }

        private static string ResolveLegendLabel(string seriesName, TSNEScatterSeriesOptions options)
        {
            if (options.LegendLabelFormatter == null)
            {
                return seriesName ?? string.Empty;
            }

            string formatted = options.LegendLabelFormatter(seriesName);
            return string.IsNullOrWhiteSpace(formatted) ? seriesName ?? string.Empty : formatted.Trim();
        }

        private static Color ResolveSeriesColor(string seriesName, int seriesIndex, TSNEScatterSeriesOptions options)
        {
            if (IsNaSeriesName(seriesName, options))
            {
                return ApplyColorAlpha(options.NaSeriesColor, options);
            }

            Color configuredColor;
            if (options.SeriesColors != null && options.SeriesColors.TryGetValue(seriesName, out configuredColor))
            {
                return ApplyColorAlpha(configuredColor, options);
            }

            Color[] palette = options.PastelPalette == null || options.PastelPalette.Length == 0
                ? TSNEScatterSeriesOptions.CreateCompanySeriesPalette()
                : options.PastelPalette;
            if (options.UsePaletteColors && palette.Length > 0)
            {
                return ApplyColorAlpha(palette[Math.Abs(seriesIndex) % palette.Length], options);
            }

            if (string.Equals(seriesName, options.PassResultName, StringComparison.OrdinalIgnoreCase))
            {
                return ApplyColorAlpha(options.PassColor, options);
            }

            if (string.Equals(seriesName, options.ReviewResultName, StringComparison.OrdinalIgnoreCase))
            {
                return ApplyColorAlpha(options.ReviewColor, options);
            }

            if (seriesIndex >= 0 && seriesIndex < palette.Length)
            {
                return ApplyColorAlpha(palette[seriesIndex], options);
            }

            return ApplyColorAlpha(options.DefaultColor, options);
        }

        private static Dictionary<string, Color> ResolveSeriesColors(IEnumerable<string> orderedNames, TSNEScatterSeriesOptions options)
        {
            var colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            int companyPaletteIndex = 0;
            foreach (string seriesName in orderedNames ?? Enumerable.Empty<string>())
            {
                bool isNaSeries = IsNaSeriesName(seriesName, options);
                int colorIndex = isNaSeries ? 0 : companyPaletteIndex++;
                colors[seriesName] = ResolveSeriesColor(seriesName, colorIndex, options);
            }

            return colors;
        }

        private static Dictionary<string, Color> ResolveSeriesBorderColors(IEnumerable<string> orderedNames, TSNEScatterSeriesOptions options)
        {
            var colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            Color[] palette = options == null || options.BorderPalette == null || options.BorderPalette.Length == 0
                ? TSNEScatterSeriesOptions.CreateCompanySeriesBorderPalette()
                : options.BorderPalette;
            int companyPaletteIndex = 0;
            foreach (string seriesName in orderedNames ?? Enumerable.Empty<string>())
            {
                bool isNaSeries = IsNaSeriesName(seriesName, options);
                int colorIndex = isNaSeries ? 0 : companyPaletteIndex++;
                colors[seriesName] = palette.Length == 0
                    ? ResolveSeriesColor(seriesName, colorIndex, options)
                    : palette[Math.Abs(colorIndex) % palette.Length];
            }

            return colors;
        }

        private static Color ResolveSelectedPointColor(string selectedSeriesName, IDictionary<string, Color> seriesColors, TSNEScatterSeriesOptions options)
        {
            if (options != null && !options.SelectedPointColor.IsEmpty)
            {
                return ApplyColorAlpha(options.SelectedPointColor, options);
            }

            Color seriesColor;
            return seriesColors != null && seriesColors.TryGetValue(selectedSeriesName, out seriesColor)
                ? seriesColor
                : ResolveSeriesColor(selectedSeriesName, 0, options);
        }

        private static float ResolveSelectedPointSize(TSNEScatterSeriesOptions options)
        {
            float basePointSize = options == null ? 7f : Math.Max(1f, options.PointSize);
            return options != null && options.SelectedPointSize > 0f
                ? Math.Max(1f, options.SelectedPointSize)
                : Math.Max(1f, basePointSize * 1.1f);
        }

        private static float ResolveHighlightedPointSize(TSNEScatterSeriesOptions options)
        {
            float basePointSize = options == null ? 7f : Math.Max(1f, options.PointSize);
            return options != null && options.HighlightPointSize > 0f
                ? Math.Max(1f, options.HighlightPointSize)
                : Math.Max(1f, basePointSize * 1.1f);
        }

        private static Color ApplyColorAlpha(Color color, TSNEScatterSeriesOptions options)
        {
            return color;
        }

        private static bool IsNaSeriesName(string seriesName, TSNEScatterSeriesOptions options)
        {
            if (options == null || string.IsNullOrWhiteSpace(options.NaSeriesName))
            {
                return false;
            }

            string naSeriesName = options.NaSeriesName.Trim();
            return string.Equals(seriesName, naSeriesName, StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> ResolveSeriesOrder(IEnumerable<string> groupNames, TSNEScatterSeriesOptions options)
        {
            HashSet<string> remaining = new HashSet<string>(
                groupNames ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            var ordered = new List<string>();

            if (options.SeriesOrder != null)
            {
                foreach (string preferredName in options.SeriesOrder)
                {
                    if (string.IsNullOrWhiteSpace(preferredName) || !remaining.Contains(preferredName))
                    {
                        continue;
                    }

                    ordered.Add(preferredName);
                    remaining.Remove(preferredName);
                }
            }

            ordered.AddRange(remaining.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
            return ordered;
        }
    }
}

