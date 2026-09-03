# Python PCA Runner

VS Code에서 `python_pca` 폴더를 열고 실행하면 PCA/KNN 분석을 수행한 뒤 matplotlib 차트 창을 바로 표시합니다.

## VS Code에서 바로 실행

1. VS Code에서 `python_pca` 폴더를 엽니다.
2. `Run and Debug > Run PCA sample`을 실행합니다.
3. 처음 실행하면 `Setup Python and packages` 작업이 먼저 실행됩니다.

`Setup Python and packages` 작업은 다음을 자동 처리합니다.

- Python 실행 파일 확인
- Python이 없고 `winget`이 있으면 Python 3.12 설치 시도
- `.venv` 생성
- `requirements.txt` 패키지 설치
- 현재 프로젝트를 editable 모드로 설치

## 터미널 실행

```powershell
cd python_pca
powershell -ExecutionPolicy Bypass -File .\scripts\setup_python.ps1
.\.venv\Scripts\python.exe -m pca_runner --mode sample
```

실행하면 `outputs/pca_points.csv`, `outputs/knn_neighbors.csv`, `outputs/pca_scatter.png`가 저장되고 PCA 차트 창이 표시됩니다.
또한 Pccb t-SNE 화면에서 사용하는 feature 선별 감사, 생존 population, 진단 JSON 형식과 맞춰 함께 저장합니다.
차트 창 없이 파일만 저장하려면 `--no-show-chart`를 추가합니다.

```powershell
.\.venv\Scripts\python.exe -m pca_runner --mode sample --no-show-chart
```

## EXE 빌드

다른 사람에게 Python 설치 없이 전달하려면 PyInstaller로 one-dir EXE 배포 폴더를 만듭니다.

```powershell
cd python_pca
powershell -ExecutionPolicy Bypass -File .\scripts\build_exe.ps1 -Clean
```

결과는 `dist/HynixTasPca` 폴더에 생성됩니다. 다른 사람에게는 `.exe` 파일 하나만 주지 말고 이 폴더 전체를 전달해야 합니다.
같은 위치에 `dist/HynixTasPca.zip`도 생성되며, 이 ZIP에는 Python 소스 코드 없이 실행 배포 파일만 들어갑니다.

배포받은 사용자는 `dist/HynixTasPca` 폴더에서:

```powershell
Copy-Item .env.example .env
notepad .env
.\HynixTasPca.exe
```

EXE는 실행 파일 옆의 `.env`와 `queries\*.sql`을 읽습니다.

## 차트 표시

차트는 `matplotlib.pyplot as plt`를 사용합니다.

- X축은 `X1`, Y축은 `X2`입니다.
- 각 축 라벨은 실제 PCA 좌표의 최소/최대 범위를 표시합니다.
- 원점 `(0, 0)` 기준선도 함께 표시합니다.
- 선택된 `DRAFT_NO`는 별표로 강조합니다.
- 차트 창에서 포인트를 클릭하면 표준화 feature 공간의 유클리드 거리 기준 최근접 3개 포인트가 노란색으로 강조됩니다.

## Pccb 호환 출력

Python 실행 결과는 Pccb 화면의 핵심 데이터 흐름을 따릅니다.

- `outputs/pca_points.csv`: `DRAFT_NO`, `PARAM_TYP`, `LABEL_Y`, `RSLT_CD`, `X1`, `X2`
- `outputs/knn_neighbors.csv`: 선택 Draft 기준 최근접 Draft 3건
- `outputs/feature_selection_audit.csv`: feature별 포함 여부와 제외 사유
- `outputs/surviving_population.csv`: PCA에 살아남은 feature와 좌표
- `outputs/diagnostic.json`: row/feature 수, 제외 feature 수, 설명분산, shape code, 표준화 검증값

KNN 거리는 C#과 동일하게 PCA 2차원 좌표가 아니라 `StandardScaler`가 만든 전체 표준화 feature 공간에서 계산합니다.

## Oracle 접속 방식

`sample` 모드는 DB 없이 즉시 실행됩니다. 실제 DB는 `.env.example`을 `.env`로 복사한 뒤 사용합니다.

- `PCA_DB_MODE=odbc`: Windows ODBC DSN 또는 전체 ODBC 연결 문자열 사용
- `PCA_DB_MODE=oracledb`: `python-oracledb` thin 모드 사용, Oracle Client 설치 불필요

`oracledb` 모드는 pandas가 직접 Oracle 연결 객체를 받지 않도록 SQLAlchemy 엔진을 사용합니다.
`ODBC` 모드는 pandas에 pyodbc 연결 객체를 직접 넘기지 않고 cursor 결과를 DataFrame으로 변환합니다.

IP/Port/계정정보가 있고 Service Name을 알고 있으면 `oracledb` 모드가 가장 단순합니다.

```env
PCA_DB_MODE=oracledb
PCA_ORACLE_HOST=10.0.0.10
PCA_ORACLE_PORT=1521
PCA_ORACLE_SERVICE_NAME=EXADATA_SERVICE
PCA_ORACLE_USER=your_user
PCA_ORACLE_PASSWORD=your_password
PCA_SQL_FILE=queries/exadata_pca.sql
PCA_PARAM_TYP=RESPONSE
```

접속만 먼저 확인하려면 VS Code 작업 `Test Oracle oracledb connection`을 실행합니다.

ODBC는 Oracle Client 전체 설치가 없어도 Oracle Instant Client Basic/Basic Lite + ODBC 패키지로 등록할 수 있습니다.
압축 해제 후 관리자 PowerShell에서 다음을 실행합니다.

```powershell
.\scripts\install_oracle_odbc_driver.ps1 -InstantClientDir C:\oracle\instantclient_23_8
.\scripts\check_odbc_drivers.ps1
```

Oracle ODBC 드라이버 등록 후 Windows "ODBC Data Sources (64-bit)"에서 DSN을 만들거나,
`.env`의 `PCA_ODBC_CONNECTION_STRING`에 전체 연결 문자열을 넣으면 됩니다.
DSN을 스크립트로 만들려면 다음을 실행합니다.

```powershell
.\scripts\create_oracle_odbc_dsn.ps1 `
  -DsnName HYNIX_TAS_EXADATA `
  -DriverName "Oracle in instantclient_23_8" `
  -Host 10.0.0.10 `
  -Port 1521 `
  -ServiceName EXADATA_SERVICE
```

ODBC DSN을 사용하는 `.env` 예시는 다음과 같습니다.

```env
PCA_DB_MODE=odbc
PCA_ODBC_DSN=HYNIX_TAS_EXADATA
PCA_ODBC_USER=your_user
PCA_ODBC_PASSWORD=your_password
```

DSN 없이 ODBC 드라이버 이름과 IP/Port/Service로 바로 연결할 수도 있습니다.

```env
PCA_DB_MODE=odbc
PCA_ODBC_DRIVER=Oracle in instantclient_23_8
PCA_ORACLE_HOST=10.0.0.10
PCA_ORACLE_PORT=1521
PCA_ORACLE_SERVICE_NAME=EXADATA_SERVICE
PCA_ODBC_USER=your_user
PCA_ODBC_PASSWORD=your_password
```

ODBC 접속 확인은 VS Code 작업 `Test Oracle ODBC connection`으로 실행합니다.

주의: Oracle Instant Client DLL/ODBC DLL은 저장소에 포함하지 않습니다. Oracle 배포 파일은 로컬 PC에 설치 또는 압축 해제한 뒤 `install_oracle_odbc_driver.ps1`로 Windows ODBC 드라이버에 등록합니다.

## 입력 쿼리 컬럼

쿼리 결과에는 아래 컬럼이 필요합니다.

- `DRAFT_NO`
- `ENGR_RSLT_VAL` 또는 `LABEL_Y`
- `RSLT_CD`
- `PARAM_TYP`
- `CONV_EXPER_CTN`

여러 줄 SQL은 `.env`에 직접 넣지 말고 `PCA_SQL_FILE`로 지정한 `.sql` 파일에 넣습니다.
기본 샘플 파일은 `queries/exadata_pca.sql`입니다.

```sql
SELECT
    M.DRAFT_NO,
    J.ENGR_RSLT_VAL,
    J.RSLT_CD,
    M.PARAM_TYP,
    M.CONV_EXPER_CTN
FROM TASADM.PCCB_INFER_RSLT_INF M
JOIN TASADM.PCCB_JUDGE_RSLT_INF J
    ON M.DRAFT_NO = J.DRAFT_NO
   AND M.PARAM_TYP = J.PARAM_TYP
WHERE M.CHG_TM > SYSDATE - 10
  AND J.ENGR_RSLT_VAL IS NOT NULL
  AND M.CONV_EXPER_CTN IS NOT NULL
```

`CONV_EXPER_CTN`은 JSON 객체 또는 객체 1개를 담은 JSON 배열이어야 합니다. 중첩 객체/배열은 `A.B`, `A[0]` 형태로 펼친 뒤 숫자 feature만 PCA에 사용합니다.

## sklearn t-SNE와 Accord 결과 비교

`scripts/compare_tsne.py`는 샘플 데이터를 생성하지 않으며, 실제 전처리 완료 feature CSV를 입력으로 받습니다. Python 기준 계산은 다음 설정으로 고정됩니다.

- `n_components=2`
- `perplexity=min(30, max(5, n_samples - 1) // 3)`
- `max_iter=1000`
- `random_state=42`
- `init="pca"`
- `learning_rate="auto"`
- `NearestNeighbors(n_neighbors=15, metric="euclidean")`

Accord 좌표 CSV를 함께 전달하면 Python 원본, Accord 원본, 정규화·회전 정렬 후 중첩 결과를 하나의 PNG로 저장합니다. 입력 CSV에는 `DRAFT_NO`와 수치 feature가 필요하며, Accord 좌표 CSV에는 `DRAFT_NO`, `X1`, `X2`가 필요합니다.

```powershell
.\.venv\Scripts\python.exe .\scripts\compare_tsne.py `
  --input C:\temp\tsne_surviving_population.csv `
  --accord-points C:\temp\accord_tsne_points.csv `
  --output C:\temp\tsne_python_vs_accord.png `
  --python-points C:\temp\python_tsne_points.csv `
  --metrics C:\temp\tsne_comparison_metrics.json `
  --dataset-label "PCCB production export"
```

비교 PNG와 CSV/JSON은 검증 산출물이므로 `outputs` 폴더에 저장할 수 있으며 Git에는 포함되지 않습니다.
저장소의 [비교 캡처](../docs/TSNE_Python_Accord_Comparison.png)와 [측정값](../docs/TSNE_Python_Accord_Comparison.json)은 실제 PCCB 데이터가 아닌 기존 40×80 synthetic 검증 데이터를 사용한 결과입니다.
