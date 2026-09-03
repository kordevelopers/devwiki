from __future__ import annotations

from dataclasses import dataclass
import os
from pathlib import Path
import sys

from dotenv import load_dotenv


DEFAULT_SQL = (
    "SELECT M.DRAFT_NO, M.PARAM_TYP, J.ENGR_RSLT_VAL AS LABEL_Y, M.CONV_EXPER_CTN "
    "FROM TASADM.PCCB_INFER_RSLT_INF M "
    "JOIN TASADM.PCCB_JUDGE_RSLT_INF J "
    "ON M.DRAFT_NO = J.DRAFT_NO AND M.PARAM_TYP = J.PARAM_TYP "
    "WHERE M.CHG_TM > SYSDATE - 10 AND J.ENGR_RSLT_VAL IS NOT NULL"
)


@dataclass(frozen=True)
class AppConfig:
    mode: str
    param_type: str
    target_draft_no: str
    sql: str
    sql_file: str
    oracle_host: str
    oracle_port: str
    oracle_service_name: str
    oracle_sid: str
    odbc_dsn: str
    odbc_driver: str
    odbc_user: str
    odbc_password: str
    odbc_connection_string: str
    oracle_user: str
    oracle_password: str
    oracle_dsn: str


def load_config(
    mode_override: str | None = None,
    target_override: str | None = None,
    resolve_sql: bool = True,
) -> AppConfig:
    _load_dotenv_files()
    mode = mode_override or os.getenv("PCA_DB_MODE", "sample")
    normalized_mode = mode.strip().lower()
    target = target_override if target_override is not None else os.getenv("PCA_TARGET_DRAFT_NO", "")
    sql_file = os.getenv("PCA_SQL_FILE", "").strip()
    return AppConfig(
        mode=normalized_mode,
        param_type=os.getenv("PCA_PARAM_TYP", "RESPONSE").strip().upper(),
        target_draft_no=target.strip(),
        sql=(
            DEFAULT_SQL
            if normalized_mode == "sample" or not resolve_sql
            else _load_sql(sql_file, os.getenv("PCA_SQL", DEFAULT_SQL))
        ),
        sql_file=sql_file,
        oracle_host=os.getenv("PCA_ORACLE_HOST", "").strip(),
        oracle_port=os.getenv("PCA_ORACLE_PORT", "1521").strip(),
        oracle_service_name=os.getenv("PCA_ORACLE_SERVICE_NAME", "").strip(),
        oracle_sid=os.getenv("PCA_ORACLE_SID", "").strip(),
        odbc_dsn=os.getenv("PCA_ODBC_DSN", ""),
        odbc_driver=os.getenv("PCA_ODBC_DRIVER", "Oracle in instantclient_23_8"),
        odbc_user=os.getenv("PCA_ODBC_USER", ""),
        odbc_password=os.getenv("PCA_ODBC_PASSWORD", ""),
        odbc_connection_string=os.getenv("PCA_ODBC_CONNECTION_STRING", ""),
        oracle_user=os.getenv("PCA_ORACLE_USER", ""),
        oracle_password=os.getenv("PCA_ORACLE_PASSWORD", ""),
        oracle_dsn=os.getenv("PCA_ORACLE_DSN", "").strip(),
    )


def _load_sql(sql_file: str, fallback_sql: str) -> str:
    if not sql_file:
        return fallback_sql
    path = _resolve_external_path(sql_file)
    if not path.exists():
        raise FileNotFoundError(f"PCA_SQL_FILE was not found: {path}")
    sql = path.read_text(encoding="utf-8-sig").strip()
    if sql.endswith(";"):
        sql = sql[:-1].strip()
    if not sql:
        raise ValueError(f"PCA_SQL_FILE is empty: {path}")
    return sql


def _load_dotenv_files() -> None:
    cwd_env = Path.cwd() / ".env"
    app_env = _application_dir() / ".env"
    if cwd_env.exists():
        load_dotenv(cwd_env)
    elif app_env.exists():
        load_dotenv(app_env)
    else:
        load_dotenv()


def _resolve_external_path(path_text: str) -> Path:
    path = Path(path_text)
    if path.is_absolute():
        return path
    cwd_path = Path.cwd() / path
    if cwd_path.exists():
        return cwd_path
    return _application_dir() / path


def _application_dir() -> Path:
    if getattr(sys, "frozen", False):
        return Path(sys.executable).resolve().parent
    return Path.cwd()
