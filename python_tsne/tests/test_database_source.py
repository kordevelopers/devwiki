from __future__ import annotations

from dataclasses import replace
import unittest
from unittest import mock

import pandas as pd

from tsne_runner.config import AppConfig
from tsne_runner.source import load_source_rows, normalize_source_columns


class DatabaseSourceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.config = AppConfig(
            mode="odbc",
            param_type="RESPONSE",
            target_draft_no="",
            sql="SELECT DRAFT_NO, PARAM_TYP, ENGR_RSLT_VAL, CONV_EXPER_CTN FROM test_rows",
            sql_file="",
            oracle_host="",
            oracle_port="1521",
            oracle_service_name="",
            oracle_sid="",
            odbc_dsn="test_dsn",
            odbc_driver="",
            odbc_user="test_user",
            odbc_password="test_password",
            odbc_connection_string="",
            oracle_user="test_user",
            oracle_password="test_password",
            oracle_dsn="test_dsn",
        )
        self.columns = ["DRAFT_NO", "PARAM_TYP", "ENGR_RSLT_VAL", "CONV_EXPER_CTN"]
        self.json_text = '{"Feature_A": 1.5, "Feature_B": 2.5}'

    def test_odbc_returns_dataframe_and_reads_lob_before_closing(self) -> None:
        for has_rows in (True, False):
            with self.subTest(has_rows=has_rows):
                connection = mock.Mock()
                cursor = connection.cursor.return_value
                cursor.description = [(name,) for name in self.columns]
                lob = mock.Mock()

                def read_lob() -> str:
                    connection.close.assert_not_called()
                    cursor.close.assert_not_called()
                    return self.json_text

                lob.read.side_effect = read_lob
                cursor.fetchall.return_value = (
                    [("001", "RESPONSE", None, lob)] if has_rows else []
                )
                with mock.patch("pyodbc.connect", return_value=connection):
                    frame = load_source_rows(self.config)

                expected = pd.DataFrame(
                    [("001", "RESPONSE", None, self.json_text)] if has_rows else [],
                    columns=self.columns,
                )
                pd.testing.assert_frame_equal(expected, frame)
                connection.close.assert_called_once_with()
                cursor.close.assert_called_once_with()
                cursor.execute.assert_called_once_with(self.config.sql)
                if has_rows:
                    lob.read.assert_called_once_with()
                    normalized = normalize_source_columns(frame)
                    self.assertEqual("001", normalized.iloc[0]["DRAFT_NO"])
                    self.assertEqual("", normalized.iloc[0]["LABEL_Y"])

    def test_odbc_closes_resources_when_query_or_read_fails(self) -> None:
        for failure in ("execute", "fetchall", "lob", "no_result"):
            with self.subTest(failure=failure):
                connection = mock.Mock()
                cursor = connection.cursor.return_value
                cursor.description = [(name,) for name in self.columns]
                cursor.fetchall.return_value = []
                expected_error = RuntimeError
                if failure in ("execute", "fetchall"):
                    getattr(cursor, failure).side_effect = RuntimeError("Query failed")
                elif failure == "lob":
                    lob = mock.Mock()
                    lob.read.side_effect = RuntimeError("LOB read failed")
                    cursor.fetchall.return_value = [("001", "RESPONSE", "Pass", lob)]
                else:
                    cursor.description = None
                    expected_error = ValueError

                with mock.patch("pyodbc.connect", return_value=connection):
                    with self.assertRaises(expected_error):
                        load_source_rows(self.config)
                cursor.close.assert_called_once_with()
                connection.close.assert_called_once_with()

    def test_oracle_returns_dataframe_and_reads_lob_before_closing(self) -> None:
        for has_rows in (True, False):
            with self.subTest(has_rows=has_rows):
                engine = mock.MagicMock()
                context = engine.connect.return_value
                lob = mock.Mock()

                def read_lob() -> str:
                    context.__exit__.assert_not_called()
                    engine.dispose.assert_not_called()
                    return self.json_text

                lob.read.side_effect = read_lob
                raw = pd.DataFrame(
                    [("001", "RESPONSE", "Pass", lob)] if has_rows else [],
                    columns=self.columns,
                )
                with (
                    mock.patch("sqlalchemy.create_engine", return_value=engine),
                    mock.patch("pandas.read_sql_query", return_value=raw) as query,
                ):
                    frame = load_source_rows(replace(self.config, mode="oracledb"))

                expected = pd.DataFrame(
                    [("001", "RESPONSE", "Pass", self.json_text)] if has_rows else [],
                    columns=self.columns,
                )
                pd.testing.assert_frame_equal(expected, frame)
                self.assertEqual(self.config.sql, str(query.call_args.args[0]))
                self.assertIs(context.__enter__.return_value, query.call_args.args[1])
                context.__exit__.assert_called_once()
                engine.dispose.assert_called_once_with()

    def test_oracle_disposes_engine_when_query_fails(self) -> None:
        engine = mock.MagicMock()
        with (
            mock.patch("sqlalchemy.create_engine", return_value=engine),
            mock.patch("pandas.read_sql_query", side_effect=RuntimeError("Query failed")),
        ):
            with self.assertRaisesRegex(RuntimeError, "Query failed"):
                load_source_rows(replace(self.config, mode="oracledb"))
        engine.connect.return_value.__exit__.assert_called_once()
        engine.dispose.assert_called_once_with()


if __name__ == "__main__":
    unittest.main()
