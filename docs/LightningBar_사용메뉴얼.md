# LightningBar 사용 메뉴얼

`LightningBar`는 WinForms 화면과 이미지 출력에서 공통으로 사용할 수 있도록 만든 가로 Bar Chart 컨트롤입니다.

이 문서는 개발자가 화면별로 옵션을 설정할 때 바로 참고할 수 있도록 기능별 사용법과 코드 예제를 정리합니다.

## 1. 기본 사용

필요한 네임스페이스:

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using LightingChartSamples;
```

가장 기본적인 생성 방식:

```csharp
string[] categories =
{
    "온도\r\n센서\r\nA라인",
    "압력\r\n센서\r\nB라인",
    "진동\r\n센서\r\nC라인"
};

LightningBarSeries[] series =
{
    new LightningBarSeries
    {
        Name = "현재값",
        LegendLabel = "현재값",
        Values = new[] { 78f, 92f, 66f },
        FillColor = Color.FromArgb(92, 168, 232),
        BorderColor = Color.FromArgb(42, 118, 190)
    }
};

LightningBarOptions options = LightningBarOptions.CreateDefault600x400();

LightningBar chart = LightningBar.Create(
    parent: pnlChartHost,
    newCategories: categories,
    newSeries: series,
    barOptions: options);

chart.Dock = DockStyle.Fill;
```

컨트롤 자체 크기를 고정하려면 WinForms 컨트롤 크기를 지정합니다.

```csharp
chart.Dock = DockStyle.None;
chart.Size = new Size(600, 400);
chart.Location = new Point(0, 0);
```

## 2. 데이터 모델

`categories`는 Y축 카테고리입니다. 문자열에 `\r\n`을 넣으면 여러 줄 라벨로 표시됩니다.

`LightningBarSeries`는 막대 시리즈입니다.

```csharp
new LightningBarSeries
{
    Name = "설비 A",
    LegendLabel = "설비 A 표시명",
    Values = new[] { 80f, 65f, 90f },
    FillColor = Color.SteelBlue,
    BorderColor = Color.Navy
};
```

값과 조회용 원본 데이터를 함께 넘겨야 하면 `DataPoints`를 사용합니다. `DataPoints`가 있으면 `DataPoint.Value` 값으로 막대를 그리고, 클릭 이벤트에서 동일한 `DataPoint`와 `UserData`를 받을 수 있습니다.

```csharp
new LightningBarSeries
{
    Name = "설비 A",
    LegendLabel = "설비 A 표시명",
    DataPoints = new[]
    {
        new LightningBarDataPoint(80f, new { EquipmentId = "EQ-A", MetricCode = "Q", LotId = "L001" }),
        new LightningBarDataPoint(65f, new { EquipmentId = "EQ-A", MetricCode = "P", LotId = "L002" }),
        new LightningBarDataPoint(90f, new { EquipmentId = "EQ-A", MetricCode = "S", LotId = "L003" })
    },
    FillColor = Color.SteelBlue,
    BorderColor = Color.Navy
};
```

이미 `float[]` 값 배열이 있고 같은 순서의 검색조건 배열이 있으면 헬퍼를 사용합니다.

```csharp
float[] values = rows
    .Select(row => Convert.ToSingle(row["VALUE"]))
    .ToArray();

new LightningBarSeries
{
    Name = "설비 A",
    LegendLabel = "설비 A 표시명",
    DataPoints = LightningBarDataPoint.FromValues(values, rows),
    FillColor = Color.SteelBlue,
    BorderColor = Color.Navy
};
```

`DataTable.Select()` 결과를 그대로 검색조건으로 보관할 수도 있습니다.

```csharp
DataRow[] rows = table.Select("EQUIPMENT_ID = 'EQ-A'");
float[] values = rows
    .Select(row => Convert.ToSingle(row["VALUE"]))
    .ToArray();

LightningBarSeries series = new LightningBarSeries
{
    Name = "설비 A",
    DataPoints = LightningBarDataPoint.FromValues(values, rows)
};

chart.BarClicked += (sender, e) =>
{
    DataRow row = e.UserData as DataRow;
    if (row == null)
    {
        return;
    }

    string equipmentId = Convert.ToString(row["EQUIPMENT_ID"]);
    string metricCode = Convert.ToString(row["METRIC_CODE"]);
    string lotId = Convert.ToString(row["LOT_ID"]);
};
```

`DataTable.Rows`를 직접 넘겨야 하면 그대로 넘길 수 있습니다. 이 경우 `values` 배열과 `table.Rows`의 순서가 같아야 합니다.

```csharp
float[] values = table.AsEnumerable()
    .Select(row => Convert.ToSingle(row["VALUE"]))
    .ToArray();

LightningBarSeries series = new LightningBarSeries
{
    Name = "설비 A",
    DataPoints = LightningBarDataPoint.FromValues(values, table.Rows)
};
```

별도 검색조건 객체로 바꾸고 싶으면 인덱스 기반 팩토리를 사용합니다.

```csharp
DataPoints = LightningBarDataPoint.FromValues(values, (index, value) => new SearchCondition
{
    EquipmentId = Convert.ToString(rows[index]["EQUIPMENT_ID"]),
    MetricCode = Convert.ToString(rows[index]["METRIC_CODE"]),
    LotId = Convert.ToString(rows[index]["LOT_ID"])
});
```

주요 속성:

| 속성 | 설명 |
| --- | --- |
| `Name` | 시리즈 이름. 범례/툴팁/클릭 이벤트에서 사용됩니다. |
| `LegendLabel` | 범례에 표시할 별도 문자열입니다. 비어 있으면 `Name`을 사용합니다. |
| `Values` | 카테고리별 값 배열입니다. |
| `DataPoints` | 카테고리별 값과 사용자 데이터를 함께 넘기는 배열입니다. 값은 `DataPoint.Value`를 사용하고, 클릭 시 `e.DataPoint`, `e.UserData`로 받을 수 있습니다. |
| `FillColor` | 막대 내부 색상입니다. |
| `BorderColor` | 막대 테두리 색상입니다. |

## 3. 데이터 바인딩과 초기화

데이터만 다시 바인딩:

```csharp
chart.SetData(categories, series);
```

데이터와 옵션을 함께 변경:

```csharp
chart.UpdateData(categories, series, options);
```

옵션만 변경:

```csharp
chart.UpdateOptions(o =>
{
    o.Legend.Alignment = LightningBarLegendAlignment.Right;
    o.Bars.FixedHeight = 30f;
});
```

조회 전 상태처럼 차트를 완전히 비우기:

```csharp
chart.Clear();
```

동일한 기능의 별칭:

```csharp
chart.ClearData();
chart.Reset();
```

`Clear()`는 카테고리, 시리즈, 마지막 저장 이미지, 마지막 저장 이미지 경로를 함께 초기화합니다.

## 4. 600x400 기본 옵션

현재 기본 옵션은 `600 x 400` 차트에 맞춰 조정되어 있습니다.

```csharp
LightningBarOptions options = LightningBarOptions.CreateDefault600x400();
```

주요 기본값:

| 영역 | 기본값 |
| --- | --- |
| 이미지 크기 | `600 x 400` |
| 차트 제목 | 빈 문자열. 제목을 그리지 않습니다. |
| 범례 | 상단 중앙 |
| Y축 라벨 | 전달한 줄바꿈 기준으로 모두 표시 |
| 막대 높이 | 수동, `30f` |
| RawData 버튼 | 숨김 |
| NoData 박스 | 차트 컨트롤 전체 영역 중앙, 텍스트 크기 기준 자동 폭 |
| 기본 폰트 | 맑은 고딕 |

## 5. 차트 제목

기본값은 제목 없음입니다. `TitleOptions.Text`가 비어 있으면 제목을 렌더링하지 않습니다.

제목을 표시하려면 필요한 화면에서만 입력합니다.

```csharp
options.TitleOptions.Text = "월간 설비 상태";
options.TitleOptions.Position = LightningBarTitlePosition.TopCenter;
options.TitleOptions.FontSize = 13f;
options.TitleOptions.Visible = true;
```

제목 위치:

```csharp
options.TitleOptions.Position = LightningBarTitlePosition.TopLeft;
options.TitleOptions.Position = LightningBarTitlePosition.TopCenter;
options.TitleOptions.Position = LightningBarTitlePosition.TopRight;
```

제목을 숨기려면:

```csharp
options.TitleOptions.Text = string.Empty;
```

또는:

```csharp
options.TitleOptions.Visible = false;
```

## 6. 레이아웃

`Layout`은 실제 차트가 그려지는 영역과 여백을 제어합니다.

```csharp
options.Layout = new LightningBarLayoutOptions
{
    ChartPadding = 20,
    TopOffset = 72,
    LegendReservedWidth = 120,
    LegendReservedWidthMode = LightningBarLegendReservedWidthMode.CollapseForTopBottomLegend,
    CategoryLabelReservedWidth = 110f,
    AutoCategoryLabelReservedWidth = true,
    MinCategoryLabelReservedWidth = 78f,
    MaxCategoryLabelReservedWidth = 150f,
    BottomScaleAreaHeight = 30f
};
```

주요 속성:

| 속성 | 설명 |
| --- | --- |
| `ChartPadding` | 차트 외곽 여백입니다. |
| `TopOffset` | 상단 영역 높이입니다. 범례/제목과 차트 사이를 조정합니다. |
| `LegendReservedWidth` | 우측 범례 등을 위해 예약할 폭입니다. |
| `LegendReservedWidthMode` | 상/하단 범례일 때 우측 예약 폭을 접을지 결정합니다. |
| `CategoryLabelReservedWidth` | Y축 카테고리 라벨 영역 폭입니다. |
| `AutoCategoryLabelReservedWidth` | 라벨 길이에 따라 Y축 라벨 영역을 자동 계산합니다. |
| `MinCategoryLabelReservedWidth` | 자동 계산 시 최소 폭입니다. |
| `MaxCategoryLabelReservedWidth` | 기존 호환용 폭 기준입니다. 실제 라벨이 더 길면 텍스트가 잘리지 않도록 필요한 폭을 우선 확보합니다. |
| `BottomScaleAreaHeight` | X축 눈금 라벨 영역 높이입니다. |

Y축 라벨 영역은 `AutoCategoryLabelReservedWidth = true`일 때 실제 텍스트 폭을 측정해 자동 확장됩니다. 이미지 저장 시에도 3줄 라벨의 모든 글자가 보이도록 이 계산을 사용합니다.

```csharp
options.Layout.AutoCategoryLabelReservedWidth = true;
options.Layout.MaxCategoryLabelReservedWidth = 180f;
options.Layout.CategoryLabelReservedWidth = 140f;
```

## 7. 차트 영역 외곽선 표시

`ChartAreaOutline`은 차트가 실제로 차지하는 영역을 확인하기 위한 디버그성 표시 옵션입니다. 기본값은 `None`이라 기존 화면에는 외곽선이나 배경색이 표시되지 않습니다.

```csharp
options.ChartAreaOutline = new LightningBarChartAreaOutlineOptions
{
    Mode = LightningBarChartAreaOutlineMode.None,
    BorderWidth = 1f,
    BorderColor = Color.FromArgb(90, 120, 170),
    BackColor = Color.Transparent,
    BackOpacity = 0
};
```

플롯 영역만 확인:

```csharp
options.ChartAreaOutline.Mode = LightningBarChartAreaOutlineMode.PlotArea;
options.ChartAreaOutline.BorderWidth = 2f;
options.ChartAreaOutline.BorderColor = Color.Red;
options.ChartAreaOutline.BackColor = Color.Yellow;
options.ChartAreaOutline.BackOpacity = 32;
```

차트 컨트롤 전체 영역 확인:

```csharp
options.ChartAreaOutline.Mode = LightningBarChartAreaOutlineMode.ControlBounds;
```

| 값 | 설명 |
| --- | --- |
| `None` | 외곽선과 배경을 표시하지 않습니다. 기본값입니다. |
| `PlotArea` | 막대와 축이 그려지는 플롯 영역 기준으로 표시합니다. |
| `ControlBounds` | `LightningBar` 컨트롤 전체 크기 기준으로 표시합니다. |

## 8. 범례

범례는 상단/하단 위치와 좌측/중앙/우측 정렬을 지원합니다.

```csharp
options.Legend = new LightningBarLegendOptions
{
    Visible = true,
    Position = LightningBarLegendPosition.Top,
    Alignment = LightningBarLegendAlignment.Center,
    MarginFromChart = 8f,
    FontSize = 7.5f,
    MarkerWidth = 22f,
    MarkerHeight = 14f,
    LabelMaxWidth = 110f,
    LabelMaxLines = 3,
    ItemSpacing = 6f,
    SectionSpacing = 20f
};
```

좌측 상단:

```csharp
options.Legend.Position = LightningBarLegendPosition.Top;
options.Legend.Alignment = LightningBarLegendAlignment.Left;
```

가운데 상단:

```csharp
options.Legend.Position = LightningBarLegendPosition.Top;
options.Legend.Alignment = LightningBarLegendAlignment.Center;
```

우측 상단:

```csharp
options.Legend.Position = LightningBarLegendPosition.Top;
options.Legend.Alignment = LightningBarLegendAlignment.Right;
```

범례를 숨기려면:

```csharp
options.Legend.Visible = false;
```

범례 텍스트는 `...`로 줄이지 않습니다. `Legend.LabelMaxWidth`, `Legend.LabelMaxLines`보다 실제 텍스트가 길거나 줄이 많으면 실제 텍스트를 모두 그리는 쪽을 우선합니다.

## 9. Y축 카테고리 라벨

Y축 카테고리 라벨은 문자열에 엔터를 넣어 여러 줄로 표시합니다. 전달한 줄은 `...`로 줄이지 않고 모두 표시합니다.

```csharp
string[] categories =
{
    "PM Motor\r\nTemperature\r\nSensor A",
    "Pump\r\nPressure\r\nSensor B",
    "Line 3\r\nVibration\r\nSensor C"
};

options.CategoryLabels.MaxLines = 3;
options.CategoryLabels.FontSize = 8f;
options.CategoryLabels.LineSpacing = 1.5f;
```

라벨이 길어서 잘리면 폰트를 더 줄이기보다 Y축 라벨 영역을 넓히는 것이 좋습니다.

```csharp
options.Layout.AutoCategoryLabelReservedWidth = true;
options.Layout.MinCategoryLabelReservedWidth = 110f;
options.Layout.MaxCategoryLabelReservedWidth = 180f;
```

## 10. X축 눈금과 값 범위

기본 최대값은 `100`입니다.

```csharp
options.Scale.MaxValue = 100f;
options.Scale.GridLineCount = 5;
```

0부터 10까지 표시하려면:

```csharp
options.Scale.MaxValue = 10f;
options.Scale.GridLineCount = 10;
options.Scale.FontSize = 8f;
```

색상과 폰트:

```csharp
options.Scale.LabelColor = Color.FromArgb(95, 95, 95);
options.Scale.AxisColor = Color.FromArgb(170, 170, 170);
options.Scale.GridColor = Color.FromArgb(225, 225, 225);
```

## 11. 막대 높이와 간격

막대 높이는 자동/수동 모드를 지원합니다.

수동 고정 높이:

```csharp
options.Bars.HeightMode = LightningBarHeightMode.Manual;
options.Bars.FixedHeight = 30f;
options.Bars.ReferenceSeriesCount = 5;
options.Bars.ClampFixedHeightToGroup = true;
options.Bars.Gap = 5f;
options.Bars.GroupPaddingRatio = 0.16f;
```

자동 높이:

```csharp
options.Bars.HeightMode = LightningBarHeightMode.Auto;
```

주요 속성:

| 속성 | 설명 |
| --- | --- |
| `HeightMode` | `Auto` 또는 `Manual`입니다. |
| `FixedHeight` | 수동 모드에서 막대 1개의 높이입니다. |
| `ReferenceSeriesCount` | 고정 높이 계산 기준 시리즈 수입니다. |
| `ClampFixedHeightToGroup` | 카테고리 행 영역보다 막대가 커지지 않도록 제한합니다. |
| `Gap` | 시리즈 막대 사이 간격입니다. |
| `GroupPaddingRatio` | 카테고리 그룹 내부 여백 비율입니다. |
| `BorderWidth` | 막대 테두리 두께입니다. |

시리즈가 1개일 때 막대가 너무 크게 보이면 수동 고정 높이를 사용합니다.

```csharp
options.Bars.HeightMode = LightningBarHeightMode.Manual;
options.Bars.FixedHeight = 30f;
```

## 12. 시리즈 라벨

막대 끝에 값 라벨을 표시할지 결정합니다.

```csharp
options.SeriesLabels.Enabled = false;
```

표시하려면:

```csharp
options.SeriesLabels.Enabled = true;
options.SeriesLabels.FontSize = 8f;
options.SeriesLabels.MaxWidth = 140f;
options.SeriesLabels.MaxLines = 3;
```

막대 끝 글씨를 보이지 않게 하려면 `Enabled = false`로 둡니다.

## 13. Tooltip

막대 위에 마우스를 올리면 Tooltip을 표시할 수 있습니다.

```csharp
options.Tooltip.Enabled = true;
options.Tooltip.Format = "Value:{2:0.#} (* 클릭할 경우 해당 계측 데이터 차트로 가 보입니다.)";
```

포맷 인덱스:

| 인덱스 | 값 |
| --- | --- |
| `{0}` | 시리즈 표시명 |
| `{1}` | 카테고리 라벨 |
| `{2}` | 값 |
| `{3}` | 시리즈 인덱스 |
| `{4}` | 카테고리 인덱스 |
| `{5}` | `LightningBarDataPoint.UserData` |

예시:

```csharp
options.Tooltip.Format = "{1}\r\n{0}\r\nValue:{2:0.##}";
```

## 14. 클릭/저장 이벤트

막대를 클릭하면 `BarClicked` 이벤트가 발생합니다. 기존 호환성을 위해 `SeriesClicked`도 동일하게 발생합니다.

```csharp
chart.BarClicked += (sender, e) =>
{
    string category = e.CategoryName;
    string seriesName = e.Series.Name;
    float value = e.Value;

    MessageBox.Show(
        string.Format("{0} / {1} / {2:0.##}", category, seriesName, value));
};
```

`DataPoints`를 사용한 경우 클릭한 막대의 원본 데이터를 함께 받을 수 있습니다.

```csharp
chart.BarClicked += (sender, e) =>
{
    LightningBarDataPoint point = e.DataPoint;
    object userData = e.UserData;

    // 예: userData에 EquipmentId, MetricCode, LotId 등을 넣어두고 상세 조회 조건으로 사용합니다.
    OpenDetailChart(userData, e.CategoryName, e.Series.Name, e.Value);
};
```

상세 차트 화면을 열고 싶을 때:

```csharp
chart.BarClicked += (sender, e) =>
{
    using (var form = new DetailChartForm(e.CategoryName, e.Series.Name))
    {
        form.ShowDialog(this);
    }
};
```

범례를 클릭하면 `LegendClicked` 이벤트가 발생합니다.

```csharp
chart.LegendClicked += (sender, e) =>
{
    MessageBox.Show(
        string.Format("Legend: {0} / SeriesIndex: {1}", e.LegendLabel, e.SeriesIndex));
};
```

이미지 저장 전/후에는 `ImageSaving`, `ImageSaved` 이벤트가 발생합니다.

```csharp
chart.ImageSaving += (sender, e) =>
{
    Console.WriteLine("Saving: " + (e.IsFileSave ? e.ImagePath : "memory"));
};

chart.ImageSaved += (sender, e) =>
{
    Console.WriteLine("Saved: " + (e.IsFileSave ? e.ImagePath : "memory"));
};
```

## 15. RawData 버튼

RawData 버튼은 기본값이 숨김입니다.

```csharp
options.RawData.ButtonMode = LightningBarRawDataButtonMode.Hidden;
```

표시하려면:

```csharp
options.RawData.ButtonMode = LightningBarRawDataButtonMode.Visible;
options.RawData.ButtonText = "RawData";
options.RawData.ButtonWidth = 88f;
options.RawData.ButtonHeight = 28f;
options.RawData.MarginTop = 8f;
options.RawData.MarginRight = 10f;
```

이미지 저장 시에는 기본적으로 RawData 버튼을 숨깁니다.

```csharp
imageOptions.HideRawDataButtonOnImage = true;
```

## 16. 데이터 없음 표시

데이터 없음 상태는 두 가지로 나눠서 처리할 수 있습니다.

| 상황 | 옵션 |
| --- | --- |
| 카테고리/시리즈/값 배열이 없어 그릴 데이터가 없는 경우 | `ShowWhenDataMissing` |
| 시리즈 값은 있으나 표시 범위의 값이 모두 0인 경우 | `ShowWhenAllValuesZero` |

기본 설정:

```csharp
options.NoData = new LightningBarNoDataOptions
{
    Text = "데이터가 없습니다.",
    ShowWhenDataMissing = true,
    ShowWhenAllValuesZero = false,
    IncludeTitle = false,
    DisplayMode = LightningBarNoDataDisplayMode.HideChartAndMessage,
    FontName = "맑은 고딕",
    FontSize = 11f,
    TextColor = Color.FromArgb(138, 118, 30),
    BadgeBackColor = Color.FromArgb(255, 249, 196),
    BadgeBackOpacity = 128,
    BadgeBorderColor = Color.FromArgb(240, 206, 84),
    BadgeWidthMode = LightningBarNoDataBadgeWidthMode.Auto,
    BadgeWidthRatio = 0.8f,
    BadgeSingleLine = true,
    BadgeHorizontalPadding = 10f,
    BadgeVerticalPadding = 4f,
    BadgeMinWidth = 0f,
    BadgeMinHeight = 0f
};
```

현재 기본값은 노란색 박스를 차트 컨트롤 전체 영역 중앙에 표시하고, 텍스트 크기에 맞춰 박스 크기를 최소화합니다. `BadgeSingleLine = true`이면 제목과 메시지 사이의 줄바꿈도 공백으로 바꿔 한 줄로 표시합니다.

차트를 그리지 않고 데이터 없음 메시지만 표시:

```csharp
options.NoData.DisplayMode = LightningBarNoDataDisplayMode.HideChartAndMessage;
options.NoData.ShowWhenDataMissing = true;

chart.UpdateData(
    new[] { "센서 A", "센서 B" },
    new[]
    {
        new LightningBarSeries
        {
            Name = "현재값",
            Values = new float[0]
        }
    },
    options);
```

값이 모두 0일 때도 데이터 없음으로 표시:

```csharp
options.NoData.ShowWhenAllValuesZero = true;

chart.UpdateData(
    new[] { "센서 A", "센서 B" },
    new[]
    {
        new LightningBarSeries
        {
            Name = "현재값",
            Values = new[] { 0f, 0f }
        }
    },
    options);
```

차트 프레임 위에 워터마크처럼 겹쳐 표시:

```csharp
options.NoData.DisplayMode = LightningBarNoDataDisplayMode.OverlayOnChartWatermark;
```

박스 폭을 고정값으로 지정:

```csharp
options.NoData.BadgeWidthMode = LightningBarNoDataBadgeWidthMode.Fixed;
options.NoData.BadgeFixedWidth = 260f;
```

박스 폭을 차트 영역 비율로 지정:

```csharp
options.NoData.BadgeWidthMode = LightningBarNoDataBadgeWidthMode.Percent;
options.NoData.BadgeWidthRatio = 0.8f;
```

박스 여백을 직접 지정:

```csharp
options.NoData.BadgeHorizontalPadding = 8f;
options.NoData.BadgeVerticalPadding = 3f;
options.NoData.BadgeSingleLine = true;
```

메시지를 코드에서 나중에 바꾸기:

```csharp
chart.SetNoDataText("조회 조건에 해당하는 계측 데이터가 없습니다.");
```

차트별로 다른 NoData 문구를 쓰고 옵션 객체를 공유하지 않으려면 옵션을 복제하거나 새로 생성합니다.

```csharp
LightningBarOptions options1 = LightningBarOptions.CreateDefault600x400();
options1.NoData.Text = "A 설비 데이터가 없습니다.";

LightningBarOptions options2 = LightningBarOptions.CreateDefault600x400();
options2.NoData.Text = "B 설비 데이터가 없습니다.";
```

이미 생성된 차트에서 차트별 문구만 바꾸려면:

```csharp
chartA.SetNoDataText("A 설비 데이터가 없습니다.");
chartB.SetNoDataText("B 설비 데이터가 없습니다.");
```

## 17. 이미지 저장 옵션

이미지 옵션 기본값:

```csharp
LightningBarImageOptions imageOptions = LightningBarImageOptions.CreateDefault();
```

기본값은 `600 x 400`, PNG, 96 DPI입니다. 저장 경로는 권한 문제가 적은 사용자 폴더를 enum으로 선택합니다.

기본 저장 방식:

- `SaveFolder = LightningBarImageSaveFolder.LocalApplicationData`
- `SubDirectoryName = "LightningBarImages"`
- `UseDateFolder = true`
- `UseGuidFileName = true`

기본 파일 경로 예시는 다음 형태입니다.

```text
%LOCALAPPDATA%\LightningBarImages\yyyyMMdd\{guid}.png
```

지원 저장 위치:

| enum | 설명 |
| --- | --- |
| `LocalApplicationData` | 사용자별 로컬 AppData입니다. 기본값이며 권장합니다. |
| `RoamingApplicationData` | 사용자별 Roaming AppData입니다. |
| `MyDocuments` | 사용자 문서 폴더입니다. |
| `Temp` | 사용자 임시 폴더입니다. |

직접 지정:

```csharp
LightningBarImageOptions imageOptions = new LightningBarImageOptions
{
    Width = 600,
    Height = 400,
    DpiX = 96f,
    DpiY = 96f,
    FileFormat = LightningBarImageFileFormat.Png,
    SaveFolder = LightningBarImageSaveFolder.LocalApplicationData,
    SubDirectoryName = "EquipmentCharts",
    UseDateFolder = true,
    UseGuidFileName = true,
    JpegQuality = 90L
};
```

사용자가 직접 입력한 폴더에 저장하려면 `SaveDirectory`를 지정합니다. 이 경우에도 기본적으로 날짜 폴더와 GUID 파일명이 적용됩니다.

```csharp
imageOptions.SaveDirectory = @"C:\Temp\ChartImages";
imageOptions.SubDirectoryName = string.Empty;
imageOptions.UseDateFolder = true;
imageOptions.UseGuidFileName = true;
```

엑셀 첨부용으로 조금 더 크게 저장:

```csharp
LightningBarImageOptions imageOptions = LightningBarImageOptions.CreateChartZoom();
imageOptions.SaveFolder = LightningBarImageSaveFolder.LocalApplicationData;
imageOptions.SubDirectoryName = "ExcelCharts";
imageOptions.UseDateFolder = true;
imageOptions.UseGuidFileName = true;
```

`CreateChartZoom()` 기본값:

| 속성 | 값 |
| --- | --- |
| `Width` | `900` |
| `Height` | `600` |
| `DpiX` / `DpiY` | `150` |
| `OptimizeForExcel` | `true` |
| `ContentScale` | `1.2f` |
| `ReduceOuterPadding` | `true` |
| `HideRawDataButtonOnImage` | `true` |

커스텀 크기:

```csharp
LightningBarImageOptions imageOptions = LightningBarImageOptions.CreateCustom(1200, 800);
imageOptions.DpiX = 150f;
imageOptions.DpiY = 150f;
```

## 18. 이미지 저장과 이미지 객체 사용

파일로 저장:

```csharp
string savedPath = chart.SaveImage(new LightningBarImageOptions
{
    Width = 600,
    Height = 400,
    FileFormat = LightningBarImageFileFormat.Png,
    SaveFolder = LightningBarImageSaveFolder.LocalApplicationData,
    SubDirectoryName = "EquipmentCharts",
    UseDateFolder = true,
    UseGuidFileName = true
});
```

저장 후 차트 인스턴스는 마지막 저장 이미지와 경로를 보관합니다.

```csharp
string path = chart.LastSavedImagePath;
bool hasImage = chart.HasLastSavedImage;
```

이미지 객체 가져오기:

```csharp
using (Image image = chart.GetLastSavedImage())
{
    // Excel 시트에 image를 삽입
}
```

Bitmap으로 가져오기:

```csharp
using (Bitmap bitmap = chart.GetLastSavedBitmap())
{
    // Excel 또는 다른 출력 로직에 사용
}
```

`LastSavedImage`와 `LastSavedBitmap` 속성도 사용할 수 있습니다.

```csharp
using (Image image = chart.LastSavedImage)
{
    // 사용
}

using (Bitmap bitmap = chart.LastSavedBitmap)
{
    // 사용
}
```

반환되는 이미지는 내부 이미지의 복사본입니다. 호출한 쪽에서 `Dispose()`해도 차트 내부에 저장된 이미지는 유지됩니다.

파일 저장 없이 메모리 이미지로만 생성:

```csharp
using (Bitmap bitmap = chart.SaveImageToMemory(new LightningBarImageOptions
{
    Width = 600,
    Height = 400,
    DpiX = 150f,
    DpiY = 150f
}))
{
    // bitmap 사용
}
```

현재 화면에 보이는 그대로 캡처:

```csharp
using (Bitmap bitmap = chart.CaptureVisibleImage())
{
    // 현재 컨트롤 크기 그대로 사용
}
```

마지막 저장 경로에서 이미지를 다시 로드:

```csharp
using (Image image = chart.LoadLastSavedImage())
{
    // image 사용
}
```

이미지 캐시 초기화:

```csharp
chart.ClearSavedImage();
```

정적 메서드로 화면에 붙이지 않고 바로 이미지 생성:

```csharp
using (Bitmap bitmap = LightningBar.RenderImage(
    newCategories: categories,
    newSeries: series,
    barOptions: options,
    imageOptions: imageOptions))
{
    // bitmap 사용
}
```

정적 메서드로 바로 파일 저장:

```csharp
string path = LightningBar.SaveImage(
    newCategories: categories,
    newSeries: series,
    barOptions: options,
    imageOptions: imageOptions);
```

## 19. 엑셀 출력용 권장 패턴

화면에서는 600x400으로 표시하고, 엑셀에는 더 큰 이미지로 저장하는 예시입니다.

```csharp
LightningBarOptions options = LightningBarOptions.CreateDefault600x400();

LightningBar chart = LightningBar.Create(
    pnlChartHost,
    categories,
    series,
    options);

chart.Size = new Size(600, 400);

LightningBarImageOptions excelImageOptions = LightningBarImageOptions.CreateChartZoom();
excelImageOptions.SaveFolder = LightningBarImageSaveFolder.LocalApplicationData;
excelImageOptions.SubDirectoryName = "LightningBarExcelImages";
excelImageOptions.UseDateFolder = true;
excelImageOptions.UseGuidFileName = true;
excelImageOptions.FileFormat = LightningBarImageFileFormat.Png;

string imagePath = chart.SaveImage(excelImageOptions);

using (Image image = chart.GetLastSavedImage())
{
    // Excel 시트에 image 또는 imagePath를 사용해서 삽입
}
```

## 20. 기존 호환 속성

기존 코드와의 호환을 위해 `LightningBarOptions`에는 단축 속성이 남아 있습니다.

| 기존 속성 | 권장 그룹 옵션 |
| --- | --- |
| `Title` | `TitleOptions.Text` |
| `TitleColor` | `TitleOptions.Color` |
| `TitleFontSize` | `TitleOptions.FontSize` |
| `ChartPadding` | `Layout.ChartPadding` |
| `TopOffset` | `Layout.TopOffset` |
| `LegendWidth` | `Layout.LegendReservedWidth` |
| `GridLineCount` | `Scale.GridLineCount` |
| `MaxValue` | `Scale.MaxValue` |
| `CategoryFontSize` | `CategoryLabels.FontSize` |
| `CategoryLabelMaxLines` | `CategoryLabels.MaxLines` |
| `LegendFontSize` | `Legend.FontSize` |
| `LegendMarkerWidth` | `Legend.MarkerWidth` |
| `LegendTextMaxLines` | `Legend.LabelMaxLines` |
| `SeriesLabelEnabled` | `SeriesLabels.Enabled` |
| `SeriesTooltipEnabled` | `Tooltip.Enabled` |
| `SeriesTooltipFormat` | `Tooltip.Format` |
| `NoDataText` | `NoData.Text` |
| `ShowNoDataMessage` | `NoData.ShowWhenDataMissing` |
| `BarHeightMode` | `Bars.HeightMode` |
| `FixedBarHeight` | `Bars.FixedHeight` |
| `BarGap` | `Bars.Gap` |

신규 화면에서는 그룹 옵션을 사용하는 것을 권장합니다.

## 21. 전체 옵션 예제

```csharp
LightningBarOptions options = new LightningBarOptions
{
    BackgroundColor = Color.White,
    TitleOptions = new LightningBarTitleOptions
    {
        Text = string.Empty,
        Visible = true,
        Position = LightningBarTitlePosition.TopCenter,
        FontSize = 13f
    },
    Layout = new LightningBarLayoutOptions
    {
        ChartPadding = 20,
        TopOffset = 72,
        LegendReservedWidth = 120,
        LegendReservedWidthMode = LightningBarLegendReservedWidthMode.CollapseForTopBottomLegend,
        CategoryLabelReservedWidth = 110f,
        AutoCategoryLabelReservedWidth = true,
        MinCategoryLabelReservedWidth = 78f,
        MaxCategoryLabelReservedWidth = 150f,
        BottomScaleAreaHeight = 30f
    },
    ChartAreaOutline = new LightningBarChartAreaOutlineOptions
    {
        Mode = LightningBarChartAreaOutlineMode.None,
        BorderWidth = 1f,
        BorderColor = Color.FromArgb(90, 120, 170),
        BackColor = Color.Transparent,
        BackOpacity = 0
    },
    Legend = new LightningBarLegendOptions
    {
        Visible = true,
        Position = LightningBarLegendPosition.Top,
        Alignment = LightningBarLegendAlignment.Center,
        FontSize = 7.5f,
        LabelMaxLines = 3
    },
    CategoryLabels = new LightningBarCategoryLabelOptions
    {
        FontSize = 8f,
        MaxLines = 3,
        LineSpacing = 1.5f
    },
    Scale = new LightningBarScaleOptions
    {
        MaxValue = 100f,
        GridLineCount = 5,
        FontSize = 8f
    },
    Bars = new LightningBarBarOptions
    {
        HeightMode = LightningBarHeightMode.Manual,
        FixedHeight = 30f,
        ReferenceSeriesCount = 5,
        ClampFixedHeightToGroup = true,
        Gap = 5f,
        GroupPaddingRatio = 0.16f
    },
    SeriesLabels = new LightningBarSeriesLabelOptions
    {
        Enabled = false
    },
    Tooltip = new LightningBarTooltipOptions
    {
        Enabled = true,
        Format = "Value:{2:0.#} (* 클릭할 경우 해당 계측 데이터 차트로 가 보입니다.)"
    },
    RawData = new LightningBarRawDataOptions
    {
        ButtonMode = LightningBarRawDataButtonMode.Hidden
    },
    NoData = new LightningBarNoDataOptions
    {
        Text = "데이터가 없습니다.",
        ShowWhenDataMissing = true,
        ShowWhenAllValuesZero = false,
        DisplayMode = LightningBarNoDataDisplayMode.HideChartAndMessage,
        BadgeWidthMode = LightningBarNoDataBadgeWidthMode.Auto,
        BadgeWidthRatio = 0.8f,
        BadgeSingleLine = true,
        BadgeHorizontalPadding = 10f,
        BadgeVerticalPadding = 4f
    },
    Image = new LightningBarImageOptions
    {
        Width = 600,
        Height = 400,
        FileFormat = LightningBarImageFileFormat.Png,
        SaveFolder = LightningBarImageSaveFolder.LocalApplicationData,
        SubDirectoryName = "LightningBarImages",
        UseDateFolder = true,
        UseGuidFileName = true
    }
};
```
