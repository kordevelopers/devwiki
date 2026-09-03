from __future__ import annotations

import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest import mock

import numpy as np
import pandas as pd

from tsne_runner.analysis import find_neighbors, resolve_perplexity, run_tsne
from tsne_runner.chart import save_scatter
from tsne_runner.main import main
from tsne_runner.source import build_feature_frame, load_source_csv, normalize_source_columns


def build_source_rows(row_count: int = 20) -> pd.DataFrame:
    rows: list[dict[str, object]] = []
    for index in range(row_count):
        experiment: dict[str, object] = {
            "Feature_B": float((index * 3) % 11),
            "Feature_A": float(index),
            "Nested": {"Feature_C": float(index % 5)},
            "Constant": 1.0,
            "Sparse": float(index) if index < row_count - 3 else None,
            "BooleanValue": index % 2 == 0,
            "PUB_NO": f"PUB-{index + 1:03d}",
        }
        if index == row_count - 1:
            experiment["Nested"] = {}
        rows.append(
            {
                "draft_no": f"DRAFT-{index + 1:03d}",
                "param_typ": "RESPONSE",
                "engr_rslt_val": "Review" if index % 4 == 0 else "Pass",
                "rslt_cd": "R" if index % 4 == 0 else "P",
                "conv_exper_ctn": json.dumps([experiment]),
            }
        )
    return pd.DataFrame(rows)


class SourceTests(unittest.TestCase):
    def test_source_contract_and_json_flattening(self) -> None:
        source = normalize_source_columns(build_source_rows())
        features = build_feature_frame(source, "RESPONSE")

        self.assertEqual(20, len(features))
        self.assertIn("Feature_A", features.columns)
        self.assertIn("Feature_B", features.columns)
        self.assertIn("Nested.Feature_C", features.columns)
        self.assertIn("BooleanValue", features.columns)
        self.assertIn("PUB_NO", features.columns)
        self.assertEqual("DRAFT-001", features.iloc[0]["DRAFT_NO"])

    def test_case_insensitive_duplicate_draft_is_rejected(self) -> None:
        source = normalize_source_columns(build_source_rows(3))
        source.loc[1, "DRAFT_NO"] = source.loc[0, "DRAFT_NO"].lower()
        with self.assertRaisesRegex(ValueError, "Duplicated DRAFT_NO"):
            build_feature_frame(source, "RESPONSE")

    def test_csv_preserves_identifiers_and_blank_json(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            source_path = Path(temporary_directory) / "source.csv"
            source_path.write_text(
                "DRAFT_NO,PARAM_TYP,LABEL_Y,CONV_EXPER_CTN\n"
                '001,RESPONSE,Pass,""\n',
                encoding="utf-8",
            )
            loaded = load_source_csv(source_path)
            self.assertEqual("001", loaded.iloc[0]["DRAFT_NO"])
            self.assertEqual("", loaded.iloc[0]["CONV_EXPER_CTN"])

    def test_database_null_json_is_normalized_to_blank(self) -> None:
        source = build_source_rows(3)
        source.loc[0, "conv_exper_ctn"] = pd.NA
        normalized = normalize_source_columns(source)
        self.assertEqual("", normalized.iloc[0]["CONV_EXPER_CTN"])


class AnalysisTests(unittest.TestCase):
    def setUp(self) -> None:
        source = normalize_source_columns(build_source_rows())
        self.features = build_feature_frame(source, "RESPONSE")

    def test_perplexity_boundaries(self) -> None:
        expected = {3: 1.0, 6: 1.0, 7: 2.0, 40: 13.0, 90: 29.0, 91: 30.0, 100: 30.0}
        for row_count, value in expected.items():
            with self.subTest(row_count=row_count):
                self.assertEqual(value, resolve_perplexity(row_count))
                self.assertLess(value, row_count)

    def test_tsne_preprocessing_outputs_and_determinism(self) -> None:
        first = run_tsne(self.features)
        second = run_tsne(self.features)

        self.assertEqual(
            ["Feature_A", "Feature_B", "Nested.Feature_C"],
            first.features,
        )
        self.assertIn("Constant", first.excluded_features)
        self.assertIn("Sparse", first.excluded_features)
        self.assertIn("BooleanValue", first.excluded_features)
        self.assertIn("PUB_NO", first.excluded_features)
        self.assertEqual(1, first.diagnostic["ImputedValueCount"])
        self.assertEqual(6.0, first.diagnostic["Perplexity"])
        self.assertEqual(2, first.points[["X1", "X2"]].shape[1])
        self.assertTrue(np.isfinite(first.points[["X1", "X2"]].to_numpy()).all())
        np.testing.assert_array_equal(
            first.points[["X1", "X2"]].to_numpy(),
            second.points[["X1", "X2"]].to_numpy(),
        )

        neighbors = find_neighbors(first, "DRAFT-001", count=3)
        self.assertEqual(3, len(neighbors))
        self.assertNotIn("DRAFT-001", [row.similar_draft for row in neighbors])

    def test_invalid_variance_threshold_is_rejected(self) -> None:
        for threshold in (-1.0, float("nan"), float("inf")):
            with self.subTest(threshold=threshold):
                with self.assertRaisesRegex(ValueError, "variance_threshold"):
                    run_tsne(self.features, variance_threshold=threshold)

    def test_chart_and_cli_outputs(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
            source_path = root / "pccb_export.csv"
            output_dir = root / "outputs"
            chart_path = root / "direct_chart.png"
            build_source_rows().to_csv(source_path, index=False, encoding="utf-8-sig")

            result = run_tsne(self.features)
            save_scatter(
                result.points,
                chart_path,
                "DRAFT-001",
                show_chart=False,
                standardized_matrix=result.standardized_matrix,
            )
            self.assertGreater(chart_path.stat().st_size, 10_000)

            arguments = [
                "tsne_runner",
                "--source-csv",
                str(source_path),
                "--output-dir",
                str(output_dir),
                "--no-show-chart",
            ]
            with mock.patch.object(sys, "argv", arguments):
                self.assertEqual(0, main())

            expected_files = {
                "tsne_points.csv",
                "knn_neighbors.csv",
                "tsne_scatter.png",
                "feature_selection_audit.csv",
                "surviving_population.csv",
                "diagnostic.json",
            }
            self.assertEqual(expected_files, {path.name for path in output_dir.iterdir()})
            diagnostic = json.loads((output_dir / "diagnostic.json").read_text(encoding="utf-8"))
            self.assertEqual("TSNE", diagnostic["ProjectionMethod"])
            self.assertEqual("csv", diagnostic["SourceMode"])


if __name__ == "__main__":
    unittest.main()
