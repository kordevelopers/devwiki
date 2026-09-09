from __future__ import annotations

import argparse

from tsne_runner.config import load_config
from tsne_runner.source import load_source_rows, normalize_source_columns


def main() -> int:
    parser = argparse.ArgumentParser(description="Test the Oracle t-SNE source query.")
    parser.parse_args()

    config = load_config()
    frame = normalize_source_columns(load_source_rows(config))
    print("Connected with python-oracledb")
    print(f"Rows fetched: {len(frame)}")
    print(f"Columns: {', '.join(frame.columns)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
