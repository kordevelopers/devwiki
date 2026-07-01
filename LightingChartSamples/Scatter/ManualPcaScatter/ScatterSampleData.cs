namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.ManualPcaScatter
{
    #region PCA Scatter Output Contract

    /// <summary>
    /// PCA 결과 한 건을 LightningChart와 KNN 결과 그리드에 전달하는 화면 데이터 계약이다.
    /// X1/X2는 임의 좌표가 아니라 PcaAnalysisPipeline에서 계산한 PC1/PC2 점수다.
    /// </summary>
    public sealed class ScatterSampleData
    {
        public int SourceIndex { get; set; }
        public string DraftNo { get; set; }
        public double X1 { get; set; }
        public double X2 { get; set; }
        public string AiResultValue { get; set; }
        public double? Distance { get; set; }
        public string ParameterType { get; set; }
        public string TooltipText { get; set; }
        public object UserData { get; set; }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(TooltipText)
                ? DraftNo ?? string.Empty
                : TooltipText;
        }
    }

    #endregion
}
