from __future__ import annotations

import argparse
from dataclasses import replace
import json
from pathlib import Path

import pandas as pd

from .config import load_config
from .db import SUPPORTED_PARAMETER_TYPES, load_source_rows, normalize_source_columns
from .json_features import build_feature_frame
from .plot import save_scatter
from .tsne_pipeline import NeighborRow, TSNEResult, find_neighbors, run_tsne


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Run t-SNE scatter and KNN analysis using the PCA project data source."
    )
    parser.add_argument(
        "--mode",
        choices=["odbc", "oracledb"],
        help="Override PCA_DB_MODE for the shared database connection.",
    )
    parser.add_argument(
        "--source-csv",
        type=Path,
        help="Read an exported PCCB query CSV instead of connecting to the database.",
    )
    parser.add_argument(
        "--param-type",
        type=str.upper,
        choices=sorted(SUPPORTED_PARAMETER_TYPES),
        help="Override PCA_PARAM_TYP for this t-SNE run.",
    )
    parser.add_argument("--target", help="Target DRAFT_NO for KNN search.")
    parser.add_argument(
        "--output-dir",
        default="outputs/tsne",
        help="Directory for t-SNE CSV, JSON, and chart output.",
    )
    parser.add_argument(
        "--no-show-chart",
        action="store_true",
        help="Save the chart image without opening a matplotlib window.",
    )
    args = parser.parse_args()

    config = load_config(
        args.mode if args.source_csv is None else (args.mode or "oracledb"),
        args.target,
        resolve_sql=args.source_csv is None,
    )
    if args.param_type:
        config = replace(config, param_type=args.param_type)

    if args.source_csv is not None:
        raw_source = _load_source_csv(args.source_csv)
        source_mode = "csv"
        source_reference = str(args.source_csv.resolve())
    else:
        if config.mode == "sample":
            raise ValueError(
                "Integrated t-SNE does not use generated sample data. Set PCA_DB_MODE to "
                "'odbc' or 'oracledb', or pass --source-csv with a real PCCB export."
            )
        raw_source = load_source_rows(config)
        source_mode = config.mode
        source_reference = config.sql_file or "inline SQL"

    source = normalize_source_columns(raw_source)
    _validate_source_identifiers(source, config.param_type)
    features = build_feature_frame(source, config.param_type)
    features = _order_feature_rows(features)
    result = run_tsne(features)

    target = config.target_draft_no or result.points["DRAFT_NO"].iloc[0]
    neighbors = find_neighbors(result, target)
    output_dir = Path(args.output_dir)
    paths = _write_outputs(
        output_dir,
        result,
        neighbors,
        source_mode,
        source_reference,
        len(source),
        target,
    )

    save_scatter(
        result.points,
        paths["chart"],
        target,
        show_chart=not args.no_show_chart,
        standardized_matrix=result.standardized_matrix,
        neighbor_count=3,
        chart_title="t-SNE Scatter",
        projection_name="t-SNE",
    )
    _print_summary(config.param_type, source_mode, target, result, neighbors, paths)
    return 0


def _load_source_csv(path: Path) -> pd.DataFrame:
    if not path.exists():
        raise FileNotFoundError(f"Source CSV was not found: {path}")
    return pd.read_csv(path, encoding="utf-8-sig", dtype=str, keep_default_na=False)


def _validate_source_identifiers(source: pd.DataFrame, param_type: str) -> None:
    selected = source[source["PARAM_TYP"].str.upper() == param_type.upper()]
    draft_numbers = selected["DRAFT_NO"].fillna("").astype(str).str.strip()
    missing_tokens = {"", "none", "nan", "<na>"}
    if draft_numbers.str.casefold().isin(missing_tokens).any():
        raise ValueError(f"PARAM_TYP '{param_type}' contains an empty DRAFT_NO.")

    normalized = draft_numbers.str.casefold()
    duplicate_mask = normalized.duplicated()
    if duplicate_mask.any():
        duplicate = draft_numbers.loc[duplicate_mask].iloc[0]
        raise ValueError(
            f"Duplicated DRAFT_NO in PARAM_TYP '{param_type}' without regard to case: "
            f"{duplicate}"
        )


def _order_feature_rows(features: pd.DataFrame) -> pd.DataFrame:
    return features.sort_values(
        "DRAFT_NO",
        key=lambda values: values.astype(str).str.casefold(),
        kind="stable",
        ignore_index=True,
    )


def _write_outputs(
    output_dir: Path,
    result: TSNEResult,
    neighbors: list[NeighborRow],
    source_mode: str,
    source_reference: str,
    source_row_count: int,
    target: str,
) -> dict[str, Path]:
    output_dir.mkdir(parents=True, exist_ok=True)
    paths = {
        "points": output_dir / "tsne_points.csv",
        "neighbors": output_dir / "knn_neighbors.csv",
        "chart": output_dir / "tsne_scatter.png",
        "audit": output_dir / "feature_selection_audit.csv",
        "population": output_dir / "surviving_population.csv",
        "diagnostic": output_dir / "diagnostic.json",
    }
    result.points.to_csv(paths["points"], index=False, encoding="utf-8-sig")
    result.feature_audit.to_csv(paths["audit"], index=False, encoding="utf-8-sig")
    result.surviving_population.to_csv(paths["population"], index=False, encoding="utf-8-sig")
    diagnostic = {
        **result.diagnostic,
        "SourceMode": source_mode,
        "SourceReference": source_reference,
        "SourceRowCount": source_row_count,
        "ParameterType": str(result.points["PARAM_TYP"].iloc[0]),
        "TargetDraftNo": target,
    }
    paths["diagnostic"].write_text(
        json.dumps(diagnostic, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    with paths["neighbors"].open("w", encoding="utf-8-sig", newline="") as file:
        file.write("Rank,Similar_Draft,Distance\n")
        for row in neighbors:
            file.write(f"{row.rank},{row.similar_draft},{row.distance:.4f}\n")
    return paths


def _print_summary(
    param_type: str,
    source_mode: str,
    target: str,
    result: TSNEResult,
    neighbors: list[NeighborRow],
    paths: dict[str, Path],
) -> None:
    print(f"Mode: {source_mode}")
    print(f"PARAM_TYP: {param_type}")
    print(f"Rows: {len(result.points)}")
    print(f"Included features: {len(result.features)}")
    print(f"Excluded features: {len(result.excluded_features)}")
    print(f"Perplexity: {result.diagnostic['Perplexity']:.6g}")
    print(f"Effective learning rate: {result.diagnostic['EffectiveLearningRate']:.6g}")
    print(f"Executed iterations: {result.diagnostic['ExecutedIterations']}")
    print(f"KL divergence: {result.diagnostic['KullbackLeiblerDivergence']:.12g}")
    print(f"Target DRAFT_NO: {target}")
    print("Nearest neighbors:")
    for row in neighbors:
        print(f"  {row.rank}. {row.similar_draft}  distance={row.distance:.4f}")
    print(f"CSV: {paths['points']}")
    print(f"KNN: {paths['neighbors']}")
    print(f"Chart: {paths['chart']}")
    print(f"Feature audit: {paths['audit']}")
    print(f"Surviving population: {paths['population']}")
    print(f"Diagnostic: {paths['diagnostic']}")


if __name__ == "__main__":
    raise SystemExit(main())
