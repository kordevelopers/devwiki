# Excel COM/RPC PC 진단 도구

PC마다 다르게 발생하는 Excel 자동화 RPC/COM 오류를 비교 진단하는 WinForms 도구입니다.

## 사용법

1. 가능하면 실행 중인 Excel을 모두 닫습니다.
2. `ExcelRpcDiagnostic.exe`를 실행하고 **전체 진단 실행**을 누릅니다.
3. 정상 PC와 오류 PC에서 각각 **결과 저장**으로 로그를 저장합니다.
4. `FAIL`, HRESULT, Office 버전/경로, 프로세스 비트수, 실행 중 Excel 정보를 비교합니다.

진단 도구는 사용자가 실행한 기존 Excel 프로세스를 강제 종료하지 않습니다. 테스트 과정에서 자신이 생성한 통합문서와 Excel COM 인스턴스만 닫습니다.
