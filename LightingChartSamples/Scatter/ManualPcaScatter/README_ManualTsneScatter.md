# Manual t-SNE Scatter WinForms

기존 `ManualPcaScatterMain`의 DataTable 입력, JSON 수치 feature 선별, 표준화, Draft 조회,
최근접 Draft 표시, 포인트 선택, 범례/툴팁 기능을 유지하면서 2차원 차트 투영만 t-SNE로 변경한 화면입니다.

## 실행

`LightingChartSamples` 프로젝트를 시작하면 `ManualTsneScatterMain`이 열립니다.
화면이 열린 뒤 고정 seed로 만든 RESPONSE 샘플 120건을 자동 분석하여 t-SNE 차트를 바로 표시합니다.

```csharp
Application.Run(new ManualTsneScatterMain());
```

서비스에서 받은 DataTable을 직접 전달할 수도 있습니다.

```csharp
using (var form = new ManualTsneScatterMain())
{
    form.AutoLoadSampleData = false;
    await form.LoadConvExperimentDataTableAsync(sourceTable);
    await form.DrawChartAsync();
    form.ShowDialog();
}
```

## 구현 기준

- Accord.NET 3.8 `Accord.MachineLearning.Clustering.TSNE`의 Barnes-Hut 구현을 사용합니다.
- 고차원 입력은 기존과 동일하게 feature 선별과 `StandardScaler` 처리를 거칩니다.
- t-SNE의 perplexity와 Barnes-Hut theta를 Accord.NET 모델에 전달합니다(perplexity 기본값 `30`, theta `0.5`).
- 반복 횟수·learning rate·seed는 Accord.NET 3.8 구현의 내부 기본값을 따릅니다.
- 데이터가 적으면 perplexity를 `(rowCount - 1) / 3` 이하로 자동 보정합니다.
- 초기화 seed는 Accord.NET 3.8 내부 동작을 따르므로 실행마다 좌표 방향·스케일이 달라질 수 있습니다.
- 전체 표준화 feature 공간을 사용하는 기존 KNN 엔진과 scaler 공유 구조를 그대로 유지합니다.
- 화면의 Draft 조회·최근접 표시 방식도 기존 `ManualPcaScatterMain` 동작을 그대로 따릅니다.

기존 PCA 화면은 `ProjectionMethod=Pca` 기본값을 유지하므로 호환 동작이 바뀌지 않습니다.
