from __future__ import annotations

import os
from pathlib import Path

import matplotlib

if not os.environ.get("MPLBACKEND"):
    try:
        matplotlib.use("TkAgg")
    except Exception:
        pass

import matplotlib.pyplot as plt
from matplotlib.gridspec import GridSpec
from matplotlib.widgets import Button, TextBox
import numpy as np
import pandas as pd


def save_scatter(
    points: pd.DataFrame,
    output_path: Path,
    target_draft_no: str = "",
    show_chart: bool = True,
    standardized_matrix: np.ndarray | None = None,
    neighbor_count: int = 3,
    chart_title: str = "PCA Scatter",
    projection_name: str = "PCA",
    chart_data: pd.DataFrame | None = None,
    chart_export_path: Path | None = None,
) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    fig = plt.figure(figsize=(11, 9))
    layout = GridSpec(2, 1, figure=fig, height_ratios=(3.8, 1.2), hspace=0.18)
    axis = fig.add_subplot(layout[0])
    table_axis = fig.add_subplot(layout[1])
    table_axis.axis("off")
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
    axis.set_title(chart_title)
    _apply_axis_range_ticks(axis, points)
    axis.grid(True, color="#d8dee9", linewidth=0.7, alpha=0.75)
    axis.legend(loc="best")
    _register_click_handler(
        fig,
        axis,
        points,
        target_overlay,
        selected_overlay,
        neighbor_overlay,
        standardized_matrix,
        neighbor_count,
        table_axis,
        target_draft_no,
    )
    if show_chart and chart_data is not None:
        button_axis = fig.add_axes((0.78, 0.015, 0.18, 0.04))
        export_button = Button(button_axis, "Export chart Excel")

        def export_from_chart(_event: object) -> None:
            import tkinter as tk
            from tkinter import filedialog

            root = tk.Tk()
            root.withdraw()
            selected_path = filedialog.asksaveasfilename(
                title="Export chart data",
                defaultextension=".xlsx",
                filetypes=[("Excel workbook", "*.xlsx")],
                initialfile=(chart_export_path.name if chart_export_path else "chart_data.xlsx"),
            )
            root.destroy()
            if selected_path:
                from .export import export_chart_data

                export_chart_data(chart_data, Path(selected_path))
                print(f"Chart Excel: {selected_path}")

        export_button.on_clicked(export_from_chart)
    fig.subplots_adjust(bottom=0.08)
    fig.savefig(output_path, dpi=150)
    if show_chart:
        print(f"Opening {projection_name} chart with matplotlib backend: {plt.get_backend()}")
        plt.show(block=True)
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
    target_overlay: plt.Collection,
    selected_overlay: plt.Collection,
    neighbor_overlay: plt.Collection,
    standardized_matrix: np.ndarray | None,
    neighbor_count: int,
    table_axis: plt.Axes,
    target_draft_no: str = "",
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
    table_cells: dict[int, int] = {}

    def update_grid(selected_index: int, neighbor_indices: np.ndarray, distances: np.ndarray) -> None:
        table_axis.clear()
        table_axis.axis("off")
        selected = points.iloc[selected_index]
        rows = [["Selected", selected["DRAFT_NO"], selected["PARAM_TYP"], selected["LABEL_Y"],
                 f"{selected['X1']:.4f}", f"{selected['X2']:.4f}", "0.0000"]]
        for rank, (source_index, distance) in enumerate(zip(neighbor_indices, distances), start=1):
            item = points.iloc[source_index]
            rows.append([f"Nearest {rank}", item["DRAFT_NO"], item["PARAM_TYP"], item["LABEL_Y"],
                         f"{item['X1']:.4f}", f"{item['X2']:.4f}", f"{distance:.4f}"])
        table = table_axis.table(
            cellText=rows,
            colLabels=["Relation", "DRAFT_NO", "PARAM_TYP", "LABEL(Y)", "X1", "X2", "Distance"],
            loc="center", cellLoc="left",
            colWidths=[0.13, 0.18, 0.13, 0.18, 0.12, 0.12, 0.14],
        )
        table.auto_set_font_size(False)
        table.set_fontsize(8.5)
        table.scale(1, 1.35)
        table_cells.clear()
        for row_index, source_index in enumerate([selected_index, *neighbor_indices], start=1):
            for column_index in range(7):
                cell = table.get_celld()[(row_index, column_index)]
                cell.set_edgecolor("#cbd5e1")
                cell.set_picker(True)
                table_cells[id(cell)] = source_index

    def select_index(selected_index: int) -> None:
        neighbor_indices, distances = _find_nearest_indices(distance_matrix, selected_index, max(1, neighbor_count))
        selected_overlay.set_offsets(coordinates[[selected_index]])
        neighbor_overlay.set_offsets(coordinates[neighbor_indices])
        update_grid(selected_index, neighbor_indices, distances)
        selected = points.iloc[selected_index]
        lines = [f"Selected: {selected['DRAFT_NO']}", "Nearest:"]
        for rank, (source_index, distance) in enumerate(zip(neighbor_indices, distances), start=1):
            lines.append(f"{rank}. {points.iloc[source_index]['DRAFT_NO']}  d={distance:.4f}")
        annotation.xy = (float(selected["X1"]), float(selected["X2"]))
        annotation.set_text("\n".join(lines))
        annotation.set_visible(True)
        fig.canvas.draw_idle()

    initial_index = 0
    if target_draft_no:
        matches = points.index[points["DRAFT_NO"].astype(str).str.casefold() == target_draft_no.casefold()]
        if len(matches):
            initial_index = int(matches[0])
    initial_neighbors, initial_distances = _find_nearest_indices(distance_matrix, initial_index, max(1, neighbor_count))
    update_grid(initial_index, initial_neighbors, initial_distances)

    def on_click(event: object) -> None:
        if getattr(event, "inaxes", None) is not axis:
            return
        xdata = getattr(event, "xdata", None)
        ydata = getattr(event, "ydata", None)
        if xdata is None or ydata is None:
            return

        clicked_index = int(np.argmin(np.sum((coordinates - np.array([xdata, ydata])) ** 2, axis=1)))
        select_index(clicked_index)

    def on_table_pick(event: object) -> None:
        index = table_cells.get(id(getattr(event, "artist", None)))
        if index is not None:
            select_index(index)

    def on_draft_submit(value: str) -> None:
        matches = points.index[points["DRAFT_NO"].astype(str).str.casefold() == value.strip().casefold()]
        if len(matches):
            _set_target_overlay(target_overlay, points, value.strip())
            select_index(int(matches[0]))
        else:
            annotation.set_text(f"Draft Number not found: {value.strip()}")
            annotation.set_visible(bool(value.strip()))
            fig.canvas.draw_idle()

    fig.canvas.mpl_connect("button_press_event", on_click)
    fig.canvas.mpl_connect("pick_event", on_table_pick)
    input_axis = fig.add_axes((0.08, 0.015, 0.26, 0.04))
    input_axis.set_zorder(100)
    draft_box = TextBox(input_axis, "Draft Number: ", initial=target_draft_no or "")
    draft_box.on_submit(on_draft_submit)
    draft_box.set_active(True)
    search_axis = fig.add_axes((0.35, 0.015, 0.10, 0.04))
    search_axis.set_zorder(100)
    search_button = Button(search_axis, "Find")

    def open_draft_input(_event: object) -> None:
        import tkinter as tk
        from tkinter import simpledialog

        root = tk.Tk()
        root.withdraw()
        value = simpledialog.askstring("Draft Number", "입력할 Draft Number:", initialvalue=draft_box.text)
        root.destroy()
        if value is not None:
            draft_box.set_val(value)
            on_draft_submit(value)

    search_button.on_clicked(open_draft_input)
    # Matplotlib 위젯은 참조가 유지되어야 키보드 입력 이벤트가 계속 동작한다.
    widgets = getattr(fig, "_pca_widgets", [])
    widgets.append(draft_box)
    if show_chart and chart_data is not None:
        widgets.append(export_button)
    widgets.append(search_button)
    fig._pca_widgets = widgets


def _find_nearest_indices(matrix: np.ndarray, target_index: int, count: int) -> tuple[np.ndarray, np.ndarray]:
    differences = matrix - matrix[target_index]
    distances = np.sqrt(np.sum(differences * differences, axis=1))
    distances[target_index] = np.inf
    order = np.argsort(distances)[: min(count, len(distances) - 1)]
    return order, distances[order]
