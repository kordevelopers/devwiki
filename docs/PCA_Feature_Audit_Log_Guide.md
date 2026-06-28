# PCA Feature Audit 로그 설명

이 문서는 `ScatterMain`에서 PCA 분석 후 생성되는 `PCA Feature Selection Audit` 로그를 해석하기 위한 개발자용 가이드입니다.

## 로그 위치

분석을 실행하면 개발자 확인용 상세 로그가 아래 경로에 저장됩니다.

```text
%LOCALAPPDATA%\SKhynix\TAS\PcaScatter\AnalysisLogs\yyyyMMdd\pca_feature_audit_{PARAM_TYP}_{timestamp}_{guid}.log
```

`DEBUG` 빌드에서는 요약 팝업도 함께 표시됩니다. 운영 사용자에게는 팝업이 노출되지 않도록 `#if DEBUG` 조건으로 분리했습니다.

## DIAG 라인

예시:

```text
DIAG R=30 F=80 X=2 M=0 PC1=69.3 PC2=26.4 SUM=95.6 SHAPE=OK KNN=BruteForce
```

| 항목 | 의미 | 확인 포인트 |
| --- | --- | --- |
| `R` | PCA 분석에 사용된 row 수 | 모집단 데이터가 예상 건수만큼 들어왔는지 확인 |
| `F` | PCA에 실제 사용된 수치 feature 수 | JSON 내부 수치 컬럼이 얼마나 살아남았는지 확인 |
| `X` | 제외된 feature 수 | 제외된 컬럼이 너무 많으면 원인 확인 필요 |
| `M` | 실험 JSON 누락/파싱 실패 row 수 | 0보다 크면 `CONV_EXPER_CTN` 누락 또는 JSON 오류 의심 |
| `PC1` | 첫 번째 주성분 설명력 | 값이 과도하게 높으면 데이터가 한 방향으로 몰렸을 수 있음 |
| `PC2` | 두 번째 주성분 설명력 | PC2가 너무 낮으면 차트가 선처럼 보일 수 있음 |
| `SUM` | PC1 + PC2 설명력 | 2차원 차트가 원본 변동을 얼마나 설명하는지 판단 |
| `SHAPE` | 분포 진단 코드 | `OK`가 아니면 데이터 분포/feature 품질 확인 |
| `KNN` | 최근접 Draft 검색 알고리즘 | `Auto` 옵션이 실제 선택한 알고리즘 확인 |

## KNN 알고리즘

최근접 Draft 3건을 찾는 단계는 PCA가 끝난 뒤 표준화된 원본 feature 공간에서 수행됩니다.
옵션 기본값은 `Auto`입니다.

| 알고리즘 | 의미 | 권장 상황 |
| --- | --- | --- |
| `Auto` | row 수와 feature 차원 수에 따라 자동 선택 | 기본값 |
| `BruteForce` | 모든 row와 거리를 직접 계산 | 고차원 또는 10,000건 이하 데이터 |
| `KdTree` | KD-tree 인덱스로 검색 | 10차원 이하, row 수가 많은 데이터 |
| `BallTree` | Ball-tree 인덱스로 검색 | 30차원 이하, row 수가 많은 데이터 |

자동 선택 기준은 다음과 같습니다.

```text
row <= 10,000              -> BruteForce
row > 10,000, feature <= 10 -> KdTree
row > 10,000, feature <= 30 -> BallTree
feature > 30               -> BruteForce
```

상세 로그에는 `KNN algorithm`, `KNN reason` 항목이 같이 기록됩니다.
예를 들어 `Auto:HighDimension Rows=2500 Dimensions=80`이면 80차원 고차원 데이터이므로 `BruteForce`가 선택된 것입니다.

## SHAPE 코드

| 코드 | 의미 |
| --- | --- |
| `OK` | 현재 기준으로 차트 분포가 정상 범위 |
| `ROWS_LT3` | PCA에 필요한 최소 row 수 부족 |
| `ROWS_LOW` | row 수가 적어 분석 안정성이 낮음 |
| `FEATURE_LT2` | PCA에 필요한 수치 feature 수 부족 |
| `FEATURE_LOW` | 수치 feature가 너무 적어 차트 해석 주의 |
| `LINE_PC1_HIGH` | PC1 설명력이 매우 높아 점들이 거의 직선으로 보일 가능성 큼 |
| `LINE_LIKELY` | PC1 중심으로 데이터가 치우쳐 선형 분포 가능성 있음 |
| `PCA2_LOW` | PC1+PC2 설명력이 낮아 2D 시각화 해석력이 낮음 |

## FEATURE_AUDIT 라인

예시:

```text
FEATURE_AUDIT ROWS=30 INCLUDED=80 EXCLUDED=2 REASONS=Metadata:2
```

| 항목 | 의미 |
| --- | --- |
| `ROWS` | feature 선택 검토 대상 row 수 |
| `INCLUDED` | PCA에 포함된 feature 수 |
| `EXCLUDED` | PCA에서 제외된 feature 수 |
| `REASONS` | 제외 사유별 집계 |

## 옵션 값

| 항목 | 의미 |
| --- | --- |
| `Numeric coverage threshold` | feature가 PCA에 포함되기 위해 숫자로 존재해야 하는 최소 비율 |
| `Mean imputation` | 일부 누락/비숫자 값을 평균값으로 보정하는 옵션 |
| `Surviving population rows` | 최종 PCA에 들어간 row 수 |
| `Surviving feature columns` | 최종 PCA에 들어간 feature 수 |

## 제외 사유

| 사유 | 의미 | 조치 |
| --- | --- | --- |
| `Metadata` | `PUB_NO`, `_VERSION_NM` 같은 식별/버전 컬럼 | 정상 제외 |
| `MissingInRows` | 일부 row에 feature 자체가 없음 | JSON 구조 차이 확인 |
| `NonNumeric` | 값이 있지만 숫자로 변환 불가 | 문자열/단위 포함 여부 확인 |
| `ConstantOrLowVariance` | 모든 값이 같거나 분산이 너무 낮음 | PCA 기여도가 낮으므로 제외 정상 |

## 상세 로그에서 볼 항목

상세 로그의 `Included feature details`와 `Excluded feature details`에는 모든 feature가 탭 구분 형식으로 기록됩니다.

| 컬럼 | 의미 |
| --- | --- |
| `FeatureName` | JSON에서 추출된 feature 이름 |
| `Included` | PCA 포함 여부 |
| `Reason` | 포함/제외 사유 |
| `Present` | 값이 존재한 row 수 |
| `Numeric` | 숫자로 변환된 row 수 |
| `Missing` | 값이 없던 row 수 |
| `NonNumeric` | 값은 있지만 숫자가 아니었던 row 수 |
| `Mean` | 평균 |
| `StdDev` | 표준편차 |
| `Variance` | 분산 |
| `Min` | 최소값 |
| `Max` | 최대값 |
| `SampleDraftNo` | 해당 feature가 발견된 샘플 Draft |

## 자주 보는 판단 기준

- `F`가 예상보다 낮다: `Excluded feature details`에서 `NonNumeric`, `MissingInRows`가 많은지 확인합니다.
- 차트가 대각선 하나처럼 보인다: `PC1`이 85% 이상이고 `PC2`가 낮은지 확인합니다.
- 실제 row 수보다 `R`이 적다: `M`과 JSON 파싱 실패 여부를 확인합니다.
- `PUB_NO`, `_VERSION_NM`이 제외된다: 메타데이터라 정상입니다.
- 소수점 자릿수가 길다: `double` 기준으로 분석하므로 문제 원인은 자릿수보다 분포, 결측, 비숫자 여부일 가능성이 큽니다.
