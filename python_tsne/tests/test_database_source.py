from __future__ import annotations

import unittest
from unittest import mock

import pandas as pd

from tsne_runner.config import AppConfig
from tsne_runner.source import load_source_rows


class DatabaseSourceTests(unittest.TestCase):
    def test_jdbc_style_values_are_used_by_oracledb(self) -> None:
        config = AppConfig(
            param_type="RESPONSE",
            target_draft_no="",
            sql="SELECT 1 FROM DUAL",
            sql_file="",
            host="db.example.test",
            database="ORCL",
            port="1521",
            username="test_user",
            password="test_password",
        )
        engine = mock.MagicMock()
        raw = pd.DataFrame({"DRAFT_NO": ["001"]})
        with (
            mock.patch("sqlalchemy.create_engine", return_value=engine) as factory,
            mock.patch("pandas.read_sql_query", return_value=raw),
        ):
            result = load_source_rows(config)

        factory.assert_called_once_with(
            "oracle+oracledb://",
            connect_args={
                "user": "test_user",
                "password": "test_password",
                "dsn": "db.example.test:1521/ORCL",
            },
            poolclass=mock.ANY,
        )
        self.assertEqual(["001"], result["DRAFT_NO"].tolist())
        engine.dispose.assert_called_once_with()


if __name__ == "__main__":
    unittest.main()
