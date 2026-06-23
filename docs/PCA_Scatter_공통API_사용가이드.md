# PCA Scatter 공통 API 사용 가이드

## 목적

`PcaScatterChart`는 `ScatterMain`에 흩어져 있던 PCA Scatter 생성 흐름을 공통화한 Facade 클래스입니다.
화면에서는 데이터와 옵션만 넘기고, 내부에서 다음 작업을 처리합니다.

- JSON 또는 ACT_DATA 문서 파싱
- 수치형 Feature 추출 및 상수/비수치 컬럼 제외
- StandardScaler 표준화
- PCA 2축 계산
- KNN 유사 데이터 검색
- `LightningScatter` 시리즈 생성 및 차트 바인딩
- 포인트 클릭, 분석 완료/실패 이벤트 처리

## 폴더 구조

- `LightingChartSamples/Scatter/Common`
  - 범용 `LightningScatter` 컨트롤
- `LightingChartSamples/Scatter/PcaScatter`
  - PCA Scatter 전용 Facade, 옵션, 데이터소스, 분석 파이프라인, ACT_DATA 로더
- `LightingChartSamples/Scatter/Samples`
  - `ScatterMain`과 샘플 Form

네임스페이스는 기존 호환성을 위해 `LightingChartSamples.Scatter`를 유지합니다.

## 최소 사용 코드

```csharp
PcaScatterChart chart = PcaScatterChart.Create(
    panelChartHost,
    PcaScatterDataSource.FromJsonSamples(jsonRows));
```

## ACT_DATA JSON 사용

```csharp
PcaScatterChart chart = PcaScatterChart.Create(
    panelChartHost,
    PcaScatterDataSource.FromActDataJson(actDataDocuments),
    PcaScatterOptions.CreateDefault());
```

`actDataDocuments`는 `SELECT ACT_DATA FROM AI_INFERNECE` 결과에서 읽은 JSON 문자열 목록입니다.
단일 JSON object, object 배열, wrapper object, 이중 인코딩된 JSON 문자열을 파서가 처리합니다.

## DB 조회 후 바인딩

```csharp
PcaScatterChart chart = PcaScatterChart.Create(panelChartHost);

chart.BindFromDatabase(new PcaScatterDatabaseOptions
{
    ConnectionStringName = "AiInferenceDatabase",
    Query = "SELECT ACT_DATA FROM AI_INFERNECE",
    CommandTimeoutSeconds = 30
});
```

WinForms UI 멈춤을 피하려면 `ScatterMain`처럼 DB 조회/분석은 `Task.Run`에서 실행하고, UI 스레드에서 `chart.Bind(result, options)`를 호출합니다.

## 옵션 사용

```csharp
PcaScatterOptions options = PcaScatterOptions.CreateDefault600x400();

options.Analysis.NeighborCount = 3;
options.Analysis.ConstantVarianceThreshold = 1e-10d;

options.Display.XAxisTitle = "X1";
options.Display.YAxisTitle = "X2";
options.Display.ShowTitle = false;
options.Display.GridLinesVisible = true;

options.Series.PointSize = 15f;
options.Series.SeriesColors["Pass"] = Color.FromArgb(151, 211, 169);
options.Series.SeriesColors["Review"] = Color.FromArgb(238, 171, 210);

options.Legend.Position = LightningScatterLegendPosition.TopCenter;
options.Tooltip.Format = "{5}\r\nX1:{1:0.###}, X2:{2:0.###}\r\nAI_RSLT_Val:{0}";
options.NoData.Text = "PCA Scatter 데이터가 없습니다.";

options.CustomizeScatterOptions = scatterOptions =>
{
    scatterOptions.XAxis.MajorDivCount = 8;
    scatterOptions.Image.SubDirectoryName = "PcaScatterImages";
};

PcaScatterChart chart = PcaScatterChart.Create(
    panelChartHost,
    PcaScatterDataSource.FromJsonSamples(jsonRows),
    options);
```

## 포인트 클릭과 KNN 결과

```csharp
chart.SampleClicked += (sender, e) =>
{
    string draftNo = e.Sample.DraftNo;
    IList<KnnNeighbor> neighbors = e.Neighbors;

    // e.Sample은 클릭한 점의 원본 PCA 결과입니다.
    // neighbors는 선택한 Draft_NO와 가장 가까운 데이터 목록입니다.
};
```

직접 검색할 수도 있습니다.

```csharp
IList<KnnNeighbor> neighbors = chart.FindNearest("DRAFT-001", 3);
```

## 분석 이벤트

```csharp
chart.AnalysisCompleted += (sender, e) =>
{
    PcaAnalysisResult result = e.AnalysisResult;
    int featureCount = result.FeatureNames.Length;
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

`LastSavedImagePath`, `LastSavedImage`도 Facade에서 그대로 제공합니다.

## 기존 코드와의 관계

- 기존 `LightningScatter.Create(...)` API는 유지됩니다.
- 기존 범용 Scatter 샘플은 `LightningScatter`를 그대로 사용합니다.
- PCA 업무 화면은 `PcaScatterChart`를 사용하면 분석/시리즈/이벤트 코드를 반복하지 않아도 됩니다.
- BarChart 코드는 이번 구조 분리에서 변경하지 않았습니다.
