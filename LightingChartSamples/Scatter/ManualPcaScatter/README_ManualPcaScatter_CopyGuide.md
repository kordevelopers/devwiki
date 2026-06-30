# ManualPcaScatter Copy Guide

This folder contains the PCA Scatter module that does not use an external PCA
library.
It includes the WinForms popup, DataTable/JSON conversion, StandardScaler,
PCA, KNN, diagnostics, and sample data path.

## Copy Scope

Copy these files together:

- `Scatter/ManualPcaScatter/*`
- `Scatter/Common/LightningScatter.cs`

The module still uses LightningChart for rendering and Newtonsoft.Json for
JSON parsing. It does not require external PCA DLLs.

## Main Form

Use `ManualPcaScatterMain` as the popup form.

```csharp
using (var form = new ManualPcaScatterMain())
{
    await form.LoadConvExperimentDataTableAsync(sourceTable);
    form.ShowDialog(owner);
}
```

Required `DataTable` columns:

- `DRAFT_NO`
- `PARAM_TYP`
- `ENGR_RSLT_VAL`
- `CONV_EXPER_CTN`

`CONV_EXPER_CTN` must contain the experiment JSON array. Numeric fields are
selected automatically after metadata, nonnumeric, missing, and low-variance
features are filtered.

## Algorithm

- Standardization: `StandardScalerModel`
- PCA: covariance matrix + eigenvector power iteration
- KNN: Euclidean distance in standardized feature space
- KNN index: Auto, BruteForce, KdTree, BallTree
- Validation: shared scaler, finite scores, PCA component checks, KNN ordering
