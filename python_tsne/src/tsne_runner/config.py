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
    param_type: str
    target_draft_no: str
    sql: str
    sql_file: str
    host: str
    database: str
    port: str
    username: str
    password: str


def load_config(
    target_override: str | None = None,
    param_type_override: str | None = None,
    resolve_sql: bool = True,
) -> AppConfig:
    _load_dotenv_files()
    target = (
        target_override
        if target_override is not None
        else os.environ.get("TSNE_TARGET_DRAFT_NO", "")
    )
    param_type = (
        param_type_override
        if param_type_override is not None
        else os.environ.get("TSNE_PARAM_TYP", "RESPONSE")
    )
    # t-SNE SQL is read only from this project's TSNE_SQL_FILE setting.
    sql_file = os.environ.get("TSNE_SQL_FILE", "queries/exadata_tsne.sql").strip()
    fallback_sql = os.environ.get("TSNE_SQL", DEFAULT_SQL)
    return AppConfig(
        param_type=param_type.strip().upper(),
        target_draft_no=target.strip(),
        sql=(
            _load_sql(sql_file, fallback_sql)
            if resolve_sql
            else _strip_sql_terminator(fallback_sql)
        ),
        sql_file=sql_file,
        host=os.environ.get("TSNE_DB_HOST", "127.0.0.1").strip(),
        database=os.environ.get("TSNE_DB_DATABASE", "ORCL").strip(),
        port=os.environ.get("TSNE_DB_PORT", "1521").strip(),
        username=os.environ.get("TSNE_DB_USERNAME", "test_user").strip(),
        password=os.environ.get("TSNE_DB_PASSWORD", "test_password"),
    )


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
    application_dir = _application_dir()
    candidates = [
        Path.cwd() / ".env",
        application_dir / ".env",
    ]
    loaded_paths: set[Path] = set()
    for env_path in candidates:
        resolved_path = env_path.resolve()
        if resolved_path in loaded_paths or not resolved_path.exists():
            continue
        load_dotenv(resolved_path, override=False)
        loaded_paths.add(resolved_path)


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
