# 수동 PCA Scatter 복사 가이드

이 폴더는 `Accord.NET`을 사용하지 않는 PCA Scatter 모듈입니다.
회사 프로젝트에서 Accord DLL을 사용할 수 없는 경우 이 폴더의 코드와 공통 Scatter 컨트롤만 복사해서 사용합니다.

## 결론

- 사용하는 Form: `ManualPcaScatterMain`
- PCA 구현: 직접 구현한 표준화, 공분산, 고유벡터 계산 로직
- KNN 구현: 직접 구현한 유클리드 거리 기반 최근접 검색
- Accord 필요 여부: 필요 없음
- 복사하지 않을 폴더: `Scatter/AccordPcaScatter`
- 복사하지 않을 DLL: `Accord.dll`, `Accord.Math.dll`, `Accord.Math.Core.dll`, `Accord.Statistics.dll`

## 복사할 파일

아래 파일은 한 묶음으로 복사합니다.

- `Scatter/ManualPcaScatter/ActDataJsonParser.cs`
- `Scatter/ManualPcaScatter/ActDataRepository.cs`
- `Scatter/ManualPcaScatter/ConvExperimentRepository.cs`
- `Scatter/ManualPcaScatter/ManualPcaScatterMain.cs`
- `Scatter/ManualPcaScatter/ManualPcaScatterMain.Designer.cs`
- `Scatter/ManualPcaScatter/ManualPcaScatterMain.resx`
- `Scatter/ManualPcaScatter/PcaAnalysisPipeline.cs`
- `Scatter/ManualPcaScatter/PcaExadataService.cs`
- `Scatter/ManualPcaScatter/PcaJsonUtility.cs`
- `Scatter/ManualPcaScatter/PcaScatterChart.cs`
- `Scatter/ManualPcaScatter/PcaScatterDataSource.cs`
- `Scatter/ManualPcaScatter/PcaScatterOptions.cs`
- `Scatter/ManualPcaScatter/PcaScatterPopupDataProvider.cs`
- `Scatter/ManualPcaScatter/PcaScatterSeriesBuilder.cs`
- `Scatter/ManualPcaScatter/ScatterSampleData.cs`
- `Scatter/Common/LightningScatter.cs`

## 필요한 외부 참조

수동 PCA 계산에는 별도 수학 라이브러리가 필요 없습니다.
다만 화면 표시와 JSON 파싱 때문에 아래 참조는 필요합니다.

- LightningChart 8 WinForms 관련 DLL
- `Newtonsoft.Json`
- `.NET Framework 4.5` 이상

## 팝업 사용 예시

화면을 먼저 띄우고, 조회 완료 후 DataTable을 전달하면 됩니다.

```csharp
using (var form = new ManualPcaScatterMain())
{
    form.Show(owner);

    DataTable sourceTable = await LoadCompanyDataAsync();
    await form.LoadConvExperimentDataTableAsync(sourceTable);

    form.Focus();
}
```

모달 팝업으로 사용할 때는 데이터를 먼저 가지고 있는 경우에만 아래처럼 사용합니다.

```csharp
using (var form = new ManualPcaScatterMain())
{
    await form.LoadConvExperimentDataTableAsync(sourceTable);
    form.ShowDialog(owner);
}
```

## DataTable 필수 컬럼

- `DRAFT_NO`
- `PARAM_TYP`
- `ENGR_RSLT_VAL`
- `CONV_EXPER_CTN`

`CONV_EXPER_CTN`에는 JSON 배열 문자열이 들어가야 합니다.
JSON 안의 수치 컬럼은 자동으로 분석 후보가 되고, 문자열/메타데이터/결측/저분산 컬럼은 제외됩니다.

## 수동 구현된 알고리즘

- 표준화: `StandardScalerModel`
- PCA: 공분산 행렬과 power iteration 기반 고유벡터 계산
- 거리 계산: 표준화된 원본 feature 공간의 Euclidean distance
- KNN 검색: Auto, BruteForce, KdTree, BallTree
- 검증: PCA와 KNN이 같은 scaler 객체를 공유하는지 확인

## 확인 방법

회사 프로젝트로 복사하기 전에 아래 문자열이 복사 대상에 없어야 합니다.

- `Accord`
- `PrincipalComponentAnalysis`
- `Accord.Statistics`

현재 `ManualPcaScatter`와 `Scatter/Common/LightningScatter.cs`에는 Accord 참조가 없습니다.
