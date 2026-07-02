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
    standardized_matrix: np.ndarray
    scaler: StandardScaler
    pca: PCA
    nearest_neighbors: NearestNeighbors


def run_pca(feature_frame: pd.DataFrame, variance_threshold: float = 1e-10) -> PcaResult:
    metadata_columns = {"DRAFT_NO", "LABEL_Y"}
    candidates = [column for column in feature_frame.columns if column not in metadata_columns]
    numeric = feature_frame[candidates].apply(pd.to_numeric, errors="coerce")
    numeric = numeric.dropna(axis=1, how="all")

    excluded: list[str] = [column for column in candidates if column not in numeric.columns]
    coverage = numeric.notna().mean(axis=0)
    low_coverage = coverage[coverage < 0.90].index.tolist()
    numeric = numeric.drop(columns=low_coverage)
    excluded.extend(low_coverage)

    imputer = SimpleImputer(strategy="mean")
    imputed = pd.DataFrame(imputer.fit_transform(numeric), columns=numeric.columns)
    variances = imputed.var(axis=0, ddof=0)
    low_variance = variances[variances <= variance_threshold].index.tolist()
    imputed = imputed.drop(columns=low_variance)
    excluded.extend(low_variance)

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
            "LABEL_Y": feature_frame["LABEL_Y"].astype(str).values,
            "X1": coordinates[:, 0],
            "X2": coordinates[:, 1],
        }
    )
    return PcaResult(
        points=points,
        features=list(imputed.columns),
        excluded_features=excluded,
        standardized_matrix=standardized,
        scaler=scaler,
        pca=pca,
        nearest_neighbors=nn,
    )


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
