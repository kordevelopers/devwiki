from __future__ import annotations

from dataclasses import dataclass
import os
from pathlib import Path
import sys

from dotenv import load_dotenv


DEFAULT_SQL = (
    "SELECT M.DRAFT_NO, M.PARAM_TYP, J.ENGR_RSLT_VAL AS LABEL_Y, "
    "J.RSLT_CD, M.CONV_EXPER_CTN "
    "FROM TASADM.PCCB_INFER_RSLT_INF M "
    "JOIN TASADM.PCCB_JUDGE_RSLT_INF J "
    "ON M.DRAFT_NO = J.DRAFT_NO AND M.PARAM_TYP = J.PARAM_TYP "
    "WHERE M.CHG_TM > SYSDATE - 10 "
    "AND J.ENGR_RSLT_VAL IS NOT NULL "
    "AND M.CONV_EXPER_CTN IS NOT NULL "
    "ORDER BY M.DRAFT_NO, M.PARAM_TYP"
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
    param_type_override: str | None = None,
    resolve_sql: bool = True,
) -> AppConfig:
    _load_dotenv_files()
    mode = mode_override or _get_setting("DB_MODE", "oracledb")
    normalized_mode = mode.strip().lower()
    if normalized_mode not in {"odbc", "oracledb"}:
        raise ValueError("TSNE_DB_MODE must be either 'odbc' or 'oracledb'.")

    target = (
        target_override
        if target_override is not None
        else _get_setting("TARGET_DRAFT_NO", "")
    )
    param_type = (
        param_type_override
        if param_type_override is not None
        else _get_setting("PARAM_TYP", "RESPONSE")
    )
    sql_file = _get_setting("SQL_FILE", "").strip()
    fallback_sql = _get_setting("SQL", DEFAULT_SQL)
    return AppConfig(
        mode=normalized_mode,
        param_type=param_type.strip().upper(),
        target_draft_no=target.strip(),
        sql=(
            _load_sql(sql_file, fallback_sql)
            if resolve_sql
            else _strip_sql_terminator(fallback_sql)
        ),
        sql_file=sql_file,
        oracle_host=_get_setting("ORACLE_HOST", "").strip(),
        oracle_port=_get_setting("ORACLE_PORT", "1521").strip(),
        oracle_service_name=_get_setting("ORACLE_SERVICE_NAME", "").strip(),
        oracle_sid=_get_setting("ORACLE_SID", "").strip(),
        odbc_dsn=_get_setting("ODBC_DSN", ""),
        odbc_driver=_get_setting("ODBC_DRIVER", "Oracle in instantclient_23_8"),
        odbc_user=_get_setting("ODBC_USER", ""),
        odbc_password=_get_setting("ODBC_PASSWORD", ""),
        odbc_connection_string=_get_setting("ODBC_CONNECTION_STRING", ""),
        oracle_user=_get_setting("ORACLE_USER", ""),
        oracle_password=_get_setting("ORACLE_PASSWORD", ""),
        oracle_dsn=_get_setting("ORACLE_DSN", "").strip(),
    )


def _get_setting(name: str, default: str) -> str:
    tsne_name = f"TSNE_{name}"
    if tsne_name in os.environ:
        return os.environ[tsne_name]
    pca_name = f"PCA_{name}"
    if pca_name in os.environ:
        return os.environ[pca_name]
    return default


def _load_sql(sql_file: str, fallback_sql: str) -> str:
    if not sql_file:
        return _strip_sql_terminator(fallback_sql)
    path = _resolve_external_path(sql_file)
    if not path.exists():
        raise FileNotFoundError(f"TSNE_SQL_FILE was not found: {path}")
    sql = _strip_sql_terminator(path.read_text(encoding="utf-8-sig"))
    if not sql:
        raise ValueError(f"TSNE_SQL_FILE is empty: {path}")
    return sql


def _strip_sql_terminator(sql: str) -> str:
    value = sql.strip()
    return value[:-1].strip() if value.endswith(";") else value


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
