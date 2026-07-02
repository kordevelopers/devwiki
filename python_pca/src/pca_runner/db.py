from __future__ import annotations

import json

import pandas as pd

from .config import AppConfig
from .sample_data import build_sample_rows


REQUIRED_COLUMNS = {"DRAFT_NO", "PARAM_TYP", "LABEL_Y", "CONV_EXPER_CTN"}
OPTIONAL_COLUMNS = ["RSLT_CD"]
SUPPORTED_PARAMETER_TYPES = {"RESPONSE", "DEFECT", "EPM", "PROBE"}


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
    result = _normalize_label_column(result)
    missing = REQUIRED_COLUMNS.difference(result.columns)
    if missing:
        raise ValueError(f"Source query is missing required columns: {sorted(missing)}")
    selected_columns = ["DRAFT_NO", "PARAM_TYP", "LABEL_Y", "CONV_EXPER_CTN"]
    selected_columns.extend(column for column in OPTIONAL_COLUMNS if column in result.columns)
    result = result[selected_columns].copy()
    result["DRAFT_NO"] = result["DRAFT_NO"].astype(str).str.strip()
    result["PARAM_TYP"] = result["PARAM_TYP"].astype(str).str.strip().str.upper()
    result["LABEL_Y"] = result["LABEL_Y"].astype(str).str.strip()
    result["CONV_EXPER_CTN"] = result["CONV_EXPER_CTN"].map(_to_json_text)
    if "RSLT_CD" in result.columns:
        result["RSLT_CD"] = result["RSLT_CD"].astype(str).str.strip()
    unsupported = sorted(set(result["PARAM_TYP"]).difference(SUPPORTED_PARAMETER_TYPES))
    if unsupported:
        raise ValueError(f"Unsupported PARAM_TYP values: {unsupported}")
    return result


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
            "PCA_ORACLE_USER, PCA_ORACLE_PASSWORD, and Oracle DSN values are required. "
            "Set PCA_ORACLE_DSN or PCA_ORACLE_HOST/PCA_ORACLE_PORT/PCA_ORACLE_SERVICE_NAME."
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
            return pd.read_sql_query(text(config.sql), connection)
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
            "ODBC mode requires one of PCA_ODBC_CONNECTION_STRING, PCA_ODBC_DSN, or "
            "PCA_ODBC_DRIVER plus PCA_ORACLE_HOST/PCA_ORACLE_PORT/PCA_ORACLE_SERVICE_NAME "
            "and PCA_ODBC_USER/PCA_ODBC_PASSWORD."
        )
    return (
        f"DRIVER={{{config.odbc_driver}}};"
        f"DBQ={host_descriptor};"
        f"UID={config.odbc_user};"
        f"PWD={config.odbc_password}"
    )


def _build_oracle_dsn(config: AppConfig) -> str:
    if config.oracle_dsn:
        return config.oracle_dsn
    return _build_host_descriptor(config)


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
    if value is None:
        return ""
    if isinstance(value, str):
        return value.strip()
    return json.dumps(value, ensure_ascii=False)
