from __future__ import annotations

import json

import pandas as pd

from .config import AppConfig
from .sample_data import build_sample_rows


REQUIRED_COLUMNS = {"DRAFT_NO", "PARAM_TYP", "LABEL_Y", "CONV_EXPER_CTN"}


def load_source_rows(config: AppConfig) -> pd.DataFrame:
    if config.mode == "sample":
        return pd.DataFrame(build_sample_rows())
    if config.mode == "odbc":
        return _load_with_odbc(config)
    if config.mode == "oracledb":
        return _load_with_oracledb(config)
    raise ValueError("PCA_DB_MODE must be one of: sample, odbc, oracledb")


def normalize_source_columns(frame: pd.DataFrame) -> pd.DataFrame:
    renamed = {name: str(name).upper() for name in frame.columns}
    result = frame.rename(columns=renamed)
    missing = REQUIRED_COLUMNS.difference(result.columns)
    if missing:
        raise ValueError(f"Source query is missing required columns: {sorted(missing)}")
    result = result[list(REQUIRED_COLUMNS)].copy()
    result["DRAFT_NO"] = result["DRAFT_NO"].astype(str).str.strip()
    result["PARAM_TYP"] = result["PARAM_TYP"].astype(str).str.strip().str.upper()
    result["LABEL_Y"] = result["LABEL_Y"].astype(str).str.strip()
    result["CONV_EXPER_CTN"] = result["CONV_EXPER_CTN"].map(_to_json_text)
    return result


def _load_with_odbc(config: AppConfig) -> pd.DataFrame:
    import pyodbc

    connection_string = config.odbc_connection_string.strip()
    if not connection_string:
        if not config.odbc_dsn:
            raise ValueError("PCA_ODBC_DSN or PCA_ODBC_CONNECTION_STRING is required for ODBC mode.")
        connection_string = f"DSN={config.odbc_dsn};UID={config.odbc_user};PWD={config.odbc_password}"
    with pyodbc.connect(connection_string) as connection:
        return pd.read_sql(config.sql, connection)


def _load_with_oracledb(config: AppConfig) -> pd.DataFrame:
    import oracledb

    if not (config.oracle_user and config.oracle_password and config.oracle_dsn):
        raise ValueError("PCA_ORACLE_USER, PCA_ORACLE_PASSWORD, and PCA_ORACLE_DSN are required.")
    with oracledb.connect(
        user=config.oracle_user,
        password=config.oracle_password,
        dsn=config.oracle_dsn,
    ) as connection:
        return pd.read_sql(config.sql, connection)


def _to_json_text(value: object) -> str:
    if value is None:
        return ""
    if isinstance(value, str):
        return value.strip()
    return json.dumps(value, ensure_ascii=False)
