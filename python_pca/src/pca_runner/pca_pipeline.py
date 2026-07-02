from __future__ import annotations

from dataclasses import dataclass

import numpy as np
import pandas as pd
from sklearn.decomposition import PCA
from sklearn.impute import SimpleImputer
from sklearn.neighbors import NearestNeighbors
from sklearn.preprocessing import StandardScaler


@dataclass(frozen=True)
class NeighborRow:
    rank: int
    similar_draft: str
    distance: float


@dataclass(frozen=True)
class PcaResult:
    points: pd.DataFrame
    features: list[str]
    excluded_features: list[str]
    feature_audit: pd.DataFrame
    surviving_population: pd.DataFrame
    diagnostic: dict[str, object]
    standardized_matrix: np.ndarray
    scaler: StandardScaler
    pca: PCA
    nearest_neighbors: NearestNeighbors


def run_pca(feature_frame: pd.DataFrame, variance_threshold: float = 1e-10) -> PcaResult:
    metadata_columns = {"DRAFT_NO", "PARAM_TYP", "LABEL_Y", "RSLT_CD"}
    candidates = [column for column in feature_frame.columns if column not in metadata_columns]
    numeric = feature_frame[candidates].apply(pd.to_numeric, errors="coerce")
    audit = _build_feature_audit(feature_frame, numeric, metadata_columns, variance_threshold)
    included_features = audit.loc[audit["Included"], "FeatureName"].astype(str).tolist()
    excluded = audit.loc[~audit["Included"], "FeatureName"].astype(str).tolist()
    numeric = numeric[included_features]

    imputer = SimpleImputer(strategy="mean")
    imputed = pd.DataFrame(imputer.fit_transform(numeric), columns=numeric.columns)

    if imputed.shape[1] < 2:
        raise ValueError("PCA requires at least 2 numeric features after filtering.")

    scaler = StandardScaler()
    standardized = scaler.fit_transform(imputed)
    pca = PCA(n_components=2, random_state=20260622)
    coordinates = pca.fit_transform(standardized)

    nn = NearestNeighbors(
        n_neighbors=min(4, len(feature_frame)),
        metric="euclidean",
        algorithm="auto",
    )
    nn.fit(standardized)

    points = pd.DataFrame(
        {
            "DRAFT_NO": feature_frame["DRAFT_NO"].astype(str).values,
            "PARAM_TYP": feature_frame["PARAM_TYP"].astype(str).values
            if "PARAM_TYP" in feature_frame.columns
            else "",
            "LABEL_Y": feature_frame["LABEL_Y"].astype(str).values,
            "RSLT_CD": feature_frame["RSLT_CD"].astype(str).values
            if "RSLT_CD" in feature_frame.columns
            else "",
            "X1": coordinates[:, 0],
            "X2": coordinates[:, 1],
        }
    )
    surviving_population = pd.concat(
        [points[["DRAFT_NO", "PARAM_TYP", "LABEL_Y", "RSLT_CD", "X1", "X2"]], imputed],
        axis=1,
    )
    diagnostic = _build_diagnostic(
        row_count=len(feature_frame),
        included_count=len(included_features),
        excluded_count=len(excluded),
        pca=pca,
        standardized=standardized,
        neighbor_algorithm=nn.algorithm,
    )
    return PcaResult(
        points=points,
        features=included_features,
        excluded_features=excluded,
        feature_audit=audit,
        surviving_population=surviving_population,
        diagnostic=diagnostic,
        standardized_matrix=standardized,
        scaler=scaler,
        pca=pca,
        nearest_neighbors=nn,
    )


def _build_feature_audit(
    feature_frame: pd.DataFrame,
    numeric: pd.DataFrame,
    metadata_columns: set[str],
    variance_threshold: float,
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
        finite_values = numeric_values.dropna()
        has_statistics = not finite_values.empty
        variance = float(finite_values.var(ddof=0)) if has_statistics else float("nan")

        reason = "Included"
        included = True
        if numeric_count == 0:
            reason = "MissingInRows" if missing_count > 0 else "NonNumeric"
            included = False
        elif numeric_count < row_count or (numeric_count / float(row_count)) < 0.90:
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
        ignore_index=True,
    )


def _build_diagnostic(
    row_count: int,
    included_count: int,
    excluded_count: int,
    pca: PCA,
    standardized: np.ndarray,
    neighbor_algorithm: str,
) -> dict[str, object]:
    pc1 = float(pca.explained_variance_ratio_[0] * 100.0)
    pc2 = float(pca.explained_variance_ratio_[1] * 100.0)
    means = np.mean(standardized, axis=0)
    stds = np.std(standardized, axis=0)
    return {
        "RowCount": row_count,
        "FeatureCount": included_count,
        "ExcludedFeatureCount": excluded_count,
        "Pc1Percent": pc1,
        "Pc2Percent": pc2,
        "Pc1Pc2Percent": pc1 + pc2,
        "ShapeCode": _resolve_shape_code(row_count, included_count, pc1, pc2),
        "KnnAlgorithm": neighbor_algorithm,
        "MaximumAbsoluteStandardizedMean": float(np.max(np.abs(means))),
        "MaximumStandardDeviationError": float(np.max(np.abs(stds - 1.0))),
    }


def _resolve_shape_code(
    row_count: int,
    feature_count: int,
    pc1_percent: float,
    pc2_percent: float,
) -> str:
    if row_count < 3:
        return "ROWS_LT3"
    if row_count < 30:
        return "ROWS_LOW"
    if feature_count < 2:
        return "FEATURE_LT2"
    if feature_count <= 5:
        return "FEATURE_LOW"
    if pc1_percent >= 95.0 and pc2_percent <= 5.0:
        return "LINE_PC1_HIGH"
    if pc1_percent >= 85.0 and pc2_percent <= 10.0:
        return "LINE_LIKELY"
    if pc1_percent + pc2_percent < 50.0:
        return "PCA2_LOW"
    return "OK"


def find_neighbors(result: PcaResult, target_draft_no: str, count: int = 3) -> list[NeighborRow]:
    draft_numbers = result.points["DRAFT_NO"].astype(str).tolist()
    normalized = target_draft_no.strip().lower()
    try:
        target_index = next(
            index for index, draft_no in enumerate(draft_numbers) if draft_no.lower() == normalized
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
