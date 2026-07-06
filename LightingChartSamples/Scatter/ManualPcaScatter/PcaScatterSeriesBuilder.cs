using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LightingChartSamples.Scatter;
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.PCAChart.Common;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.ManualPcaScatter
{
    public sealed class PcaScatterSeriesBuilder
    {
        public IEnumerable<LightningScatterSeries> Build(PcaAnalysisResult analysisResult, PcaScatterSeriesOptions seriesOptions)
        {
            PcaScatterSeriesOptions options = seriesOptions == null
                ? new PcaScatterSeriesOptions()
                : seriesOptions.Clone();
            IList<ScatterSampleData> samples = analysisResult == null || analysisResult.ScatterData == null
                ? new List<ScatterSampleData>()
                : analysisResult.ScatterData.Where(item => item != null).ToList();
            ScatterSampleData highlightedSample = ResolveHighlightedSample(samples, options);
            ScatterSampleData selectedSample = ResolveSelectedSample(samples, options);
            IList<ScatterSampleData> regularSamples = highlightedSample == null
                ? samples
                : samples.Where(item => !object.ReferenceEquals(item, highlightedSample)).ToList();
            if (selectedSample != null)
            {
                regularSamples = regularSamples
                    .Where(item => !object.ReferenceEquals(item, selectedSample))
                    .ToList();
            }

            Dictionary<string, List<ScatterSampleData>> groups = regularSamples
                .GroupBy(item => ResolveSeriesName(item, options), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            List<string> orderedNames = ResolveSeriesOrder(groups.Keys, options);
            var result = new List<LightningScatterSeries>();
            for (int index = 0; index < orderedNames.Count; index++)
            {
                string seriesName = orderedNames[index];
                result.Add(new LightningScatterSeries
                {
                    Name = seriesName,
                    LegendLabel = ResolveLegendLabel(seriesName, options),
                    LineColor = ResolveSeriesColor(seriesName, index, options),
                    PointColor = ResolveSeriesColor(seriesName, index, options),
                    PointSize = Math.Max(1f, options.PointSize),
                    ShowLine = options.ShowLine,
                    ShowPoints = options.ShowPoints,
                    Points = groups[seriesName]
                        .Select(item => new LightningScatterPoint(item.X1, item.X2, item))
                        .ToList()
                });
            }

            if (highlightedSample != null)
            {
                result.Add(CreateSinglePointSeries(
                    highlightedSample, highlightedSample.DraftNo.Trim(), options.HighlightColor, options.HighlightColor,
                    Math.Max(1f, options.HighlightPointSize), 1.8f, true));
            }

            if (selectedSample != null && !object.ReferenceEquals(selectedSample, highlightedSample))
            {
                result.Add(CreateSinglePointSeries(
                    selectedSample, selectedSample.DraftNo.Trim(), options.SelectedPointColor, options.SelectedPointBorderColor,
                    Math.Max(1f, options.SelectedPointSize), Math.Max(0f, options.SelectedPointBorderWidth), false));
            }

            return result;
        }

        private static ScatterSampleData ResolveHighlightedSample(IEnumerable<ScatterSampleData> samples, PcaScatterSeriesOptions options)
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

        private static ScatterSampleData ResolveSelectedSample(IEnumerable<ScatterSampleData> samples, PcaScatterSeriesOptions options)
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
            float pointSize, float borderWidth, bool showInLegend)
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
                ShowLine = false,
                ShowPoints = true,
                ShowInLegend = showInLegend,
                Points = new List<LightningScatterPoint>
                {
                    new LightningScatterPoint(sample.X1, sample.X2, sample)
                }
            };
        }

        private static string ResolveSeriesName(ScatterSampleData sample, PcaScatterSeriesOptions options)
        {
            string seriesName = options.SeriesNameSelector == null
                ? sample.AiResultValue
                : options.SeriesNameSelector(sample);
            return string.IsNullOrWhiteSpace(seriesName) ? "Unknown" : seriesName.Trim();
        }

        private static string ResolveLegendLabel(string seriesName, PcaScatterSeriesOptions options)
        {
            if (options.LegendLabelFormatter == null)
            {
                return seriesName ?? string.Empty;
            }

            string formatted = options.LegendLabelFormatter(seriesName);
            return string.IsNullOrWhiteSpace(formatted) ? seriesName ?? string.Empty : formatted.Trim();
        }

        private static Color ResolveSeriesColor(string seriesName, int seriesIndex, PcaScatterSeriesOptions options)
        {
            Color configuredColor;
            if (options.SeriesColors != null && options.SeriesColors.TryGetValue(seriesName, out configuredColor))
            {
                return configuredColor;
            }

            if (string.Equals(seriesName, options.PassResultName, StringComparison.OrdinalIgnoreCase))
            {
                return options.PassColor;
            }

            if (string.Equals(seriesName, options.ReviewResultName, StringComparison.OrdinalIgnoreCase))
            {
                return options.ReviewColor;
            }

            Color[] palette = options.PastelPalette == null || options.PastelPalette.Length == 0
                ? LightningScatterOptions.CreateDefaultPastelPalette()
                : options.PastelPalette;
            if (seriesIndex >= 0 && seriesIndex < palette.Length)
            {
                return palette[seriesIndex];
            }

            return options.DefaultColor;
        }

        private static List<string> ResolveSeriesOrder(IEnumerable<string> groupNames, PcaScatterSeriesOptions options)
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
