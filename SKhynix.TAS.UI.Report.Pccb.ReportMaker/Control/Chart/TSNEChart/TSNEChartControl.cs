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
    public sealed class TSNESampleClickedEventArgs : EventArgs
    {
        public TSNESampleClickedEventArgs(TSNEPointData sample, IList<KnnNeighbor> neighbors, LightningScatterPointClickEventArgs sourceEventArgs)
        {
            Sample = sample;
            Neighbors = neighbors == null ? new List<KnnNeighbor>() : neighbors.ToList();
            SourceEventArgs = sourceEventArgs;
        }

        public TSNEPointData Sample { get; private set; }
        public IList<KnnNeighbor> Neighbors { get; private set; }
        public LightningScatterPointClickEventArgs SourceEventArgs { get; private set; }
    }

    public sealed class TSNEAnalysisCompletedEventArgs : EventArgs
    {
        public TSNEAnalysisCompletedEventArgs(TSNEAnalysisResult analysisResult)
        {
            AnalysisResult = analysisResult;
        }

        public TSNEAnalysisResult AnalysisResult { get; private set; }
    }

    public sealed class TSNEAnalysisFailedEventArgs : EventArgs
    {
        public TSNEAnalysisFailedEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }
    }

    public sealed class TSNEChart : IDisposable
    {
        private readonly WinFormsControl parent;
        private readonly TSNEScatterSeriesBuilder seriesBuilder;
        private LightningScatter scatterChart;
        private TSNEScatterOptions options;
        private TSNEAnalysisResult analysisResult;
        private DataTable rawData;
        private bool disposed;

        private TSNEChart(WinFormsControl parent, TSNEScatterOptions options)
        {
            this.parent = parent;
            this.options = options == null ? TSNEScatterOptions.CreateDefault() : options.Clone();
            seriesBuilder = new TSNEScatterSeriesBuilder();
            scatterChart = LightningScatter.Create(parent, Enumerable.Empty<LightningScatterSeries>(), this.options.ToScatterOptions(null));
            scatterChart.PointClicked += ScatterChart_PointClicked;
            scatterChart.Clear();
        }

        public event EventHandler<TSNESampleClickedEventArgs> SampleClicked;
        public event EventHandler<TSNEAnalysisCompletedEventArgs> AnalysisCompleted;
        public event EventHandler<TSNEAnalysisFailedEventArgs> AnalysisFailed;

        public LightningScatter ScatterControl
        {
            get { return scatterChart; }
        }

        public TSNEScatterOptions Options
        {
            get { return options.Clone(); }
        }

        public TSNEAnalysisResult AnalysisResult
        {
            get { return analysisResult; }
        }

        public DataTable RawData
        {
            get { return rawData == null ? null : rawData.Copy(); }
        }

        public string LastSavedImagePath
        {
            get { return scatterChart.LastSavedImagePath; }
        }

        public Image LastSavedImage
        {
            get { return scatterChart.LastSavedImage; }
        }

        public static TSNEChart Create(WinFormsControl parent)
        {
            return Create(parent, TSNEScatterOptions.CreateDefault());
        }

        public static TSNEChart Create(WinFormsControl parent, TSNEScatterOptions options)
        {
            return new TSNEChart(parent, options);
        }

        public static TSNEChart Create(WinFormsControl parent, TSNEScatterDataSource dataSource)
        {
            return Create(parent, dataSource, TSNEScatterOptions.CreateDefault());
        }

        public static TSNEChart Create(WinFormsControl parent, TSNEScatterDataSource dataSource, TSNEScatterOptions options)
        {
            TSNEChart chart = Create(parent, options);
            chart.Bind(dataSource, options);
            return chart;
        }

        public void Bind(TSNEScatterDataSource dataSource)
        {
            Bind(dataSource, options);
        }

        public void Bind(TSNEScatterDataSource dataSource, TSNEScatterOptions newOptions)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException("dataSource");
            }

            TSNEScatterOptions nextOptions = newOptions == null ? TSNEScatterOptions.CreateDefault() : newOptions.Clone();
            try
            {
                TSNEAnalysisResult result = dataSource.Analyze(nextOptions.Analysis);
                Bind(result, nextOptions);
            }
            catch (Exception ex)
            {
                OnAnalysisFailed(new TSNEAnalysisFailedEventArgs(ex));
                throw;
            }
        }

        public void Bind(TSNEAnalysisResult result)
        {
            Bind(result, options);
        }

        public void Bind(TSNEAnalysisResult result, TSNEScatterOptions newOptions)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            options = newOptions == null ? TSNEScatterOptions.CreateDefault() : newOptions.Clone();
            analysisResult = result;
            rawData = null;
            IEnumerable<LightningScatterSeries> series = seriesBuilder.Build(analysisResult, options.Series);
            scatterChart.UpdateData(series, options.ToScatterOptions(analysisResult));
            OnAnalysisCompleted(new TSNEAnalysisCompletedEventArgs(analysisResult));
        }

        public void Bind(TSNEExadataAnalysisResult result, TSNEScatterOptions newOptions)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            Bind(result.AnalysisResult, newOptions);
            rawData = result.CreateRawDataTable();
        }

        public void BindFromDatabase(TSNEScatterDatabaseOptions databaseOptions)
        {
            BindFromDatabase(databaseOptions, options);
        }

        public void BindFromDatabase(DataTable sourceTable)
        {
            BindFromDatabase(sourceTable, TSNEScatterDatabaseOptions.CreateDefault(), options);
        }

        public void BindFromDatabase(DataTable sourceTable, TSNEScatterDatabaseOptions databaseOptions)
        {
            BindFromDatabase(sourceTable, databaseOptions, options);
        }

        public void BindFromDatabase(DataTable sourceTable, TSNEScatterDatabaseOptions databaseOptions, TSNEScatterOptions newOptions)
        {
            TSNEScatterDatabaseOptions effectiveDatabaseOptions = databaseOptions ?? TSNEScatterDatabaseOptions.CreateDefault();
            effectiveDatabaseOptions.SourceTable = sourceTable;
            BindFromDatabase(effectiveDatabaseOptions, newOptions);
        }

        public void BindFromDatabase(TSNEScatterDatabaseOptions databaseOptions, TSNEScatterOptions newOptions)
        {
            TSNEScatterDatabaseOptions effectiveDatabaseOptions = databaseOptions ?? TSNEScatterDatabaseOptions.CreateDefault();
            TSNEScatterOptions nextOptions = newOptions == null ? TSNEScatterOptions.CreateDefault() : newOptions.Clone();

            try
            {
                if (effectiveDatabaseOptions.SourceTable == null)
                {
                    throw new InvalidOperationException(
                        "BindFromDatabase requires a DataTable. Load data in the UI/service layer and pass it to the chart.");
                }

                ActDataRepository repository = new ActDataRepository(effectiveDatabaseOptions.SourceTable, effectiveDatabaseOptions.ToActDataQueryOptions());
                IList<string> actDataDocuments = repository.LoadActData();
                TSNEAnalysisResult result = TSNEScatterDataSource
                    .FromActDataJson(actDataDocuments)
                    .Analyze(nextOptions.Analysis);
                Bind(result, nextOptions);
            }
            catch (Exception ex)
            {
                OnAnalysisFailed(new TSNEAnalysisFailedEventArgs(ex));
                throw;
            }
        }

        public void BindFromExadata(TSNEScatterExadataOptions exadataOptions)
        {
            BindFromExadata(exadataOptions, options);
        }

        public void BindFromExadata(DataTable sourceTable)
        {
            BindFromExadata(sourceTable, TSNEScatterExadataOptions.CreateDefault(), options);
        }

        public void BindFromExadata(DataTable sourceTable, TSNEScatterExadataOptions exadataOptions)
        {
            BindFromExadata(sourceTable, exadataOptions, options);
        }

        public void BindFromExadata(DataTable sourceTable, TSNEScatterExadataOptions exadataOptions, TSNEScatterOptions newOptions)
        {
            TSNEScatterExadataOptions effectiveOptions = exadataOptions ?? TSNEScatterExadataOptions.CreateDefault();
            effectiveOptions.SourceTable = sourceTable;
            BindFromExadata(effectiveOptions, newOptions);
        }

        public void BindFromExadata(TSNEScatterExadataOptions exadataOptions, TSNEScatterOptions newOptions)
        {
            TSNEScatterExadataOptions effectiveOptions = exadataOptions ?? TSNEScatterExadataOptions.CreateDefault();
            TSNEScatterOptions nextOptions = newOptions == null ? TSNEScatterOptions.CreateDefault() : newOptions.Clone();

            try
            {
                if (effectiveOptions.SourceTable == null)
                {
                    throw new InvalidOperationException(
                        "BindFromExadata requires a DataTable. Load data in the UI/service layer and pass it to the chart.");
                }

                var repository = new ConvExperimentRepository(effectiveOptions.SourceTable, effectiveOptions.ToQueryOptions());
                IList<TSNEExadataSourceRow> rows = repository.LoadAll();
                var snapshot = new TSNEExadataSnapshot(rows, DateTime.UtcNow);
                var service = new TSNEExadataService(repository);
                TSNEExadataAnalysisResult result = service.AnalyzeSnapshot(snapshot, effectiveOptions.ParameterType, nextOptions.Analysis);
                Bind(result, nextOptions);
            }
            catch (Exception ex)
            {
                OnAnalysisFailed(new TSNEAnalysisFailedEventArgs(ex));
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

        public void HighlightSelectedDraft(string draftNo)
        {
            if (analysisResult == null)
            {
                return;
            }

            TSNEScatterOptions nextOptions = options == null ? TSNEScatterOptions.CreateDefault() : options.Clone();
            if (nextOptions.Series == null)
            {
                nextOptions.Series = new TSNEScatterSeriesOptions();
            }

            nextOptions.Series.SelectedDraftNo = string.IsNullOrWhiteSpace(draftNo) ? string.Empty : draftNo.Trim();
            RefreshSeries(nextOptions);
        }

        public void HighlightDraft(string draftNo)
        {
            if (analysisResult == null)
            {
                return;
            }

            TSNEScatterOptions nextOptions = options == null ? TSNEScatterOptions.CreateDefault() : options.Clone();
            if (nextOptions.Series == null)
            {
                nextOptions.Series = new TSNEScatterSeriesOptions();
            }

            nextOptions.Series.HighlightDraftNo = string.IsNullOrWhiteSpace(draftNo) ? string.Empty : draftNo.Trim();
            nextOptions.Series.SelectedDraftNo = string.Empty;
            RefreshSeries(nextOptions);
        }

        public void ClearDraftHighlight()
        {
            HighlightDraft(string.Empty);
        }

        public void ClearSelectedDraftHighlight()
        {
            HighlightSelectedDraft(string.Empty);
        }

        public void Clear()
        {
            analysisResult = null;
            rawData = null;
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
            TSNEPointData sample = e == null || e.Point == null ? null : e.Point.Tag as TSNEPointData;
            if (sample == null)
            {
                return;
            }

            IList<KnnNeighbor> neighbors = FindNearest(sample.DraftNo, options.Analysis.NeighborCount);
            OnSampleClicked(new TSNESampleClickedEventArgs(sample, neighbors, e));
        }

        private void RefreshSeries(TSNEScatterOptions nextOptions)
        {
            options = nextOptions == null ? TSNEScatterOptions.CreateDefault() : nextOptions.Clone();
            IEnumerable<LightningScatterSeries> series = seriesBuilder.Build(analysisResult, options.Series);
            scatterChart.UpdateData(series, options.ToScatterOptions(analysisResult), true);
        }

        private void OnSampleClicked(TSNESampleClickedEventArgs e)
        {
            EventHandler<TSNESampleClickedEventArgs> handler = SampleClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnAnalysisCompleted(TSNEAnalysisCompletedEventArgs e)
        {
            EventHandler<TSNEAnalysisCompletedEventArgs> handler = AnalysisCompleted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnAnalysisFailed(TSNEAnalysisFailedEventArgs e)
        {
            EventHandler<TSNEAnalysisFailedEventArgs> handler = AnalysisFailed;
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









namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    /// <summary>
    /// Converts caller-supplied CONV_EXPER_CTN service results into TSNE source rows.
    /// DB access is intentionally outside this class. The UI or service layer should
    /// call the company data service and pass the completed DataTable here.
    /// </summary>
}
