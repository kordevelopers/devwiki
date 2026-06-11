# LightningBar 사용 메뉴얼

## 1. 개요
`LightningBar`는 WinForms(`.NET Framework 4.5.1`)용 Bar Chart 컨트롤입니다.

- 데이터 바인딩: Category + Series
- 옵션 기반 렌더링 제어
- 런타임 옵션 변경
- 클릭 이벤트 처리
- RawData 버튼/팝업
- 이미지 렌더링/저장

---

## 2. 빠른 시작

```csharp
var chart = LightningBar.Create(
    parent: pnlChartHost,
    newCategories: new[] { "품질", "생산성", "안전" },
    newSeries: new[]
    {
        new LightningBarSeries
        {
            Name = "설비 A",
            Values = new[] { 88f, 82f, 91f },
            FillColor = Color.LightBlue,
            BorderColor = Color.SteelBlue
        }
    },
    barOptions: new LightningBarOptions());

chart.SeriesClicked += (s, e) =>
{
    MessageBox.Show($"{e.Series.Name} / {e.CategoryName} / {e.Value}");
};
```

---

## 3. 데이터 모델

### 3.1 `LightningBarSeries`
- `Name`: 시리즈 이름
- `LegendLabel`: 레전드 표시 라벨(멀티라인 가능)
- `Values`: 카테고리별 값 배열
- `FillColor`, `BorderColor`: bar 색상

### 3.2 데이터 설정 API
- `SetData(IEnumerable<string> categories, IEnumerable<LightningBarSeries> series)`
- `SetCategories(...)`
- `SetSeries(...)`
- `UpdateData(categories, series, options)`
- `Clear()`, `ClearData()`, `Reset()`

---

## 4. 생성/배치 API

- `LightningBar.Create(parent, categories, series, options)`
- `LightningBar.AttachTo<T>(parent, dock, bounds, options)`
- `AddTo(Control parent)`

---

## 5. 옵션 구조 (기능별 분류)

## 5.1 타이틀 옵션
`options.TitleOptions`

주요 속성:
- `Text`
- `Position`: `TopLeft`, `TopCenter`, `TopRight`
- `FontSize`, `FontStyle`, `Color`
- `MarginTop`, `MarginHorizontal`
- `Visible`

예시:
```csharp
options.TitleOptions.Text = "월간 KPI";
options.TitleOptions.Position = LightningBarTitlePosition.TopCenter;
options.TitleOptions.FontSize = 14f;
```

## 5.2 레이아웃 옵션
`options.Layout`

- `ChartPadding`
- `TopOffset`
- `LegendReservedWidth`
- `CategoryLabelReservedWidth`
- `BottomScaleAreaHeight`

## 5.3 레전드 옵션
`options.Legend`

- `Visible`
- `Position`: `Top`, `Bottom`
- `Alignment`: `Left`, `Center`, `Right`
- `MarginFromChart`
- `FontSize`, `TextColor`
- `MarkerWidth`, `MarkerHeight`
- `LabelMaxWidth`, `LabelMaxLines`
- `ItemSpacing`, `SectionSpacing`

예시:
```csharp
options.Legend.Visible = true;
options.Legend.Position = LightningBarLegendPosition.Bottom;
options.Legend.Alignment = LightningBarLegendAlignment.Right;
```

## 5.4 축/스케일 옵션
`options.Scale`

- `GridLineCount`
- `FontSize`
- `LabelColor`
- `AxisColor`
- `GridColor`
- `MaxValue`

## 5.5 Y축(카테고리 라벨) 옵션
`options.CategoryLabels`

- `FontSize`
- `Color`
- `MaxLines`
- `LineSpacing`

예시:
```csharp
options.CategoryLabels.MaxLines = 4;
options.CategoryLabels.LineSpacing = 3f;
```

## 5.6 바(시리즈 높이 포함) 옵션
`options.Bars`

- `HeightMode`: `Manual`, `Auto`
- `FixedHeight`: 고정 높이
- `ReferenceSeriesCount`: 기준 시리즈 개수
- `Gap`: 시리즈 간 간격
- `GroupPaddingRatio`
- `BorderWidth`
- `MinHeight`

시리즈 높이 고정 추천 예시:
```csharp
options.Bars.HeightMode = LightningBarHeightMode.Manual;
options.Bars.FixedHeight = 16f;
options.Bars.ReferenceSeriesCount = 5;
options.Bars.Gap = 6f;
```

## 5.7 시리즈 라벨 옵션
`options.SeriesLabels`

- `Enabled`
- `FontSize`, `Color`
- `MaxWidth`, `MaxLines`

## 5.8 툴팁 옵션
`options.Tooltip`

- `Enabled`
- `Format`

포맷 플레이스홀더:
- `{0}`: series label
- `{1}`: category label
- `{2}`: value
- `{3}`: series index
- `{4}`: category index

## 5.9 NoData 옵션
`options.NoData`

- `DisplayMode`: `HideChartAndMessage`, `OverlayOnChartWatermark`
- `Text`
- `TextColor`
- `FontName`, `FontSize`
- `ShowWhenDataMissing`
- `ShowWhenAllValuesZero`
- `IncludeTitle`
- `BadgeBackColor`
- `BadgeBackOpacity` (0~255)
- `BadgeBorderColor`

모드 설명:
- `HideChartAndMessage`: 기존 방식. no-data 상태에서 차트를 숨기고 메시지만 표시
- `OverlayOnChartWatermark`: 신규 방식. 차트 프레임 위에 둥근 사각형 워터마크 메시지 표시

예시(오버레이 워터마크 스타일):
```csharp
options.NoData = new LightningBarNoDataOptions
{
    DisplayMode = LightningBarNoDataDisplayMode.OverlayOnChartWatermark,
    Text = "데이터가 없습니다.",
    IncludeTitle = true,
    FontName = "맑은 고딕",
    FontSize = 12f,
    TextColor = Color.FromArgb(100, 100, 100),
    BadgeBackColor = Color.White,
    BadgeBackOpacity = 220,
    BadgeBorderColor = Color.FromArgb(180, 180, 180),
    ShowWhenDataMissing = true,
    ShowWhenAllValuesZero = true
};
```

## 5.10 RawData 버튼 옵션
`options.RawData`

- `ButtonMode`: `Hidden`, `Visible`
- `ButtonText`
- `ButtonWidth`, `ButtonHeight`
- `MarginTop`, `MarginRight`

예시:
```csharp
options.RawData.ButtonMode = LightningBarRawDataButtonMode.Visible;
options.RawData.ButtonText = "원본데이터";
```

## 5.11 이미지 옵션
`options.Image`

- `Width`, `Height`
- `FileFormat`: `Png`, `Jpeg`
- `SaveDirectory`
- `FileName`
- `JpegQuality`

---

## 6. 런타임 업데이트 패턴

## 6.1 옵션만 변경
```csharp
chart.UpdateOptions(o =>
{
    o.TitleOptions.Text = "실시간 모니터링";
    o.Bars.FixedHeight = 14f;
    o.Legend.Alignment = LightningBarLegendAlignment.Left;
});
```

## 6.2 데이터와 옵션 동시 변경
```csharp
chart.UpdateData(categories, series, options);
```

---

## 7. 이벤트

## 7.1 시리즈 클릭 이벤트
- 이벤트: `SeriesClicked`
- 인자: `LightningBarSeriesClickEventArgs`
  - `CategoryName`, `CategoryIndex`
  - `Series`, `SeriesIndex`
  - `Value`

예시:
```csharp
chart.SeriesClicked += (s, e) =>
{
    // 상세 화면 이동 등
    MessageBox.Show($"{e.Series.Name} / {e.CategoryName} / {e.Value:0.###}");
};
```

---

## 8. 이미지 API

- `Bitmap RenderImage()`
- `Bitmap RenderImage(LightningBarImageOptions imageOptions)`
- `string SaveImage()`
- `string SaveImage(LightningBarImageOptions imageOptions)`
- `string SaveImage(string filePath, LightningBarImageOptions imageOptions)`
- `static Bitmap RenderImage(categories, series, options, imageOptions)`
- `static string SaveImage(categories, series, options, imageOptions)`
- `static Image LoadImage(string imagePath)`

예시:
```csharp
string path = chart.SaveImage(new LightningBarImageOptions
{
    Width = 1280,
    Height = 720,
    FileFormat = LightningBarImageFileFormat.Png,
    SaveDirectory = @"C:\Temp",
    FileName = "kpi_chart"
});
```

---

## 9. 호환 속성(기존 코드용)

`LightningBarOptions`에는 기존 평면 속성도 유지됩니다.

예)
- `Title`, `TitleColor`, `TitleFontSize`
- `BarHeightMode`, `FixedBarHeight`, `BarGap`
- `LegendFontSize`, `LegendMarkerWidth` 등

신규 개발은 그룹 옵션(`TitleOptions`, `Bars`, `Legend` 등) 사용을 권장합니다.

---

## 10. 권장 설정 템플릿

```csharp
var options = new LightningBarOptions
{
    TitleOptions = new LightningBarTitleOptions
    {
        Text = "생산 지표",
        Position = LightningBarTitlePosition.TopCenter,
        FontSize = 13f
    },
    Bars = new LightningBarBarOptions
    {
        HeightMode = LightningBarHeightMode.Manual,
        FixedHeight = 16f,
        ReferenceSeriesCount = 5,
        Gap = 6f
    },
    Legend = new LightningBarLegendOptions
    {
        Position = LightningBarLegendPosition.Top,
        Alignment = LightningBarLegendAlignment.Center
    },
    RawData = new LightningBarRawDataOptions
    {
        ButtonMode = LightningBarRawDataButtonMode.Visible
    },
    NoData = new LightningBarNoDataOptions
    {
        DisplayMode = LightningBarNoDataDisplayMode.OverlayOnChartWatermark,
        Text = "데이터가 없습니다.",
        IncludeTitle = true,
        ShowWhenDataMissing = true,
        ShowWhenAllValuesZero = true,
        BadgeBackColor = Color.White,
        BadgeBackOpacity = 220,
        BadgeBorderColor = Color.FromArgb(180, 180, 180)
    }
};
```
