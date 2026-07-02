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
차트 창 없이 파일만 저장하려면 `--no-show-chart`를 추가합니다.

```powershell
.\.venv\Scripts\python.exe -m pca_runner --mode sample --no-show-chart
```

## 차트 표시

차트는 `matplotlib.pyplot as plt`를 사용합니다.

- X축은 `X1`, Y축은 `X2`입니다.
- 각 축은 실제 PCA 좌표 범위의 시작부터 끝까지 눈금 라벨을 표시합니다.
- 원점 `(0, 0)` 기준선도 함께 표시합니다.
- 선택된 `DRAFT_NO`는 별표로 강조합니다.

## Oracle 접속 방식

`sample` 모드는 DB 없이 즉시 실행됩니다. 실제 DB는 `.env.example`을 `.env`로 복사한 뒤 사용합니다.

- `PCA_DB_MODE=odbc`: Windows ODBC DSN 또는 전체 ODBC 연결 문자열 사용
- `PCA_DB_MODE=oracledb`: `python-oracledb` thin 모드 사용, Oracle Client 설치 불필요

ODBC는 Oracle Client 전체 설치가 없어도 Oracle Instant Client Basic/Basic Lite + ODBC 패키지로 등록할 수 있습니다.
압축 해제 후 관리자 PowerShell에서 다음을 실행합니다.

```powershell
.\scripts\install_oracle_odbc_driver.ps1 -InstantClientDir C:\oracle\instantclient_23_8
.\scripts\check_odbc_drivers.ps1
```

Oracle ODBC 드라이버 등록 후 Windows "ODBC Data Sources (64-bit)"에서 DSN을 만들거나,
`.env`의 `PCA_ODBC_CONNECTION_STRING`에 전체 연결 문자열을 넣으면 됩니다.

## 입력 쿼리 컬럼

쿼리 결과에는 아래 컬럼이 필요합니다.

- `DRAFT_NO`
- `PARAM_TYP`
- `LABEL_Y`
- `CONV_EXPER_CTN`

`CONV_EXPER_CTN`은 JSON 객체 또는 객체 1개를 담은 JSON 배열이어야 합니다. 중첩 객체/배열은 `A.B`, `A[0]` 형태로 펼친 뒤 숫자 feature만 PCA에 사용합니다.
