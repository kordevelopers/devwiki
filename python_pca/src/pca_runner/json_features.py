from __future__ import annotations

from collections.abc import Mapping, Sequence
import json
import math
from typing import Any

import pandas as pd


METADATA_NAMES = {"DRAFT_NO", "AI_RSLT_VAL", "AI_RSLT_VAL", "PUB_NO", "_VERSION_NM"}


def build_feature_frame(source: pd.DataFrame, param_type: str) -> pd.DataFrame:
    filtered = source[source["PARAM_TYP"].str.upper() == param_type.upper()].copy()
    if filtered.empty:
        raise ValueError(f"No rows found for PARAM_TYP '{param_type}'.")
    if filtered["DRAFT_NO"].duplicated().any():
        duplicate = filtered.loc[filtered["DRAFT_NO"].duplicated(), "DRAFT_NO"].iloc[0]
        raise ValueError(f"Duplicated DRAFT_NO in PARAM_TYP '{param_type}': {duplicate}")

    records: list[dict[str, Any]] = []
    for source_index, row in filtered.reset_index(drop=True).iterrows():
        raw_json = row["CONV_EXPER_CTN"]
        if not raw_json:
            continue
        experiment = _extract_single_experiment(raw_json, source_index, row["DRAFT_NO"])
        flattened: dict[str, Any] = {}
        _flatten(experiment, flattened)
        numeric = {
            key: value
            for key, value in ((_normalize_key(k), _to_finite_number(v)) for k, v in flattened.items())
            if value is not None and not _is_metadata_key(key)
        }
        if not numeric:
            continue
        numeric["DRAFT_NO"] = row["DRAFT_NO"]
        numeric["LABEL_Y"] = row["LABEL_Y"]
        records.append(numeric)

    if len(records) < 3:
        raise ValueError("PCA requires at least 3 rows that contain numeric experiment data.")
    return pd.DataFrame(records)


def _extract_single_experiment(raw_json: str, source_index: int, draft_no: str) -> Any:
    try:
        root = json.loads(raw_json)
    except json.JSONDecodeError as exc:
        raise ValueError(
            f"CONV_EXPER_CTN[{source_index}] JSON parse failed. DRAFT_NO={draft_no}: {exc}"
        ) from exc

    if isinstance(root, str):
        try:
            root = json.loads(root)
        except json.JSONDecodeError:
            pass

    if isinstance(root, list):
        if len(root) != 1:
            raise ValueError(
                f"CONV_EXPER_CTN[{source_index}] must contain exactly one experiment object. "
                f"DRAFT_NO={draft_no}, Count={len(root)}"
            )
        root = root[0]

    if not isinstance(root, Mapping):
        raise ValueError(f"CONV_EXPER_CTN[{source_index}] is not a JSON object. DRAFT_NO={draft_no}")
    return root


def _flatten(value: Any, output: dict[str, Any], prefix: str = "") -> None:
    if isinstance(value, Mapping):
        for key, child in value.items():
            child_key = str(key) if not prefix else f"{prefix}.{key}"
            _flatten(child, output, child_key)
        return
    if isinstance(value, Sequence) and not isinstance(value, (str, bytes, bytearray)):
        for index, child in enumerate(value):
            _flatten(child, output, f"{prefix}[{index}]")
        return
    output[prefix] = value


def _to_finite_number(value: Any) -> float | None:
    if value is None or isinstance(value, bool):
        return None
    try:
        numeric = float(str(value).strip()) if isinstance(value, str) else float(value)
    except (TypeError, ValueError):
        return None
    return numeric if math.isfinite(numeric) else None


def _normalize_key(key: str) -> str:
    return key.strip()


def _is_metadata_key(key: str) -> bool:
    leaf = key.rsplit(".", 1)[-1].upper()
    return leaf in METADATA_NAMES
