# t-SNE performance and regression verification

Open `TsneDemo.slnx`, restore NuGet packages, and run the `TsneVerification` console project. It includes this regression harness alongside standalone engine and DataTable integration checks. No PowerShell script is required.

To save or compare snapshots explicitly, run the built executable from a command prompt:

```bat
TsneVerification.exe --performance results.json
TsneVerification.exe --performance --compare baseline.json results.json
```

To compare revisions, build separate checkouts and save a snapshot from each executable. Do not load baseline and current assemblies into the same process. The previous automatic Git archive/build script has been removed.

The harness uses deterministic synthetic input and Accord's actual 1,000-iteration optimizer. It compares selected features, standardized values, scaler statistics, coordinates, KNN results, identifiers, labels, feature audits and exported DataTables. Cases cover missing/null/nonnumeric data, mean imputation, complete-only selection, metadata, nested objects/arrays, case-insensitive fields, decimal conversion, numeric metadata collisions and malformed inputs. Comparison uses relative numerical tolerance `1e-12`; structure and strings must match exactly.

Projection-cache checks cover exact-input reuse, changed values/perplexity/seed/row-order invalidation, input/output mutation isolation, wide input (1,031 features), and fresh identifiers/labels on cached coordinates.

Timings cover service `AnalyzeSnapshot` and `QueryDraftAsync`, including preprocessing and analysis. The 160-row × 80-feature synthetic case does not predict other workloads. UI rendering, Oracle reads and LightningChart interactions are outside this console harness.
