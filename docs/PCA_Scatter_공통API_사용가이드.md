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

## Oracle Exadata CONV_EXPER_CTN 사용

운영 화면의 기본 데이터 경로는 다음 쿼리입니다.

```sql
SELECT
    J.ENGR_RSLT_VAL AS LABEL_Y,
    M.*
FROM TASADM.PCCB_INFER_RSLT_INF M
LEFT JOIN TASDEV.PCCB_JUDGE_RSLT_INF J
    ON M.DRAFT_NO = J.DRAFT_NO
   AND M.PARAM_TYP = J.PARAM_TYP
WHERE M.CHG_TM > SYSDATE - 10
  AND J.ENGR_RSLT_VAL IS NOT NULL
```

DB 행 하나가 PCA 실험 한 건입니다. 식별자는 `M.DRAFT_NO`, 모집단 구분은
`M.PARAM_TYP`, Y 라벨은 `LABEL_Y`, 특징 데이터는 `M.CONV_EXPER_CTN`을 사용합니다.
JSON 배열에는 실험 객체가 한 건 들어 있어야 합니다. `PUB_NO`, `_VERSION_NM` 및
나머지 문자열은 특징에서 제외하고 유한한 숫자값만 PCA/KNN에 사용합니다.

```xml
<connectionStrings>
  <add name="PcaExadataDatabase"
       connectionString="운영 환경 Oracle 연결 문자열"
       providerName="Oracle.ManagedDataAccess.Client" />
</connectionStrings>
<appSettings>
  <add key="PcaExadataQuery"
       value="위 운영 SQL" />
  <add key="PcaExadataJsonColumn" value="CONV_EXPER_CTN" />
  <add key="PcaExadataCommandTimeoutSeconds" value="120" />
</appSettings>
```

ODP.NET 공급자 종류에 따라 `providerName`은 배포 환경에 등록된 값을 사용합니다.
프로젝트는 `DbProviderFactory`로 연결하므로 연결 문자열과 공급자 등록을 소스 코드에
고정하지 않습니다.

지원 타입은 `Response`, `Defect`, `Epm`, `Probe`이며 현재 샘플 UI에는
`RESPONSE`, `DEFECT`만 표시합니다.

전체 데이터를 강제로 새로 읽고 선택 타입을 분석합니다.

```csharp
var service = new PcaExadataService();
PcaExadataAnalysisResult analysis = await service.RefreshAndAnalyzeAsync(
    PcaParameterType.Response,
    chartOptions.Analysis);

chart.Bind(analysis.AnalysisResult, chartOptions);
```

Draft 조회 시 새로고침 정책을 선택할 수 있습니다.

```csharp
PcaDraftQueryResult query = await service.QueryDraftAsync(
    "DRAFT-001",
    PcaParameterType.Response,
    PcaExadataRefreshMode.PreferMemorySnapshot,
    chartOptions.Analysis);

PcaExperimentRecord target = query.Target;
IList<KnnNeighbor> neighbors = query.Neighbors;
chart.Bind(query.AnalysisResult, chartOptions);
```

- `AlwaysReload`: Draft 조회마다 Exadata 전체 데이터를 새로 조회
- `PreferMemorySnapshot`: 정상 메모리 스냅샷이 있으면 DB를 다시 조회하지 않음
- `RefreshAndAnalyzeAsync`: 정책과 관계없이 항상 새로 조회
- 대상 Draft가 없으면 PCA 전에 중단하며 기존 차트와 정상 스냅샷을 유지

`PcaExperimentRecord`에는 원본 JSON, 평탄화 값, 숫자 특징, 표준화 벡터와 X1/X2가
함께 보관됩니다. PCA와 KNN은 같은 `StandardScalerModel` 객체와 같은 특징 순서를
사용하며 자체 검증에서 객체 참조까지 확인합니다.

### 가상 Exadata 데이터

`PcaExadataSampleDataFactory`는 DB에 접속하지 않고 운영 행 모델과 같은
`PcaExadataSnapshot`을 생성합니다. RESPONSE/DEFECT 각각 30건과 수치 특징 80개를
포함하며 실제 운영과 같은 `PcaExadataService` 분석 경로를 사용합니다.

```csharp
PcaExadataSnapshot sampleSnapshot =
    new PcaExadataSampleDataFactory(20260626).CreateDefaultSnapshot();

service.SetSnapshot(sampleSnapshot);

PcaExadataAnalysisResult sampleAnalysis = service.AnalyzeSnapshot(
    sampleSnapshot,
    PcaParameterType.Response,
    chartOptions.Analysis);
```

샘플 화면의 `가상 데이터` 버튼은 샘플 스냅샷을 메모리에 올리고
`메모리 데이터 우선`을 자동 선택합니다. 이후 `SAMPLE-R-001` 같은 Draft를 입력하면
DB 접속 없이 검색과 KNN 동작을 시연할 수 있습니다.

### 조회 Draft 포인트 강조

조회한 Draft는 기존 `LABEL_Y` 시리즈에서 분리하여 검정색 단일 포인트로 표시합니다.
강조 시리즈의 이름과 범례는 조회한 `DRAFT_NO`입니다.

```csharp
chartOptions.Series.HighlightDraftNo = query.Target.DraftNo;
chartOptions.Series.HighlightColor = Color.Black;
chartOptions.Series.HighlightPointSize = 19f;

chart.Bind(query.AnalysisResult, chartOptions);
```

강조 포인트는 원래 PASS/FAIL 시리즈에 중복으로 들어가지 않습니다.

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
