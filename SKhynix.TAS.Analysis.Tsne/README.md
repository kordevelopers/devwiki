# Accord 없는 t-SNE 비교 라이브러리

새 클래스 라이브러리의 C# 코드는 **`Tsne.cs` 한 파일**이다. `Options`, `Result`, Hybrid t-SNE와 tsne-csharp 호출부를 포함한다. `.NET Framework 4.5.1`을 대상으로 하며 Accord, Newtonsoft.Json, LightningChart를 참조하지 않는다.

## Visual Studio에서 실행

1. 저장소 루트의 **`TsneDemo.slnx`**를 연다. 폼, 클래스 라이브러리, 검증 프로젝트만 포함한 솔루션이다. 기존 `hynixTas.slnx`에서도 사용할 수 있다.
2. t-SNE 의존성은 저장소의 **`lib/Tsne`**에 포함되어 있어 바로 빌드할 수 있다. 처음 폼을 빌드할 때는 기존 Accord·LightningChart·Newtonsoft.Json 패키지만 NuGet으로 복원한다.
3. **`SKhynix.TAS.UI.Report.Pccb`를 시작 프로젝트로 설정**하고 F5를 누른다. 호스트는 프로젝트 내부에서 x64로 설정되어 있다.
4. WinForms 프로젝트의 `oracle.env.example`을 `oracle.env`로 복사하고 실제 Oracle 접속정보를 입력한다. 빌드 후 **Oracle 조회**를 누르면 하단 그리드에 원본 행을 표시한다. Python 설정과는 독립적이다.
5. RESPONSE/DEFECT를 선택하고 `Draw Chart`를 눌러 **Hybrid t-SNE**로 분석한다. `Library`에서 **tsne-csharp**를 선택해 비교할 수 있다. DB 없는 테스트에는 `Virtual Data` 버튼을 사용한다.

실제 프로젝트에 코드를 옮기지 않고 단독 실행할 수 있다. Oracle 설정·기본 SQL·DLL 배치는 [Oracle WinForms 실행 안내](../docs/TSNE_Oracle_WinForms.md)를 참고한다. Oracle 드라이버는 로컬 DLL 수동 참조를 우선하고, 없으면 첫 빌드에서 공식 패키지로부터 자동 준비한다.

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

## 엔진을 한 곳에서 한 번만 설정

호스트 프로그램의 `Program.Main()`에서 **폼·서비스·옵션을 만들기 전에** 한 줄만 지정한다. 데모의 설정 위치도 `SKhynix.TAS.UI.Report.Pccb/Program.cs`이다.

```csharp
using TsneRunner = SKhynix.TAS.Analysis.Tsne.Tsne;

// 프로그램 시작 시 한 번만 설정
TsneRunner.DefaultEngine = TsneRunner.Engine.Hybrid; // 또는 CSharp
```

공통 초기값은 **Hybrid**이다. CSharp의 기본 2,000행 제한으로 큰 데이터 분석이 중단되지 않도록 변경했다. Hybrid 실행에는 x64 호스트와 위 MKL 런타임이 필요하다. `Tsne.Options`, `TSNEAnalysisOptions`, `TSNEScatterAnalysisOptions`, `TSNEScatterOptions.Analysis`, 데모 폼이 이 값을 기본 엔진으로 사용한다. 옵션을 생략한 서비스·파이프라인 호출과 `TSNEProjectionModel.FitTransform`의 5개 인수 호출도 이 설정을 따른다.

설정은 실행 중인 호스트의 공통 기본값이며 파일에 저장되지 않는다. 생성된 폼·옵션은 당시 엔진을 유지하므로 프로그램 시작 시 먼저 지정한다. 개별 `Engine` / `TSNELibraryEngine` 지정이나 데모의 엔진 선택은 비교 테스트용으로 공통 기본값보다 우선한다. 모든 호출에 공통 설정을 적용하려면 기존 호출부의 개별 엔진 대입을 제거한다. 이미 계산한 결과는 새 엔진으로 다시 분석해야 한다.

**변경된 기본 동작:** 옵션을 생략하면 더 이상 Accord로 실행하지 않는다. Accord 비교가 필요할 때만 `TSNELibraryEngine = null`을 명시하거나 `TSNEProjectionModel.FitTransform`의 6번째 인수에 `null`을 전달한다. 옵션 객체 자체를 `null`로 전달하는 것은 공통 기본값 사용이다.

DLL을 수동 배치하는 호스트는 `SKhynix.TAS.Analysis.Tsne.dll`과 `SKhynix.TAS.UI.Report.Pccb.ReportMaker.dll`을 함께 다시 빌드해 교체한다. 클래스 라이브러리의 소스는 계속 `Tsne.cs` 한 파일이다.

## 기존 폼에서 사용

DataTable 컬럼 `DRAFT_NO`, `PARAM_TYP`, `CONV_EXPER_CTN`, `AI_RSLT_VAL`, `ENGR_RSLT_VAL`의 규칙, 수치 feature 선택, 평균 대치, 표준화, 검색, KNN 및 차트는 기존대로 사용한다.

```csharp
using TsneRunner = SKhynix.TAS.Analysis.Tsne.Tsne;

var form = new SKhynix.TAS.UI.Report.Pccb.TSNEChartForm
{
    TSNEIterations = 1000,
    TSNEPerplexity = 30,
    TSNELearningRate = 200,
    TSNERandomSeed = 42,
    ShowAnalysisLogButton = true
};
await form.LoadConvExperimentDataTableAsync(table);
using (form) form.ShowDialog(owner);
```

테이블 준비 후 `Draw Chart`를 누르면 계산한다. 엔진 변경 시 이전 차트와 그리드 결과를 지운다. `Analysis Log`에는 실제 엔진과 적용 설정, 소요 시간을 기록한다. 기존 파이프라인에는 `TSNEScatterAnalysisOptions.TSNELibraryEngine` 또는 `TSNEAnalysisOptions.TSNELibraryEngine`을 전달한다. 옵션을 생성할 때 공통 기본 엔진이 적용되며, `null`을 명시하면 Accord 비교를 선택한다.

## 다른 UI에서 엔진 지정

시작 시 `TsneRunner.DefaultEngine`을 지정했다면 다른 UI에서는 엔진을 다시 지정할 필요가 없다. 아래 예제의 옵션도 공통 엔진을 사용한다.

DataTable을 사용하는 UI에서는 다음처럼 분석한다.

```csharp
using SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart;
using TsneRunner = SKhynix.TAS.Analysis.Tsne.Tsne;

var service = new TSNEExadataService(table);
var snapshot = service.SetDataTable(table);
var options = new TSNEScatterAnalysisOptions(); // 공통 기본 엔진 사용
var result = service.AnalyzeSnapshot(snapshot, TSNEParameterType.Response, options);
System.Diagnostics.Debug.WriteLine(result.AnalysisResult.TSNEModel.EngineName);
```

- 특정 호출만 다른 엔진으로 비교할 때: `options.TSNELibraryEngine` 또는 `options.Analysis.TSNELibraryEngine`을 명시한다.
- `TSNEProjectionModel.FitTransform` 직접 호출: 5개 인수는 공통 기본값, 6번째 인수는 해당 호출의 엔진을 선택한다.
- 이미 계산한 결과를 `TSNEScatterDataSource.FromAnalysisResult`로 전달하는 경우, 렌더링 옵션을 바꾸어도 재계산하지 않는다. 선택한 엔진으로 분석을 다시 실행하고 새 결과를 전달한다.

실제 실행 엔진은 `TSNEModel.EngineName`으로 확인한다. 이전 버전의 진단 요약은 `ENGINE=ACCORD`가 고정되어 있어 다른 엔진도 Accord로 표시했다. 이 표시 오류는 ReportMaker에서 수정했으므로, DLL을 수동 배치하는 UI에서는 `SKhynix.TAS.UI.Report.Pccb.ReportMaker.dll`도 다시 빌드해 교체한다.

## 클래스 라이브러리 직접 호출

```csharp
using TsneRunner = SKhynix.TAS.Analysis.Tsne.Tsne;

var result = TsneRunner.FitTransform(standardizedMatrix,
    new TsneRunner.Options
    {
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

## MaximumCSharpRows 예외가 발생할 때

`MaximumCSharpRows`는 CSharp 엔진의 기본 2,000행 제한이다. 이전 버전에서는 CSharp를 명시한 호출이 이 한도를 초과하면 예외가 발생했다. 현재는 **CSharp로 요청해도 제한을 초과하면 Hybrid로 전환하여 전체 행을 계산한다.** 5,610행의 기존 CSharp 호출도 같은 경로로 처리한다.

기본 엔진은 Hybrid로 변경했다. 기존 호스트에서 `Tsne.DefaultEngine = Tsne.Engine.CSharp`를 사용했다면 시작 코드의 한 줄을 `Hybrid`로 변경하고, 개별 `Engine = CSharp` 또는 `TSNELibraryEngine = CSharp` 지정도 제거한다. 옵션과 폼은 설정 후 새로 만든다. 수동 DLL 참조 환경에서는 수정된 `SKhynix.TAS.Analysis.Tsne.dll`을 교체하고 Hybrid 의존성 및 `x64` 런타임 폴더를 배치한다.

전환 여부는 `Tsne.Result.RequestedEngine`과 `Engine`으로 확인한다. `EngineSelectionReason`에 입력 행 수와 전환 사유가 기록되며, ReportMaker의 `TSNEModel.EngineSelectionReason`과 데모 `Analysis Log`에도 전달한다. `EngineName`과 진단 요약은 실제 계산 엔진을 표시한다. 전환 시 Hybrid에 요청한 학습률과 시드를 적용한다.

엔진 비교에서 자동 전환을 금지하려면 직접 호출의 `Tsne.Options.FallbackToHybridForLargeInputs = false`를 지정한다. 이 경우에는 기존 제한 초과 예외가 발생한다. CSharp를 반드시 사용해야 하는 테스트에서는 입력을 줄이거나 필요한 메모리와 실행 시간을 확인한 후 `MaximumCSharpRows`를 늘릴 수 있다.

수정된 `Tsne.cs` 또는 DLL을 실제 호스트에 반영해야 한다. 기존 UI가 이전 DLL을 계속 로드하면 같은 예외가 발생한다. 수동 DLL 배치는 `SKhynix.TAS.Analysis.Tsne.dll`과 `SKhynix.TAS.UI.Report.Pccb.ReportMaker.dll`을 함께 교체하고 Hybrid 의존성과 `x64` 런타임 폴더를 포함한다.

## seed 대입문 근처에서 ArgumentOutOfRangeException이 표시될 때

`int seed = engine == Engine.CSharp ? 1 : requestedSeed;`는 정수 대입이므로 범위 예외를 던지지 않는다. 표시된 줄 번호만으로 원인을 판단하지 말고 예외의 `Message`, `ParamName`, `ActualValue`, 호출 스택을 확인한다. 최적화된 코드나 실제 DLL과 다른 소스·PDB를 사용할 때 표시 위치가 달라질 수 있다.

바로 다음 검사는 Hybrid의 학습률이다. 5,610행에서 `LearningRate = 0`을 전달하면 여기서 예외가 재현된다. CSharp는 학습률을 500으로 고정해 입력값을 무시하지만, Hybrid로 직접 실행하거나 자동 전환되면 전달한 학습률이 유한한 양수여야 한다. 래퍼 기본값은 200이므로 다른 UI가 `TSNELearningRate` / `LearningRate`에 0을 다시 대입하는지 확인한다. 실행할 옵션에 200 등 양수를 전달한다.

현재 범위 예외에는 `options` 대신 정확한 항목명과 실제 값을 기록한다. 학습률 오류에는 요청 엔진, 실제 엔진, 행 수, 시드도 포함한다. 이 정보로 실제 잘못된 설정을 확인한 다음 수정한다. 시드 0, -1, `int.MinValue`, `int.MaxValue`는 로컬 Hybrid 실행 검증을 통과했다.

다른 UI의 예외 처리부에서는 다음 정보를 확인할 수 있다.

```csharp
catch (ArgumentOutOfRangeException ex)
{
    System.Diagnostics.Debug.WriteLine(ex.ToString());
    System.Diagnostics.Debug.WriteLine("ParamName=" + ex.ParamName + "; ActualValue=" + ex.ActualValue);
    System.Diagnostics.Debug.WriteLine(typeof(TsneRunner).Assembly.Location);
    throw;
}
```

`ParamName=LearningRate`이면 학습률 설정을 수정한다. 다른 항목이나 라이브러리 내부에서 던진 예외라면 해당 메시지와 호출 스택으로 별도 진단해야 한다. DLL을 교체할 때에는 같은 빌드의 PDB도 함께 배치하고 실행 중인 호스트를 다시 시작한다.

## 검증

**`TsneVerification`을 시작 프로젝트로 설정하고 Ctrl+F5**를 누르면 모든 검증 그룹을 별도 프로세스로 실행한다. 그룹별 60초 제한이 있으며 로그와 JSON은 표시된 임시 폴더에 저장한다.

두 엔진 실행, 기본 Hybrid 및 CSharp 자동 전환의 5,610행 처리, 기존 CSharp DataTable 호출의 전체 행 보존과 진단 정보, 엄격 비교 모드의 제한 오류, 잘못된 학습률과 시드 경계값 진단, 입력 보존, CSharp 원본과의 정확한 좌표 일치, 기본 1,000회 반복, 중복 행, 오류 입력, CSharp 단독 의존성, Accord/CSharp/Hybrid 전환의 DataTable·KNN·export 연결, 공통 엔진 설정의 서비스·파이프라인·직접 호출 전파 및 기존 Accord 회귀를 확인한다. 검증 코드는 별도 프로젝트에 있으며 실제 클래스 라이브러리는 계속 `Tsne.cs` 하나만 컴파일한다.

Visual Studio의 솔루션 빌드도 Debug와 Release에서 확인한다. 일반 MSBuild 실행만으로는 Visual Studio가 프로젝트 구성을 인식하는지 검증할 수 없다. 클래스 라이브러리와 검증 프로젝트의 `Debug|AnyCPU`, `Release|AnyCPU` 조건부 PropertyGroup을 유지해야 한다. 검증 실행 파일의 실제 프로세스 대상은 x64다.

이전 버전에서 `TsneVerification`의 프로젝트 참조가 해결되지 않거나 `CS0006`가 발생했다면 수정된 프로젝트를 다시 로드하고 솔루션을 다시 빌드한다. 구성 정의 누락으로 두 프로젝트가 Visual Studio 빌드에서 건너뛰어지던 문제를 수정했다. DLL 파일을 직접 참조로 추가하지 않고 기존 `ProjectReference`를 사용한다.
