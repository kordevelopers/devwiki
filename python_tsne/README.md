# Python t-SNE Runner

`python_tsne`는 기존 `python_pca`와 동일한 PCCB 데이터베이스, 테이블, JSON 데이터 및 차트 기능을 사용하면서 2차원 투영만 PCA에서 sklearn t-SNE로 변경한 독립 실행 프로젝트입니다. 운영 실행기에는 샘플 데이터 생성 기능이 없습니다.

## 처리 흐름

1. Oracle 또는 ODBC로 PCCB 조회 SQL을 실행합니다.
2. `PARAM_TYP`으로 분석 대상을 선택합니다.
3. `CONV_EXPER_CTN` JSON 객체를 `A.B`, `A[0]` 형태의 feature로 펼칩니다.
4. 숫자 coverage가 90% 이상이고 분산이 `1e-10`보다 큰 feature를 선택합니다.
5. 누락된 값은 해당 feature 평균으로 대체하고 `StandardScaler`로 표준화합니다.
6. sklearn t-SNE를 실행하고 전체 표준화 feature 공간에서 KNN을 계산합니다.
7. CSV, 진단 JSON, PNG, 바인딩 원본 Excel을 저장한 뒤 matplotlib 차트를 표시합니다.

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

차트에서 마커를 클릭하면 WinForms와 동일하게 화면에 표시된 X1/X2 좌표 기준의 가까운 3개가 차트 하단 그리드에 표시됩니다. 배치 KNN CSV는 기존 동작대로 전체 표준화 feature 공간 기준입니다. 창에서 `Export original Excel`을 누르면 차트에 바인딩된 원본 행을 엑셀로 저장할 수 있습니다.

## VS Code에서 실행

1. 저장소 루트를 VS Code로 엽니다.
2. `python_tsne/.env.example`을 `python_tsne/.env`로 복사하고 Oracle 접속 정보를 입력합니다.
3. 최초 1회 `Terminal > Run Task > Setup Python t-SNE (run once)`를 실행합니다.
4. `Run and Debug`에서 `Python t-SNE (DB)`를 선택하고 실행합니다. 중단점은 Python 코드에 바로 설정할 수 있습니다.

차트 없이 디버깅하려면 `Python t-SNE (no chart)`를 선택합니다. 디버깅 구성은 실행 때 패키지를 설치하지 않습니다.

`python_tsne` 폴더만 VS Code로 열었다면 최초 1회 `Setup Python t-SNE` 작업을 실행한 뒤 `Run t-SNE from .env`로 디버깅합니다. 두 방식 모두 이 프로젝트의 `.venv/Scripts/python.exe`를 명시적으로 사용하며, F5를 누를 때마다 설치 작업을 실행하지 않습니다. 실행 중 표시되는 프로그램 메시지는 모두 영어입니다.

중단점은 `src/tsne_runner/main.py`의 `load_source_rows(config)` 호출 또는 `src/tsne_runner/source.py`의 `_load_with_oracledb()`에 설정하면 접속과 쿼리 실행 과정을 확인할 수 있습니다.

### 기존 `.venv`의 Python 버전 오류

`The existing .venv does not use Python 3.12. Remove .venv ...`는 기존 설치 스크립트에서 발생하던 메시지입니다. 이 프로젝트는 `pyproject.toml`에서 Python 3.12를 사용하도록 지정합니다. 다른 버전으로 생성했거나 다른 PC에서 복사한 `.venv`는 사용할 수 없을 수 있습니다. PC에 Python 3.12를 설치해도 기존 `.venv`가 자동으로 변경되지는 않습니다.

수정된 설치 스크립트는 정상적인 3.12 환경을 재사용합니다. 버전이 다르거나 실행할 수 없는 환경은 프로젝트 내부의 `.venv.backup-<고유번호>`로 보존하고 새 `.venv`를 만듭니다. 먼저 실행 가능한 Python 3.12를 찾으며, 백업 폴더는 Git에서 제외됩니다. 디버깅과 해당 환경을 사용하는 터미널을 닫은 뒤 `python_tsne` 폴더에서 다음을 실행합니다.

```powershell
py -0p
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup_python.ps1
.\.venv\Scripts\python.exe --version
```

Python이 사용자 지정 위치에 설치되어 자동 검색되지 않는 경우에는 실제 설치 경로를 직접 지정할 수 있습니다.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup_python.ps1 -PythonExecutable "C:\Python312\python.exe"
```

다른 PC에서 가져온 환경처럼 버전은 3.12여도 다시 만들어야 하는 경우에는 `-RecreateVenv`를 추가합니다. 이 경우에도 기존 환경을 백업합니다. 다른 PC에 프로젝트를 전달할 때는 `.venv`와 `.venv.backup-*` 폴더를 제외하고, 대상 PC에서 설치 작업을 실행합니다.

## 터미널에서 실행

```powershell
cd python_tsne
Copy-Item .env.example .env
notepad .env
powershell -ExecutionPolicy Bypass -File .\scripts\setup_python.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\run_tsne.ps1
```

`setup_python.ps1`은 최초 1회 또는 패키지 변경 시에만 실행합니다. `run_tsne.ps1`은 가상환경 존재 여부만 확인하고 패키지를 설치하거나 비교하지 않습니다.

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
- `original_data.xlsx`: 선택한 `PARAM_TYP`의 DB/CSV 원본 바인딩 데이터(`OriginalData` 시트)

`max_iter=1000`은 최대 반복 횟수입니다. sklearn의 조기 종료 조건이 충족되면 실제 실행 횟수는 더 작을 수 있으며, 두 값은 `diagnostic.json`에 구분해서 저장됩니다.

## Oracle 설정

Oracle이 설치되지 않은 Windows PC에서는 `TSNE_DB_MODE=oracledb`를 사용합니다. python-oracledb의 기본 Thin 모드로 접속하므로 Oracle Client, Instant Client, ODBC 드라이버를 설치하지 않아도 됩니다. 이 프로젝트의 해당 연결 경로는 `init_oracle_client()`를 호출하지 않습니다. DB 서버 주소와 계정, 서버로의 네트워크 접속은 필요합니다.

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

DBeaver를 JDBC로 접속하더라도 Python에는 JDBC URL 전체를 넣지 않습니다. DBeaver의 URL이 `jdbc:oracle:thin:@db-server:1521/ORCL`이면 `.env`에 `HOST=db-server`, `PORT=1521`, `SERVICE_NAME=ORCL`을 각각 입력합니다. 사용자명과 비밀번호는 DBeaver 접속정보와 동일하게 `TSNE_ORACLE_USER`, `TSNE_ORACLE_PASSWORD`에 입력합니다. SID 형식인 `jdbc:oracle:thin:@db-server:1521:ORCL`이면 `SERVICE_NAME`을 비우고 `TSNE_ORACLE_SID=ORCL`을 입력합니다.

Exadata 접속 정보는 일반적으로 `SERVICE_NAME`을 사용합니다. SID를 사용하는 DB는 `TSNE_ORACLE_SERVICE_NAME`을 비우고 `TSNE_ORACLE_SID`를 지정하면 Oracle 접속 descriptor를 구성합니다. `TSNE_ORACLE_DSN`을 직접 지정하면 HOST/PORT/SERVICE_NAME/SID보다 우선합니다.

기존 WinForms 조회 SQL을 사용하려면 `queries/exadata_tsne.sql`에 해당 SELECT를 넣습니다. 기본 파일은 `python_pca/queries/exadata_pca.sql`과 같은 테이블·조인·조회 조건을 사용하며 정렬을 추가한 것입니다. WinForms 차트는 외부에서 전달받은 `DataTable`을 사용하므로 실제 서비스의 쿼리를 가져올 때는 `DRAFT_NO`, `PARAM_TYP`, `ENGR_RSLT_VAL` 또는 `LABEL_Y`, `CONV_EXPER_CTN` 컬럼을 유지합니다. 선택 컬럼은 `RSLT_CD`입니다.

DB 조회를 먼저 확인하려면 다음을 실행합니다. 이 스크립트는 설정한 SQL을 실행하여 조회 행 수와 컬럼 이름을 출력합니다.

```powershell
.\.venv\Scripts\python.exe .\scripts\test_oracle_connection.py --mode oracledb
```

ODBC는 Oracle ODBC 드라이버가 설치된 환경에서만 사용합니다. 해당 환경에서는 `TSNE_DB_MODE=odbc`와 `TSNE_ODBC_DSN` 또는 전체 연결 문자열을 설정합니다.

```powershell
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
