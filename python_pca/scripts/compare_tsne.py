from __future__ import annotations

import argparse
import json
from pathlib import Path

import matplotlib

matplotlib.use("Agg")

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from sklearn.manifold import TSNE
from sklearn.neighbors import NearestNeighbors
from sklearn.preprocessing import StandardScaler


METADATA_COLUMNS = {
    "DRAFT_NO",
    "PARAM_TYP",
    "LABEL_Y",
    "RSLT_CD",
    "AI_RSLT_VAL",
    "ENGR_RSLT_VAL",
    "X1",
    "X2",
}
PALETTE = ["#2563eb", "#e76f51", "#2a9d8f", "#8b5cf6", "#d97706"]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Run the sklearn t-SNE reference and optionally compare Accord coordinates."
    )
    parser.add_argument("--input", required=True, type=Path, help="Feature CSV to analyze.")
    parser.add_argument(
        "--accord-points",
        type=Path,
        help="Optional CSV containing DRAFT_NO, X1, and X2 from Accord.NET.",
    )
    parser.add_argument("--output", required=True, type=Path, help="Comparison PNG path.")
    parser.add_argument(
        "--python-points",
        type=Path,
        help="Optional output CSV path for sklearn coordinates.",
    )
    parser.add_argument(
        "--metrics",
        type=Path,
        help="Optional output JSON path for comparison metrics.",
    )
    parser.add_argument(
        "--dataset-label",
        default="Input feature data",
        help="Factual dataset label shown in the figure title.",
    )
    return parser.parse_args()


def prepare_input(frame: pd.DataFrame) -> tuple[pd.DataFrame, np.ndarray, list[str]]:
    candidates = [column for column in frame.columns if column.upper() not in METADATA_COLUMNS]
    numeric = frame[candidates].apply(pd.to_numeric, errors="coerce")
    numeric = numeric.replace([np.inf, -np.inf], np.nan)
    coverage = numeric.notna().mean(axis=0)
    included = [column for column in candidates if coverage[column] >= 0.90]
    if not included:
        raise ValueError("No numeric features meet the 90% coverage requirement.")

    numeric = numeric[included]
    numeric = numeric.fillna(numeric.mean(axis=0))
    variances = numeric.var(axis=0, ddof=0)
    included = [column for column in included if variances[column] > 1e-10]
    if len(included) < 2:
        raise ValueError("At least two finite, non-constant numeric features are required.")

    matrix = numeric[included].to_numpy(dtype=np.float64, copy=True)
    standardized = StandardScaler().fit_transform(matrix)
    return frame.reset_index(drop=True), standardized, included


def resolve_perplexity(sample_count: int) -> float:
    if sample_count < 3:
        raise ValueError("t-SNE requires at least three rows.")
    return float(min(30, max(5, sample_count - 1) // 3))


def resolve_accord_perplexity(sample_count: int, requested: float) -> float:
    boundary = (sample_count - 1.0) / 3.0 - 1e-6
    return max(min(1.0, boundary), min(requested, boundary))


def run_sklearn_tsne(standardized: np.ndarray, perplexity: float) -> tuple[np.ndarray, TSNE]:
    model = TSNE(
        n_components=2,
        perplexity=perplexity,
        max_iter=1000,
        random_state=42,
        init="pca",
        learning_rate="auto",
        metric="euclidean",
        method="barnes_hut",
        angle=0.5,
    )
    return model.fit_transform(standardized), model


def verify_reference_knn(standardized: np.ndarray) -> int:
    neighbor_count = min(15, len(standardized) - 1)
    model = NearestNeighbors(n_neighbors=neighbor_count, metric="euclidean")
    model.fit(standardized)
    distances, _ = model.kneighbors(X=None)
    if not np.isfinite(distances).all():
        raise ValueError("The Euclidean KNN result contains a non-finite distance.")
    return neighbor_count


def read_accord_points(path: Path, identifiers: pd.Series) -> np.ndarray:
    frame = pd.read_csv(path)
    required = {"DRAFT_NO", "X1", "X2"}
    missing = required.difference(frame.columns)
    if missing:
        raise ValueError("Accord points are missing columns: " + ", ".join(sorted(missing)))
    if frame["DRAFT_NO"].astype(str).duplicated().any():
        raise ValueError("Accord points contain duplicate DRAFT_NO values.")

    indexed = frame.assign(DRAFT_NO=frame["DRAFT_NO"].astype(str)).set_index("DRAFT_NO")
    ordered_ids = identifiers.astype(str).tolist()
    missing_ids = [identifier for identifier in ordered_ids if identifier not in indexed.index]
    if missing_ids:
        raise ValueError("Accord points do not contain all input DRAFT_NO values.")
    return indexed.loc[ordered_ids, ["X1", "X2"]].to_numpy(dtype=np.float64)


def normalize_coordinates(coordinates: np.ndarray) -> np.ndarray:
    centered = coordinates - coordinates.mean(axis=0, keepdims=True)
    norm = float(np.linalg.norm(centered))
    if norm <= 0.0:
        raise ValueError("Coordinate matrix has no measurable spread.")
    return centered / norm


def align_accord_to_python(
    accord: np.ndarray, python: np.ndarray
) -> tuple[np.ndarray, float, bool, float]:
    accord_normalized = normalize_coordinates(accord)
    python_normalized = normalize_coordinates(python)
    left, _, right_transposed = np.linalg.svd(accord_normalized.T @ python_normalized)
    transform = left @ right_transposed
    aligned = accord_normalized @ transform
    rmse = float(np.sqrt(np.mean((aligned - python_normalized) ** 2)))
    rotation_degrees = float(np.degrees(np.arctan2(transform[0, 1], transform[0, 0])))
    return aligned, rmse, bool(np.linalg.det(transform) < 0.0), rotation_degrees


def pairwise_distance_correlation(first: np.ndarray, second: np.ndarray) -> float:
    rows, columns = np.triu_indices(len(first), k=1)
    first_distances = np.linalg.norm(first[rows] - first[columns], axis=1)
    second_distances = np.linalg.norm(second[rows] - second[columns], axis=1)
    return float(np.corrcoef(first_distances, second_distances)[0, 1])


def mean_neighbor_overlap(first: np.ndarray, second: np.ndarray, count: int = 15) -> float:
    neighbor_count = min(count, len(first) - 1)
    first_distances = np.linalg.norm(first[:, None, :] - first[None, :, :], axis=2)
    second_distances = np.linalg.norm(second[:, None, :] - second[None, :, :], axis=2)
    np.fill_diagonal(first_distances, np.inf)
    np.fill_diagonal(second_distances, np.inf)
    first_neighbors = np.argsort(first_distances, axis=1)[:, :neighbor_count]
    second_neighbors = np.argsort(second_distances, axis=1)[:, :neighbor_count]
    overlaps = [
        len(set(first_neighbors[row]).intersection(second_neighbors[row])) / neighbor_count
        for row in range(len(first))
    ]
    return float(np.mean(overlaps))


def resolve_labels(frame: pd.DataFrame) -> pd.Series:
    if "LABEL_Y" not in frame.columns:
        return pd.Series(["All"] * len(frame), dtype=str)
    labels = frame["LABEL_Y"].fillna("Unlabeled").astype(str).str.strip()
    return labels.replace("", "Unlabeled")


def draw_scatter(axis: plt.Axes, coordinates: np.ndarray, labels: pd.Series, title: str) -> None:
    categories = sorted(labels.unique(), key=str.casefold)
    for index, category in enumerate(categories):
        mask = labels == category
        axis.scatter(
            coordinates[mask, 0],
            coordinates[mask, 1],
            s=42,
            alpha=0.86,
            color=PALETTE[index % len(PALETTE)],
            edgecolor="white",
            linewidth=0.55,
            label=category,
        )
    axis.set_title(title, color="#111827", fontsize=12, weight="bold")
    axis.set_xlabel("t-SNE 1")
    axis.set_ylabel("t-SNE 2")
    axis.grid(True, color="#e5e7eb", linewidth=0.7)
    axis.set_aspect("equal", adjustable="datalim")


def draw_alignment_overlay(
    axis: plt.Axes,
    aligned_accord: np.ndarray,
    python_coordinates: np.ndarray,
    labels: pd.Series,
) -> None:
    normalized_python = normalize_coordinates(python_coordinates)
    draw_scatter(
        axis,
        aligned_accord,
        labels,
        "Normalized overlay (Accord dots / Python rings)",
    )
    for row in range(len(aligned_accord)):
        axis.plot(
            [aligned_accord[row, 0], normalized_python[row, 0]],
            [aligned_accord[row, 1], normalized_python[row, 1]],
            color="#9ca3af",
            alpha=0.35,
            linewidth=0.65,
            zorder=1,
        )
    axis.scatter(
        normalized_python[:, 0],
        normalized_python[:, 1],
        s=68,
        facecolors="none",
        edgecolors="#111827",
        linewidths=0.8,
        zorder=4,
    )


def save_figure(
    output_path: Path,
    dataset_label: str,
    labels: pd.Series,
    python_coordinates: np.ndarray,
    accord_coordinates: np.ndarray | None,
    aligned_coordinates: np.ndarray | None,
    metrics: dict[str, object],
) -> None:
    panel_count = 3 if accord_coordinates is not None else 1
    figure, axes = plt.subplots(1, panel_count, figsize=(18 if panel_count == 3 else 7, 6.5))
    axes_array = np.atleast_1d(axes)
    draw_scatter(axes_array[0], python_coordinates, labels, "sklearn t-SNE (Python)")
    if accord_coordinates is not None and aligned_coordinates is not None:
        draw_scatter(axes_array[1], accord_coordinates, labels, "Accord.NET t-SNE (raw)")
        draw_alignment_overlay(
            axes_array[2],
            aligned_coordinates,
            python_coordinates,
            labels,
        )

    handles, legend_labels = axes_array[0].get_legend_handles_labels()
    figure.legend(handles, legend_labels, loc="lower center", ncol=max(1, len(legend_labels)))
    subtitle = (
        f"Rows {metrics['row_count']} | Features {metrics['feature_count']} | "
        f"Perplexity Python {metrics['python_perplexity']:.6g} / Accord "
        f"{metrics['accord_perplexity']:.9g} | max_iter 1000 | "
        f"random_state 42 | init PCA | learning_rate auto="
        f"{metrics['python_effective_learning_rate']:.6g} | "
        f"n_iter_ {metrics['python_iterations']} | KNN 15 Euclidean"
    )
    if accord_coordinates is not None:
        subtitle += (
            f"\nProcrustes RMSE {metrics['procrustes_rmse']:.4f} | "
            f"Pairwise-distance Pearson {metrics['pairwise_distance_correlation']:.4f} | "
            f"15-NN overlap {metrics['neighbor_overlap_15']:.1%} | "
            f"Rotation {metrics['rotation_degrees']:.1f} deg | "
            f"Reflection needed {metrics['reflection_needed']}"
        )
    figure.suptitle(f"t-SNE implementation comparison\n{dataset_label}\n{subtitle}", fontsize=12)
    figure.patch.set_facecolor("#ffffff")
    figure.tight_layout(rect=(0.02, 0.10, 0.98, 0.82))
    output_path.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(output_path, dpi=180, facecolor="white")
    plt.close(figure)


def main() -> None:
    args = parse_args()
    source = pd.read_csv(args.input)
    frame, standardized, features = prepare_input(source)
    if "DRAFT_NO" not in frame.columns:
        frame.insert(0, "DRAFT_NO", [str(index + 1) for index in range(len(frame))])

    perplexity = resolve_perplexity(len(frame))
    accord_perplexity = resolve_accord_perplexity(len(frame), perplexity)
    python_coordinates, python_model = run_sklearn_tsne(standardized, perplexity)
    knn_neighbor_count = verify_reference_knn(standardized)
    labels = resolve_labels(frame)
    python_points = pd.DataFrame(
        {
            "DRAFT_NO": frame["DRAFT_NO"].astype(str),
            "LABEL_Y": labels,
            "X1": python_coordinates[:, 0],
            "X2": python_coordinates[:, 1],
        }
    )

    accord_coordinates = None
    aligned_coordinates = None
    metrics: dict[str, object] = {
        "row_count": len(frame),
        "feature_count": len(features),
        "python_perplexity": perplexity,
        "accord_perplexity": accord_perplexity,
        "python_barnes_hut_neighbors": min(len(frame) - 1, int(3.0 * perplexity + 1)),
        "accord_barnes_hut_neighbors": int(3.0 * accord_perplexity),
        "sklearn_version": __import__("sklearn").__version__,
        "python_effective_learning_rate": float(python_model.learning_rate_),
        "python_iterations": int(python_model.n_iter_),
        "python_kl_divergence": float(python_model.kl_divergence_),
        "knn_neighbor_count": knn_neighbor_count,
        "knn_metric": "euclidean",
    }
    if args.accord_points:
        accord_coordinates = read_accord_points(args.accord_points, frame["DRAFT_NO"])
        aligned_coordinates, rmse, reflected, rotation_degrees = align_accord_to_python(
            accord_coordinates, python_coordinates
        )
        metrics.update(
            {
                "procrustes_rmse": rmse,
                "pairwise_distance_correlation": pairwise_distance_correlation(
                    accord_coordinates, python_coordinates
                ),
                "neighbor_overlap_15": mean_neighbor_overlap(
                    accord_coordinates, python_coordinates, 15
                ),
                "reflection_needed": reflected,
                "rotation_degrees": rotation_degrees,
            }
        )

    save_figure(
        args.output,
        args.dataset_label,
        labels,
        python_coordinates,
        accord_coordinates,
        aligned_coordinates,
        metrics,
    )
    if args.python_points:
        args.python_points.parent.mkdir(parents=True, exist_ok=True)
        python_points.to_csv(args.python_points, index=False)
    if args.metrics:
        args.metrics.parent.mkdir(parents=True, exist_ok=True)
        args.metrics.write_text(json.dumps(metrics, indent=2), encoding="utf-8")

    print(json.dumps(metrics, indent=2))
    print(f"Saved chart: {args.output.resolve()}")


if __name__ == "__main__":
    main()
