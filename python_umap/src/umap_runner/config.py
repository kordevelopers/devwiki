from dataclasses import dataclass
import os
from pathlib import Path
import sys
from dotenv import load_dotenv

DEFAULT_SQL = ("SELECT M.DRAFT_NO, M.PARAM_TYP, J.ENGR_RSLT_VAL AS LABEL_Y, J.RSLT_CD, "
               "M.CONV_EXPER_CTN FROM TASADM.PCCB_INFER_RSLT_INF M "
               "JOIN TASADM.PCCB_JUDGE_RSLT_INF J ON M.DRAFT_NO = J.DRAFT_NO "
               "AND M.PARAM_TYP = J.PARAM_TYP WHERE M.CHG_TM > SYSDATE - 10 "
               "AND J.ENGR_RSLT_VAL IS NOT NULL AND M.CONV_EXPER_CTN IS NOT NULL "
               "ORDER BY M.DRAFT_NO, M.PARAM_TYP")

@dataclass(frozen=True)
class AppConfig:
    data_type: str
    target_draft_no: str
    sql: str
    sql_file: str
    host: str
    database: str
    port: str
    username: str
    password: str

def load_config(target_override=None, data_type_override=None, resolve_sql=True):
    for path in (Path.cwd() / ".env", _application_dir() / ".env"):
        if path.exists():
            load_dotenv(path, override=False)
    data_type = data_type_override or os.getenv("UMAP_DATA_TYPE", "RESPONSE")
    sql_file = os.getenv("UMAP_SQL_FILE", "").strip()
    fallback = os.getenv("UMAP_SQL", DEFAULT_SQL)
    sql = _load_sql(sql_file, fallback) if resolve_sql else fallback.rstrip().rstrip(";").strip()
    return AppConfig(data_type.strip().upper(),
                     (target_override if target_override is not None else os.getenv("UMAP_TARGET_DRAFT_NO", "")).strip(),
                     sql, sql_file, os.getenv("UMAP_DB_HOST", "127.0.0.1").strip(),
                     os.getenv("UMAP_DB_DATABASE", "ORCL").strip(), os.getenv("UMAP_DB_PORT", "1521").strip(),
                     os.getenv("UMAP_DB_USERNAME", "test_user").strip(), os.getenv("UMAP_DB_PASSWORD", "test_password"))

def _load_sql(sql_file, fallback):
    if not sql_file:
        return fallback.rstrip().rstrip(";").strip()
    path = Path(sql_file)
    if not path.is_absolute():
        path = Path.cwd() / path if (Path.cwd() / path).exists() else _application_dir() / path
    if not path.exists():
        raise FileNotFoundError(f"UMAP_SQL_FILE was not found: {path}")
    return path.read_text(encoding="utf-8-sig").rstrip().rstrip(";").strip()

def _application_dir():
    return Path(sys.executable).resolve().parent if getattr(sys, "frozen", False) else Path.cwd()
