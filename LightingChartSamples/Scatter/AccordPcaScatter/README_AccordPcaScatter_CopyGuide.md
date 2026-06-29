# AccordPcaScatter Copy Guide

This folder is the Accord.NET-only PCA Scatter module. It does not use the
manual PCA pipeline. When moving the feature into another project, copy the
whole `Scatter/AccordPcaScatter` folder as one unit.

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
- `LightningScatter.cs`: LightningChart 8 based general scatter wrapper.

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

You can also bind after the form is created:

```csharp
var form = new AccordScatterMain();
await form.LoadConvExperimentDataTableAsync(table);
form.ShowDialog(owner);
```

## Manual PCA Location

The previous manual PCA implementation has been moved to
`Scatter/LegacyManualPcaScatter`. It is not required when copying the
Accord.NET-only module.
