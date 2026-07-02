from __future__ import annotations

import argparse

from pca_runner.config import load_config
from pca_runner.db import load_source_rows, normalize_source_columns


def main() -> int:
    parser = argparse.ArgumentParser(description="Test Oracle/ODBC PCA source query.")
    parser.add_argument("--mode", choices=["odbc", "oracledb"], required=True)
    parser.add_argument("--limit", type=int, default=5)
    args = parser.parse_args()

    config = load_config(mode_override=args.mode)
    frame = normalize_source_columns(load_source_rows(config))
    print(f"Connected with mode: {args.mode}")
    print(f"Rows fetched: {len(frame)}")
    print(frame.head(args.limit).to_string(index=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
