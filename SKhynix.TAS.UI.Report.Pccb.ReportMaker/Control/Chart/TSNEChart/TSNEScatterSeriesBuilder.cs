using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.Common;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    public sealed class TSNEScatterSeriesBuilder
    {
        public IEnumerable<LightningScatterSeries> Build(TSNEAnalysisResult analysisResult, TSNEScatterSeriesOptions seriesOptions)
        {
            TSNEScatterSeriesOptions options = seriesOptions == null
                ? new TSNEScatterSeriesOptions()
                : seriesOptions.Clone();
            IList<ScatterSampleData> samples = analysisResult == null || analysisResult.ScatterData == null
                ? new List<ScatterSampleData>()
                : analysisResult.ScatterData.Where(item => item != null).ToList();
            ScatterSampleData highlightedSample = ResolveHighlightedSample(samples, options);
            ScatterSampleData selectedSample = ResolveSelectedSample(samples, options);
            IList<ScatterSampleData> regularSamples = samples
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

            Dictionary<string, List<ScatterSampleData>> allGroups = regularSamples
                .GroupBy(item => ResolveSeriesName(item, options), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<ScatterSampleData>> groups = regularSamples
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

        private static ScatterSampleData ResolveHighlightedSample(IEnumerable<ScatterSampleData> samples, TSNEScatterSeriesOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.HighlightDraftNo))
            {
                return null;
            }

            string draftNo = options.HighlightDraftNo.Trim();
            return (samples ?? Enumerable.Empty<ScatterSampleData>()).FirstOrDefault(item =>
                item != null
                && string.Equals(item.DraftNo, draftNo, StringComparison.OrdinalIgnoreCase));
        }

        private static ScatterSampleData ResolveSelectedSample(IEnumerable<ScatterSampleData> samples, TSNEScatterSeriesOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.SelectedDraftNo))
            {
                return null;
            }

            string draftNo = options.SelectedDraftNo.Trim();
            return (samples ?? Enumerable.Empty<ScatterSampleData>()).FirstOrDefault(item =>
                item != null
                && string.Equals(item.DraftNo, draftNo, StringComparison.OrdinalIgnoreCase));
        }

        private static LightningScatterSeries CreateSinglePointSeries(
            ScatterSampleData sample, string seriesName, Color fillColor, Color borderColor,
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

        private static string ResolveSeriesName(ScatterSampleData sample, TSNEScatterSeriesOptions options)
        {
            string seriesName = ResolveRawSeriesName(sample, options);
            return string.IsNullOrWhiteSpace(seriesName) ? "Unknown" : seriesName.Trim();
        }

        private static string ResolveRawSeriesName(ScatterSampleData sample, TSNEScatterSeriesOptions options)
        {
            if (sample == null)
            {
                return string.Empty;
            }

            return options != null && options.SeriesNameSelector != null
                ? options.SeriesNameSelector(sample)
                : sample.AiResultValue;
        }

        private static bool ShouldIncludeInRegularSeries(ScatterSampleData sample, TSNEScatterSeriesOptions options)
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





