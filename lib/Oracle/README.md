# Oracle managed driver

WinForms 직접 조회용 `Oracle.ManagedDataAccess` 19.13.0 (`net40`)을 사용한다. 기존 .NET Framework 4.5.1 프로젝트에 맞춘 버전이다. Oracle Client 설치는 필요하지 않다. 사용 범위는 사용자명/비밀번호 기반 TCP 접속이며 Kerberos용 IOP DLL은 사용하지 않는다.

- 공식 패키지: https://www.nuget.org/packages/Oracle.ManagedDataAccess/19.13.0
- Oracle 이용 및 배포 조건: https://www.oracle.com/downloads/licenses/distribution-license.html
- 패키지 출처·해시: `manifest.json`
- 원본 제3자 고지: `info.txt`

Oracle 바이너리는 Git에 재배포하지 않는다. `Oracle.Runtime.targets`는 `lib/Oracle/Oracle.ManagedDataAccess.dll`이 있으면 수동 참조를 우선한다. 없으면 첫 빌드에서 공식 NuGet CDN의 고정 패키지를 프로젝트 `obj/oracle-driver/19.13.0`으로 내려받아 SHA256을 확인한 뒤 DLL을 직접 참조한다. PowerShell이나 NuGet 복원 명령은 사용하지 않는다. MSBuild 16.5 이상이 필요하다. 최초 다운로드에는 인터넷 연결이 필요하며, 이후에는 추출한 파일을 재사용한다.

망이 차단된 개발 PC에서는 공식 패키지의 `lib/net40/Oracle.ManagedDataAccess.dll`을 이 폴더에 직접 넣으면 다운로드를 생략한다. 실제 사용·배포 시 Oracle의 조건을 따른다. 관리 DLL은 호스트 EXE 출력 폴더로 자동 복사된다. t-SNE 클래스 라이브러리에는 Oracle 참조가 추가되지 않는다.
