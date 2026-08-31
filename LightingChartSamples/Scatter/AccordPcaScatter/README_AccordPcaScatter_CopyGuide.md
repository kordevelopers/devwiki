# AccordPcaScatter Copy Guide

This folder is the Accord.NET-only PCA Scatter module. It does not use the
manual PCA pipeline. When moving the feature into another project, copy the
whole `Scatter/AccordPcaScatter` folder with `Scatter/Common/LightningScatter.cs`.

## Included Files

- `AccordScatterMain.cs`: WinForms sample/popup screen for Accord.NET PCA.
- `AccordPcaScatterAnalyzer.cs`: PCA analyzer based on `Accord.Statistics.Analysis.PrincipalComponentAnalysis`.
- `AccordPcaCoreModels.cs`: standard scaler, KNN, diagnostics, options, result models.
- `AccordPcaExperimentModels.cs`: DataTable/JSON source rows, analysis result, Draft query models.
- `ConvExperimentRepository.cs`: converts caller-supplied `DataTable` rows into PCA input rows.
- `PcaJsonUtility.cs`: Newtonsoft.Json based JSON utility.
- `PcaScatterChart.cs`: facade that binds PCA results to Lightning Scatter.
- `PcaScatterOptions.cs`: PCA/chart/legend/tooltip/no-data/image options.
- `PcaScatterSeriesBuilder.cs`: converts PCA result rows into Lightning Scatter series.
- `PcaExadataSampleDataFactory.cs`, `ScatterSampleData.cs`, `PcaScatterPopupDataProvider.cs`: sample data and popup data provider examples.
- `Scatter/Common/LightningScatter.cs`: LightningChart 8 based general scatter wrapper.

## External References

The current project uses these external DLLs:

- Accord.NET 3.8: `Accord.dll`, `Accord.Math.dll`, `Accord.Math.Core.dll`, `Accord.Statistics.dll`.
- Newtonsoft.Json 13.x.
- LightningChart 8.5.1.1 WinForms DLLs.

## Input DataTable Columns

Pass a `DataTable` returned by the company service. Required columns:

- `DRAFT_NO`
- `PARAM_TYP`: `RESPONSE`, `DEFECT`, `EPM`, `PROBE`
- `ENGR_RSLT_VAL`: source label such as Pass or Review
- `CONV_EXPER_CTN`: JSON array string

Example:

```csharp
DataTable table = serviceResult;
using (var form = new AccordScatterMain(table))
{
    form.ShowDialog(owner);
}
```

The constructor queues the table and loads it after the popup is shown. The
chart is not drawn immediately after binding. Click `Chart Draw` to run PCA and
render the scatter chart.

If the form is already visible and you need to reload another table, call
`LoadConvExperimentDataTableAsync` from the visible form:

```csharp
await form.LoadConvExperimentDataTableAsync(table);
```

Do not call `await form.LoadConvExperimentDataTableAsync(table)` before
`ShowDialog()`. If you do, the analysis can start before the user sees the
popup. The constructor `new AccordScatterMain(table)` queues the table and
loads it after the form is shown, so the progress overlay is visible.

## Draft Search And Memory First

`Draft Search` uses the value in the `DRAFT_NO` textbox.

- `Memory First` checked: search and redraw from the already loaded in-memory
  snapshot.
- `Memory First` unchecked: call the injected `IPcaScatterPopupDataProvider`,
  rebuild the chart from the returned table, and then search the requested
  `DRAFT_NO`.

If no provider is injected and `Memory First` is unchecked, the popup shows a
message instead of silently using virtual data.

## Refresh And KNN Algorithm

`Refresh All` does not call the popup data provider again. It refreshes the
latest `DataTable` passed through `AccordScatterMain(table)` or
`LoadConvExperimentDataTableAsync(table)`. If the current data came from
`Virtual Data`, it refreshes that in-memory sample snapshot.

To compare KNN algorithms:

1. Load the popup with a `DataTable`.
2. Click `Chart Draw`.
3. Enter `DRAFT_NO`.
4. Select `Auto`, `BruteForce`, `KdTree`, or `BallTree` in the KNN combo box.
5. Click `Refresh All`.

The chart is rebuilt from the same table, and the nearest-neighbor grid is
recalculated for the entered `DRAFT_NO`.

The `Virtual Data` button is only a sample data path.

## Manual PCA Location

The current t-SNE implementation is separated into the `SKhynix.TAS.UI.Report.Pccb` and `SKhynix.TAS.UI.Report.Pccb.ReportMaker` projects.
It is not required when copying the Accord.NET-only module.
