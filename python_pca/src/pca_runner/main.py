from __future__ import annotations

import argparse
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

    result.points.to_csv(points_path, index=False, encoding="utf-8-sig")
    with neighbors_path.open("w", encoding="utf-8-sig", newline="") as file:
        file.write("Rank,Similar_Draft,Distance\n")
        for row in neighbors:
            file.write(f"{row.rank},{row.similar_draft},{row.distance:.4f}\n")
    save_scatter(result.points, chart_path, target)

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
    return 0
