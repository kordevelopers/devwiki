# LightningScatter 구현 분석 및 사용 가이드

`LightningScatter`는 WinForms 화면에서 Scatter 차트를 공통으로 사용하기 위한 래퍼 컨트롤입니다.
Bar 차트처럼 화면마다 긴 생성 코드를 반복하지 않도록, `데이터 + 옵션`만 넘기면 차트 컨트롤이 생성되도록 구성했습니다.

## 1. 구현 방향

- 순수 GDI로 차트를 직접 그리지 않고 LightningChart WinForms API를 사용합니다.
- Scatter 데이터는 `PointLineSeries`로 표현합니다.
- 축 범위, 범례, 포인트 스타일, 선 표시, 툴팁 근접 판정, 이미지 저장은 LightningChart 컨트롤의 기능을 우선 사용합니다.
- 데이터 없음 문구는 화면과 저장 이미지에 함께 포함되도록 LightningChart `AnnotationXY`를 사용합니다.
- 파일명은 기본적으로 GUID로 생성하고, 저장 폴더 아래 `yyyyMMdd` 폴더를 자동 생성합니다.

## 2. 주요 파일

- `LightingChartSamples/Scatter/LightningScatter.cs`
  - 공통 Scatter 차트 컨트롤, 데이터 모델, 옵션, 이벤트, 이미지 저장 기능
- `LightingChartSamples/Scatter/LightningScatterSampleForm.cs`
  - Scatter 사용 예제 폼
- `LightingChartSamples/MainWindowForm.cs`
  - 샘플 실행 메뉴에 Scatter 샘플 버튼 추가

## 3. 기본 생성 코드

```csharp
IEnumerable<LightningScatterSeries> series = new[]
{
    new LightningScatterSeries
    {
        Name = "Temperature",
        LegendLabel = "온도",
        PointColor = Color.FromArgb(217, 94, 86),
        LineColor = Color.FromArgb(217, 94, 86),
        PointSize = 9f,
        ShowLine = true,
        Points = new List<LightningScatterPoint>
        {
            new LightningScatterPoint(0, 21, "T-001"),
            new LightningScatterPoint(1, 26, "T-002"),
            new LightningScatterPoint(2, 33, "T-003")
        }
    }
};

LightningScatterOptions options = new LightningScatterOptions
{
    ShowTitle = false,
    Legend = new LightningScatterLegendOptions
    {
        Visible = true,
        Position = LightningScatterLegendPosition.TopCenter
    },
    Tooltip = new LightningScatterTooltipOptions
    {
        Enabled = true,
        Format = "{0}\r\nX:{1:0.###}, Y:{2:0.###}"
    }
};

LightningScatter chart = LightningScatter.Create(panelChartHost, series, options);
```

## 4. 여러 차트 생성 패턴

화면에 Scatter 차트를 여러 개 배치해야 할 때도 차트 내부 구조를 직접 작성하지 않아도 됩니다.

```csharp
foreach (var chartModel in chartModels)
{
    Panel host = CreateChartHostPanel();
    LightningScatter scatter = LightningScatter.Create(
        host,
        chartModel.Series,
        chartModel.Options);
}
```

이 방식에서는 화면별 코드는 데이터 구성과 옵션 설정에만 집중하면 됩니다.

## 5. 옵션 구조

### 전체 옵션

```csharp
var options = new LightningScatterOptions
{
    Title = string.Empty,
    ShowTitle = false,
    BackgroundColor = Color.White,
    GraphBackgroundColor = Color.White,
    XAxis = new LightningScatterAxisOptions(),
    YAxis = new LightningScatterAxisOptions(),
    Legend = new LightningScatterLegendOptions(),
    Tooltip = new LightningScatterTooltipOptions(),
    NoData = new LightningScatterNoDataOptions(),
    Image = new LightningScatterImageOptions()
};
```

### 축 옵션

```csharp
options.XAxis = new LightningScatterAxisOptions
{
    Title = "시간",
    AutoFit = true,
    Minimum = 0,
    Maximum = 100,
    MajorDivCount = 5,
    LabelFormat = "0.#",
    FontSize = 8f
};
```

`AutoFit = true`이면 전달된 포인트 기준으로 축 범위를 자동 계산합니다.
수동 범위를 사용하려면 `AutoFit = false`로 설정하고 `Minimum`, `Maximum`을 지정합니다.

### 범례 위치

```csharp
options.Legend.Position = LightningScatterLegendPosition.TopLeft;
options.Legend.Position = LightningScatterLegendPosition.TopCenter;
options.Legend.Position = LightningScatterLegendPosition.TopRight;
```

지원 위치:

- `TopLeft`
- `TopCenter`
- `TopRight`
- `BottomLeft`
- `BottomCenter`
- `BottomRight`

### 툴팁

```csharp
options.Tooltip = new LightningScatterTooltipOptions
{
    Enabled = true,
    HitPixelTolerance = 14,
    Format = "{0}\r\nX:{1:0.###}, Y:{2:0.###}\r\n* 클릭할 경우 해당 계측 데이터 차트로 이동합니다."
};
```

`Format` 인자:

- `{0}`: 시리즈 범례명
- `{1}`: X 값
- `{2}`: Y 값
- `{3}`: 시리즈 인덱스
- `{4}`: 포인트 인덱스
- `{5}`: 포인트 `Tag`

## 6. 데이터 없음 표시

데이터 없음 표시는 LightningChart의 `AnnotationXY`로 처리합니다.
따라서 별도 WinForms 라벨을 올리는 방식이 아니며, 이미지 저장 시에도 문구가 함께 포함됩니다.

```csharp
options.NoData = new LightningScatterNoDataOptions
{
    Text = "Scatter 조회 데이터가 없습니다.",
    ShowWhenDataMissing = true,
    ShowWhenAllValuesZero = true,
    FontSize = 10f,
    TextColor = Color.Gray,
    BadgeBackColor = Color.FromArgb(255, 249, 196),
    BadgeBorderColor = Color.FromArgb(240, 206, 84),
    BadgeWidthRatio = 0.8f,
    BadgeHeight = 52f
};
```

`Clear()`는 조회 전 상태를 의미하므로 데이터 없음 문구도 숨깁니다.
데이터 조회 결과가 비어 있는 상태를 표시하려면 `SetData()` 또는 `UpdateData()`에 빈 시리즈/빈 포인트를 전달합니다.

## 7. 클릭 이벤트

포인트 클릭:

```csharp
chart.PointClicked += delegate(object sender, LightningScatterPointClickEventArgs e)
{
    string seriesName = e.Series.Name;
    double x = e.Point.X;
    double y = e.Point.Y;
    object tag = e.Point.Tag;
};
```

범례 클릭:

```csharp
chart.LegendClicked += delegate(object sender, LightningScatterLegendClickEventArgs e)
{
    string legendLabel = e.LegendLabel;
};
```

이벤트에서는 새 폼 오픈, 다른 차트 조회, 상세 데이터 이동 등을 화면별로 연결하면 됩니다.

## 8. 이미지 저장

기본 저장 위치는 `LocalApplicationData`입니다.
권한 문제가 적은 사용자별 폴더를 열거형으로 선택할 수 있습니다.

```csharp
LightningScatterImageOptions imageOptions = new LightningScatterImageOptions
{
    Width = 600,
    Height = 400,
    FileFormat = LightningScatterImageFileFormat.Png,
    SaveFolder = LightningScatterImageSaveFolder.LocalApplicationData,
    SubDirectoryName = "LightningScatterImages",
    UseDateFolder = true,
    UseGuidFileName = true
};

string imagePath = chart.SaveImage(imageOptions);
Image savedImage = chart.GetLastSavedImage();
string savedPath = chart.LastSavedImagePath;
```

저장 경로 예시:

```text
%LOCALAPPDATA%\LightningScatterImages\yyyyMMdd\{guid}.png
```

이미지 저장 전/후 이벤트:

```csharp
chart.ImageSaving += delegate(object sender, LightningScatterImageSavingEventArgs e)
{
    string path = e.ImagePath;
};

chart.ImageSaved += delegate(object sender, LightningScatterImageSavedEventArgs e)
{
    string path = e.ImagePath;
};
```

## 9. 구현 결과

- Scatter 공통 컨트롤을 `LightingChartSamples.Scatter.LightningScatter`로 추가했습니다.
- 데이터와 옵션만 넘기면 컨트롤이 생성되는 `Create()` 정적 메서드를 제공합니다.
- LightningChart의 `PointLineSeries`, `LegendBoxXY`, `AxisX/AxisY`, `AnnotationXY`, `SaveToFile()`을 사용했습니다.
- 포인트 툴팁, 포인트 클릭, 범례 클릭, 데이터 없음 표시, 이미지 저장, 마지막 이미지 객체/경로 보관 기능을 제공합니다.
- Bar 차트 기존 코드는 변경하지 않았고, Scatter 기능은 별도 폴더와 네임스페이스로 분리했습니다.

## 10. 제한 사항

- LightningChart 8.5.1 API 기준으로 구현했습니다.
- 차트 이미지는 LightningChart `SaveToFile()` 결과를 사용합니다. 차트 자체를 GDI로 다시 그리지 않습니다.
- 이미지 DPI 메타데이터 조정이나 벡터 출력은 현재 Scatter 래퍼 범위에 포함하지 않았습니다.
- 엑셀 첨부 목적의 더 큰 이미지는 `LightningScatterImageOptions.Width`, `Height` 값을 크게 지정하는 방식이 가장 안정적입니다.
