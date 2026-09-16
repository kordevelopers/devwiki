# Multicore-TSNE 분석 및 .NET Interop

## 선택한 원본과 연결 방식

- 원본: [DmitryUlyanov/Multicore-TSNE](https://github.com/DmitryUlyanov/Multicore-TSNE)
- 고정 커밋: [`c1dbf84eb550980876d8ed822af4e9dfd21c5e05`](https://github.com/DmitryUlyanov/Multicore-TSNE/tree/c1dbf84eb550980876d8ed822af4e9dfd21c5e05)
- 실제 계산: C++의 VP-tree 이웃 검색, Barnes-Hut gradient, OpenMP 병렬 처리
- 원본 공개 C 함수: `tsne_run_double` (`multicore_tsne/tsne.cpp`)
- .NET 연결: **C ABI + P/Invoke**, `.NET Framework 4.5.1`, Windows x64

원본에 `extern "C"`/Windows `dllexport` 함수가 있으므로 C++/CLI나 Python 프로세스 없이 연결할 수 있다. 원본 CMake에는 Unix의 `m` 라이브러리 링크 등 Windows에서 그대로 쓰기 불편한 설정이 있어, 설치된 MSVC 도구 집합을 찾아 두 원본 `.cpp`를 직접 DLL로 빌드한다. 알고리즘 소스는 수정하지 않는다.

OpenMP는 이웃 확률 계산과 gradient의 샘플별 계산을 병렬화한다. 모든 단계가 병렬화되지는 않으며, 작은 데이터에서는 병렬 처리 비용 때문에 스레드 수에 비례한 속도 향상을 보장하지 않는다. 원본 README의 오래된 다른 플랫폼 성능 수치는 이 Windows 환경의 측정값으로 사용하지 않았다.

## 파일 구성

| 파일 | 역할 |
|---|---|
| `SKhynix.TAS.Analysis.Tsne/Tsne.cs` | 기존 한 개 C# 파일에 `Engine.Multicore`, P/Invoke와 결과 변환 추가 |
| `native/MulticoreTsne/MulticoreTsneInterop.cpp` | C ABI 버전, 입력 길이/범위 검증, C++ 오류 변환, 실행 직렬화 |
| `native/MulticoreTsne/LICENSE.txt` | 원본 라이선스 전문 |
| `scripts/Restore-MulticoreTsne.ps1` | 고정 소스 다운로드, MSVC x64/OpenMP DLL 빌드, 실행 검증 |
| `scripts/AlternativeTsne.Dependencies.targets` | 네이티브 DLL·OpenMP 런타임·라이선스를 호스트 출력에 복사 |

새 C# 클래스 파일은 추가하지 않았다. 네이티브 연결 코드는 C++ 한 파일이고, 내려받은 원본 소스·생성된 vcxproj·DLL은 Git에서 제외된 `packages/AlternativeTsne` 아래에만 저장된다.

## 빌드와 실행

Visual Studio의 **Desktop development with C++**, Windows SDK, .NET Framework 4.5.1 targeting pack이 필요하다.

```powershell
# 이전 두 엔진이 이미 준비되어 있다면 새 네이티브 엔진만 복원
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Restore-MulticoreTsne.ps1

# 처음 설치할 때: 세 엔진 전체 복원 (위 스크립트도 호출)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Restore-AlternativeTsne.ps1
```

네이티브 빌드는 `/openmp`, 최적화, precise 부동소수점, 정적 MSVC CRT(`/MT`)를 사용한다. OpenMP가 빠지면 연결부의 `#error`로 빌드에 실패한다. 런타임 파일은 다음과 같다.

```text
SKhynix.TAS.Analysis.Tsne.dll
MulticoreTsne.Native.dll
vcomp140.dll
Multicore-TSNE.LICENSE.txt
```

Multicore만 실행하는 별도 프로세스에서 위 파일만으로 검증했다. Python, Accord, Hybrid, MathNet/MKL, TsneCSharp DLL을 로드하지 않는다. 기존 전체 비교 폼에는 다른 엔진용 의존성도 함께 복사된다.

`SKhynix.TAS.UI.Report.Pccb`를 시작 프로젝트로 실행하면 Multicore와 가상 데이터를 선택한다. `Library`에서 엔진을 바꾸고 `Threads`에서 스레드 수를 고른 뒤 `Draw Chart`를 누른다. 계산 중 설정 변경을 잠그고, 엔진 또는 스레드 수 변경 시 이전 결과를 지운다. `Analysis Log`에 실제 OpenMP 팀 크기, theta, 최종 근사 KL divergence를 기록한다.

## 호출 예시

```csharp
using TsneRunner = SKhynix.TAS.Analysis.Tsne.Tsne;

var result = TsneRunner.FitTransform(standardizedMatrix,
    new TsneRunner.Options
    {
        Engine = TsneRunner.Engine.Multicore,
        NumberOfThreads = Math.Min(4, Environment.ProcessorCount),
        Iterations = 1000,
        Perplexity = 30,
        LearningRate = 200,
        RandomSeed = 42,
        Theta = 0.5
    });
double[][] xy = result.Coordinates;
int actualThreads = result.NumberOfThreads;
double finalKl = result.KullbackLeiblerDivergence;
```

기존 폼에는 `TSNELibraryEngine = TsneRunner.Engine.Multicore`, `TSNENumberOfThreads`, `TSNETheta`를 설정한다. DataTable 입력 API와 회사 데이터 컬럼 규칙은 그대로다. 기존 분석 옵션에도 같은 속성을 전달한다.

## ABI와 적용 설정

- `double[][]`를 **행 우선 `double[]`**로 변환해 호출한다. C++가 입력을 정규화하므로 호출자 행렬 대신 복사본을 전달한다. 결과는 원본 행 순서의 `N×2`다.
- C 함수 `tas_tsne_run`은 Cdecl, 버퍼와 길이, 32비트 정수와 double만 사용한다. C++ `bool`은 공개 연결 경계를 넘기지 않는다. 반환 코드와 오류 문자열을 C# 예외로 변환한다.
- `tas_tsne_abi_version() == 1`을 확인하며, 누락·x86/x64 불일치·호환되지 않는 DLL은 복원 안내와 함께 실패한다. 다른 엔진으로 자동 전환하지 않는다.
- 최소 4행/2개 feature, 유한한 값, 행 사이 변화가 필요하다. perplexity는 기존 비교 정책과 동일하게 정수 `min(요청값, floor((N-1)/3))`를 적용한다.
- `NumberOfThreads` 기본값은 `min(4, Environment.ProcessorCount)`이며 1 이상이어야 한다. 0/-1은 자동 선택 의미로 사용하지 않는다. 실제 OpenMP 팀 크기를 실행 시 확인해 반환한다.
- 학습률·반복 수·0 이상의 seed·`0 < theta <= 1`은 원본에 전달한다. 거리 모드는 원본의 squared Euclidean, 출력 2차원, random 초기화, exaggeration 12, early-exaggeration 설정 250을 사용한다. PCA 초기화는 사용하지 않는다.
- 원본의 난수와 OpenMP 상태 간섭을 방지하도록 **동시 FitTransform 요청은 네이티브 입구에서 직렬화**한다. 한 번의 계산 안에서는 지정한 수의 OpenMP 스레드로 병렬 실행한다. 스레드 설정은 호출 종료 시 복원한다.
- 최종 KL은 원본이 계산한 근삿값이다. 다른 엔진과 초기화/확률 정의가 달라 값만으로 모든 엔진의 품질을 직접 순위 매기지 않는다. 스레드 수·컴파일러·하드웨어가 달라지면 부동소수점 오차가 달라질 수 있다.

## 검증 결과

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File diagnostics\tsne-alternatives\Verify-AlternativeTsne.ps1
```

임시 디렉터리에서 x64 호스트와 클래스 라이브러리를 빌드하고 실제 네이티브 함수를 실행했다.

- 96행, 1,000회 반복, 1/2/4개 실제 OpenMP 스레드: 유한한 2D 결과와 최종 KL 확인
- 동일 옵션에서 원본 `tsne_run_double` 직접 호출과 래퍼 결과의 **좌표 완전 일치**, KL 허용 오차 `1e-10` 이내
- 4행/12행과 중복 행, 입력 원본 보존, 같은 설정 반복, 서로 다른 seed의 동시 요청 검증
- 잘못된 버퍼 길이, 잘못된 설정, 상수 입력, DLL 누락 오류 확인
- Accord → CSharp → Hybrid → Multicore → CSharp 전환에서 DataTable 행 인덱스·export 좌표·표준화·feature·KNN 유지
- Multicore 전용 실행에서 다른 분석 라이브러리 없이 실행되고 `vcomp140.dll`이 실제 로드됨을 확인

기존 체크아웃의 변경된 `bin/obj`는 건드리지 않았다. 데스크톱 화면 자동 조작 검증은 수행하지 않았으며, 위 검증은 빌드와 실제 계산/데이터 경로에 대한 것이다.

## 원본 표시

원본 `LICENSE.txt`에는 광고 자료의 acknowledgement를 포함한 네 가지 조건이 있다. 단순히 다른 패키지 메타데이터의 BSD-3-Clause 표기를 그대로 적용하지 않고, 실제 고정 커밋의 라이선스 전문을 소스 및 배포 출력에 포함했다.

This product includes software developed by the Delft University of Technology.
