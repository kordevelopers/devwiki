# WinForms에서 Oracle 직접 조회

`TsneDemo.slnx`의 `SKhynix.TAS.UI.Report.Pccb`를 실행하면 Oracle 데이터를 조회하고 t-SNE를 시험할 수 있다. Python 실행이나 실제 업무 프로젝트로의 코드 이식은 필요하지 않다.

## 접속정보는 WinForms 한 곳에서 관리

1. WinForms 프로젝트의 `oracle.env.example`을 같은 폴더의 `oracle.env`로 복사한다.
2. `ORACLE_HOST`, `ORACLE_PORT`, `ORACLE_SERVICE_NAME`, `ORACLE_USERNAME`, `ORACLE_PASSWORD`를 입력한다. 기존 Python의 `TSNE_DB_DATABASE` 값이 서비스 이름에 해당한다. `test_user` / `test_password`는 예제이며 실제 접속용 계정이 아니다.
3. 프로젝트를 빌드하면 이 파일이 EXE 옆으로 복사된다. F5로 실행하고 **Oracle 조회**를 누른다.
4. 조회가 끝나면 하단 그리드에서 원본 행을 확인하고 RESPONSE 또는 DEFECT를 선택한 뒤 **Draw Chart**를 누른다.

별도 배포 폴더에서는 EXE 옆의 `oracle.env.example`을 `oracle.env`로 복사해 수정한다. 런타임은 **EXE 옆 oracle.env만** 읽는다. Python `.env`, 상위 폴더, 작업 폴더, `TSNE_*` 및 기타 환경변수에서 접속정보를 찾지 않는다. 각 조회마다 파일을 다시 읽으므로 실행 중 EXE 옆 파일을 수정했다면 재시작 없이 조회할 수 있다. 소스 프로젝트의 파일을 수정한 경우에는 다시 빌드한다. 프로젝트 파일과 EXE 옆 파일을 번갈아 수정하지 말고 실행 방식에 맞는 한 곳에서 관리한다.

실제 `oracle.env`는 Git 제외 대상이다. 공개 예제와 배포 ZIP에는 실제 비밀번호를 넣지 않는다. Python 설정을 바꿔도 WinForms 설정은 바뀌지 않는다.

| 설정 | 용도 / 기본값 |
|---|---|
| `ORACLE_HOST` | Oracle 호스트 |
| `ORACLE_PORT` | 포트 / 1521 |
| `ORACLE_SERVICE_NAME` | Oracle 서비스 이름 |
| `ORACLE_USERNAME` | 조회 계정 |
| `ORACLE_PASSWORD` | 비밀번호; 공백이나 `#` 등이 있으면 작은따옴표로 감싼다 |
| `SQL_FILE` | 선택 SQL 파일; oracle.env 폴더 기준 상대 경로 또는 절대 경로 |
| `SQL` | SQL_FILE이 비어 있을 때 사용할 SQL; 생략하면 기본 쿼리 |
| `QUERY_TIMEOUT_SECONDS` | 연결·실행·행/CLOB 수신 전체의 취소 요청 시점 / 180초 |
| `FETCH_SIZE_BYTES` | Oracle 행 수신 버퍼 / 1,048,576바이트 |
| `LOB_PREFETCH_CHARACTERS` | CLOB/NCLOB 미리 읽기 / 32,768자; 0이면 미리 읽기 해제 |

설정값의 `${...}`는 환경변수로 치환하지 않고 그대로 사용한다. 큰따옴표로 감싼 `SQL`에는 여러 줄을 넣을 수 있다. 별도 SQL 파일은 배포 폴더에도 함께 복사해야 한다.

## Read에서 오래 기다릴 때

`reader.Read()`는 다음 행을 받기 위한 동기 I/O이므로 DB 실행이나 네트워크 수신 때문에 기다릴 수 있다. WinForms에서는 이를 백그라운드에서 실행하고 **현재 단계 · 수신 완료 행 수 · 경과 초**를 1초 간격으로 표시한다. 행 수는 현재 CLOB까지 모두 읽은 행만 센다.

- **SQL 실행 · 첫 결과 대기**: 쿼리 실행 결과를 기다리는 단계.
- **행 수신 대기 (Read)**: 다음 묶음의 행을 기다리는 단계. 첫 행 전에도 발생할 수 있다.
- **현재 행 CLOB/NCLOB 읽는 중**: JSON 원문을 가져오는 단계.
- **조회 취소**: 조회 버튼이 취소 버튼으로 바뀐다. 누르면 Oracle에 취소 요청을 보낸다.

쿼리와 수신을 합친 시간이 설정값을 넘으면 자동으로 취소를 요청한다. 실제 중단은 드라이버와 서버의 응답에 의존하므로 설정 시간이 지나자마자 연결이 반드시 닫히는 것은 아니다. 응답 대기 중에는 그 상태와 경과 시간을 계속 표시하고 중복 조회를 막는다. 취소 호출 자체도 UI 스레드 밖에서 실행한다. 연결을 여는 중에는 Open이 반환되어야 취소를 확인할 수 있다.

CLOB는 기본적으로 32,768자까지 미리 받아 추가 통신을 줄이도록 했다. 더 긴 값도 전체 문자열로 읽으며, 5,610행을 포함해 행 수를 임의로 줄이지 않는다. 이는 서버 쿼리 자체의 지연을 해결하는 설정은 아니다. 특정 단계에서 계속 기다린다면 같은 SQL의 실행 계획·조인/정렬 비용·네트워크 응답을 함께 확인해야 한다. 실패나 취소 때는 불완전한 결과를 분석에 넘기지 않고 기존 데이터를 유지한다.

설정 근거: Oracle의 [LOB 미리 읽기](https://docs.oracle.com/en/database/oracle/oracle-database/19/odpnt/CommandInitialLOBFetchSize.html), [FetchSize](https://docs.oracle.com/en/database/oracle/oracle-database/19/odpnt/DataReaderFetchSize.html), [취소 동작](https://docs.oracle.com/en/database/oracle/oracle-database/19/odpnt/CommandCancel.html).

## SQL과 분석

기본 SQL은 `TASADM.PCCB_INFER_RSLT_INF`와 `TASADM.PCCB_JUDGE_RSLT_INF`를 DRAFT_NO·PARAM_TYP로 조인하고, 최근 10일 중 결과 라벨과 JSON이 있는 행을 조회한다. 이는 WinForms 코드가 소유하는 기본 SQL이며 Python 파일을 읽지 않는다. 결과 컬럼은 `DRAFT_NO`, `PARAM_TYP`, `LABEL_Y`, `RSLT_CD`, `CONV_EXPER_CTN`이다. 라벨 컬럼은 `ENGR_RSLT_VAL`, `AI_RSLT_VAL`도 지원한다.

읽기 전용 트랜잭션에서 SELECT/WITH 쿼리를 실행한다. 조회 후 엔진만 바꾸어 다시 그릴 때는 메모리의 데이터를 재사용한다. 다시 DB를 조회하려면 **Oracle 조회**를 누른다. 다른 폼에 적용할 때도 기존 DataTable 호출과 단일 `Tsne.cs` 라이브러리를 계속 사용할 수 있다.

## 빌드와 검증

`lib/Oracle/Oracle.Runtime.targets`가 `lib/Oracle/Oracle.ManagedDataAccess.dll`의 수동 참조를 우선한다. DLL이 없으면 첫 빌드 시 공식 패키지에서 내려받고 해시를 검증한다. PowerShell 스크립트는 필요하지 않다. 자세한 버전·출처는 [Oracle 드라이버 안내](../lib/Oracle/README.md)를 참고한다.

`TsneVerification --oracle`은 설정 분리, SQL 읽기, 100,000자를 넘는 JSON, null, 5,610행/순서 보존, 취소, 진행 상태 및 모의 대기 시간 초과를 검증한다. `--oracle-live`는 검증 EXE 옆의 `oracle.env`로 실제 DB를 조회한다. 모의 검증은 실제 Oracle 서버에서의 수신·CLOB·취소 검증을 대신하지 않는다.

배포할 때는 EXE와 DLL뿐 아니라 `x64`, `licenses` 폴더도 함께 복사한다. LightningChart 라이선스와 Oracle 조회는 별개이며 차트 표시에는 유효한 차트 라이선스가 필요하다.
