# t-SNE performance and regression verification

Windows / Visual Studio MSBuild / .NET Framework 4.5.1 reference assemblies and the repository's restored `packages` are required. Run from the repository root:

```powershell
# Export and build committed baseline sources into a new temporary directory.
.\diagnostics\tsne-performance\Verify-TsnePerformance.ps1 -Revision HEAD

# Substitute the baseline results path printed by the first command.
.\diagnostics\tsne-performance\Verify-TsnePerformance.ps1 -WorkingTree -BaselineResult 'C:\...\results.json'
```

Both ReportMaker and the Pccb host are built in Release. Source snapshots, binaries and JSON results stay in new directories outside the checkout; existing `bin` / `obj` files are untouched. Optional `-MsBuildPath` and `-OutputDirectory` override discovery and output location. `-Revision` accepts a commit or ref, allowing comparisons after the improvement has been committed.

The console harness uses deterministic synthetic input and Accord's actual 1,000-iteration optimizer. It compares selected features, standardized values, scaler statistics, projected coordinates, KNN results, source identifiers, labels, feature audit details and exported DataTables with the baseline. Cases cover missing/null/nonnumeric data, mean imputation, complete-only selection, metadata, nested objects/arrays, case-insensitive fields, decimal conversion, numeric metadata collisions and malformed inputs. Comparison uses a relative numerical tolerance of `1e-12`; structure and strings must match exactly.

If the assembly exposes the projection cache, additional assertions verify exact-input reuse, changed values/perplexity/seed/row-order invalidation, input and output mutation isolation, rows exceeding a hash buffer (1,031 features), and fresh identifiers/labels on cached coordinates.

Timings describe **service `AnalyzeSnapshot` and service `QueryDraftAsync` calls**, which include preprocessing and analysis. These are not UI TextBox search timings: the UI's existing search path can already reuse its displayed analysis. The 160-row × 80-feature case is a synthetic comparison and does not predict the user's 70-second workload. Run multiple fresh baseline/current processes when interpreting timing differences; cold-run differences of a few percent can be noise. UI rendering, Oracle reads and LightningChart interactions are outside this console harness.
