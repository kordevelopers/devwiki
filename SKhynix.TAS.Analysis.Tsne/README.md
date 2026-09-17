# Accord 없는 t-SNE 비교 라이브러리

새 클래스 라이브러리의 C# 코드는 **`Tsne.cs` 한 파일**이다. `Options`, `Result`, Hybrid t-SNE와 tsne-csharp 호출부를 포함한다. `.NET Framework 4.5.1`을 대상으로 하며 Accord, Newtonsoft.Json, LightningChart를 참조하지 않는다.

## Visual Studio에서 실행

1. 저장소 루트의 **`TsneDemo.slnx`**를 연다. 폼, 클래스 라이브러리, 검증 프로젝트만 포함한 솔루션이다. 기존 `hynixTas.slnx`에서도 사용할 수 있다.
2. t-SNE 의존성은 저장소의 **`lib/Tsne`**에 포함되어 있어 바로 빌드할 수 있다. 처음 폼을 빌드할 때는 기존 Accord·LightningChart·Newtonsoft.Json 패키지만 NuGet으로 복원한다.
3. **`SKhynix.TAS.UI.Report.Pccb`를 시작 프로젝트로 설정**하고 F5를 누른다. 호스트는 프로젝트 내부에서 x64로 설정되어 있다.
4. 시작 시 RESPONSE/DEFECT 각각 96행인 가상 데이터로 **tsne-csharp** 차트를 그린다.
5. `Library`에서 **Hybrid t-SNE**를 선택하고 `Draw Chart`를 눌러 같은 데이터로 비교한다.

Windows, Visual Studio의 .NET 데스크톱 개발 환경과 .NET Framework 4.5.1 targeting pack이 필요하다. PowerShell 실행과 C++ SDK 설치는 필요 없다. `Accord.NET (comparison)`은 기존 기준 결과를 확인하는 선택 항목이다. 새 두 엔진은 Accord를 호출하지 않는다.

## DLL 직접 참조

| 파일 | 용도 | C# 참조 추가 |
|---|---|---|
| `lib/Tsne/HybridTsne.dll` | Hybrid 계산 엔진, Any CPU | 필요 |
| `lib/Tsne/TsneCSharp.dll` | tsne-csharp 계산 엔진, Any CPU | 필요 |
| `lib/Tsne/MathNet.Numerics.dll` | Hybrid 수치 계산, 4.7.0 | 필요 |
| `MathNet.Numerics.MKL.dll` | x64 네이티브 런타임 | 참조 대신 파일 복사 |
| `libiomp5md.dll` | x64 네이티브 런타임 | 참조 대신 파일 복사 |

`SKhynix.TAS.Analysis.Tsne`의 `packages.config`, 로컬 NuGet 패키지 및 복원 강제 검사를 제거했다. 프로젝트의 `HintPath`는 위 세 관리 DLL을 직접 가리킨다. DLL 자체는 이미 컴파일되어 있으므로 별도 엔진 프로젝트를 빌드할 필요가 없다. 기존 버전의 NuGet 참조와 수동 참조를 중복해서 남기지 않는다.

네이티브 파일은 `lib/Tsne/MathNet.MKL.x64.zip`에 포함된다. `Tsne.Runtime.targets`가 빌드 시 로컬 ZIP을 출력 폴더에 풀어 `bin/Debug/x64` 또는 `bin/Release/x64`에 배치한다. 다운로드나 PowerShell 실행은 하지 않는다. 압축 해제에는 MSBuild 15.8 이상이 필요하다.

출처, 고정 커밋, 수동 설정 방법은 [lib/Tsne/README.md](../lib/Tsne/README.md), 해시는 `lib/Tsne/manifest.json`에 기록했다. 라이선스는 `lib/Tsne/licenses`에 포함한다. 고정 Hybrid 커밋에는 LICENSE 파일이 없으므로 테스트용 DLL 제공이 재배포 권한을 부여하는 것은 아니다.

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

## 다른 UI에서 엔진 지정

엔진은 폼의 전역 설정이 아니라 **분석 호출에 전달하는 옵션**에서 선택한다. 기존 서비스와 파이프라인의 `TSNELibraryEngine` 기본값은 `null`이므로, 지정하지 않으면 Accord가 실행된다. 데모 폼의 기본값을 바꾸어도 다른 UI에는 적용되지 않는다.

DataTable을 사용하는 UI에서는 다음처럼 분석 직전에 지정한다.

```csharp
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart;
using TsneRunner = SKhynix.TAS.Analysis.Tsne.Tsne;

var service = new TSNEExadataService(table);
var snapshot = service.SetDataTable(table);
var options = new TSNEScatterAnalysisOptions
{
    TSNELibraryEngine = TsneRunner.Engine.CSharp // Hybrid 사용 시 Engine.Hybrid
};
var result = service.AnalyzeSnapshot(snapshot, TSNEParameterType.Response, options);
System.Diagnostics.Debug.WriteLine(result.AnalysisResult.TSNEModel.EngineName);
```

- `TSNEScatterOptions`를 받는 차트 API: `options.Analysis.TSNELibraryEngine`을 지정하고 그 옵션을 전달한다.
- `TSNEAnalysisPipeline` 직접 호출: 생성자에 전달하는 `TSNEAnalysisOptions.TSNELibraryEngine`을 지정한다.
- `TSNEProjectionModel.FitTransform` 직접 호출: 6번째 인수에 `TsneRunner.Engine.CSharp` 또는 `Hybrid`를 전달한다. 기존 5개 인수 오버로드는 Accord를 사용한다.
- 이미 계산한 결과를 `TSNEScatterDataSource.FromAnalysisResult`로 전달하는 경우, 렌더링 옵션을 바꾸어도 재계산하지 않는다. 선택한 엔진으로 분석을 다시 실행하고 새 결과를 전달한다.

실제 실행 엔진은 `TSNEModel.EngineName`으로 확인한다. 이전 버전의 진단 요약은 `ENGINE=ACCORD`가 고정되어 있어 다른 엔진도 Accord로 표시했다. 이 표시 오류는 ReportMaker에서 수정했으므로, DLL을 수동 배치하는 UI에서는 `SKhynix.TAS.UI.Report.Pccb.ReportMaker.dll`도 다시 빌드해 교체한다.

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

입력 배열을 복사하여 원본을 보존하고 `Coordinates`도 복사본을 반환한다. 이 API의 표준화는 호출자가 수행하며 기존 폼과 파이프라인은 이미 표준화를 수행한다. 다른 프로젝트에서는 `Tsne.cs` 한 파일 또는 빌드된 DLL과 위 의존성 DLL을 사용한다. Hybrid 호스트에는 `Tsne.Runtime.targets`를 적용하거나 네이티브 ZIP을 직접 풀어 배치한다.

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

Visual Studio의 솔루션 빌드도 Debug와 Release에서 확인한다. 일반 MSBuild 실행만으로는 Visual Studio가 프로젝트 구성을 인식하는지 검증할 수 없다. 클래스 라이브러리와 검증 프로젝트의 `Debug|AnyCPU`, `Release|AnyCPU` 조건부 PropertyGroup을 유지해야 한다. 검증 실행 파일의 실제 프로세스 대상은 x64다.

이전 버전에서 `TsneVerification`의 프로젝트 참조가 해결되지 않거나 `CS0006`가 발생했다면 수정된 프로젝트를 다시 로드하고 솔루션을 다시 빌드한다. 구성 정의 누락으로 두 프로젝트가 Visual Studio 빌드에서 건너뛰어지던 문제를 수정했다. DLL 파일을 직접 참조로 추가하지 않고 기존 `ProjectReference`를 사용한다.
