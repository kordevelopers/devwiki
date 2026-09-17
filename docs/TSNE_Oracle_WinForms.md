# WinForms에서 Oracle 직접 조회

실제 프로젝트에 코드를 옮기기 전에 이 프로그램만 실행해서 Oracle 조회와 t-SNE를 시험할 수 있다. Python 실행은 필요하지 않다. `Program.cs`가 `OracleTsneDataProvider`를 폼에 연결한다. 기존 DataTable 호출과 t-SNE 클래스 라이브러리의 단일 `Tsne.cs` 구조는 유지한다.

## 실행 순서

1. `TsneDemo.slnx`를 열고 `SKhynix.TAS.UI.Report.Pccb`를 시작 프로젝트로 설정한다.
2. Python에서 사용하던 `python_tsne/.env`에 실제 접속정보를 입력한다. 이 파일이 없다면 같은 폴더의 `.env.example`을 복사한다. `test_user` / `test_password`는 실행용 계정이 아니다.
3. F5로 실행하고 **Oracle 조회**를 누른다. 조회 중에는 버튼이 비활성화되고 진행 안내가 표시된다.
4. 하단 그리드에서 조회된 원본 행과 `CONV_EXPER_CTN`을 확인한다.
5. RESPONSE 또는 DEFECT를 선택하고 **Draw Chart**를 누른다. 다시 DB에서 조회하려면 **Oracle 조회**를 누른다. 엔진만 바꾸어 다시 그릴 때는 이미 조회한 데이터를 사용한다.

실행 폴더를 별도로 옮기는 경우에는 `bin/Release` 전체와 `x64`·`licenses` 하위 폴더를 함께 복사한다. EXE 옆의 `.env.example`을 `.env`로 복사해 접속정보를 입력한 뒤 `SKhynix.TAS.UI.Report.Pccb.exe`를 실행한다. 실제 `.env`와 비밀번호는 Git 또는 배포 예제에 포함하지 않는다.

## Python과 공유하는 설정

| 설정 | 용도 |
|---|---|
| `TSNE_DB_HOST` | Oracle 호스트 |
| `TSNE_DB_PORT` | 포트, 기본 1521 |
| `TSNE_DB_DATABASE` | 서비스 이름 (`host:port/service`의 service) |
| `TSNE_DB_USERNAME` | 조회 계정 |
| `TSNE_DB_PASSWORD` | 비밀번호 |
| `TSNE_SQL_FILE` | 선택 SQL 파일 |
| `TSNE_SQL` | SQL 파일이 없을 때 사용할 직접 SQL |
| `TSNE_ENV_FILE` | WinForms에서 설정 파일 경로를 명시할 때 사용하는 환경변수 |

프로세스 환경변수가 `.env`의 같은 키보다 우선한다. 설정 파일은 `TSNE_ENV_FILE` → 현재 작업 폴더의 `.env` → EXE 폴더의 `.env` → 상위 저장소의 `python_tsne/.env` 순으로 찾는다. 각 조회마다 다시 읽으므로 수정 후 프로그램을 재시작하지 않고 **Oracle 조회**를 다시 누르면 된다.

`TSNE_SQL_FILE`은 현재 작업 폴더, `.env` 폴더, EXE 폴더를 기준으로 찾는다. Python의 `queries/exadata_tsne.sql` 같은 상대 경로도 저장소에서 사용할 수 있다. 단독 EXE에 상대 경로를 설정했다면 SQL 파일도 같은 상대 위치에 복사한다. SQL 파일이 없으면 Python `config.py`와 같은 기본 SQL을 사용한다.

Python의 `TSNE_DATA_TYPE` / `TSNE_PARAM_TYP` / `TSNE_TARGET_DRAFT_NO`는 이 폼의 초기 선택값으로 가져오지 않는다. WinForms에서는 Data Type과 Draft Number 컨트롤로 선택한다.

## 조회 및 연결 방식

기본 SQL은 `TASADM.PCCB_INFER_RSLT_INF`와 `TASADM.PCCB_JUDGE_RSLT_INF`를 DRAFT_NO·PARAM_TYP로 조인하고, 최근 10일 중 결과 라벨과 JSON이 있는 행을 조회한다. 행 수를 임의로 제한하지 않는다. 결과 컬럼은 `DRAFT_NO`, `PARAM_TYP`, `LABEL_Y`, `RSLT_CD`, `CONV_EXPER_CTN`이다. 라벨 컬럼은 `ENGR_RSLT_VAL`도 지원한다.

Oracle 관리 드라이버가 백그라운드 스레드에서 직접 접속한다. 읽기 전용 트랜잭션에서 SELECT/WITH 쿼리를 실행한다. 연결 시간 제한은 15초, 쿼리 실행 시간 제한은 120초다. CLOB/NCLOB는 연결을 닫기 전에 전체 문자열로 읽는다. 조회 실패 시 기존 데이터를 유지하며, 오류 안내에 Oracle 오류 코드와 설정 확인 방법을 표시한다. 접속 문자열과 비밀번호는 출력하지 않는다.

## 드라이버와 빌드

`lib/Oracle/Oracle.Runtime.targets`가 `lib/Oracle/Oracle.ManagedDataAccess.dll`의 수동 참조를 우선한다. 로컬 DLL이 없으면 공식 패키지에서 첫 빌드 시 자동으로 내려받고 해시를 검증한다. 기존 NuGet 복원 설정 및 PowerShell 스크립트에 의존하지 않는다. 최초 다운로드가 차단된 PC에서는 공식 패키지의 `lib/net40/Oracle.ManagedDataAccess.dll`을 해당 경로에 넣는다. 자세한 버전·출처·이용 조건은 [Oracle 드라이버 안내](../lib/Oracle/README.md)를 참고한다.

## 확인한 범위

`TsneVerification`의 `--oracle`은 .env·SQL 읽기, 환경변수 우선순위, 긴 JSON과 null 보존, 기존 분석 저장소 연결을 확인한다. `--oracle-live`는 실제 설정으로 DB 조회를 실행하고 행·컬럼 수만 출력한다. 실제 서버 검증에는 유효한 접속정보와 서버에 접근 가능한 네트워크가 필요하다.

Oracle 데이터 조회와 차트 라이선스는 별개다. LightningChart 평가판이 만료된 머신에서는 DB 원본 그리드와 계산 검증을 확인할 수 있지만 차트 표시에는 유효한 차트 라이선스가 필요하다.
