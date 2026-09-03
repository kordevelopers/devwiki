from __future__ import annotations

import argparse
import json
from pathlib import Path

from .analysis import find_neighbors, run_tsne
from .chart import save_scatter
from .config import load_config
from .source import (
    SUPPORTED_PARAMETER_TYPES,
    build_feature_frame,
    load_source_csv,
    load_source_rows,
    normalize_source_columns,
)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Run t-SNE scatter and KNN analysis from the PCCB source data."
    )
    parser.add_argument("--mode", choices=["odbc", "oracledb"], help="Override TSNE_DB_MODE.")
    parser.add_argument(
        "--source-csv",
        type=Path,
        help="Read an exported PCCB query CSV instead of connecting to the database.",
    )
    parser.add_argument(
        "--param-type",
        type=str.upper,
        choices=sorted(SUPPORTED_PARAMETER_TYPES),
        help="Override TSNE_PARAM_TYP.",
    )
    parser.add_argument("--target", help="Target DRAFT_NO for KNN search.")
    parser.add_argument("--output-dir", default="outputs", help="CSV and chart output directory.")
    parser.add_argument(
        "--no-show-chart",
        action="store_true",
        help="Save the chart image without opening a matplotlib window.",
    )
    args = parser.parse_args()

    config = load_config(
        args.mode if args.source_csv is None else (args.mode or "oracledb"),
        args.target,
        args.param_type,
        resolve_sql=args.source_csv is None,
    )
    if args.source_csv is not None:
        raw_source = load_source_csv(args.source_csv)
        source_mode = "csv"
        source_reference = str(args.source_csv.resolve())
    else:
        raw_source = load_source_rows(config)
        source_mode = config.mode
        source_reference = config.sql_file or "inline SQL"

    source = normalize_source_columns(raw_source)
    feature_frame = build_feature_frame(source, config.param_type)
    result = run_tsne(feature_frame)

    target = config.target_draft_no or result.points["DRAFT_NO"].iloc[0]
    neighbors = find_neighbors(result, target)
    output_dir = Path(args.output_dir)
    points_path = output_dir / "tsne_points.csv"
    neighbors_path = output_dir / "knn_neighbors.csv"
    chart_path = output_dir / "tsne_scatter.png"
    audit_path = output_dir / "feature_selection_audit.csv"
    population_path = output_dir / "surviving_population.csv"
    diagnostic_path = output_dir / "diagnostic.json"

    output_dir.mkdir(parents=True, exist_ok=True)
    result.points.to_csv(points_path, index=False, encoding="utf-8-sig")
    result.feature_audit.to_csv(audit_path, index=False, encoding="utf-8-sig")
    result.surviving_population.to_csv(population_path, index=False, encoding="utf-8-sig")
    diagnostic = {
        **result.diagnostic,
        "SourceMode": source_mode,
        "SourceReference": source_reference,
        "SourceRowCount": len(source),
        "ParameterType": config.param_type,
        "TargetDraftNo": target,
    }
    diagnostic_path.write_text(
        json.dumps(diagnostic, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    with neighbors_path.open("w", encoding="utf-8-sig", newline="") as file:
        file.write("Rank,Similar_Draft,Distance\n")
        for row in neighbors:
            file.write(f"{row.rank},{row.similar_draft},{row.distance:.4f}\n")

    save_scatter(
        result.points,
        chart_path,
        target,
        show_chart=not args.no_show_chart,
        standardized_matrix=result.standardized_matrix,
        neighbor_count=3,
    )

    print(f"Mode: {source_mode}")
    print(f"PARAM_TYP: {config.param_type}")
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
    print(f"CSV: {points_path}")
    print(f"KNN: {neighbors_path}")
    print(f"Chart: {chart_path}")
    print(f"Feature audit: {audit_path}")
    print(f"Surviving population: {population_path}")
    print(f"Diagnostic: {diagnostic_path}")
    return 0
