# Accord 없는 t-SNE 비교 라이브러리

새 라이브러리의 C# 코드는 **`Tsne.cs` 한 파일**이다. `Options`, `Result`, 엔진 선택과 두 GitHub 구현의 호출부를 이 파일에 모았다. `.NET Framework 4.5.1` 클래스 라이브러리이며 Accord, Newtonsoft.Json, LightningChart를 참조하지 않는다. 외부 엔진 DLL은 별도로 필요하다.

기존 `SKhynix.TAS.UI.Report.Pccb` 테스트 폼에 엔진 선택을 연결했다. 기존 DataTable/JSON 파싱, 수치 feature 선택, 평균 대치, 표준화, DRAFT_NO 검색, 원본 feature 공간의 KNN, LightningChart를 그대로 사용한다.

## 실행

저장소 루트에서 최초 한 번 실행한다. Windows, Visual Studio Roslyn 컴파일러, .NET Framework 4.5.1 targeting pack이 필요하다.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\Restore-AlternativeTsne.ps1
```

스크립트는 고정된 Git 커밋의 원본 소스를 내려받아 DLL로 컴파일하고 MathNet/MKL 패키지 체크섬을 검증한다. 이 머신에서는 복원과 두 원본 엔진 실행 확인을 완료했다.

Visual Studio에서 `SKhynix.TAS.UI.Report.Pccb`를 시작 프로젝트로 실행한다. 호스트는 `x64`로 설정되어 있고 필요한 DLL은 빌드 시 출력 폴더에 복사된다.

1. 시작하면 RESPONSE/DEFECT 각각 96행인 가상 데이터가 로드되고 `tsne-csharp` 차트를 그린다.
2. 상단 `Library`에서 `Hybrid t-SNE`를 선택하고 `Draw Chart`를 누른다.
3. 같은 원본 데이터로 결과를 비교하고 `Analysis Log`에서 실제 엔진, perplexity, 반복 횟수, 학습률, 시드와 소요 시간을 확인한다.
4. DRAFT_NO 검색과 KNN 그리드는 기존 방식으로 사용한다. 엔진 변경 시 이전 차트·그리드 결과는 지워진다.

`Accord.NET (comparison)`은 기존 기준 결과를 확인하는 선택 항목이다. 새 라이브러리의 두 엔진 실행은 Accord를 호출하지 않는다. 대체 엔진은 매번 계산하며 Accord 결과 캐시를 사용하지 않는다.

## 기존 DataTable로 폼 사용

기존 컬럼 `DRAFT_NO`, `PARAM_TYP`, `CONV_EXPER_CTN`, `AI_RSLT_VAL`, `ENGR_RSLT_VAL`의 의미와 파싱 규칙은 동일하다.

```csharp
using TsneRunner = SKhynix.TAS.Analysis.Tsne.Tsne;

var form = new SKhynix.TAS.UI.Report.Pccb.TSNEChartForm
{
    TSNELibraryEngine = TsneRunner.Engine.Hybrid,
    TSNEIterations = 1000,
    TSNEPerplexity = 30,
    TSNELearningRate = 200,
    TSNERandomSeed = 42,
    ShowAnalysisLogButton = true
};
// 기존 메서드는 테이블을 준비한다. 계산은 Draw Chart 버튼에서 시작한다.
await form.LoadConvExperimentDataTableAsync(table);
using (form) form.ShowDialog(owner);
```

엔진 값만 `TsneRunner.Engine.CSharp`로 바꾸면 같은 폼에서 tsne-csharp를 사용한다. `TSNEScatterAnalysisOptions.TSNELibraryEngine`과 `TSNEAnalysisOptions.TSNELibraryEngine`으로 폼 없는 기존 분석 파이프라인에도 전달할 수 있다. 이 기존 옵션들의 `null` 기본값은 Accord 호출 호환성을 유지한다. 테스트 폼과 새 `Tsne.Options` 기본값은 CSharp다.

## 클래스 라이브러리 직접 호출

```csharp
using TsneRunner = SKhynix.TAS.Analysis.Tsne.Tsne;

// 행 = 샘플, 열 = feature. 공정한 비교를 위해 같은 표준화 행렬을 전달한다.
TsneRunner.Result result = TsneRunner.FitTransform(standardizedMatrix,
    new TsneRunner.Options
    {
        Engine = TsneRunner.Engine.Hybrid, // 또는 CSharp
        Perplexity = 30,
        Iterations = 1000,
        LearningRate = 200,
        RandomSeed = 42
    });
double[][] xy = result.Coordinates; // 원본 행 순서 그대로 [N][2]
```

입력 배열은 복사하여 원본이 변하지 않는다. `Coordinates`도 복사본을 반환한다. 독립 API는 행렬만 받으며 표준화는 호출자가 수행한다. 기존 폼/파이프라인은 이미 표준화를 수행한다.

## 엔진별 실제 동작

| 항목 | Hybrid t-SNE | tsne-csharp |
|---|---|---|
| 원본 | [Orlinski/Hybrid_t-SNE](https://github.com/Orlinski/Hybrid_t-SNE) | [jdmccaffrey/tsne-csharp](https://github.com/jdmccaffrey/tsne-csharp) |
| 고정 커밋 | `8a19f4e19ffc0b8e5b142533401e157972e2cdbf` | `7739a9ded14ff004b1b213146858ce25f8a97386` |
| 계산 | 원본 auto 모드: Barnes-Hut/FFT 선택 | 원본 dense exact 계산 |
| 반복 횟수 | 요청값 적용 | 요청값 적용 |
| 학습률 / 시드 | 요청값 적용 | 원본 고정값 **500 / 1** |
| 최소 행 수 | 4 | 3 |
| 기본 행 제한 | 별도 제한 없음 | 2,000 (`MaximumCSharpRows`로 명시적 변경) |
| 의존성 | HybridTsne, MathNet, 네이티브 MKL x64 | TsneCSharp |

두 구현에 전달할 perplexity는 정수이며 `min(요청값, max(1, floor((행 수 - 1) / 3)))`를 내림하여 사용한다. Hybrid 이웃 수 제약과 tsne-csharp의 정수 API에 맞춘 값이며 `EffectivePerplexity`로 확인한다. Hybrid의 LSH 트리 수를 조정해 작은 데이터와 중복 행에서 원본 구현의 빈 partition 재귀 문제를 피한다.

`tsne-csharp`에는 학습률/시드 설정 API가 없다. 원본 알고리즘을 수정하지 않았으므로 전달된 `LearningRate`/`RandomSeed`와 관계없이 결과에는 실제 값 500/1을 기록한다. dense 구현의 메모리 사용량은 행 수의 제곱에 비례하므로 폼에서는 2,000행을 넘으면 안내 오류가 나온다.

Hybrid는 내부에서 float 변환·정규화와 자체 초기화를 수행하며 auto 모드가 실행 시간에 따라 계산 방식을 선택한다. 시드를 고정해도 실행 간 완전히 같은 좌표를 보장하지 않는다. 두 구현의 초기화·최적화 차이 때문에 좌표 자체가 같아야 하는 비교는 아니다. 로그의 대체 엔진 시간은 내부 전처리·초기화·최적화를 포함하며 PCA 초기화는 사용하지 않는다.

## 복사 및 의존성

다른 프로젝트에서 직접 사용하려면 `Tsne.cs` 한 파일 또는 빌드된 `SKhynix.TAS.Analysis.Tsne.dll`을 가져간다. 컴파일 참조는 `HybridTsne.dll`, `TsneCSharp.dll`이며 Hybrid 실행 시 다음 파일을 실행 파일 옆에 둔다.

```text
HybridTsne.dll
TsneCSharp.dll
MathNet.Numerics.dll
MathNet.Numerics.MKL.dll
libiomp5md.dll
```

기존 폼·데이터 처리·차트 코드는 기존 프로젝트에서 재사용한다. “한 파일”은 새 엔진 래퍼의 소스 파일 수이며 외부 DLL이나 기존 폼 전체를 한 파일에 합친다는 뜻은 아니다.

원본 소스와 빌드한 외부 DLL은 Git에서 제외된 `packages/AlternativeTsne`에만 저장한다. tsne-csharp는 MIT이며 해당 고정 Hybrid 커밋에는 LICENSE 파일이 없어 이 작업에서는 원본 소스/DLL을 저장소에 포함하지 않았다. 저장된 출처·라이선스 문서는 `packages/AlternativeTsne/licenses`, 버전 정보는 `manifest.json`에서 확인한다.

## 검증

```powershell
powershell -ExecutionPolicy Bypass -File diagnostics\tsne-alternatives\Verify-AlternativeTsne.ps1
```

빌드와 실행 검증은 새 임시 디렉터리에서 수행하여 기존 checkout의 `bin/obj`를 건드리지 않는다. 실제 두 엔진 실행, 입력 보존, 유한한 N×2 결과, CSharp 원본 호출과의 일치, 엔진별 적용값, 잘못된 입력, 중복 행, 기존 DataTable/KNN 연결을 확인한다. 기존 `diagnostics/tsne-performance/Verify-TsnePerformance.ps1 -WorkingTree`의 Accord 회귀 검증도 통과했다.

화면 자동 조작은 이 세션의 데스크톱 도구 접근 권한 오류로 확인하지 못했다. 폼 프로젝트 빌드와 실제 계산·데이터 연결 검증은 완료했으며, 화면 확인은 위 실행 절차로 진행한다.
