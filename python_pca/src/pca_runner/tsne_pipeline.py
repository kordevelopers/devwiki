from __future__ import annotations

from dataclasses import dataclass
import hashlib
import math
import platform

import numpy as np
import pandas as pd
import scipy
import sklearn
from sklearn.manifold import TSNE
from sklearn.neighbors import NearestNeighbors
from sklearn.preprocessing import StandardScaler

from .pca_pipeline import prepare_features


TSNE_COMPONENTS = 2
TSNE_MAX_ITERATIONS = 1000
TSNE_RANDOM_STATE = 42
TSNE_INITIALIZATION = "pca"
TSNE_LEARNING_RATE = "auto"
TSNE_METRIC = "euclidean"
TSNE_METHOD = "barnes_hut"
TSNE_ANGLE = 0.5
TSNE_EARLY_EXAGGERATION = 12.0
TSNE_ITERATIONS_WITHOUT_PROGRESS = 300
TSNE_MINIMUM_GRADIENT_NORM = 1e-7
KNN_NEIGHBOR_COUNT = 15
DEFAULT_VARIANCE_THRESHOLD = 1e-10


@dataclass(frozen=True)
class NeighborRow:
    rank: int
    similar_draft: str
    distance: float


@dataclass(frozen=True)
class TSNEResult:
    points: pd.DataFrame
    features: list[str]
    excluded_features: list[str]
    feature_audit: pd.DataFrame
    surviving_population: pd.DataFrame
    diagnostic: dict[str, object]
    standardized_matrix: np.ndarray
    scaler: StandardScaler
    tsne: TSNE
    nearest_neighbors: NearestNeighbors


def resolve_perplexity(sample_count: int) -> float:
    if sample_count < 3:
        raise ValueError("t-SNE requires at least 3 rows.")
    return float(min(30, max(5, sample_count - 1) // 3))


def run_tsne(
    feature_frame: pd.DataFrame,
    variance_threshold: float = DEFAULT_VARIANCE_THRESHOLD,
) -> TSNEResult:
    if len(feature_frame) < 3:
        raise ValueError("t-SNE requires at least 3 rows.")
    if not math.isfinite(variance_threshold) or variance_threshold < 0.0:
        raise ValueError("variance_threshold must be finite and no less than 0.")

    _validate_feature_names(feature_frame)
    prepared = prepare_features(feature_frame, variance_threshold, analysis_name="t-SNE")
    standardized = prepared.standardized_matrix
    if not np.isfinite(standardized).all():
        raise ValueError("Standardization produced a non-finite feature value.")

    perplexity = resolve_perplexity(len(feature_frame))
    tsne = TSNE(
        n_components=TSNE_COMPONENTS,
        perplexity=perplexity,
        max_iter=TSNE_MAX_ITERATIONS,
        random_state=TSNE_RANDOM_STATE,
        init=TSNE_INITIALIZATION,
        learning_rate=TSNE_LEARNING_RATE,
        metric=TSNE_METRIC,
        method=TSNE_METHOD,
        angle=TSNE_ANGLE,
        early_exaggeration=TSNE_EARLY_EXAGGERATION,
        n_iter_without_progress=TSNE_ITERATIONS_WITHOUT_PROGRESS,
        min_grad_norm=TSNE_MINIMUM_GRADIENT_NORM,
        n_jobs=1,
    )
    coordinates = tsne.fit_transform(standardized)
    if not np.isfinite(coordinates).all():
        raise ValueError("t-SNE produced a non-finite coordinate.")
    if not math.isfinite(float(tsne.kl_divergence_)):
        raise ValueError("t-SNE produced a non-finite KL divergence.")

    knn_neighbor_count = min(KNN_NEIGHBOR_COUNT, len(feature_frame) - 1)
    nearest_neighbors = NearestNeighbors(
        n_neighbors=knn_neighbor_count,
        metric=TSNE_METRIC,
        algorithm="auto",
        n_jobs=1,
    )
    nearest_neighbors.fit(standardized)

    points = pd.DataFrame(
        {
            "DRAFT_NO": feature_frame["DRAFT_NO"].astype(str).values,
            "PARAM_TYP": _metadata_values(feature_frame, "PARAM_TYP"),
            "LABEL_Y": feature_frame["LABEL_Y"].astype(str).values,
            "RSLT_CD": _metadata_values(feature_frame, "RSLT_CD"),
            "X1": coordinates[:, 0],
            "X2": coordinates[:, 1],
        }
    )
    surviving_population = pd.concat(
        [
            points[["DRAFT_NO", "PARAM_TYP", "LABEL_Y", "RSLT_CD", "X1", "X2"]],
            prepared.imputed_frame,
        ],
        axis=1,
    )
    diagnostic = _build_diagnostic(
        points=points,
        included_features=prepared.features,
        excluded_count=len(prepared.excluded_features),
        standardized=standardized,
        tsne=tsne,
        perplexity=perplexity,
        nearest_neighbors=nearest_neighbors,
        knn_neighbor_count=knn_neighbor_count,
        imputed_value_count=prepared.imputed_value_count,
        variance_threshold=variance_threshold,
    )
    return TSNEResult(
        points=points,
        features=prepared.features,
        excluded_features=prepared.excluded_features,
        feature_audit=prepared.feature_audit,
        surviving_population=surviving_population,
        diagnostic=diagnostic,
        standardized_matrix=standardized,
        scaler=prepared.scaler,
        tsne=tsne,
        nearest_neighbors=nearest_neighbors,
    )


def find_neighbors(
    result: TSNEResult,
    target_draft_no: str,
    count: int = 3,
) -> list[NeighborRow]:
    if count < 1:
        raise ValueError("Neighbor count must be at least 1.")
    draft_numbers = result.points["DRAFT_NO"].astype(str).tolist()
    normalized = target_draft_no.strip().casefold()
    try:
        target_index = next(
            index
            for index, draft_no in enumerate(draft_numbers)
            if draft_no.casefold() == normalized
        )
    except StopIteration as exc:
        raise KeyError(f"DRAFT_NO was not found: {target_draft_no}") from exc

    distances, indices = result.nearest_neighbors.kneighbors(
        result.standardized_matrix[target_index : target_index + 1],
        n_neighbors=min(count + 1, len(draft_numbers)),
    )
    rows: list[NeighborRow] = []
    for distance, source_index in zip(distances[0], indices[0]):
        if source_index == target_index:
            continue
        rows.append(
            NeighborRow(
                rank=len(rows) + 1,
                similar_draft=draft_numbers[source_index],
                distance=round(float(distance), 4),
            )
        )
        if len(rows) >= count:
            break
    return rows


def _validate_feature_names(feature_frame: pd.DataFrame) -> None:
    metadata_columns = {"DRAFT_NO", "PARAM_TYP", "LABEL_Y", "RSLT_CD"}
    canonical_names: dict[str, str] = {}
    for column in feature_frame.columns:
        if column in metadata_columns:
            continue
        normalized = column.casefold()
        previous = canonical_names.get(normalized)
        if previous is not None:
            raise ValueError(
                "Feature names must be unique without regard to case: "
                f"{previous}, {column}"
            )
        canonical_names[normalized] = column


def _metadata_values(frame: pd.DataFrame, column: str) -> object:
    if column in frame.columns:
        return frame[column].fillna("").astype(str).values
    return ""


def _build_diagnostic(
    points: pd.DataFrame,
    included_features: list[str],
    excluded_count: int,
    standardized: np.ndarray,
    tsne: TSNE,
    perplexity: float,
    nearest_neighbors: NearestNeighbors,
    knn_neighbor_count: int,
    imputed_value_count: int,
    variance_threshold: float,
) -> dict[str, object]:
    means = np.mean(standardized, axis=0)
    standard_deviations = np.std(standardized, axis=0)
    coordinates = points[["X1", "X2"]].to_numpy(dtype=np.float64)
    n_iter_attribute = int(tsne.n_iter_)
    return {
        "ProjectionMethod": "TSNE",
        "PreprocessingMethod": "PCA_SHARED",
        "RowCount": len(points),
        "FeatureCount": len(included_features),
        "ExcludedFeatureCount": excluded_count,
        "FeatureNames": included_features,
        "VarianceThreshold": variance_threshold,
        "ImputedValueCount": imputed_value_count,
        "Components": TSNE_COMPONENTS,
        "Perplexity": perplexity,
        "BarnesHutNeighborCount": min(len(points) - 1, int(3.0 * perplexity + 1.0)),
        "RequestedMaxIterations": TSNE_MAX_ITERATIONS,
        "IterationAttribute": n_iter_attribute,
        "ExecutedIterations": n_iter_attribute + 1,
        "RandomState": TSNE_RANDOM_STATE,
        "Initialization": TSNE_INITIALIZATION,
        "LearningRate": TSNE_LEARNING_RATE,
        "EffectiveLearningRate": float(tsne.learning_rate_),
        "Metric": TSNE_METRIC,
        "Method": TSNE_METHOD,
        "BarnesHutAngle": TSNE_ANGLE,
        "EarlyExaggeration": TSNE_EARLY_EXAGGERATION,
        "IterationsWithoutProgress": TSNE_ITERATIONS_WITHOUT_PROGRESS,
        "MinimumGradientNorm": TSNE_MINIMUM_GRADIENT_NORM,
        "KullbackLeiblerDivergence": float(tsne.kl_divergence_),
        "KnnRequestedNeighborCount": KNN_NEIGHBOR_COUNT,
        "KnnNeighborCount": knn_neighbor_count,
        "KnnMetric": TSNE_METRIC,
        "KnnRequestedAlgorithm": "auto",
        "KnnActualAlgorithm": str(getattr(nearest_neighbors, "_fit_method", "unknown")),
        "ShapeCode": _resolve_shape_code(len(points), len(included_features)),
        "MaximumAbsoluteStandardizedMean": float(np.max(np.abs(means))),
        "MaximumStandardDeviationError": float(
            np.max(np.abs(standard_deviations - 1.0))
        ),
        "StandardizedMatrixSha256": _matrix_sha256(standardized),
        "DraftOrderSha256": hashlib.sha256(
            "\n".join(points["DRAFT_NO"].astype(str)).encode("utf-8")
        ).hexdigest(),
        "CoordinateSha256": _matrix_sha256(coordinates),
        "CoordinateSummary": {
            "X1Min": float(np.min(coordinates[:, 0])),
            "X1Max": float(np.max(coordinates[:, 0])),
            "X1Mean": float(np.mean(coordinates[:, 0])),
            "X1StdDev": float(np.std(coordinates[:, 0])),
            "X2Min": float(np.min(coordinates[:, 1])),
            "X2Max": float(np.max(coordinates[:, 1])),
            "X2Mean": float(np.mean(coordinates[:, 1])),
            "X2StdDev": float(np.std(coordinates[:, 1])),
        },
        "Runtime": {
            "Python": platform.python_version(),
            "ScikitLearn": sklearn.__version__,
            "NumPy": np.__version__,
            "SciPy": scipy.__version__,
            "Pandas": pd.__version__,
        },
    }


def _resolve_shape_code(row_count: int, feature_count: int) -> str:
    if row_count < 3:
        return "ROWS_LT3"
    if row_count < 30:
        return "ROWS_LOW"
    if feature_count < 2:
        return "FEATURE_LT2"
    if feature_count <= 5:
        return "FEATURE_LOW"
    return "OK"


def _matrix_sha256(matrix: np.ndarray) -> str:
    contiguous = np.ascontiguousarray(matrix, dtype=np.float64)
    return hashlib.sha256(contiguous.tobytes()).hexdigest()
