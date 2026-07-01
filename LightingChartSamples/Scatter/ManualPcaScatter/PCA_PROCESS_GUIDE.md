# 수동 PCA Scatter 처리 로직 설명

이 문서는 `ManualPcaScatter`가 회사 DataTable 데이터를 받아 PCA Scatter 차트와 KNN 유사 Draft 그리드를 만드는 전체 과정을 설명한다.

## 1. 입력 데이터

화면 또는 팝업 호출부는 다음 컬럼을 가진 `DataTable`을 전달한다.

- `DRAFT_NO`: Draft 식별자. 검색과 클릭 이벤트의 기준이다.
- `PARAM_TYP`: `RESPONSE`, `DEFECT` 같은 분석 타입이다.
- `ENGR_RSLT_VAL`: Pass, Review, Fail 같은 Y 라벨이다. Scatter 시리즈 그룹으로 사용한다.
- `CONV_EXPER_CTN`: JSON 배열 문자열이다. 내부 실험 데이터 객체가 들어 있다.

예시:

```json
[
  {
    "PUB_NO": "DRAFT-001",
    "_VERSION_NM": "V1",
    "TEMPERATURE": 31.5,
    "PRESSURE": 12.8,
    "SPEED": 210,
    "VOLTAGE": 3.2
  }
]
```

이 예시에서 `PUB_NO`, `_VERSION_NM`은 식별/메타데이터라 PCA feature에서 제외된다. `TEMPERATURE`, `PRESSURE`, `SPEED`, `VOLTAGE`는 숫자라 PCA 후보 feature가 된다.

## 2. JSON 파싱과 펼치기

`ConvExperimentRowParser`가 `CONV_EXPER_CTN`을 JSON 배열/객체로 읽는다.

객체가 중첩되어 있으면 key를 펼친다.

```json
{
  "EQUIP": {
    "TEMP": 31.5
  },
  "POINTS": [10, 20]
}
```

위 데이터는 내부적으로 다음처럼 바뀐다.

```text
EQUIP.TEMP = 31.5
POINTS[0] = 10
POINTS[1] = 20
```

이렇게 펼쳐야 JSON 구조가 조금 달라도 feature 이름과 값을 안정적으로 비교할 수 있다.

## 3. 수치형 feature 선택

각 Draft에서 추출한 값 중 숫자로 변환 가능한 값만 PCA 후보가 된다.

제외되는 값:

- `PUB_NO`, `_VERSION_NM`, `Draft_NO`, `AI_RSLT_Val` 같은 메타데이터
- 문자열 설명값
- 숫자로 변환할 수 없는 값
- `NaN`, `Infinity`
- 대부분의 row에서 누락된 값
- 모든 row에서 거의 같은 상수값

기본 설정은 `MinimumNumericFeatureCoverageRatio = 0.90`이다. 즉 전체 row 중 90% 이상에서 숫자로 읽힌 feature만 포함한다. 일부 누락값은 평균값으로 채울 수 있다.

## 4. 입력 수치행렬 생성

살아남은 feature로 행렬을 만든다.

```text
행(row) = Draft 한 건
열(column) = 선택된 수치 feature 한 개
값(value) = 해당 Draft의 원본 수치값
```

예시:

```text
DRAFT_NO     TEMPERATURE   PRESSURE   SPEED
DRAFT-001    31.5          12.8       210
DRAFT-002    29.1          13.2       205
DRAFT-003    35.0          11.7       230
```

이 행렬이 PCA의 원본 입력이다.

## 5. 정규화

feature마다 단위와 범위가 다르다. 예를 들어 `SPEED`는 200대이고 `VOLTAGE`는 3대일 수 있다. 원본값 그대로 거리와 PCA를 계산하면 값의 단위가 큰 feature가 결과를 과하게 지배한다.

그래서 `StandardScalerModel`이 feature별 평균과 표준편차를 계산한다.

정규화는 다음 의미다.

```text
정규화값 = (현재 Draft의 원래 값 - 전체 Draft 평균) / 전체 Draft 표준편차
```

예시:

```text
TEMPERATURE 평균 = 30
TEMPERATURE 표준편차 = 2
DRAFT-001 TEMPERATURE 원래 값 = 31.5
정규화값 = (31.5 - 30) / 2 = 0.75
```

PCA와 KNN은 원본값이 아니라 이 정규화값 행렬을 사용한다. 그래서 PCA 시각화와 KNN 거리 계산은 같은 좌표계를 공유한다.

## 6. PCA로 X1, X2 만들기

PCA는 많은 feature를 2개 축으로 줄여서 보여주는 방법이다.

중요한 점:

- X1은 어떤 feature 하나가 아니다.
- X2도 어떤 feature 하나가 아니다.
- X1은 모든 feature의 정규화값에 PC1 가중치를 곱해서 더한 값이다.
- X2는 모든 feature의 정규화값에 PC2 가중치를 곱해서 더한 값이다.

개념적으로는 다음과 같다.

```text
X1 = FEATURE_1_정규화값 * PC1_WEIGHT_1
   + FEATURE_2_정규화값 * PC1_WEIGHT_2
   + ...

X2 = FEATURE_1_정규화값 * PC2_WEIGHT_1
   + FEATURE_2_정규화값 * PC2_WEIGHT_2
   + ...
```

PC1은 전체 데이터가 가장 크게 퍼지는 방향이다. PC2는 PC1과 겹치지 않는 다음 큰 변화 방향이다.

## 7. X/Y축 범위가 정해지는 방식

차트의 X축은 모든 Draft의 X1 최소값과 최대값으로 계산한다.

차트의 Y축은 모든 Draft의 X2 최소값과 최대값으로 계산한다.

옵션에 따라 0을 축 범위에 포함하고, 보기 좋게 padding을 더한다.

즉 축 범위는 특정 한두 Draft가 가진 가장 작은 X1, 가장 큰 X1, 가장 작은 X2, 가장 큰 X2 값에 영향을 받는다. 로그 파일에는 축 범위를 만든 Draft 번호와 padding 계산값이 함께 기록된다.

## 8. Distance 계산

그리드의 `Distance`는 차트의 X1/X2 거리만 의미하지 않는다.

현재 구현의 Distance는 정규화된 전체 feature 벡터의 유클리드 거리다.

```text
Distance = sqrt(
  (대상 FEATURE_1 정규화값 - 비교 FEATURE_1 정규화값)^2
  + (대상 FEATURE_2 정규화값 - 비교 FEATURE_2 정규화값)^2
  + ...
)
```

feature가 80개라면 80개 차이를 모두 제곱해서 더한다. 그래서 다른 시스템에서 0.0079처럼 작은 값이 나오고 여기서는 32처럼 큰 값이 나올 수 있다. 두 값은 거리 정의가 다를 가능성이 높다.

로그에는 다음 값이 함께 기록된다.

- `Distance`: 전체 feature 기준 거리
- `DistanceSquared`: 제곱합
- `RmsPerFeature`: `Distance / sqrt(featureCount)`로 feature 수 영향을 줄여 본 참고값
- `Pca2DChartDistance`: 화면에 보이는 X1/X2만 사용한 2D 거리
- feature별 거리 기여도

## 9. 최신 분석 로그 파일

조회 또는 차트 갱신 때마다 다음 파일 하나가 덮어써진다.

```text
%LOCALAPPDATA%\SKhynix\TAS\PcaScatter\AnalysisLogs\manual_pca_latest_analysis.log
```

여러 로그 파일을 만들지 않는다. 화면의 `로그 열기` 버튼은 항상 이 최신 파일을 연다.

로그에서 볼 수 있는 내용:

- 선택된 PARAM_TYP
- row 수, feature 수, 제외 feature 수
- 제외 사유 요약
- 포함된 feature 목록
- 정규화 전 원래값, 평균, 표준편차, 정규화값
- PC1/PC2 가중치와 X1/X2 기여도
- X/Y축 범위 산정 근거
- Distance 계산식과 가까운 Draft별 거리
- 가장 가까운 Draft와의 feature별 거리 기여도
