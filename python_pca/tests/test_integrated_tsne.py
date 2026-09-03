from __future__ import annotations

import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

import numpy as np
import pandas as pd


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SOURCE_ROOT = PROJECT_ROOT / "src"
if str(SOURCE_ROOT) not in sys.path:
    sys.path.insert(0, str(SOURCE_ROOT))
os.environ.setdefault("MPLBACKEND", "Agg")

from pca_runner.pca_pipeline import run_pca
from pca_runner.plot import save_scatter
from pca_runner.tsne_main import (
    _order_feature_rows,
    _validate_source_identifiers,
    main as tsne_main,
)
from pca_runner.tsne_pipeline import find_neighbors, resolve_perplexity, run_tsne


def _feature_frame(row_count: int = 20) -> pd.DataFrame:
    rows: list[dict[str, object]] = []
    for index in range(row_count):
        rows.append(
            {
                "DRAFT_NO": f"DRAFT-{index + 1:03d}",
                "PARAM_TYP": "RESPONSE",
                "LABEL_Y": "PASS" if index % 4 else "REVIEW",
                "RSLT_CD": "00",
                "FEATURE_A": float(np.sin(index / 3.0) + index * 0.02),
                "FEATURE_B": float(np.cos(index / 4.0) - index * 0.01),
                "FEATURE_C": float((index * 7) % 13),
                "FEATURE_SPARSE": None if index == 0 else float(index * 0.25),
                "FEATURE_CONSTANT": 1.0,
            }
        )
    return pd.DataFrame(rows)


def _source_frame(row_count: int = 20) -> pd.DataFrame:
    features = _feature_frame(row_count)
    rows: list[dict[str, object]] = []
    feature_names = [name for name in features.columns if name.startswith("FEATURE_")]
    for _, row in features.iterrows():
        payload = {
            name: row[name]
            for name in feature_names
            if not pd.isna(row[name])
        }
        rows.append(
            {
                "DRAFT_NO": row["DRAFT_NO"],
                "PARAM_TYP": row["PARAM_TYP"],
                "ENGR_RSLT_VAL": row["LABEL_Y"],
                "RSLT_CD": row["RSLT_CD"],
                "CONV_EXPER_CTN": json.dumps(payload),
            }
        )
    return pd.DataFrame(rows)


class IntegratedTsneTests(unittest.TestCase):
    def test_tsne_orders_drafts_before_projection(self) -> None:
        shuffled = _feature_frame().sample(frac=1.0, random_state=17)
        ordered = _order_feature_rows(shuffled)
        self.assertEqual(
            ordered["DRAFT_NO"].tolist(),
            sorted(shuffled["DRAFT_NO"].tolist(), key=str.casefold),
        )

    def test_perplexity_boundaries(self) -> None:
        expected = {3: 1.0, 6: 1.0, 7: 2.0, 40: 13.0, 90: 29.0, 91: 30.0}
        for row_count, value in expected.items():
            with self.subTest(row_count=row_count):
                self.assertEqual(resolve_perplexity(row_count), value)

    def test_requested_settings_knn_and_determinism(self) -> None:
        first = run_tsne(_feature_frame())
        second = run_tsne(_feature_frame())

        self.assertTrue(np.array_equal(first.points[["X1", "X2"]], second.points[["X1", "X2"]]))
        self.assertEqual(first.tsne.n_components, 2)
        self.assertEqual(first.tsne.perplexity, 6.0)
        self.assertEqual(first.tsne.max_iter, 1000)
        self.assertEqual(first.tsne.random_state, 42)
        self.assertEqual(first.tsne.init, "pca")
        self.assertEqual(first.tsne.learning_rate, "auto")
        self.assertEqual(first.tsne.metric, "euclidean")
        self.assertEqual(first.tsne.method, "barnes_hut")
        self.assertEqual(first.tsne.angle, 0.5)
        self.assertEqual(first.tsne.early_exaggeration, 12.0)
        self.assertEqual(first.tsne.n_iter_without_progress, 300)
        self.assertEqual(first.tsne.min_grad_norm, 1e-7)
        self.assertEqual(first.tsne.n_jobs, 1)
        self.assertEqual(first.nearest_neighbors.n_neighbors, 15)
        self.assertEqual(first.nearest_neighbors.algorithm, "auto")
        self.assertEqual(first.nearest_neighbors.n_jobs, 1)
        pca = run_pca(_feature_frame())
        self.assertEqual(first.features, pca.features)
        self.assertTrue(np.array_equal(first.standardized_matrix, pca.standardized_matrix))
        self.assertIn("FEATURE_SPARSE", first.excluded_features)
        self.assertIn("FEATURE_CONSTANT", first.excluded_features)
        self.assertEqual(first.diagnostic["ImputedValueCount"], 0)
        self.assertEqual(first.diagnostic["PreprocessingMethod"], "PCA_SHARED")

        neighbors = find_neighbors(first, "draft-001")
        self.assertEqual(len(neighbors), 3)
        self.assertNotIn("DRAFT-001", [row.similar_draft for row in neighbors])

    def test_cli_writes_isolated_tsne_outputs(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            temporary_path = Path(temporary_directory)
            source_path = temporary_path / "pccb.csv"
            output_path = temporary_path / "tsne"
            _source_frame().to_csv(source_path, index=False, encoding="utf-8-sig")

            arguments = [
                "tsne_main.py",
                "--source-csv",
                str(source_path),
                "--param-type",
                "RESPONSE",
                "--target",
                "DRAFT-001",
                "--output-dir",
                str(output_path),
                "--no-show-chart",
            ]
            with patch.dict(os.environ, {"PCA_SQL_FILE": "missing-query.sql"}, clear=False):
                with patch.object(sys, "argv", arguments):
                    self.assertEqual(tsne_main(), 0)

            expected_files = {
                "tsne_points.csv",
                "knn_neighbors.csv",
                "tsne_scatter.png",
                "feature_selection_audit.csv",
                "surviving_population.csv",
                "diagnostic.json",
            }
            self.assertEqual({path.name for path in output_path.iterdir()}, expected_files)
            diagnostic = json.loads((output_path / "diagnostic.json").read_text(encoding="utf-8"))
            self.assertEqual(diagnostic["ProjectionMethod"], "TSNE")
            self.assertEqual(diagnostic["SourceMode"], "csv")
            self.assertEqual(diagnostic["SourceRowCount"], 20)

    def test_tsne_rejects_the_pca_sample_mode(self) -> None:
        environment = {"PCA_DB_MODE": "sample", "PCA_TARGET_DRAFT_NO": ""}
        with patch.dict(os.environ, environment, clear=False):
            with patch.object(sys, "argv", ["tsne_main.py"]):
                with self.assertRaisesRegex(ValueError, "does not use generated sample data"):
                    tsne_main()

    def test_empty_and_case_insensitive_duplicate_drafts_are_rejected(self) -> None:
        empty = pd.DataFrame(
            {"DRAFT_NO": [""], "PARAM_TYP": ["RESPONSE"]}
        )
        with self.assertRaisesRegex(ValueError, "empty DRAFT_NO"):
            _validate_source_identifiers(empty, "RESPONSE")

        duplicate = pd.DataFrame(
            {
                "DRAFT_NO": ["DRAFT-001", "draft-001"],
                "PARAM_TYP": ["RESPONSE", "RESPONSE"],
            }
        )
        with self.assertRaisesRegex(ValueError, "without regard to case"):
            _validate_source_identifiers(duplicate, "RESPONSE")

    def test_existing_pca_pipeline_and_default_chart_still_work(self) -> None:
        result = run_pca(_feature_frame().drop(columns=["FEATURE_SPARSE"]))
        self.assertEqual(result.points.shape, (20, 6))
        with tempfile.TemporaryDirectory() as temporary_directory:
            chart_path = Path(temporary_directory) / "pca.png"
            save_scatter(
                result.points,
                chart_path,
                "DRAFT-001",
                show_chart=False,
                standardized_matrix=result.standardized_matrix,
            )
            self.assertTrue(chart_path.exists())


if __name__ == "__main__":
    unittest.main()
