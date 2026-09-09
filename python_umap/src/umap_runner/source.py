from collections.abc import Mapping, Sequence
import json
import math
from pathlib import Path
from typing import Any
import pandas as pd
from .config import AppConfig

SUPPORTED_DATA_TYPES = {"RESPONSE", "DEFECT", "EPM"}
REQUIRED_COLUMNS = {"DRAFT_NO", "PARAM_TYP", "LABEL_Y", "CONV_EXPER_CTN"}

def load_source_rows(config: AppConfig):
    from sqlalchemy import create_engine, text
    from sqlalchemy.pool import NullPool
    if not all((config.host, config.database, config.port, config.username, config.password)):
        raise ValueError("UMAP_DB_HOST, UMAP_DB_DATABASE, UMAP_DB_PORT, UMAP_DB_USERNAME, and UMAP_DB_PASSWORD are required.")
    engine = create_engine("oracle+oracledb://", connect_args={"user": config.username, "password": config.password,
                         "dsn": f"{config.host}:{config.port}/{config.database}"}, poolclass=NullPool)
    try:
        with engine.connect() as connection:
            frame = pd.read_sql_query(text(config.sql), connection)
            return _materialize_lobs(frame)
    finally:
        engine.dispose()

def load_source_csv(path: Path):
    return pd.read_csv(path, encoding="utf-8-sig", dtype=str, keep_default_na=False)

def normalize_source_columns(frame):
    result = frame.rename(columns={name: str(name).upper() for name in frame.columns}).copy()
    if "LABEL_Y" not in result and "ENGR_RSLT_VAL" in result:
        result = result.rename(columns={"ENGR_RSLT_VAL": "LABEL_Y"})
    missing = REQUIRED_COLUMNS.difference(result.columns)
    if missing:
        raise ValueError(f"Source query is missing required columns: {sorted(missing)}")
    result["DRAFT_NO"] = result["DRAFT_NO"].fillna("").astype(str).str.strip()
    result["PARAM_TYP"] = result["PARAM_TYP"].fillna("").astype(str).str.strip().str.upper()
    result["LABEL_Y"] = result["LABEL_Y"].fillna("").astype(str).str.strip()
    result["CONV_EXPER_CTN"] = result["CONV_EXPER_CTN"].map(_json_text)
    if "RSLT_CD" not in result:
        result["RSLT_CD"] = ""
    result["RSLT_CD"] = result["RSLT_CD"].fillna("").astype(str).str.strip()
    invalid = sorted(set(result["PARAM_TYP"]).difference(SUPPORTED_DATA_TYPES))
    if invalid:
        raise ValueError(f"Unsupported PARAM_TYP values: {invalid}")
    return result.loc[:, ["DRAFT_NO", "PARAM_TYP", "LABEL_Y", "RSLT_CD", "CONV_EXPER_CTN"]]

def build_feature_frame(source, data_type):
    filtered = source.loc[source["PARAM_TYP"].eq(data_type.upper())].copy()
    if filtered.empty:
        raise ValueError(f"No rows found for PARAM_TYP '{data_type}'.")
    records = []
    for _, row in filtered.iterrows():
        if not row["CONV_EXPER_CTN"]:
            continue
        flattened = {}
        _flatten(_single_json(row["CONV_EXPER_CTN"]), flattened)
        features = {key: value for key, value in flattened.items() if _number(value) is not None and key.rsplit(".", 1)[-1].upper() not in {"DRAFT_NO", "PUB_NO"}}
        if features:
            features.update({key: row[key] for key in ("DRAFT_NO", "PARAM_TYP", "LABEL_Y", "RSLT_CD")})
            records.append(features)
    if len(records) < 3:
        raise ValueError("UMAP requires at least 3 rows containing numeric experiment data.")
    result = pd.DataFrame(records)
    if result["DRAFT_NO"].str.casefold().duplicated().any():
        raise ValueError(f"Duplicated DRAFT_NO in PARAM_TYP '{data_type}'.")
    return result

def _json_text(value):
    if value is None or (not isinstance(value, (dict, list)) and pd.isna(value)):
        return ""
    if hasattr(value, "read"):
        value = value.read()
    if isinstance(value, bytes): value = value.decode("utf-8-sig")
    return value.strip() if isinstance(value, str) else json.dumps(value, ensure_ascii=False)

def _single_json(value):
    root = json.loads(value)
    if isinstance(root, str): root = json.loads(root)
    if isinstance(root, list):
        if len(root) != 1: raise ValueError("CONV_EXPER_CTN must contain exactly one experiment object.")
        root = root[0]
    if not isinstance(root, Mapping): raise ValueError("CONV_EXPER_CTN is not a JSON object.")
    return root

def _flatten(value: Any, output: dict, prefix=""):
    if isinstance(value, Mapping):
        for key, child in value.items(): _flatten(child, output, str(key) if not prefix else f"{prefix}.{key}")
    elif isinstance(value, Sequence) and not isinstance(value, (str, bytes, bytearray)):
        for index, child in enumerate(value): _flatten(child, output, f"{prefix}[{index}]")
    else: output[prefix] = value

def _number(value):
    if value is None or isinstance(value, bool): return None
    try:
        number = float(value)
        return number if math.isfinite(number) else None
    except (TypeError, ValueError): return None

def _materialize_lobs(frame):
    return frame.map(lambda value: value.read() if hasattr(value, "read") and callable(value.read) else value)
