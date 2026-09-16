# Accord 없는 t-SNE 비교 라이브러리

새 클래스 라이브러리의 C# 코드는 **`Tsne.cs` 한 파일**이다. `Options`, `Result`, Hybrid t-SNE와 tsne-csharp 호출부를 포함한다. `.NET Framework 4.5.1`을 대상으로 하며 Accord, Newtonsoft.Json, LightningChart를 참조하지 않는다.

## Visual Studio에서 실행

1. 저장소 루트의 **`TsneDemo.slnx`**를 연다. 폼, 클래스 라이브러리, 검증 프로젝트만 포함한 솔루션이다. 기존 `hynixTas.slnx`에서도 사용할 수 있다.
2. 솔루션을 오른쪽 클릭하여 **NuGet 패키지 복원**을 실행한다.
3. **`SKhynix.TAS.UI.Report.Pccb`를 시작 프로젝트로 설정**하고 F5를 누른다. 호스트는 프로젝트 내부에서 x64로 설정되어 있다.
4. 시작 시 RESPONSE/DEFECT 각각 96행인 가상 데이터로 **tsne-csharp** 차트를 그린다.
5. `Library`에서 **Hybrid t-SNE**를 선택하고 `Draw Chart`를 눌러 같은 데이터로 비교한다.

Windows, Visual Studio의 .NET 데스크톱 개발 환경과 .NET Framework 4.5.1 targeting pack이 필요하다. PowerShell 실행과 C++ SDK 설치는 필요 없다. `Accord.NET (comparison)`은 기존 기준 결과를 확인하는 선택 항목이다. 새 두 엔진은 Accord를 호출하지 않는다.

## NuGet 구성

| 패키지 | 버전 | 복원 위치 |
|---|---|---|
| `TAS.Experimental.HybridTsne` | 1.0.0 | 저장소 `.nuget/local` |
| `TAS.Experimental.TsneCSharp` | 1.0.0 | 저장소 `.nuget/local` |
| `MathNet.Numerics` | 4.7.0 | NuGet.org |
| `MathNet.Numerics.MKL.Win-x64` | 2.3.0 | NuGet.org |

두 GitHub 구현은 공식 NuGet 패키지가 확인되지 않아 고정 커밋을 컴파일한 **프로젝트 전용 로컬 패키지**로 묶었다. 별도 원격 피드에 게시하지 않는다. `NuGet.Config`가 로컬 피드와 NuGet.org를 등록하며 `packages.config`와 프로젝트 참조로 복원한다. MathNet/MKL 버전은 원본 Hybrid와 검증한 조합을 유지했다.

기존 `packages/AlternativeTsne` 폴더는 사용하지 않는다. 네이티브 MKL 파일은 공식 패키지의 MSBuild targets가 출력 폴더의 `x64` 하위 디렉터리에 복사한다. 직접 파일을 옮기거나 스크립트를 실행할 필요가 없다.

출처와 고정 커밋은 [.nuget/README.md](../.nuget/README.md), DLL 해시는 각 패키지의 `UPSTREAM.txt`에 기록했다. tsne-csharp 패키지는 MIT 라이선스를 포함한다. 고정 Hybrid 커밋에는 LICENSE 파일이 없으므로 로컬 테스트용 패키징이 재배포 권한을 부여하는 것은 아니다.

## 기존 폼에서 사용

DataTable 컬럼 `DRAFT_NO`, `PARAM_TYP`, `CONV_EXPER_CTN`, `AI_RSLT_VAL`, `ENGR_RSLT_VAL`의 규칙, 수치 feature 선택, 평균 대치, 표준화, 검색, KNN 및 차트는 기존대로 사용한다.

```csharp
using TsneRunner = SKhynix.TAS.Analysis.Tsne.Tsne;

var form = new SKhynix.TAS.UI.Report.Pccb.TSNEChartForm
{
    TSNELibraryEngine = TsneRunner.Engine.Hybrid, // 또는 CSharp
    TSNEIterations = 1000,
    TSNEPerplexity = 30,
    TSNELearningRate = 200,
    TSNERandomSeed = 42,
    ShowAnalysisLogButton = true
};
await form.LoadConvExperimentDataTableAsync(table);
using (form) form.ShowDialog(owner);
```

테이블 준비 후 `Draw Chart`를 누르면 계산한다. 엔진 변경 시 이전 차트와 그리드 결과를 지운다. `Analysis Log`에는 실제 엔진과 적용 설정, 소요 시간을 기록한다. 기존 파이프라인에는 `TSNEScatterAnalysisOptions.TSNELibraryEngine` 또는 `TSNEAnalysisOptions.TSNELibraryEngine`을 전달한다. 해당 옵션의 `null` 값은 기존 Accord 동작을 유지한다.

## 클래스 라이브러리 직접 호출

```csharp
using TsneRunner = SKhynix.TAS.Analysis.Tsne.Tsne;

var result = TsneRunner.FitTransform(standardizedMatrix,
    new TsneRunner.Options
    {
        Engine = TsneRunner.Engine.CSharp, // 또는 Hybrid
        Perplexity = 30,
        Iterations = 1000,
        LearningRate = 200,
        RandomSeed = 42
    });
double[][] xy = result.Coordinates; // 원본 행 순서의 N×2 좌표
```

입력 배열을 복사하여 원본을 보존하고 `Coordinates`도 복사본을 반환한다. 이 API의 표준화는 호출자가 수행하며 기존 폼과 파이프라인은 이미 표준화를 수행한다. 다른 프로젝트에서는 `Tsne.cs` 한 파일 또는 빌드된 DLL과 해당 NuGet 의존성을 사용한다. Hybrid 호스트에는 MKL 패키지의 targets도 적용한다.

## 엔진 차이

| 항목 | Hybrid t-SNE | tsne-csharp |
|---|---|---|
| 원본 | [Orlinski/Hybrid_t-SNE](https://github.com/Orlinski/Hybrid_t-SNE) | [jdmccaffrey/tsne-csharp](https://github.com/jdmccaffrey/tsne-csharp) |
| 계산 | auto 모드: Barnes-Hut / FFT 선택 | dense exact |
| 반복 횟수 | 요청값 적용 | 요청값 적용 |
| 학습률 / 시드 | 요청값 적용 | 원본 고정값 **500 / 1** |
| 최소 행 수 | 4 | 3 |
| 기본 행 제한 | 별도 제한 없음 | 2,000 |
| 실행 의존성 | HybridTsne, MathNet, MKL x64 | TsneCSharp |

두 엔진의 perplexity는 `min(요청값, max(1, floor((행 수 - 1) / 3)))`를 정수로 내림하여 적용한다. Hybrid의 작은 데이터와 중복 행 문제는 LSH 트리 수를 조정해 처리한다. 원본 알고리즘은 수정하지 않았다. tsne-csharp의 메모리는 행 수의 제곱에 비례하며 `MaximumCSharpRows`로 제한을 조정할 수 있다.

초기화와 최적화가 달라 두 엔진의 좌표가 같을 필요는 없다. Hybrid는 auto 모드의 실행 시간 측정도 사용하므로 같은 seed에서 완전히 동일한 좌표를 보장하지 않는다.

## 검증

**`TsneVerification`을 시작 프로젝트로 설정하고 Ctrl+F5**를 누르면 모든 검증 그룹을 별도 프로세스로 실행한다. 그룹별 60초 제한이 있으며 로그와 JSON은 표시된 임시 폴더에 저장한다.

두 엔진 실행, 입력 보존, CSharp 원본과의 정확한 좌표 일치, 기본 1,000회 반복, 중복 행, 오류 입력, CSharp 단독 의존성, Accord/CSharp/Hybrid 전환의 DataTable·KNN·export 연결 및 기존 Accord 회귀를 확인한다. 검증 코드는 별도 프로젝트에 있으며 실제 클래스 라이브러리는 계속 `Tsne.cs` 하나만 컴파일한다.
