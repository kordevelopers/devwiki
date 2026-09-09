import argparse
import json
from pathlib import Path
from .analysis import run_umap, find_neighbors
from .chart import save_chart
from .config import load_config
from .export import export_chart_data
from .source import SUPPORTED_DATA_TYPES, build_feature_frame, load_source_csv, load_source_rows, normalize_source_columns

def main() -> int:
    parser = argparse.ArgumentParser(description="Run UMAP scatter and nearest-neighbor analysis.")
    parser.add_argument("--source-csv", type=Path)
    parser.add_argument("--data-type", type=str.upper, choices=sorted(SUPPORTED_DATA_TYPES))
    parser.add_argument("--target")
    parser.add_argument("--output-dir", default="outputs")
    parser.add_argument("--no-show-chart", action="store_true")
    args = parser.parse_args()
    config = load_config(args.target, args.data_type, resolve_sql=args.source_csv is None)
    raw = load_source_csv(args.source_csv) if args.source_csv else load_source_rows(config)
    source = normalize_source_columns(raw)
    results = {}
    for data_type in sorted(SUPPORTED_DATA_TYPES):
        try: results[data_type] = run_umap(build_feature_frame(source, data_type))
        except ValueError: pass
    if not results: raise ValueError("No usable RESPONSE, DEFECT, or EPM data was found.")
    selected_type = config.data_type if config.data_type in results else next(iter(results))
    result = results[selected_type]; target = config.target_draft_no or result.points.DRAFT_NO.iloc[0]
    output = Path(args.output_dir); output.mkdir(parents=True, exist_ok=True)
    result.points.to_csv(output / "umap_points.csv", index=False, encoding="utf-8-sig")
    export_chart_data(result.points, output / "umap_chart_data.xlsx")
    neighbors = find_neighbors(result, target)
    (output / "knn_neighbors.csv").write_text("Rank,Similar_Draft,Distance\n" + "\n".join(f"{n},{result.points.iloc[i].DRAFT_NO},{d:.4f}" for n,(i,d) in enumerate(neighbors,1)) + "\n", encoding="utf-8-sig")
    (output / "diagnostic.json").write_text(json.dumps({**result.diagnostic, "DataType": selected_type, "SourceRowCount": len(source), "TargetDraftNo": target}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    save_chart(results, {key: value.points for key, value in results.items()}, output / "umap_scatter.png", selected_type, target, not args.no_show_chart)
    print(f"Mode: {'csv' if args.source_csv else 'oracledb'}")
    print(f"DataType: {selected_type}")
    print(f"Rows: {len(result.points)}")
    print(f"Chart: {output / 'umap_scatter.png'}")
    print(f"Chart Excel: {output / 'umap_chart_data.xlsx'}")
    return 0
