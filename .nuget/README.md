# Local t-SNE experiment packages

`NuGet.Config` restores `TAS.Experimental.*` from `local/` and other dependencies from NuGet.org. These are unofficial packages prepared for this repository's tests; neither GitHub implementation was found in the NuGet.org package search. These package IDs are not upstream releases.

| Package (1.0.0) | Source revision | Assembly |
|---|---|---|
| TAS.Experimental.HybridTsne | [Orlinski/Hybrid_t-SNE, 8a19f4e](https://github.com/Orlinski/Hybrid_t-SNE/tree/8a19f4e19ffc0b8e5b142533401e157972e2cdbf) | HybridTsne.dll, x64, net451 |
| TAS.Experimental.TsneCSharp | [jdmccaffrey/tsne-csharp, 7739a9d](https://github.com/jdmccaffrey/tsne-csharp/tree/7739a9ded14ff004b1b213146858ce25f8a97386) | TsneCSharp.dll, AnyCPU, net451 |

Each archive includes `UPSTREAM.txt` with the full commit and assembly SHA256, plus NuGet repository metadata identifying the pinned source. CSharp includes the upstream MIT license. The pinned Hybrid source contains no LICENSE file; local packaging is not a grant of redistribution rights. No package has been published to a remote feed.

Assemblies were compiled from unmodified upstream sources with Visual Studio Roslyn, C# 7.3, optimization and deterministic compilation, against .NET Framework 4.5.1 references. Hybrid includes Config, FFTRepulsion, Gradient, HashSpatialTree, LSHForest, Quicksort, Selection, tSNE and AssemblyInfo sources. CSharp includes TSNEProgram.cs. Hybrid pins MathNet.Numerics 4.7.0 and MathNet.Numerics.MKL.Win-x64 2.3.0 as NuGet dependencies; those official packages are not bundled here.

For an engine update, compile the selected upstream revision, refresh provenance/license metadata, pack a new version with NuGet, and update `packages.config` and project HintPaths together. Verify a clean restore and run `TsneVerification` before replacing pinned versions. Runtime builds only restore NuGet packages; they never download or compile upstream sources.
