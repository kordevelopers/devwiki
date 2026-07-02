from __future__ import annotations

import json
import math
import random


def build_sample_rows(row_count: int = 80, feature_count: int = 80) -> list[dict[str, str]]:
    random.seed(20260622)
    rows: list[dict[str, str]] = []
    for index in range(row_count):
        label = "Review" if index % 5 in (0, 3) else "Pass"
        param_type = "RESPONSE" if index < row_count // 2 else "DEFECT"
        phase = index / 6.0
        experiment: dict[str, object] = {
            "PUB_NO": f"PUB-{index + 1:03d}",
            "_VERSION_NM": "sample",
            "CONST_ONE": 1,
        }
        for feature_index in range(feature_count):
            base = math.sin(phase + feature_index * 0.17) * 8.0
            trend = (index % 11) * 0.18
            label_shift = 2.5 if label == "Review" and feature_index % 7 == 0 else 0.0
            noise = random.gauss(0.0, 0.55)
            experiment[f"Feature_{feature_index + 1:03d}"] = round(
                base + trend + label_shift + noise,
                6,
            )
        rows.append(
            {
                "DRAFT_NO": f"DRAFT-{index + 1:03d}",
                "PARAM_TYP": param_type,
                "LABEL_Y": label,
                "CONV_EXPER_CTN": json.dumps([experiment], ensure_ascii=False),
            }
        )
    return rows
