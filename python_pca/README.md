# Python PCA Runner

VS Code에서 `python_pca` 폴더를 열어 바로 실행할 수 있는 PCA/KNN 샘플 프로젝트입니다.

## 빠른 실행

```powershell
cd python_pca
py -3 -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\.venv\Scripts\python.exe -m pip install -e .
.\.venv\Scripts\python.exe -m pca_runner --mode sample
```

결과는 `outputs/pca_points.csv`, `outputs/knn_neighbors.csv`, `outputs/pca_scatter.png`에 저장됩니다.

## VS Code

1. VS Code에서 `python_pca` 폴더를 엽니다.
2. `Terminal > Run Task > Create venv and install packages`를 실행합니다.
3. `Run and Debug > Run PCA sample`을 실행합니다.

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
