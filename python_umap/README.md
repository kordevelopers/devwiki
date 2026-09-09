# Python UMAP

WinForms Accord UMAP 화면과 같은 방식으로 PCCB 데이터를 조회하고, RESPONSE/DEFECT/EPM 데이터 타입을 상단에서 선택해 차트를 다시 바인딩합니다.

## VS Code 실행

1. `.env.example`을 `.env`로 복사하고 DBeaver JDBC의 host, database, port, username, password를 입력합니다.
2. 최초 1회 `.scriptssetup_python.ps1`을 실행합니다.
3. VS Code에서 `UMAP: Run (Database)` 또는 `UMAP: Run (CSV)` 디버그 구성을 선택합니다.

차트 상단에 Data Type, Draft No 입력/검색, Export Excel 컨트롤이 배치됩니다. 하단에는 선택 마커와 인접 3개가 표시됩니다.

## EXE

`powershell -ExecutionPolicy Bypass -File .\scripts\build_exe.ps1 -Clean`으로 `dist/HynixTasUmap.exe`를 생성합니다. 대상 PC에는 EXE와 설정된 `.env`만 전달하며 실행 시 패키지를 설치하지 않습니다.
