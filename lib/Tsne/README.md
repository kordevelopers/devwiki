# t-SNE 직접 참조 DLL

이 폴더는 t-SNE 라이브러리를 NuGet 복원 없이 빌드하고 실행하기 위한 파일 모음이다. `TsneCSharp.dll`, `HybridTsne.dll`, `MathNet.Numerics.dll`은 모두 컴파일된 .NET 어셈블리다. 두 엔진은 .NET Framework 4.5.1을 대상으로 한다.

## 다른 프로젝트에 수동으로 연결

1. `Tsne.cs`를 가져온 프로젝트의 대상 프레임워크를 .NET Framework 4.5.1 이상으로 설정한다.
2. **참조 추가 → 찾아보기**에서 `TsneCSharp.dll`, `HybridTsne.dll`, `MathNet.Numerics.dll` 세 파일을 추가한다. 로컬 복사 속성은 True로 둔다.
3. 기존 `TAS.Experimental.*` NuGet 참조와 `EnsureNuGetPackageBuildImports` 등 이전 t-SNE 복원 검사·targets를 제거한다. DLL 참조만 추가하면 기존 NuGet 복원 오류는 남을 수 있다.
4. Hybrid를 실행할 EXE는 x64로 설정한다. `MathNet.MKL.x64.zip`을 EXE가 있는 디렉터리에 풀면 `x64/MathNet.Numerics.MKL.dll`, `x64/libiomp5md.dll`이 배치된다. 두 파일은 네이티브 DLL이므로 **C# 참조에 추가하지 않는다**.

이 저장소의 프로젝트에는 위 연결을 적용했다. `Tsne.Runtime.targets`가 관리 DLL과 라이선스를 복사하고, MSBuild의 Unzip 작업으로 ZIP을 로컬에서 해제한다. PowerShell이나 네트워크 접근은 사용하지 않는다. 이 targets를 가져오는 경우 ZIP을 수동으로 풀 필요는 없다. 테스트 폼의 기존 Accord·LightningChart·Newtonsoft.Json은 별도 의존성이다.

`TsneCSharp.dll`의 어셈블리 버전 `0.0.0.0`은 원본에 버전 지정이 없다는 뜻이며 미빌드 파일이라는 뜻이 아니다. `CS0006`에서 찾지 못하는 파일이 `SKhynix.TAS.Analysis.Tsne.dll`이면 래퍼 프로젝트의 선행 오류와 빌드 구성을 확인해야 한다.

## 출처 및 빌드

| 파일 | 원본 / 버전 | 설정 |
|---|---|---|
| TsneCSharp.dll | jdmccaffrey/tsne-csharp, `7739a9ded14ff004b1b213146858ce25f8a97386` | 원본 그대로, Any CPU, net451 |
| HybridTsne.dll | Orlinski/Hybrid_t-SNE, `8a19f4e19ffc0b8e5b142533401e157972e2cdbf` | 원본 그대로, Any CPU, net451 |
| MathNet.Numerics.dll | MathNet.Numerics 4.7.0 | 공식 NuGet 패키지의 net40 DLL |
| MathNet.MKL.x64.zip | MathNet.Numerics.MKL.Win-x64 2.3.0 | 공식 패키지의 x64 런타임 두 파일 |

Hybrid의 관리 어셈블리는 불필요한 AMD64 참조 경고를 없애기 위해 Any CPU로 컴파일했다. 계산 코드는 수정하지 않았고 `Tsne.cs`의 x64 실행 검사는 유지했다. Roslyn C# 7.3, 최적화, deterministic 빌드와 .NET Framework 4.5.1 참조 어셈블리를 사용했다. Hybrid 소스 목록은 Config, FFTRepulsion, Gradient, HashSpatialTree, LSHForest, Quicksort, Selection, tSNE, AssemblyInfo이며 CSharp는 TSNEProgram.cs다.

각 파일의 SHA256은 `manifest.json`에 기록한다. tsne-csharp와 MathNet/MKL 라이선스 전문은 `licenses`에 있다. 고정 Hybrid 커밋에는 LICENSE 파일이 없으며, 테스트용 DLL 제공은 재배포 권한을 부여하지 않는다.
