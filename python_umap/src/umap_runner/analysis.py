from dataclasses import dataclass
import math
import numpy as np
import pandas as pd
from sklearn.impute import SimpleImputer
from sklearn.neighbors import NearestNeighbors
from sklearn.preprocessing import StandardScaler
from umap import UMAP

@dataclass(frozen=True)
class UMAPResult:
    points: pd.DataFrame
    standardized_matrix: np.ndarray
    features: list[str]
    diagnostic: dict[str, object]
    nearest_neighbors: NearestNeighbors

def run_umap(frame: pd.DataFrame) -> UMAPResult:
    metadata = {"DRAFT_NO", "PARAM_TYP", "LABEL_Y", "RSLT_CD"}
    candidates = sorted([c for c in frame.columns if c not in metadata], key=str.casefold)
    numeric = frame[candidates].apply(pd.to_numeric, errors="coerce").replace([np.inf, -np.inf], np.nan)
    included = [c for c in numeric if numeric[c].notna().sum() > 0 and numeric[c].nunique(dropna=True) > 1]
    if len(included) < 2: raise ValueError("UMAP requires at least 2 numeric features.")
    imputed = SimpleImputer(strategy="mean").fit_transform(numeric[included])
    standardized = StandardScaler().fit_transform(imputed)
    reducer = UMAP(n_components=2, n_neighbors=min(15, len(frame)-1), min_dist=0.1,
                   metric="euclidean", random_state=42, transform_seed=42)
    coordinates = reducer.fit_transform(standardized)
    if not np.isfinite(coordinates).all(): raise ValueError("UMAP produced a non-finite coordinate.")
    knn = NearestNeighbors(n_neighbors=min(15, len(frame)-1), metric="euclidean").fit(standardized)
    points = pd.DataFrame({"DRAFT_NO": frame["DRAFT_NO"].astype(str).values,
                           "PARAM_TYP": frame["PARAM_TYP"].astype(str).values,
                           "LABEL_Y": frame["LABEL_Y"].astype(str).values,
                           "RSLT_CD": frame["RSLT_CD"].astype(str).values,
                           "X1": coordinates[:, 0], "X2": coordinates[:, 1]})
    return UMAPResult(points, standardized, included,
                      {"Method": "UMAP", "Neighbors": min(15, len(frame)-1), "MinDist": 0.1,
                       "RandomState": 42, "FeatureCount": len(included)}, knn)

def find_neighbors(result: UMAPResult, draft_no: str, count=3):
    matches = result.points.index[result.points["DRAFT_NO"].str.casefold() == draft_no.casefold()]
    if not len(matches): raise ValueError(f"DRAFT_NO not found: {draft_no}")
    distances, indices = result.nearest_neighbors.kneighbors(result.standardized_matrix[int(matches[0])].reshape(1, -1), n_neighbors=min(count+1, len(result.points)))
    return [(int(i), float(d)) for i, d in zip(indices[0], distances[0]) if int(i) != int(matches[0])][:count]
