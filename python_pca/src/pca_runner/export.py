from __future__ import annotations

from pathlib import Path

import pandas as pd


def export_chart_data(chart_data: pd.DataFrame, output_path: Path) -> None:
    """Write the exact rows and coordinates bound to the chart."""
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with pd.ExcelWriter(output_path, engine="openpyxl") as writer:
        chart_data.to_excel(writer, sheet_name="ChartData", index=False)
