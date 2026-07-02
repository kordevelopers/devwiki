from __future__ import annotations

from dataclasses import dataclass
import os

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
    odbc_dsn: str
    odbc_user: str
    odbc_password: str
    odbc_connection_string: str
    oracle_user: str
    oracle_password: str
    oracle_dsn: str


def load_config(mode_override: str | None = None, target_override: str | None = None) -> AppConfig:
    load_dotenv()
    mode = mode_override or os.getenv("PCA_DB_MODE", "sample")
    target = target_override if target_override is not None else os.getenv("PCA_TARGET_DRAFT_NO", "")
    return AppConfig(
        mode=mode.strip().lower(),
        param_type=os.getenv("PCA_PARAM_TYP", "RESPONSE").strip().upper(),
        target_draft_no=target.strip(),
        sql=os.getenv("PCA_SQL", DEFAULT_SQL),
        odbc_dsn=os.getenv("PCA_ODBC_DSN", ""),
        odbc_user=os.getenv("PCA_ODBC_USER", ""),
        odbc_password=os.getenv("PCA_ODBC_PASSWORD", ""),
        odbc_connection_string=os.getenv("PCA_ODBC_CONNECTION_STRING", ""),
        oracle_user=os.getenv("PCA_ORACLE_USER", ""),
        oracle_password=os.getenv("PCA_ORACLE_PASSWORD", ""),
        oracle_dsn=os.getenv("PCA_ORACLE_DSN", ""),
    )
