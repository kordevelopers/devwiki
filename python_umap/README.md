# Python UMAP

WinForms Accord UMAP 화면과 같은 방식으로 PCCB 데이터를 조회하고, RESPONSE/DEFECT/EPM 데이터 타입을 상단에서 선택해 차트를 다시 바인딩합니다.

## VS Code 실행

1. `.env.example`을 `.env`로 복사하고 DBeaver JDBC의 host, database, port, username, password를 입력합니다. 기본 SQL은 `queries/umap.sql`입니다.
2. 최초 1회 `.\scripts\setup_python.ps1`을 실행합니다.
3. VS Code에서 `UMAP: Run (Database)` 또는 `UMAP: Run (CSV)` 디버그 구성을 선택합니다.

차트 상단에 Data Type, Draft No 입력/검색, Export Excel 컨트롤이 배치됩니다. 하단에는 선택 마커와 인접 3개가 표시됩니다.

저장소 전체를 VS Code로 열면 루트 `.vscode/launch.json`의 `Python UMAP (DB)`를 사용합니다. `python_umap` 폴더만 열면 `python_umap/.vscode/launch.json`을 사용합니다. 이 경우 `${workspaceFolder}`가 이미 `python_umap` 폴더이므로 `.venv/Scripts/python.exe`가 올바른 경로입니다.

## EXE

`powershell -ExecutionPolicy Bypass -File .\scripts\build_exe.ps1 -Clean`으로 `dist/HynixTasUmap.exe`를 생성합니다. `queries/umap.sql`은 EXE에 포함되며, 다른 쿼리를 사용하려면 `.env`의 `UMAP_SQL_FILE`에 SQL 파일 절대 경로를 입력합니다. 대상 PC에는 EXE와 설정된 `.env`만 전달하며 실행 시 패키지를 설치하지 않습니다.
