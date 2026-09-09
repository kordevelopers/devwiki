from __future__ import annotations

from collections.abc import Mapping, Sequence
from contextlib import closing
import json
import math
from pathlib import Path
from typing import Any

import pandas as pd

from .config import AppConfig


REQUIRED_COLUMNS = {"DRAFT_NO", "PARAM_TYP", "LABEL_Y", "CONV_EXPER_CTN"}
OPTIONAL_COLUMNS = ["RSLT_CD"]
SUPPORTED_PARAMETER_TYPES = {"RESPONSE", "DEFECT", "EPM", "PROBE"}
METADATA_LEAF_NAMES = {"DRAFT_NO", "AI_RSLT_VAL", "PUB_NO", "_VERSION_NM"}


def load_source_rows(config: AppConfig) -> pd.DataFrame:
    return _load_with_oracledb(config)


def load_source_csv(path: Path) -> pd.DataFrame:
    if not path.exists():
        raise FileNotFoundError(f"Source CSV was not found: {path}")
    return pd.read_csv(
        path,
        encoding="utf-8-sig",
        dtype=str,
        keep_default_na=False,
    )


def normalize_source_columns(frame: pd.DataFrame) -> pd.DataFrame:
    renamed = {name: str(name).upper() for name in frame.columns}
    if len(set(renamed.values())) != len(renamed):
        raise ValueError("Source query contains duplicate column names after normalization.")

    result = frame.rename(columns=renamed)
    result = _normalize_label_column(result)
    missing = REQUIRED_COLUMNS.difference(result.columns)
    if missing:
        raise ValueError(f"Source query is missing required columns: {sorted(missing)}")

    selected_columns = ["DRAFT_NO", "PARAM_TYP", "LABEL_Y", "CONV_EXPER_CTN"]
    selected_columns.extend(column for column in OPTIONAL_COLUMNS if column in result.columns)
    result = result.loc[:, selected_columns].copy()
    result["DRAFT_NO"] = result["DRAFT_NO"].fillna("").astype(str).str.strip()
    result["PARAM_TYP"] = result["PARAM_TYP"].fillna("").astype(str).str.strip().str.upper()
    result["LABEL_Y"] = result["LABEL_Y"].fillna("").astype(str).str.strip()
    result["CONV_EXPER_CTN"] = result["CONV_EXPER_CTN"].map(_to_json_text)
    if "RSLT_CD" in result.columns:
        result["RSLT_CD"] = result["RSLT_CD"].fillna("").astype(str).str.strip()

    unsupported = sorted(set(result["PARAM_TYP"]).difference(SUPPORTED_PARAMETER_TYPES))
    if unsupported:
        raise ValueError(f"Unsupported PARAM_TYP values: {unsupported}")
    if (result["DRAFT_NO"] == "").any():
        raise ValueError("Source query contains an empty DRAFT_NO.")
    return result


def build_feature_frame(source: pd.DataFrame, param_type: str) -> pd.DataFrame:
    filtered: pd.DataFrame = source.loc[
        source["PARAM_TYP"].str.upper().eq(param_type.upper()), :
    ].copy()
    if filtered.empty:
        raise ValueError(f"No rows found for PARAM_TYP '{param_type}'.")
    normalized_draft_numbers = filtered["DRAFT_NO"].astype(str).str.casefold()
    duplicate_mask = normalized_draft_numbers.duplicated()
    if duplicate_mask.any():
        duplicate = filtered.loc[duplicate_mask, "DRAFT_NO"].iloc[0]
        raise ValueError(f"Duplicated DRAFT_NO in PARAM_TYP '{param_type}': {duplicate}")

    records: list[dict[str, Any]] = []
    canonical_feature_names: dict[str, str] = {}
    for source_index, row in enumerate(filtered.to_dict(orient="records")):
        raw_json = row["CONV_EXPER_CTN"]
        if not raw_json:
            continue
        experiment = _extract_single_experiment(raw_json, source_index, row["DRAFT_NO"])
        flattened: dict[str, Any] = {}
        _flatten(experiment, flattened)

        feature_values: dict[str, Any] = {}
        has_numeric_feature = False
        for raw_key, raw_value in flattened.items():
            key = raw_key.strip()
            value = _to_finite_number(raw_value)
            if not key:
                continue
            folded_key = key.casefold()
            canonical_key = canonical_feature_names.setdefault(folded_key, key)
            if canonical_key.upper() in {"DRAFT_NO", "PARAM_TYP", "LABEL_Y", "RSLT_CD"}:
                canonical_key = f"CONV_EXPER_CTN.{canonical_key}"
            if canonical_key in feature_values:
                raise ValueError(
                    "CONV_EXPER_CTN contains duplicate feature names that differ only by case. "
                    f"DRAFT_NO={row['DRAFT_NO']}, Feature={key}"
                )
            feature_values[canonical_key] = raw_value
            if value is not None and not _is_metadata_key(key):
                has_numeric_feature = True

        if not has_numeric_feature:
            continue
        feature_values["DRAFT_NO"] = row["DRAFT_NO"]
        feature_values["PARAM_TYP"] = row["PARAM_TYP"]
        feature_values["LABEL_Y"] = row["LABEL_Y"]
        if "RSLT_CD" in row:
            feature_values["RSLT_CD"] = row["RSLT_CD"]
        records.append(feature_values)

    if len(records) < 3:
        raise ValueError("t-SNE requires at least 3 rows that contain numeric experiment data.")
    return pd.DataFrame(records)


def _normalize_label_column(frame: pd.DataFrame) -> pd.DataFrame:
    if "LABEL_Y" in frame.columns:
        return frame
    if "ENGR_RSLT_VAL" in frame.columns:
        return frame.rename(columns={"ENGR_RSLT_VAL": "LABEL_Y"})
    return frame


def _load_with_oracledb(config: AppConfig) -> pd.DataFrame:
    from sqlalchemy import create_engine, text
    from sqlalchemy.pool import NullPool

    # DBeaver JDBC URL의 jdbc:oracle:thin:@호스트:포트/데이터베이스 값을
    # TSNE_DB_HOST, TSNE_DB_PORT, TSNE_DB_DATABASE로 나누어 입력한다.
    # Python에서는 JDBC 드라이버 대신 python-oracledb Thin 모드를 사용한다.
    dsn = f"{config.host}:{config.port}/{config.database}"
    if not (config.username and config.password and config.host and config.database):
        raise ValueError(
            "TSNE_DB_HOST, TSNE_DB_DATABASE, TSNE_DB_PORT, TSNE_DB_USERNAME, "
            "and TSNE_DB_PASSWORD are required."
        )
    engine = create_engine(
        "oracle+oracledb://",
        connect_args={
            "user": config.username,
            "password": config.password,
            "dsn": dsn,
        },
        poolclass=NullPool,
    )
    try:
        with engine.connect() as connection:
            frame = pd.read_sql_query(text(config.sql), connection)
            return _materialize_lob_values(frame)
    finally:
        engine.dispose()


def _to_json_text(value: object) -> str:
    if _is_scalar_missing(value):
        return ""
    reader = getattr(value, "read", None)
    if callable(reader):
        value = reader()
    if _is_scalar_missing(value):
        return ""
    if isinstance(value, bytes):
        value = value.decode("utf-8-sig")
    if isinstance(value, str):
        return value.strip()
    return json.dumps(value, ensure_ascii=False)


def _is_scalar_missing(value: object) -> bool:
    if value is None:
        return True
    try:
        return bool(pd.isna(value))
    except (TypeError, ValueError):
        return False


def _materialize_lob_values(frame: pd.DataFrame) -> pd.DataFrame:
    result = frame.copy()
    for column in result.columns:
        result[column] = result[column].map(
            lambda value: value.read()
            if hasattr(value, "read") and callable(value.read)
            else value
        )
    return result


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
        raise ValueError(
            f"CONV_EXPER_CTN[{source_index}] is not a JSON object. DRAFT_NO={draft_no}"
        )
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


def _is_metadata_key(key: str) -> bool:
    leaf = key.rsplit(".", 1)[-1].upper()
    return leaf in METADATA_LEAF_NAMES
