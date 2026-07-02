from __future__ import annotations

from pathlib import Path

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd


def save_scatter(
    points: pd.DataFrame,
    output_path: Path,
    target_draft_no: str = "",
    show_chart: bool = True,
    standardized_matrix: np.ndarray | None = None,
    neighbor_count: int = 3,
) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    fig, axis = plt.subplots(figsize=(11, 7))
    points = points.reset_index(drop=True)

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

    target_overlay = axis.scatter([], [], s=170, marker="*", color="#f4a261",
                                  edgecolor="#1d3557", linewidth=1.0,
                                  label="Target", zorder=6)
    selected_overlay = axis.scatter([], [], s=185, marker="*", color="#ffd166",
                                    edgecolor="#073b4c", linewidth=1.2,
                                    label="Selected", zorder=7)
    neighbor_overlay = axis.scatter([], [], s=115, marker="o", color="#ffca3a",
                                    edgecolor="#111827", linewidth=1.3,
                                    label="Nearest 3", zorder=6)
    _set_target_overlay(target_overlay, points, target_draft_no)

    axis.axhline(0, color="#7a7f87", linewidth=0.8, linestyle="--")
    axis.axvline(0, color="#7a7f87", linewidth=0.8, linestyle="--")
    axis.set_title("PCA Scatter")
    _apply_axis_range_ticks(axis, points)
    axis.grid(True, color="#d8dee9", linewidth=0.7, alpha=0.75)
    axis.legend(loc="best")
    _register_click_handler(
        fig,
        axis,
        points,
        selected_overlay,
        neighbor_overlay,
        standardized_matrix,
        neighbor_count,
    )
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
    axis.set_xlabel(f"X1  Range [{_format_tick(x_min)} - {_format_tick(x_max)}]")
    axis.set_ylabel(f"X2  Range [{_format_tick(y_min)} - {_format_tick(y_max)}]")

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


def _set_target_overlay(target_overlay: plt.Collection, points: pd.DataFrame, target_draft_no: str) -> None:
    if not target_draft_no:
        target_overlay.set_offsets(np.empty((0, 2)))
        return
    target = points[points["DRAFT_NO"].str.lower() == target_draft_no.lower()]
    if target.empty:
        target_overlay.set_offsets(np.empty((0, 2)))
        return
    target_overlay.set_offsets(target[["X1", "X2"]].to_numpy())


def _register_click_handler(
    fig: plt.Figure,
    axis: plt.Axes,
    points: pd.DataFrame,
    selected_overlay: plt.Collection,
    neighbor_overlay: plt.Collection,
    standardized_matrix: np.ndarray | None,
    neighbor_count: int,
) -> None:
    coordinates = points[["X1", "X2"]].to_numpy(dtype=float)
    distance_matrix = (
        standardized_matrix
        if standardized_matrix is not None and len(standardized_matrix) == len(points)
        else coordinates
    )
    annotation = axis.annotate(
        "",
        xy=(0, 0),
        xytext=(12, 12),
        textcoords="offset points",
        bbox={"boxstyle": "round,pad=0.35", "fc": "white", "ec": "#111827", "alpha": 0.92},
        fontsize=9,
        visible=False,
    )

    def on_click(event: object) -> None:
        if getattr(event, "inaxes", None) is not axis:
            return
        xdata = getattr(event, "xdata", None)
        ydata = getattr(event, "ydata", None)
        if xdata is None or ydata is None:
            return

        clicked_index = int(np.argmin(np.sum((coordinates - np.array([xdata, ydata])) ** 2, axis=1)))
        neighbor_indices, distances = _find_nearest_indices(
            distance_matrix,
            clicked_index,
            max(1, neighbor_count),
        )
        selected_overlay.set_offsets(coordinates[[clicked_index]])
        neighbor_overlay.set_offsets(coordinates[neighbor_indices])

        selected = points.iloc[clicked_index]
        lines = [
            f"Selected: {selected['DRAFT_NO']}",
            "Nearest:",
        ]
        for rank, (source_index, distance) in enumerate(zip(neighbor_indices, distances), start=1):
            lines.append(f"{rank}. {points.iloc[source_index]['DRAFT_NO']}  d={distance:.4f}")
        annotation.xy = (float(selected["X1"]), float(selected["X2"]))
        annotation.set_text("\n".join(lines))
        annotation.set_visible(True)
        fig.canvas.draw_idle()

    fig.canvas.mpl_connect("button_press_event", on_click)


def _find_nearest_indices(matrix: np.ndarray, target_index: int, count: int) -> tuple[np.ndarray, np.ndarray]:
    differences = matrix - matrix[target_index]
    distances = np.sqrt(np.sum(differences * differences, axis=1))
    distances[target_index] = np.inf
    order = np.argsort(distances)[: min(count, len(distances) - 1)]
    return order, distances[order]
