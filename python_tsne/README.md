# Python t-SNE Runner

`python_tsne`는 기존 `python_pca`와 동일한 PCCB 데이터베이스, 테이블, JSON 데이터 및 차트 기능을 사용하면서 2차원 투영만 PCA에서 sklearn t-SNE로 변경한 독립 실행 프로젝트입니다. 운영 실행기에는 샘플 데이터 생성 기능이 없습니다.

## 처리 흐름

1. Oracle 또는 ODBC로 PCCB 조회 SQL을 실행합니다.
2. `PARAM_TYP`으로 분석 대상을 선택합니다.
3. `CONV_EXPER_CTN` JSON 객체를 `A.B`, `A[0]` 형태의 feature로 펼칩니다.
4. 숫자 coverage가 90% 이상이고 분산이 `1e-10`보다 큰 feature를 선택합니다.
5. 누락된 값은 해당 feature 평균으로 대체하고 `StandardScaler`로 표준화합니다.
6. sklearn t-SNE를 실행하고 전체 표준화 feature 공간에서 KNN을 계산합니다.
7. CSV, 진단 JSON, PNG를 저장한 뒤 matplotlib 차트를 표시합니다.

기본 SQL은 기존 PCA와 동일한 테이블을 조회합니다.

- `TASADM.PCCB_INFER_RSLT_INF`
- `TASADM.PCCB_JUDGE_RSLT_INF`
- 조인 키: `DRAFT_NO`, `PARAM_TYP`
- 분석 JSON: `CONV_EXPER_CTN`
- 라벨: `ENGR_RSLT_VAL`

## t-SNE 설정

```python
perplexity = float(min(30, max(5, n_samples - 1) // 3))

TSNE(
    n_components=2,
    perplexity=perplexity,
    max_iter=1000,
    random_state=42,
    init="pca",
    learning_rate="auto",
    metric="euclidean",
    method="barnes_hut",
    angle=0.5,
    early_exaggeration=12.0,
    n_iter_without_progress=300,
    min_grad_norm=1e-7,
    n_jobs=1,
)
```

KNN은 다음 설정으로 동일한 표준화 feature 행렬에 적용됩니다.

```python
NearestNeighbors(
    n_neighbors=min(15, n_samples - 1),
    metric="euclidean",
    algorithm="auto",
    n_jobs=1,
)
```

차트에서 선택한 점에는 이 KNN 결과 중 가까운 3개가 표시됩니다. t-SNE 좌표상의 거리가 아니라 전체 표준화 feature 공간의 유클리드 거리입니다.

## VS Code에서 실행

1. VS Code에서 `python_tsne` 폴더를 엽니다.
2. `.env.example`을 `.env`로 복사하고 Oracle 접속 정보를 입력합니다.
3. `Run and Debug > Run t-SNE from .env`를 실행합니다.

처음 실행하면 `Setup Python t-SNE` 작업이 Python 3.12 가상환경과 고정 버전 패키지를 설치합니다. 실행 중 표시되는 프로그램 메시지는 모두 영어입니다.

## 터미널에서 실행

```powershell
cd python_tsne
Copy-Item .env.example .env
notepad .env
powershell -ExecutionPolicy Bypass -File .\scripts\setup_python.ps1
.\.venv\Scripts\python.exe -m tsne_runner
```

차트를 열지 않고 결과 파일만 생성하려면 다음과 같이 실행합니다.

```powershell
.\.venv\Scripts\python.exe -m tsne_runner --no-show-chart
```

기존 `python_pca`의 Oracle 접속값은 재사용할 수 있습니다. `TSNE_*` 설정이 없으면 같은 이름의 `PCA_*` 설정을 읽지만, `PCA_DB_MODE=sample`은 지원하지 않습니다. PCA `.env`를 복사했다면 `TSNE_DB_MODE`을 `odbc` 또는 `oracledb`로 지정하고 `TSNE_SQL_FILE=queries/exadata_tsne.sql`을 추가해야 합니다. 두 접두사의 설정이 모두 있으면 `TSNE_*`가 우선합니다.

## PCCB CSV로 실행

DB에 접속할 수 없는 개발 PC에서는 동일 쿼리 결과를 CSV로 내보내 실행할 수 있습니다. 운영 샘플을 생성하는 기능이 아니라 실제 PCCB 조회 결과를 입력받는 경로입니다.

```powershell
.\.venv\Scripts\python.exe -m tsne_runner `
  --source-csv C:\temp\pccb_export.csv `
  --param-type RESPONSE `
  --target DRAFT-001 `
  --no-show-chart
```

입력에는 다음 컬럼이 필요합니다.

- `DRAFT_NO`
- `PARAM_TYP`
- `ENGR_RSLT_VAL` 또는 `LABEL_Y`
- `CONV_EXPER_CTN`
- `RSLT_CD`는 선택 사항입니다.

## 출력 파일

기본 출력 위치는 `outputs`입니다.

- `tsne_points.csv`: `DRAFT_NO`, `PARAM_TYP`, `LABEL_Y`, `RSLT_CD`, `X1`, `X2`
- `knn_neighbors.csv`: 선택 Draft 기준 최근접 3개
- `tsne_scatter.png`: 기존 PCA 차트와 동일한 색상·범례·강조 기능의 t-SNE 차트
- `feature_selection_audit.csv`: feature별 포함 여부와 제외 이유
- `surviving_population.csv`: 최종 feature와 t-SNE 좌표
- `diagnostic.json`: t-SNE 설정, 유효 learning rate, 실제 반복 횟수, KL divergence, KNN 설정, 런타임 버전 및 입력 행렬 해시

`max_iter=1000`은 최대 반복 횟수입니다. sklearn의 조기 종료 조건이 충족되면 실제 실행 횟수는 더 작을 수 있으며, 두 값은 `diagnostic.json`에 구분해서 저장됩니다.

## Oracle 설정

python-oracledb thin 모드는 Oracle Client 설치 없이 사용할 수 있습니다.

```env
TSNE_DB_MODE=oracledb
TSNE_ORACLE_HOST=10.0.0.10
TSNE_ORACLE_PORT=1521
TSNE_ORACLE_SERVICE_NAME=EXADATA_SERVICE
TSNE_ORACLE_USER=your_user
TSNE_ORACLE_PASSWORD=your_password
TSNE_SQL_FILE=queries/exadata_tsne.sql
TSNE_PARAM_TYP=RESPONSE
TSNE_TARGET_DRAFT_NO=
```

ODBC를 사용할 때는 `TSNE_DB_MODE=odbc`와 `TSNE_ODBC_DSN` 또는 전체 연결 문자열을 설정합니다. 접속만 확인하려면 다음을 실행합니다.

```powershell
.\.venv\Scripts\python.exe .\scripts\test_oracle_connection.py --mode oracledb
.\.venv\Scripts\python.exe .\scripts\test_oracle_connection.py --mode odbc
```

## 테스트 및 EXE 빌드

```powershell
.\.venv\Scripts\python.exe -m unittest discover -s tests -v
powershell -ExecutionPolicy Bypass -File .\scripts\build_exe.ps1 -Clean
```

EXE 결과는 `dist/HynixTasTsne`에 생성됩니다. 다른 PC에는 EXE 파일 하나가 아니라 해당 폴더 전체를 전달해야 합니다.

## 결과 해석 주의사항

- t-SNE는 새 데이터만 기존 좌표계에 추가하는 `transform()`을 제공하지 않습니다. 데이터가 변경되면 전체 모집단을 다시 계산합니다.
- 동일한 분포도 회전, 이동 또는 반전된 좌표로 표현될 수 있으므로 X/Y 원시값이나 좌우 방향만으로 두 구현을 비교하면 안 됩니다.
- Python과 Accord.NET은 learning rate, 조기 종료 및 Barnes-Hut 내부 구현이 달라 동일 입력에서도 원시 좌표가 완전히 같지 않을 수 있습니다.
