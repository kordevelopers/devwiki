from __future__ import annotations

from dataclasses import dataclass
import hashlib
import math
import platform

import numpy as np
import pandas as pd
import scipy
import sklearn
from sklearn.impute import SimpleImputer
from sklearn.manifold import TSNE
from sklearn.neighbors import NearestNeighbors
from sklearn.preprocessing import StandardScaler

from .source import METADATA_LEAF_NAMES


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
MINIMUM_NUMERIC_COVERAGE = 0.90
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
    minimum_coverage: float = MINIMUM_NUMERIC_COVERAGE,
) -> TSNEResult:
    if len(feature_frame) < 3:
        raise ValueError("t-SNE requires at least 3 rows.")
    if not math.isfinite(variance_threshold) or variance_threshold < 0.0:
        raise ValueError("variance_threshold must be finite and no less than 0.")
    if not 0.0 < minimum_coverage <= 1.0:
        raise ValueError("minimum_coverage must be greater than 0 and no greater than 1.")

    metadata_columns = {"DRAFT_NO", "PARAM_TYP", "LABEL_Y", "RSLT_CD"}
    candidates = sorted(
        (column for column in feature_frame.columns if column not in metadata_columns),
        key=str.casefold,
    )
    candidate_values = feature_frame[candidates].copy()
    for column in candidate_values.columns:
        candidate_values[column] = candidate_values[column].map(
            lambda value: np.nan if isinstance(value, (bool, np.bool_)) else value
        )
    numeric = candidate_values.apply(pd.to_numeric, errors="coerce")
    numeric = numeric.replace([np.inf, -np.inf], np.nan)
    audit = _build_feature_audit(
        feature_frame,
        numeric,
        metadata_columns,
        variance_threshold,
        minimum_coverage,
    )
    included_features = sorted(
        audit.loc[audit["Included"], "FeatureName"].astype(str).tolist(),
        key=str.casefold,
    )
    excluded_features = sorted(
        audit.loc[~audit["Included"], "FeatureName"].astype(str).tolist(),
        key=str.casefold,
    )
    if len(included_features) < 2:
        raise ValueError("t-SNE requires at least 2 numeric features after filtering.")

    numeric = numeric[included_features]
    imputed_value_count = int(numeric.isna().sum().sum())
    imputer = SimpleImputer(strategy="mean")
    imputed = pd.DataFrame(
        imputer.fit_transform(numeric),
        columns=included_features,
        index=feature_frame.index,
    )

    scaler = StandardScaler()
    standardized = scaler.fit_transform(imputed)
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
            imputed.reset_index(drop=True),
        ],
        axis=1,
    )
    diagnostic = _build_diagnostic(
        points=points,
        included_features=included_features,
        excluded_count=len(excluded_features),
        standardized=standardized,
        tsne=tsne,
        perplexity=perplexity,
        nearest_neighbors=nearest_neighbors,
        knn_neighbor_count=knn_neighbor_count,
        imputed_value_count=imputed_value_count,
        variance_threshold=variance_threshold,
        minimum_coverage=minimum_coverage,
    )
    return TSNEResult(
        points=points,
        features=included_features,
        excluded_features=excluded_features,
        feature_audit=audit,
        surviving_population=surviving_population,
        diagnostic=diagnostic,
        standardized_matrix=standardized,
        scaler=scaler,
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


def _metadata_values(frame: pd.DataFrame, column: str) -> object:
    if column in frame.columns:
        return frame[column].fillna("").astype(str).values
    return ""


def _build_feature_audit(
    feature_frame: pd.DataFrame,
    numeric: pd.DataFrame,
    metadata_columns: set[str],
    variance_threshold: float,
    minimum_coverage: float,
) -> pd.DataFrame:
    row_count = len(feature_frame)
    details: list[dict[str, object]] = []
    for column in feature_frame.columns:
        if column in metadata_columns:
            continue
        source = feature_frame[column]
        numeric_values = numeric[column]
        present_count = int(source.notna().sum())
        numeric_count = int(numeric_values.notna().sum())
        nonnumeric_count = max(0, present_count - numeric_count)
        missing_count = max(0, row_count - present_count)
        imputation_count = max(0, row_count - numeric_count)
        coverage = numeric_count / float(row_count)
        finite_values = numeric_values.dropna()
        has_statistics = not finite_values.empty
        variance = float(finite_values.var(ddof=0)) if has_statistics else float("nan")

        reason = "Included"
        included = True
        if _is_metadata_feature(column):
            reason = "Metadata"
            included = False
        elif numeric_count == 0:
            reason = "MissingInRows" if missing_count > 0 else "NonNumeric"
            included = False
        elif coverage < minimum_coverage:
            reason = "MissingInRows"
            included = False
        elif variance <= variance_threshold:
            reason = "ConstantOrLowVariance"
            included = False

        details.append(
            {
                "FeatureName": column,
                "Included": included,
                "Reason": reason,
                "RowCount": row_count,
                "PresentCount": present_count,
                "NumericCount": numeric_count,
                "MissingCount": missing_count,
                "NonNumericCount": nonnumeric_count,
                "ImputationCount": imputation_count,
                "NumericCoverage": coverage,
                "Mean": float(finite_values.mean()) if has_statistics else None,
                "Variance": variance if has_statistics else None,
                "StdDev": float(finite_values.std(ddof=0)) if has_statistics else None,
                "Min": float(finite_values.min()) if has_statistics else None,
                "Max": float(finite_values.max()) if has_statistics else None,
            }
        )
    return pd.DataFrame(details).sort_values(
        by=["Included", "Reason", "FeatureName"],
        ascending=[False, True, True],
        key=lambda values: values.map(str.casefold) if values.name == "FeatureName" else values,
        ignore_index=True,
    )


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
    minimum_coverage: float,
) -> dict[str, object]:
    means = np.mean(standardized, axis=0)
    standard_deviations = np.std(standardized, axis=0)
    coordinate_values = points[["X1", "X2"]].to_numpy(dtype=np.float64)
    n_iter_attribute = int(tsne.n_iter_)
    return {
        "ProjectionMethod": "TSNE",
        "RowCount": len(points),
        "FeatureCount": len(included_features),
        "ExcludedFeatureCount": excluded_count,
        "FeatureNames": included_features,
        "MinimumNumericCoverage": minimum_coverage,
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
        "CoordinateSha256": _matrix_sha256(coordinate_values),
        "CoordinateSummary": {
            "X1Min": float(np.min(coordinate_values[:, 0])),
            "X1Max": float(np.max(coordinate_values[:, 0])),
            "X1Mean": float(np.mean(coordinate_values[:, 0])),
            "X1StdDev": float(np.std(coordinate_values[:, 0])),
            "X2Min": float(np.min(coordinate_values[:, 1])),
            "X2Max": float(np.max(coordinate_values[:, 1])),
            "X2Mean": float(np.mean(coordinate_values[:, 1])),
            "X2StdDev": float(np.std(coordinate_values[:, 1])),
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


def _is_metadata_feature(feature_name: str) -> bool:
    leaf = feature_name.rsplit(".", 1)[-1].upper()
    return leaf in METADATA_LEAF_NAMES
