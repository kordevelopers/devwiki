from __future__ import annotations

import argparse
import json
from pathlib import Path

from .config import load_config
from .db import load_source_rows, normalize_source_columns
from .json_features import build_feature_frame
from .pca_pipeline import find_neighbors, run_pca
from .plot import save_scatter


def main() -> int:
    parser = argparse.ArgumentParser(description="Run PCA scatter and KNN analysis.")
    parser.add_argument("--mode", choices=["sample", "odbc", "oracledb"], help="Override PCA_DB_MODE")
    parser.add_argument("--target", help="Target DRAFT_NO for KNN search")
    parser.add_argument("--output-dir", default="outputs", help="Directory for CSV and chart output")
    parser.add_argument(
        "--no-show-chart",
        action="store_true",
        help="Save the chart image without opening a matplotlib window.",
    )
    args = parser.parse_args()

    config = load_config(args.mode, args.target)
    output_dir = Path(args.output_dir)
    source = normalize_source_columns(load_source_rows(config))
    features = build_feature_frame(source, config.param_type)
    result = run_pca(features)

    target = config.target_draft_no or result.points["DRAFT_NO"].iloc[0]
    neighbors = find_neighbors(result, target)

    output_dir.mkdir(parents=True, exist_ok=True)
    points_path = output_dir / "pca_points.csv"
    neighbors_path = output_dir / "knn_neighbors.csv"
    chart_path = output_dir / "pca_scatter.png"
    audit_path = output_dir / "feature_selection_audit.csv"
    population_path = output_dir / "surviving_population.csv"
    diagnostic_path = output_dir / "diagnostic.json"

    result.points.to_csv(points_path, index=False, encoding="utf-8-sig")
    result.feature_audit.to_csv(audit_path, index=False, encoding="utf-8-sig")
    result.surviving_population.to_csv(population_path, index=False, encoding="utf-8-sig")
    diagnostic_path.write_text(
        json.dumps(result.diagnostic, ensure_ascii=False, indent=2),
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

    print(f"Mode: {config.mode}")
    print(f"PARAM_TYP: {config.param_type}")
    print(f"Rows: {len(result.points)}")
    print(f"Included features: {len(result.features)}")
    print(f"Excluded features: {len(result.excluded_features)}")
    print(
        "Explained variance: "
        f"PC1={result.pca.explained_variance_ratio_[0] * 100:.2f}%, "
        f"PC2={result.pca.explained_variance_ratio_[1] * 100:.2f}%"
    )
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
