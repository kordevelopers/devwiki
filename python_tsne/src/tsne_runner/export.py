from __future__ import annotations

from pathlib import Path

import pandas as pd


def export_original_data(source: pd.DataFrame, output_path: Path) -> None:
    """Write the rows bound to the chart without feature transformation."""
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with pd.ExcelWriter(output_path, engine="openpyxl") as writer:
        source.to_excel(writer, sheet_name="OriginalData", index=False)

