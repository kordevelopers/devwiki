# PCA Scatter 공통 API 사용가이드

## 목적

`PcaScatterChart`는 PCA Scatter 차트를 최소 코드로 생성하기 위한 Facade입니다.
`LightningScatter`는 저수준 차트 래퍼로 유지하고, PCA 업무 로직은 `PcaExadataService`,
`PcaAnalysisPipeline`, `ConvExperimentRepository`에서 처리합니다.

현재 운영 데이터 경로는 DB에 직접 접속하지 않습니다. 회사 내부 서비스 또는 화면 코드에서
데이터를 받아온 뒤, 결과 `DataTable`을 PCA 모듈에 전달합니다.

## 기본 JSON 사용

```csharp
PcaScatterChart chart = PcaScatterChart.Create(
    panelChartHost,
    PcaScatterDataSource.FromJsonSamples(jsonRows),
    PcaScatterOptions.CreateDefault());
```

## ACT_DATA DataTable 사용

```csharp
DataTable table = companyService.GetActDataTable();

PcaScatterChart chart = PcaScatterChart.Create(panelChartHost);
chart.BindFromDatabase(table, new PcaScatterDatabaseOptions
{
    ActDataColumnName = "ACT_DATA"
});
```

`ActDataRepository`도 DB에 직접 접속하지 않습니다. 회사 서비스 또는 화면 코드에서
`ACT_DATA` 컬럼을 포함한 `DataTable`을 만든 뒤 PCA 모듈에 전달합니다. 단일 JSON object,
object 배열, wrapper object, 이중 인코딩된 JSON 문자열을 파서가 처리합니다.

## CONV_EXPER_CTN DataTable 사용

회사 서비스에서 다음 형태의 `DataTable`을 만들어 전달합니다.

필수 컬럼 기본값:

- `DRAFT_NO`: 검색 및 결과 식별 기준
- `PARAM_TYP`: `RESPONSE`, `DEFECT`, `EPM`, `PROBE`
- `ENGR_RSLT_VAL`: `Review`, `Pass` 등 원본 판정 라벨
- `CONV_EXPER_CTN`: JSON 배열 문자열

DB 조회 SQL, Oracle 연결 문자열, provider 설정은 PCA 모듈이 관리하지 않습니다.
서비스에서 `M.DRAFT_NO`, `M.PARAM_TYP`, `J.ENGR_RSLT_VAL`, `M.CONV_EXPER_CTN` 컬럼을
포함한 `DataTable`을 넘겨주면 됩니다. `LABEL_Y` 컬럼명은 기존 alias 호환용으로만 허용합니다.

```csharp
DataTable table = companyService.GetConvExperimentTable();

var service = new PcaExadataService(table);
PcaExadataAnalysisResult analysis = await service.RefreshAndAnalyzeAsync(
    PcaParameterType.Response,
    chartOptions.Analysis);

chart.Bind(analysis.AnalysisResult, chartOptions);
```

컬럼명이 다르면 옵션으로 지정합니다.

```csharp
var tableOptions = new ConvExperimentQueryOptions
{
    DraftNoColumnName = "DRAFT_NO",
    ParameterTypeColumnName = "PARAM_TYP",
    LabelColumnName = "ENGR_RSLT_VAL",
    JsonColumnName = "CONV_EXPER_CTN"
};

var service = new PcaExadataService(table, tableOptions);
```

Draft 조회도 `DataTable`을 직접 넘길 수 있습니다.

```csharp
PcaDraftQueryResult query = await service.QueryDraftFromDataTableAsync(
    "DRAFT-001",
    PcaParameterType.Response,
    table,
    chartOptions.Analysis,
    tableOptions);

PcaExperimentRecord target = query.Target;
IList<KnnNeighbor> neighbors = query.Neighbors;
chart.Bind(query.AnalysisResult, chartOptions);
```

차트 Facade에서 바로 바인딩할 수도 있습니다.

```csharp
chart.BindFromExadata(table, new PcaScatterExadataOptions
{
    ParameterType = PcaParameterType.Response,
    JsonColumnName = "CONV_EXPER_CTN"
});
```

`ScatterMain` 같은 WinForms 화면에서는 외부 서비스 조회 후 다음처럼 주입합니다.

```csharp
DataTable table = companyService.GetConvExperimentTable();
await scatterMain.LoadConvExperimentDataTableAsync(table);
```

## 분석 흐름

`ConvExperimentRepository`는 `DataTable`을 `PcaExadataSourceRow` 목록으로 변환합니다.
그 뒤에는 기존 PCA/KNN 분석 흐름을 그대로 사용합니다.

1. 선택된 `PARAM_TYP` 모집단 필터링
2. `CONV_EXPER_CTN` JSON 배열에서 실험 객체 한 건 추출
3. `PUB_NO`, `_VERSION_NM`, 문자열 필드 제외
4. 모든 행에 공통으로 존재하는 유한 수치 feature 선택
5. 상수 및 저분산 feature 제거
6. 하나의 `StandardScalerModel`로 전체 모집단 표준화
7. 같은 scaler와 표준화 행렬로 PCA/KNN 생성
8. Scatter 시리즈 표시 및 Draft 클릭/KNN 조회

PCA와 KNN은 동일한 `StandardScalerModel` 객체 참조를 공유해야 하며, 검증 실패 시 분석은 중단됩니다.

## 가상 데이터

```csharp
PcaExadataSnapshot sampleSnapshot =
    new PcaExadataSampleDataFactory(20260626).CreateDefaultSnapshot();

service.SetSnapshot(sampleSnapshot);

PcaExadataAnalysisResult sampleAnalysis = service.AnalyzeSnapshot(
    sampleSnapshot,
    PcaParameterType.Response,
    chartOptions.Analysis);
```

샘플 화면의 `가상 데이터` 버튼은 DB 접속 없이 `PcaExadataSnapshot`을 만들어 같은 분석 경로를 검증합니다.

## 옵션 예시

```csharp
PcaScatterOptions options = PcaScatterOptions.CreateDefault600x400();

options.Analysis.NeighborCount = 3;
options.Analysis.ConstantVarianceThreshold = 1e-10d;

options.Display.XAxisTitle = "X1";
options.Display.YAxisTitle = "X2";
options.Display.ShowTitle = false;
options.Display.GridLinesVisible = true;

options.Series.PointSize = 15f;
options.Series.HighlightDraftNo = "DRAFT-001";
options.Series.HighlightColor = Color.Black;

options.Legend.Position = LightningScatterLegendPosition.TopCenter;
options.Tooltip.Format = "{5}\r\nX1:{1:0.###}, X2:{2:0.###}\r\nAI_RSLT_Val:{0}";
options.NoData.Text = "PCA Scatter 데이터가 없습니다.";
```

## 이벤트

```csharp
chart.SampleClicked += (sender, e) =>
{
    string draftNo = e.Sample.DraftNo;
    IList<KnnNeighbor> neighbors = e.Neighbors;
    PcaExperimentRecord raw = e.Sample.UserData as PcaExperimentRecord;
};

chart.AnalysisCompleted += (sender, e) =>
{
    PcaAnalysisResult result = e.AnalysisResult;
};

chart.AnalysisFailed += (sender, e) =>
{
    MessageBox.Show(e.Exception.Message);
};
```

## 이미지 저장

```csharp
string imagePath = chart.SaveImage(new LightningScatterImageOptions
{
    Width = 900,
    Height = 600,
    SaveFolder = LightningScatterImageSaveFolder.LocalApplicationData,
    SubDirectoryName = "PcaScatterExcelImages"
});

Image image = chart.LoadLastSavedImage();
```

`LastSavedImagePath`, `LastSavedImage`는 `PcaScatterChart`에서 그대로 제공합니다.

## 호환성 메모

- 기존 `LightningScatter.Create(...)` API는 유지됩니다.
- BarChart 코드는 이번 변경에서 수정하지 않습니다.
- `ActDataRepository` 기반 ACT_DATA 호환 API는 남아 있지만, `CONV_EXPER_CTN` 업무 경로는 `DataTable` 주입 방식만 사용합니다.
