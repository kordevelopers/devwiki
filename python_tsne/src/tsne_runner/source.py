from __future__ import annotations

from collections.abc import Mapping, Sequence
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
    if config.mode == "odbc":
        return _load_with_odbc(config)
    if config.mode == "oracledb":
        return _load_with_oracledb(config)
    raise ValueError("TSNE_DB_MODE must be either 'odbc' or 'oracledb'.")


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
    result = result[selected_columns].copy()
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
    filtered = source[source["PARAM_TYP"].str.upper() == param_type.upper()].copy()
    if filtered.empty:
        raise ValueError(f"No rows found for PARAM_TYP '{param_type}'.")
    normalized_draft_numbers = filtered["DRAFT_NO"].astype(str).str.casefold()
    duplicate_mask = normalized_draft_numbers.duplicated()
    if duplicate_mask.any():
        duplicate = filtered.loc[duplicate_mask, "DRAFT_NO"].iloc[0]
        raise ValueError(f"Duplicated DRAFT_NO in PARAM_TYP '{param_type}': {duplicate}")

    records: list[dict[str, Any]] = []
    canonical_feature_names: dict[str, str] = {}
    for source_index, row in filtered.reset_index(drop=True).iterrows():
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
        if "RSLT_CD" in row.index:
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


def _load_with_odbc(config: AppConfig) -> pd.DataFrame:
    import pyodbc

    connection_string = _build_odbc_connection_string(config)
    with pyodbc.connect(connection_string) as connection:
        cursor = connection.cursor()
        cursor.execute(config.sql)
        columns = [description[0] for description in cursor.description]
        return pd.DataFrame.from_records((tuple(row) for row in cursor.fetchall()), columns=columns)


def _load_with_oracledb(config: AppConfig) -> pd.DataFrame:
    from sqlalchemy import create_engine, text
    from sqlalchemy.pool import NullPool

    dsn = _build_oracle_dsn(config)
    if not (config.oracle_user and config.oracle_password and dsn):
        raise ValueError(
            "TSNE_ORACLE_USER, TSNE_ORACLE_PASSWORD, and Oracle DSN values are required. "
            "Set TSNE_ORACLE_DSN or "
            "TSNE_ORACLE_HOST/TSNE_ORACLE_PORT/TSNE_ORACLE_SERVICE_NAME."
        )
    engine = create_engine(
        "oracle+oracledb://",
        connect_args={
            "user": config.oracle_user,
            "password": config.oracle_password,
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


def _build_odbc_connection_string(config: AppConfig) -> str:
    if config.odbc_connection_string.strip():
        return config.odbc_connection_string.strip()
    if config.odbc_dsn.strip():
        return f"DSN={config.odbc_dsn};UID={config.odbc_user};PWD={config.odbc_password}"

    host_descriptor = _build_host_descriptor(config)
    if not (config.odbc_driver and host_descriptor and config.odbc_user and config.odbc_password):
        raise ValueError(
            "ODBC mode requires one of TSNE_ODBC_CONNECTION_STRING, TSNE_ODBC_DSN, or "
            "TSNE_ODBC_DRIVER plus Oracle host values and TSNE_ODBC_USER/TSNE_ODBC_PASSWORD."
        )
    return (
        f"DRIVER={{{config.odbc_driver}}};"
        f"DBQ={host_descriptor};"
        f"UID={config.odbc_user};"
        f"PWD={config.odbc_password}"
    )


def _build_oracle_dsn(config: AppConfig) -> str:
    return config.oracle_dsn or _build_host_descriptor(config)


def _build_host_descriptor(config: AppConfig) -> str:
    if not config.oracle_host:
        return ""
    port = config.oracle_port or "1521"
    if config.oracle_service_name:
        return f"{config.oracle_host}:{port}/{config.oracle_service_name}"
    if config.oracle_sid:
        return f"{config.oracle_host}:{port}:{config.oracle_sid}"
    return ""


def _to_json_text(value: object) -> str:
    if _is_scalar_missing(value):
        return ""
    if hasattr(value, "read") and callable(value.read):
        value = value.read()
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
