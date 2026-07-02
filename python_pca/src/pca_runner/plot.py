from __future__ import annotations

from pathlib import Path

import matplotlib.pyplot as plt
import pandas as pd


def save_scatter(
    points: pd.DataFrame,
    output_path: Path,
    target_draft_no: str = "",
    show_chart: bool = True,
) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    fig, axis = plt.subplots(figsize=(11, 7))

    palette = {"PASS": "#2a9d8f", "REVIEW": "#e76f51"}
    for label, group in points.groupby(points["LABEL_Y"].str.upper()):
        axis.scatter(
            group["X1"],
            group["X2"],
            s=48,
            alpha=0.84,
            label=label.title(),
            color=palette.get(label, "#457b9d"),
            edgecolor="white",
            linewidth=0.6,
        )

    if target_draft_no:
        target = points[points["DRAFT_NO"].str.lower() == target_draft_no.lower()]
        if not target.empty:
            axis.scatter(
                target["X1"],
                target["X2"],
                s=160,
                marker="*",
                color="#f4a261",
                edgecolor="#1d3557",
                linewidth=1.0,
                label=f"Target {target_draft_no}",
                zorder=5,
            )

    axis.axhline(0, color="#7a7f87", linewidth=0.8, linestyle="--")
    axis.axvline(0, color="#7a7f87", linewidth=0.8, linestyle="--")
    axis.set_xlabel("X1")
    axis.set_ylabel("X2")
    axis.set_title("PCA Scatter")
    _apply_axis_range_ticks(axis, points)
    axis.grid(True, color="#d8dee9", linewidth=0.7, alpha=0.75)
    axis.legend(loc="best")
    fig.tight_layout()
    fig.savefig(output_path, dpi=150)
    if show_chart:
        plt.show()
    else:
        plt.close(fig)


def _apply_axis_range_ticks(axis: plt.Axes, points: pd.DataFrame) -> None:
    x_min, x_max = _resolve_axis_bounds(points["X1"])
    y_min, y_max = _resolve_axis_bounds(points["X2"])
    axis.set_xlim(x_min, x_max)
    axis.set_ylim(y_min, y_max)

    x_ticks = _build_ticks(x_min, x_max)
    y_ticks = _build_ticks(y_min, y_max)
    axis.set_xticks(x_ticks)
    axis.set_yticks(y_ticks)
    axis.set_xticklabels([_format_tick(value) for value in x_ticks])
    axis.set_yticklabels([_format_tick(value) for value in y_ticks])


def _resolve_axis_bounds(series: pd.Series) -> tuple[float, float]:
    minimum = min(float(series.min()), 0.0)
    maximum = max(float(series.max()), 0.0)
    if minimum == maximum:
        minimum -= 1.0
        maximum += 1.0
    padding = (maximum - minimum) * 0.05
    return minimum - padding, maximum + padding


def _build_ticks(minimum: float, maximum: float, count: int = 7) -> list[float]:
    if count <= 1:
        return [minimum, maximum]
    step = (maximum - minimum) / float(count - 1)
    return [minimum + (step * index) for index in range(count)]


def _format_tick(value: float) -> str:
    return f"{value:.2f}"
