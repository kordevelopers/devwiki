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
from matplotlib.collections import PathCollection
from matplotlib.gridspec import GridSpec
from matplotlib.widgets import Button, RadioButtons, TextBox
import numpy as np
import pandas as pd


def save_scatter(
    points: pd.DataFrame,
    output_path: Path,
    target_draft_no: str = "",
    show_chart: bool = True,
    standardized_matrix: np.ndarray | None = None,
    neighbor_count: int = 3,
    chart_data: pd.DataFrame | None = None,
    chart_export_path: Path | None = None,
    results_by_type: dict[str, object] | None = None,
    data_type: str | None = None,
) -> None:
    if results_by_type:
        _save_multi_type_scatter(
            results_by_type,
            output_path,
            data_type or target_draft_no,
            target_draft_no,
            show_chart,
            chart_export_path,
        )
        return
    if not show_chart:
        plt.switch_backend("Agg")
    output_path.parent.mkdir(parents=True, exist_ok=True)
    figure = plt.figure(figsize=(11, 9))
    layout = GridSpec(2, 1, figure=figure, height_ratios=(3.8, 1.2), hspace=0.18)
    axis = figure.add_subplot(layout[0])
    table_axis = figure.add_subplot(layout[1])
    table_axis.axis("off")
    points = points.reset_index(drop=True)

    palette = {"PASS": "#2a9d8f", "REVIEW": "#e76f51"}
    labels = points["LABEL_Y"].fillna("").astype(str).str.upper()
    for label, group in points.groupby(labels):
        axis.scatter(
            group["X1"],
            group["X2"],
            s=48,
            alpha=0.84,
            label=label.title() if label else "Unlabeled",
            color=palette.get(label, "#457b9d"),
            edgecolor="white",
            linewidth=0.6,
        )

    target_overlay = axis.scatter(
        [],
        [],
        s=170,
        marker="*",
        color="#f4a261",
        edgecolor="#1d3557",
        linewidth=1.0,
        label="Target",
        zorder=6,
    )
    selected_overlay = axis.scatter(
        [],
        [],
        s=185,
        marker="*",
        color="#ffd166",
        edgecolor="#073b4c",
        linewidth=1.2,
        label="Selected",
        zorder=7,
    )
    neighbor_overlay = axis.scatter(
        [],
        [],
        s=115,
        marker="o",
        color="#ffca3a",
        edgecolor="#111827",
        linewidth=1.3,
        label=f"Nearest {neighbor_count}",
        zorder=6,
    )
    _set_target_overlay(target_overlay, points, target_draft_no)

    axis.axhline(0, color="#7a7f87", linewidth=0.8, linestyle="--")
    axis.axvline(0, color="#7a7f87", linewidth=0.8, linestyle="--")
    axis.set_title("t-SNE Scatter")
    _apply_axis_range_ticks(axis, points)
    axis.grid(True, color="#d8dee9", linewidth=0.7, alpha=0.75)
    axis.legend(loc="best")
    controls_axis = figure.add_axes((0.04, 0.012, 0.92, 0.062), facecolor="#f8fafc")
    controls_axis.set_zorder(90)
    controls_axis.set_xticks([])
    controls_axis.set_yticks([])
    for spine in controls_axis.spines.values():
        spine.set_color("#cbd5e1")
        spine.set_linewidth(0.8)
    _register_click_handler(
        figure,
        axis,
        points,
        selected_overlay,
        neighbor_overlay,
        standardized_matrix,
        neighbor_count,
        table_axis,
        target_draft_no,
    )
    if show_chart and chart_data is not None:
        button_axis = figure.add_axes((0.72, 0.024, 0.20, 0.038))
        export_button = Button(button_axis, "Export Excel", color="#2563eb", hovercolor="#1d4ed8")
        _style_button_axis(button_axis)

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
        widgets = getattr(figure, "_tsne_widgets", [])
        widgets.append(export_button)
        figure._tsne_widgets = widgets
    figure.subplots_adjust(bottom=0.10)
    figure.savefig(output_path, dpi=150, facecolor="white")
    if show_chart:
        print(f"Opening t-SNE chart with matplotlib backend: {plt.get_backend()}")
        plt.show(block=True)
    else:
        plt.close(figure)


def _save_multi_type_scatter(
    results_by_type: dict[str, object],
    output_path: Path,
    initial_type: str,
    target_draft_no: str,
    show_chart: bool,
    chart_export_path: Path | None,
) -> None:
    if not show_chart:
        plt.switch_backend("Agg")
    output_path.parent.mkdir(parents=True, exist_ok=True)
    figure = plt.figure(figsize=(12, 9.5), facecolor="white")
    layout = GridSpec(2, 1, figure=figure, height_ratios=(3.8, 1.2), hspace=0.18)
    axis = figure.add_subplot(layout[0])
    table_axis = figure.add_subplot(layout[1])
    table_axis.axis("off")
    types = list(results_by_type)
    active_type = initial_type if initial_type in results_by_type else types[0]
    state = {"data_type": active_type, "target": target_draft_no}
    table_cells: dict[int, int] = {}

    controls_axis = figure.add_axes((0.04, 0.885, 0.92, 0.085), facecolor="#f8fafc")
    controls_axis.set_xticks([])
    controls_axis.set_yticks([])
    for spine in controls_axis.spines.values():
        spine.set_color("#cbd5e1")
        spine.set_linewidth(0.8)
    type_axis = figure.add_axes((0.07, 0.895, 0.23, 0.065), facecolor="#f8fafc")
    radio = RadioButtons(type_axis, types, active=types.index(active_type), activecolor="#2563eb")
    type_axis.set_title("Data Type", loc="left", fontsize=9, color="#334155", pad=3)
    for label in radio.labels:
        label.set_fontsize(9)
        label.set_color("#334155")

    input_axis = figure.add_axes((0.38, 0.905, 0.23, 0.04), facecolor="white")
    draft_box = TextBox(input_axis, "Draft No ", initial=target_draft_no, color="white", hovercolor="#eff6ff")
    draft_box.set_active(True)
    search_axis = figure.add_axes((0.63, 0.905, 0.11, 0.04))
    search_button = Button(search_axis, "Search", color="#0f766e", hovercolor="#115e59")
    export_axis = figure.add_axes((0.77, 0.905, 0.15, 0.04))
    export_button = Button(export_axis, "Export Excel", color="#2563eb", hovercolor="#1d4ed8")
    for widget_axis in (input_axis, search_axis, export_axis):
        widget_axis.set_zorder(100)
        widget_axis.set_xticks([])
        widget_axis.set_yticks([])
        for spine in widget_axis.spines.values():
            spine.set_color("#cbd5e1")
            spine.set_linewidth(0.8)

    def target_index(points: pd.DataFrame) -> int | None:
        if not state["target"]:
            return None
        matches = points.index[
            points["DRAFT_NO"].astype(str).str.casefold()
            == str(state["target"]).strip().casefold()
        ]
        return int(matches[0]) if len(matches) else None

    def draw(current_type: str, selected_index: int | None = None) -> None:
        result = results_by_type[current_type]
        points = result.points.reset_index(drop=True)
        state["data_type"] = current_type
        selected_index = target_index(points) if selected_index is None else selected_index
        if selected_index is None or selected_index < 0 or selected_index >= len(points):
            selected_index = 0

        axis.clear()
        table_axis.clear()
        table_axis.axis("off")
        labels = points["LABEL_Y"].fillna("").astype(str).str.upper()
        palette = {"PASS": "#2a9d8f", "REVIEW": "#e76f51"}
        for label, group in points.groupby(labels):
            axis.scatter(
                group["X1"], group["X2"], s=48, alpha=0.84,
                label=label.title() if label else "Unlabeled",
                color=palette.get(label, "#457b9d"), edgecolor="white", linewidth=0.6,
            )

        distances, indices = result.nearest_neighbors.kneighbors(
            result.standardized_matrix[selected_index : selected_index + 1],
            n_neighbors=min(4, len(points)),
        )
        nearest = [
            (int(index), float(distance))
            for index, distance in zip(indices[0], distances[0])
            if int(index) != selected_index
        ][:3]
        nearest_indices = np.array([index for index, _ in nearest], dtype=int)
        axis.scatter(
            [points.iloc[selected_index]["X1"]], [points.iloc[selected_index]["X2"]],
            s=185, marker="*", color="#ffd166", edgecolor="#073b4c", linewidth=1.2,
            label="Selected", zorder=7,
        )
        if len(nearest_indices):
            axis.scatter(
                points.iloc[nearest_indices]["X1"], points.iloc[nearest_indices]["X2"],
                s=115, marker="o", color="#ffca3a", edgecolor="#111827", linewidth=1.3,
                label="Nearest 3", zorder=6,
            )
        target = target_index(points)
        if target is not None:
            axis.scatter(
                [points.iloc[target]["X1"]], [points.iloc[target]["X2"]],
                s=170, marker="*", color="#f4a261", edgecolor="#1d3557", linewidth=1.0,
                label="Target", zorder=6,
            )
        axis.axhline(0, color="#7a7f87", linewidth=0.8, linestyle="--")
        axis.axvline(0, color="#7a7f87", linewidth=0.8, linestyle="--")
        axis.set_title(f"t-SNE Scatter · {current_type}")
        _apply_axis_range_ticks(axis, points)
        axis.grid(True, color="#d8dee9", linewidth=0.7, alpha=0.75)
        axis.legend(loc="best")

        selected = points.iloc[selected_index]
        rows = [[
            "Selected", selected["DRAFT_NO"], selected["PARAM_TYP"], selected["LABEL_Y"],
            f"{selected['X1']:.4f}", f"{selected['X2']:.4f}", "0.0000",
        ]]
        rows.extend([
            [
                f"Nearest {rank}", points.iloc[index]["DRAFT_NO"], points.iloc[index]["PARAM_TYP"],
                points.iloc[index]["LABEL_Y"], f"{points.iloc[index]['X1']:.4f}",
                f"{points.iloc[index]['X2']:.4f}", f"{distance:.4f}",
            ]
            for rank, (index, distance) in enumerate(nearest, 1)
        ])
        table = table_axis.table(
            cellText=rows,
            colLabels=["Relation", "DRAFT_NO", "PARAM_TYP", "LABEL(Y)", "X1", "X2", "Distance"],
            loc="center", cellLoc="left",
            colWidths=[0.13, 0.18, 0.13, 0.18, 0.12, 0.12, 0.14],
        )
        table.auto_set_font_size(False)
        table.set_fontsize(8.5)
        table.scale(1, 1.35)
        for (row, column), cell in table.get_celld().items():
            cell.set_edgecolor("#cbd5e1")
            if row == 0:
                cell.set_facecolor("#e2e8f0")
                cell.get_text().set_weight("bold")
            elif row == 1:
                cell.set_facecolor("#fef3c7")
        table_cells.clear()
        for row, index in enumerate([selected_index, *nearest_indices], 1):
            for column in range(7):
                cell = table.get_celld()[(row, column)]
                cell.set_picker(True)
                table_cells[id(cell)] = int(index)

        lines = [f"Selected: {selected['DRAFT_NO']}", "Nearest:"]
        lines.extend(
            f"{rank}. {points.iloc[index]['DRAFT_NO']}  d={distance:.4f}"
            for rank, (index, distance) in enumerate(nearest, 1)
        )
        annotation = axis.annotate(
            "\n".join(lines),
            xy=(float(selected["X1"]), float(selected["X2"])),
            xytext=(12, 12), textcoords="offset points",
            bbox={"boxstyle": "round,pad=0.35", "fc": "white", "ec": "#111827", "alpha": 0.92},
            fontsize=9,
        )
        figure.canvas.draw_idle()

    def submit(value: str) -> None:
        points = results_by_type[state["data_type"]].points.reset_index(drop=True)
        matches = points.index[
            points["DRAFT_NO"].astype(str).str.casefold() == value.strip().casefold()
        ]
        if len(matches):
            state["target"] = value.strip()
            draw(state["data_type"], int(matches[0]))
            return
        axis.set_title(f"t-SNE Scatter · {state['data_type']} · Draft No not found: {value.strip()}")
        figure.canvas.draw_idle()

    def on_click(event: object) -> None:
        if getattr(event, "inaxes", None) is not axis:
            return
        if getattr(event, "xdata", None) is None or getattr(event, "ydata", None) is None:
            return
        points = results_by_type[state["data_type"]].points.reset_index(drop=True)
        coordinates = points[["X1", "X2"]].to_numpy(dtype=float)
        selected = int(np.argmin(np.sum((coordinates - np.array([event.xdata, event.ydata])) ** 2, axis=1)))
        draw(state["data_type"], selected)

    def on_pick(event: object) -> None:
        selected = table_cells.get(id(getattr(event, "artist", None)))
        if selected is not None:
            draw(state["data_type"], selected)

    def export(_event: object) -> None:
        import tkinter as tk
        from tkinter import filedialog

        root = tk.Tk()
        root.withdraw()
        selected_path = filedialog.asksaveasfilename(
            title="Export chart data", defaultextension=".xlsx",
            filetypes=[("Excel workbook", "*.xlsx")],
            initialfile=(chart_export_path.name if chart_export_path else "chart_data.xlsx"),
        )
        root.destroy()
        if selected_path:
            from .export import export_chart_data
            export_chart_data(results_by_type[state["data_type"]].points, Path(selected_path))
            print(f"Chart Excel: {selected_path}")

    radio.on_clicked(lambda label: draw(label))
    draft_box.on_submit(submit)
    search_button.on_clicked(lambda _event: submit(draft_box.text))
    export_button.on_clicked(export)
    figure.canvas.mpl_connect("button_press_event", on_click)
    figure.canvas.mpl_connect("pick_event", on_pick)
    figure._tsne_widgets = [radio, draft_box, search_button, export_button]
    draw(active_type)
    figure.subplots_adjust(top=0.86, bottom=0.08)
    figure.savefig(output_path, dpi=150, facecolor="white")
    if show_chart:
        print(f"Opening t-SNE chart with matplotlib backend: {plt.get_backend()}")
        plt.show(block=True)
    else:
        plt.close(figure)


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


def _set_target_overlay(
    target_overlay: PathCollection,
    points: pd.DataFrame,
    target_draft_no: str,
) -> None:
    if not target_draft_no:
        target_overlay.set_offsets(np.empty((0, 2)))
        return
    target = points[
        points["DRAFT_NO"].astype(str).str.casefold() == target_draft_no.casefold()
    ]
    if target.empty:
        target_overlay.set_offsets(np.empty((0, 2)))
        return
    target_overlay.set_offsets(target[["X1", "X2"]].to_numpy())


def _register_click_handler(
    figure: plt.Figure,
    axis: plt.Axes,
    points: pd.DataFrame,
    selected_overlay: PathCollection,
    neighbor_overlay: PathCollection,
    standardized_matrix: np.ndarray | None,
    neighbor_count: int,
    table_axis: plt.Axes,
    target_draft_no: str = "",
) -> None:
    coordinates = points[["X1", "X2"]].to_numpy(dtype=float)
    # WinForms uses the rendered X1/X2 coordinates for chart selection.
    # Keep the click behavior consistent even though the batch KNN CSV uses
    # the standardized feature space.
    distance_matrix = coordinates
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
        for cell in table.get_celld().values():
            cell.set_edgecolor("#cbd5e1")
        table_cells.clear()
        for row_index, source_index in enumerate([selected_index, *neighbor_indices], start=1):
            for column_index in range(7):
                cell = table.get_celld()[(row_index, column_index)]
                cell.set_picker(True)
                table_cells[id(cell)] = source_index

    def select_index(selected_index: int, show_annotation: bool = True) -> None:
        neighbor_indices, distances = _find_nearest_indices(
            distance_matrix, selected_index, max(1, neighbor_count)
        )
        selected_overlay.set_offsets(coordinates[[selected_index]])
        neighbor_overlay.set_offsets(coordinates[neighbor_indices])
        update_grid(selected_index, neighbor_indices, distances)
        selected = points.iloc[selected_index]
        if show_annotation:
            lines = [f"Selected: {selected['DRAFT_NO']}", "Nearest:"]
            for rank, (source_index, distance) in enumerate(zip(neighbor_indices, distances), start=1):
                lines.append(f"{rank}. {points.iloc[source_index]['DRAFT_NO']}  d={distance:.4f}")
            annotation.xy = (float(selected["X1"]), float(selected["X2"]))
            annotation.set_text("\n".join(lines))
            annotation.set_visible(True)
        figure.canvas.draw_idle()

    initial_neighbors, initial_distances = _find_nearest_indices(
        distance_matrix, 0, max(1, neighbor_count)
    )
    update_grid(0, initial_neighbors, initial_distances)

    def on_click(event: object) -> None:
        if getattr(event, "inaxes", None) is not axis:
            return
        xdata = getattr(event, "xdata", None)
        ydata = getattr(event, "ydata", None)
        if xdata is None or ydata is None:
            return

        clicked_index = int(
            np.argmin(np.sum((coordinates - np.array([xdata, ydata])) ** 2, axis=1))
        )
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
            return
        annotation.set_text(f"Draft Number not found: {value.strip()}")
        annotation.set_visible(bool(value.strip()))
        figure.canvas.draw_idle()

    figure.canvas.mpl_connect("button_press_event", on_click)
    figure.canvas.mpl_connect("pick_event", on_table_pick)
    input_axis = figure.add_axes((0.10, 0.024, 0.31, 0.038))
    input_axis.set_zorder(100)
    draft_box = TextBox(input_axis, "Draft No ", initial=target_draft_no or "", color="white", hovercolor="#eff6ff")
    input_axis.set_facecolor("white")
    for spine in input_axis.spines.values():
        spine.set_color("#94a3b8")
    draft_box.on_submit(on_draft_submit)
    draft_box.set_active(True)
    search_axis = figure.add_axes((0.43, 0.024, 0.12, 0.038))
    search_axis.set_zorder(100)
    search_button = Button(search_axis, "Search", color="#0f766e", hovercolor="#115e59")
    _style_button_axis(search_axis)

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
    widgets = getattr(figure, "_tsne_widgets", [])
    widgets.append(draft_box)
    widgets.append(search_button)
    figure._tsne_widgets = widgets


def _style_button_axis(axis: plt.Axes) -> None:
    axis.set_xticks([])
    axis.set_yticks([])
    for spine in axis.spines.values():
        spine.set_color("#cbd5e1")
        spine.set_linewidth(0.8)


def _find_nearest_indices(
    matrix: np.ndarray,
    target_index: int,
    count: int,
) -> tuple[np.ndarray, np.ndarray]:
    differences = matrix - matrix[target_index]
    distances = np.sqrt(np.sum(differences * differences, axis=1))
    distances[target_index] = np.inf
    order = np.argsort(distances)[: min(count, len(distances) - 1)]
    return order, distances[order]
