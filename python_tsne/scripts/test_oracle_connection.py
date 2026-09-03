from __future__ import annotations

import argparse

from tsne_runner.config import load_config
from tsne_runner.source import load_source_rows, normalize_source_columns


def main() -> int:
    parser = argparse.ArgumentParser(description="Test the Oracle/ODBC t-SNE source query.")
    parser.add_argument("--mode", choices=["odbc", "oracledb"], required=True)
    args = parser.parse_args()

    config = load_config(mode_override=args.mode)
    frame = normalize_source_columns(load_source_rows(config))
    print(f"Connected with mode: {args.mode}")
    print(f"Rows fetched: {len(frame)}")
    print(f"Columns: {', '.join(frame.columns)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
