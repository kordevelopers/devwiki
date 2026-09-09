from pathlib import Path
import tkinter as tk
from tkinter import filedialog
import matplotlib
if not matplotlib.get_backend().lower().startswith("agg"):
    try: matplotlib.use("TkAgg")
    except Exception: pass
import matplotlib.pyplot as plt
from matplotlib.gridspec import GridSpec
from matplotlib.widgets import Button, RadioButtons, TextBox
import numpy as np
import pandas as pd
from .export import export_chart_data

def save_chart(results: dict[str, object], source_by_type: dict[str, pd.DataFrame], output_path: Path,
               data_type: str, target_draft_no: str = "", show_chart=True) -> None:
    if not show_chart: plt.switch_backend("Agg")
    output_path.parent.mkdir(parents=True, exist_ok=True)
    fig = plt.figure(figsize=(12, 9.5), facecolor="white")
    layout = GridSpec(2, 1, figure=fig, height_ratios=(3.8, 1.2), hspace=0.18)
    axis, table_axis = fig.add_subplot(layout[0]), fig.add_subplot(layout[1]); table_axis.axis("off")
    state = {"type": data_type, "target": target_draft_no}
    overlays = {}
    controls = fig.add_axes((0.04, 0.885, 0.92, 0.085), facecolor="#f8fafc"); controls.axis("off")
    type_axis = fig.add_axes((0.07, 0.895, 0.23, 0.065), facecolor="#f8fafc")
    radio = RadioButtons(type_axis, list(results), active=list(results).index(data_type) if data_type in results else 0,
                         activecolor="#2563eb")
    type_axis.set_title("Data Type", loc="left", fontsize=9, color="#334155", pad=3)
    input_axis = fig.add_axes((0.38, 0.905, 0.23, 0.04)); draft_box = TextBox(input_axis, "Draft No ", initial=target_draft_no)
    search_axis = fig.add_axes((0.63, 0.905, 0.11, 0.04)); search_button = Button(search_axis, "Search", color="#0f766e", hovercolor="#115e59")
    export_axis = fig.add_axes((0.77, 0.905, 0.15, 0.04)); export_button = Button(export_axis, "Export Excel", color="#2563eb", hovercolor="#1d4ed8")
    for ax in (input_axis, search_axis, export_axis):
        ax.set_zorder(100)
        for spine in ax.spines.values(): spine.set_color("#cbd5e1")
    table_cells = {}

    def draw(current_type, selected_index=0):
        result = results[current_type]; points = result.points; state["type"] = current_type
        axis.clear(); table_axis.clear(); table_axis.axis("off")
        for label, group in points.groupby(points["LABEL_Y"].str.upper()):
            axis.scatter(group.X1, group.X2, s=48, alpha=.84, label=label.title() or "Unlabeled",
                         color={"PASS":"#2a9d8f", "REVIEW":"#e76f51"}.get(label, "#457b9d"), edgecolor="white", linewidth=.6)
        axis.axhline(0, color="#94a3b8", ls="--", lw=.8); axis.axvline(0, color="#94a3b8", ls="--", lw=.8)
        axis.set_title(f"UMAP Scatter · {current_type}"); axis.grid(True, color="#d8dee9", lw=.7, alpha=.75); axis.legend(loc="best")
        selected = points.iloc[selected_index]; neighbor_rows = result.nearest_neighbors.kneighbors(result.standardized_matrix[selected_index].reshape(1,-1), n_neighbors=min(4,len(points)))
        indices = [int(i) for i in neighbor_rows[1][0] if int(i) != selected_index][:3]
        axis.scatter([selected.X1], [selected.X2], s=185, marker="*", color="#ffd166", edgecolor="#073b4c", zorder=7)
        axis.scatter(points.iloc[indices].X1, points.iloc[indices].X2, s=115, color="#ffca3a", edgecolor="#111827", zorder=6, label="Nearest 3")
        rows = [["Selected", selected.DRAFT_NO, selected.PARAM_TYP, selected.LABEL_Y, f"{selected.X1:.4f}", f"{selected.X2:.4f}", "0.0000"]]
        rows += [[f"Nearest {n}", points.iloc[i].DRAFT_NO, points.iloc[i].PARAM_TYP, points.iloc[i].LABEL_Y, f"{points.iloc[i].X1:.4f}", f"{points.iloc[i].X2:.4f}", f"{d:.4f}"] for n,(i,d) in enumerate(zip(indices, neighbor_rows[0][0][1:]),1)]
        table = table_axis.table(cellText=rows, colLabels=["Relation","DRAFT_NO","PARAM_TYP","LABEL(Y)","X1","X2","Distance"], loc="center", cellLoc="left", colWidths=[.13,.18,.13,.18,.12,.12,.14])
        table.auto_set_font_size(False); table.set_fontsize(8.5); table.scale(1, 1.35)
        for cell in table.get_celld().values(): cell.set_edgecolor("#cbd5e1")
        table_cells.clear()
        for row, idx in enumerate([selected_index, *indices], 1):
            for col in range(7):
                cell = table.get_celld()[(row, col)]
                cell.set_picker(True)
                table_cells[id(cell)] = idx
        axis.set_xlim(_bounds(points.X1)); axis.set_ylim(_bounds(points.X2)); fig.canvas.draw_idle()

    def submit(value):
        points = results[state["type"]].points; matches = points.index[points.DRAFT_NO.str.casefold() == value.strip().casefold()]
        if len(matches): draw(state["type"], int(matches[0]))
        else: axis.set_title(f"UMAP Scatter · {state['type']} · Draft No not found: {value.strip()}"); fig.canvas.draw_idle()
    def on_click(event):
        if event.inaxes is not axis or event.xdata is None or event.ydata is None:
            return
        points = results[state["type"]].points
        coordinates = points[["X1", "X2"]].to_numpy(dtype=float)
        selected = int(np.argmin(np.sum((coordinates - np.array([event.xdata, event.ydata])) ** 2, axis=1)))
        draw(state["type"], selected)

    def on_pick(event):
        selected = table_cells.get(id(event.artist))
        if selected is not None:
            draw(state["type"], selected)

    radio.on_clicked(lambda label: draw(label, 0)); draft_box.on_submit(submit); search_button.on_clicked(lambda _: submit(draft_box.text))
    fig.canvas.mpl_connect("button_press_event", on_click)
    fig.canvas.mpl_connect("pick_event", on_pick)
    def export(_):
        root=tk.Tk(); root.withdraw(); path=filedialog.asksaveasfilename(title="Export chart data", defaultextension=".xlsx", filetypes=[("Excel workbook","*.xlsx")], initialfile="umap_chart_data.xlsx"); root.destroy()
        if path: export_chart_data(results[state["type"]].points, Path(path))
    export_button.on_clicked(export); draw(data_type, 0); fig.subplots_adjust(top=.86, bottom=.08)
    fig.savefig(output_path, dpi=150, facecolor="white")
    if show_chart: plt.show(block=True)
    else: plt.close(fig)

def _bounds(series):
    low, high = min(float(series.min()),0), max(float(series.max()),0); pad=max((high-low)*.05, 1e-6); return low-pad, high+pad
