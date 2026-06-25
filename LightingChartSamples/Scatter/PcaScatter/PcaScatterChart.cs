using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LightingChartSamples.Scatter
{
    public sealed class PcaScatterSampleClickedEventArgs : EventArgs
    {
        public PcaScatterSampleClickedEventArgs(
            ScatterSampleData sample,
            IList<KnnNeighbor> neighbors,
            LightningScatterPointClickEventArgs sourceEventArgs)
        {
            Sample = sample;
            Neighbors = neighbors == null ? new List<KnnNeighbor>() : neighbors.ToList();
            SourceEventArgs = sourceEventArgs;
        }

        public ScatterSampleData Sample { get; private set; }
        public IList<KnnNeighbor> Neighbors { get; private set; }
        public LightningScatterPointClickEventArgs SourceEventArgs { get; private set; }
    }

    public sealed class PcaScatterAnalysisCompletedEventArgs : EventArgs
    {
        public PcaScatterAnalysisCompletedEventArgs(PcaAnalysisResult analysisResult)
        {
            AnalysisResult = analysisResult;
        }

        public PcaAnalysisResult AnalysisResult { get; private set; }
    }

    public sealed class PcaScatterAnalysisFailedEventArgs : EventArgs
    {
        public PcaScatterAnalysisFailedEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }
    }

    public sealed class PcaScatterChart : IDisposable
    {
        private readonly Control parent;
        private readonly PcaScatterSeriesBuilder seriesBuilder;
        private LightningScatter scatterChart;
        private PcaScatterOptions options;
        private PcaAnalysisResult analysisResult;
        private bool disposed;

        private PcaScatterChart(Control parent, PcaScatterOptions options)
        {
            this.parent = parent;
            this.options = options == null ? PcaScatterOptions.CreateDefault() : options.Clone();
            seriesBuilder = new PcaScatterSeriesBuilder();
            scatterChart = LightningScatter.Create(
                parent,
                Enumerable.Empty<LightningScatterSeries>(),
                this.options.ToScatterOptions(null));
            scatterChart.PointClicked += ScatterChart_PointClicked;
            scatterChart.Clear();
        }

        public event EventHandler<PcaScatterSampleClickedEventArgs> SampleClicked;
        public event EventHandler<PcaScatterAnalysisCompletedEventArgs> AnalysisCompleted;
        public event EventHandler<PcaScatterAnalysisFailedEventArgs> AnalysisFailed;

        public LightningScatter ScatterControl
        {
            get { return scatterChart; }
        }

        public PcaScatterOptions Options
        {
            get { return options.Clone(); }
        }

        public PcaAnalysisResult AnalysisResult
        {
            get { return analysisResult; }
        }

        public string LastSavedImagePath
        {
            get { return scatterChart.LastSavedImagePath; }
        }

        public Image LastSavedImage
        {
            get { return scatterChart.LastSavedImage; }
        }

        public static PcaScatterChart Create(Control parent)
        {
            return Create(parent, PcaScatterOptions.CreateDefault());
        }

        public static PcaScatterChart Create(Control parent, PcaScatterOptions options)
        {
            return new PcaScatterChart(parent, options);
        }

        public static PcaScatterChart Create(Control parent, PcaScatterDataSource dataSource)
        {
            return Create(parent, dataSource, PcaScatterOptions.CreateDefault());
        }

        public static PcaScatterChart Create(Control parent, PcaScatterDataSource dataSource, PcaScatterOptions options)
        {
            PcaScatterChart chart = Create(parent, options);
            chart.Bind(dataSource, options);
            return chart;
        }

        public void Bind(PcaScatterDataSource dataSource)
        {
            Bind(dataSource, options);
        }

        public void Bind(PcaScatterDataSource dataSource, PcaScatterOptions newOptions)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException("dataSource");
            }

            PcaScatterOptions nextOptions = newOptions == null ? PcaScatterOptions.CreateDefault() : newOptions.Clone();
            try
            {
                PcaAnalysisResult result = dataSource.Analyze(nextOptions.Analysis);
                Bind(result, nextOptions);
            }
            catch (Exception ex)
            {
                OnAnalysisFailed(new PcaScatterAnalysisFailedEventArgs(ex));
                throw;
            }
        }

        public void Bind(PcaAnalysisResult result)
        {
            Bind(result, options);
        }

        public void Bind(PcaAnalysisResult result, PcaScatterOptions newOptions)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            options = newOptions == null ? PcaScatterOptions.CreateDefault() : newOptions.Clone();
            analysisResult = result;
            IEnumerable<LightningScatterSeries> series = seriesBuilder.Build(analysisResult, options.Series);
            scatterChart.UpdateData(series, options.ToScatterOptions(analysisResult));
            OnAnalysisCompleted(new PcaScatterAnalysisCompletedEventArgs(analysisResult));
        }

        public void BindFromDatabase(PcaScatterDatabaseOptions databaseOptions)
        {
            BindFromDatabase(databaseOptions, options);
        }

        public void BindFromDatabase(PcaScatterDatabaseOptions databaseOptions, PcaScatterOptions newOptions)
        {
            PcaScatterDatabaseOptions effectiveDatabaseOptions = databaseOptions ?? PcaScatterDatabaseOptions.CreateDefault();
            PcaScatterOptions nextOptions = newOptions == null ? PcaScatterOptions.CreateDefault() : newOptions.Clone();

            try
            {
                ActDataRepository repository = new ActDataRepository(effectiveDatabaseOptions.ToActDataQueryOptions());
                IList<string> actDataDocuments = repository.LoadActData();
                PcaAnalysisResult result = PcaScatterDataSource
                    .FromActDataJson(actDataDocuments)
                    .Analyze(nextOptions.Analysis);
                Bind(result, nextOptions);
            }
            catch (Exception ex)
            {
                OnAnalysisFailed(new PcaScatterAnalysisFailedEventArgs(ex));
                throw;
            }
        }

        public void BindFromExadata(PcaScatterExadataOptions exadataOptions)
        {
            BindFromExadata(exadataOptions, options);
        }

        public void BindFromExadata(
            PcaScatterExadataOptions exadataOptions,
            PcaScatterOptions newOptions)
        {
            PcaScatterExadataOptions effectiveOptions =
                exadataOptions ?? PcaScatterExadataOptions.CreateDefault();
            PcaScatterOptions nextOptions =
                newOptions == null ? PcaScatterOptions.CreateDefault() : newOptions.Clone();

            try
            {
                var repository = new ConvExperimentRepository(effectiveOptions.ToQueryOptions());
                IList<PcaExadataSourceRow> rows = repository.LoadAll();
                var snapshot = new PcaExadataSnapshot(rows, DateTime.UtcNow);
                var service = new PcaExadataService(repository);
                PcaExadataAnalysisResult result = service.AnalyzeSnapshot(
                    snapshot,
                    effectiveOptions.ParameterType,
                    nextOptions.Analysis);
                Bind(result.AnalysisResult, nextOptions);
            }
            catch (Exception ex)
            {
                OnAnalysisFailed(new PcaScatterAnalysisFailedEventArgs(ex));
                throw;
            }
        }

        public IList<KnnNeighbor> FindNearest(string draftNo)
        {
            return FindNearest(draftNo, options.Analysis.NeighborCount);
        }

        public IList<KnnNeighbor> FindNearest(string draftNo, int count)
        {
            if (analysisResult == null)
            {
                return new List<KnnNeighbor>();
            }

            return analysisResult.FindNearest(draftNo, Math.Max(1, count));
        }

        public void Clear()
        {
            analysisResult = null;
            scatterChart.Clear();
        }

        public string SaveImage()
        {
            return scatterChart.SaveImage();
        }

        public string SaveImage(LightningScatterImageOptions imageOptions)
        {
            return scatterChart.SaveImage(imageOptions);
        }

        public Image LoadLastSavedImage()
        {
            return scatterChart.LoadLastSavedImage();
        }

        public Image GetLastSavedImage()
        {
            return scatterChart.GetLastSavedImage();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ScatterChart_PointClicked(object sender, LightningScatterPointClickEventArgs e)
        {
            ScatterSampleData sample = e == null || e.Point == null ? null : e.Point.Tag as ScatterSampleData;
            if (sample == null)
            {
                return;
            }

            IList<KnnNeighbor> neighbors = FindNearest(sample.DraftNo, options.Analysis.NeighborCount);
            OnSampleClicked(new PcaScatterSampleClickedEventArgs(sample, neighbors, e));
        }

        private void OnSampleClicked(PcaScatterSampleClickedEventArgs e)
        {
            EventHandler<PcaScatterSampleClickedEventArgs> handler = SampleClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnAnalysisCompleted(PcaScatterAnalysisCompletedEventArgs e)
        {
            EventHandler<PcaScatterAnalysisCompletedEventArgs> handler = AnalysisCompleted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnAnalysisFailed(PcaScatterAnalysisFailedEventArgs e)
        {
            EventHandler<PcaScatterAnalysisFailedEventArgs> handler = AnalysisFailed;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing && scatterChart != null)
            {
                scatterChart.PointClicked -= ScatterChart_PointClicked;
                if (parent != null && parent.Controls.Contains(scatterChart))
                {
                    parent.Controls.Remove(scatterChart);
                }

                scatterChart.Dispose();
                scatterChart = null;
            }

            disposed = true;
        }
    }
}
