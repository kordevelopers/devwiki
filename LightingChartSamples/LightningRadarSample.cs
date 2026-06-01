using System.Collections.Generic;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace LightingChartSamples
{
    public partial class LightningRadarSample : Form
    {
        private readonly LightningRadarCanvasControl radarCanvas;
        private readonly LightningRadarImagePathInfo imagePathInfo;

        public LightningRadarSample()
        {
            InitializeComponent();

            BackColor = Color.White;

            radarCanvas = new LightningRadarCanvasControl
            {
                Dock = DockStyle.Fill,
                ChartTitle = "LightningRadar Common Sample",
                ChartAlignment = HorizontalAlignment.Center
            };

            pnlChartHost.Controls.Add(radarCanvas);

            LightningRadarOptions options = CreateOptions();
            options.ShowTitle = false;
            options.ShowLegend = false;
            radarCanvas.Radar.SetOptions(options);
            radarCanvas.SetData(CreateCategories(), CreateSeries());
            imagePathInfo = CreateImagePathInfo();

            // 차트 이미지를 지정 경로에 저장하는 방법:
            // - FAB_ID / LOT_CD / DRAFT_NO 조합으로 폴더가 자동 생성됩니다.
            // - 예: Documents\LightningRadarImages\FAB1\LOT1001\DRAFT01
            // - SaveImage 반환값은 실제 저장된 전체 파일 경로입니다.
            // string savedFilePath = radar.SaveImage(imagePathInfo);

            // 저장된 이미지 검색 방법:
            // - 동일한 FAB_ID / LOT_CD / DRAFT_NO 조건을 넣으면 해당 폴더의 이미지 목록을 가져옵니다.
            // string[] imageFiles = radar.FindImages(imagePathInfo);

            // 가장 최근 이미지 1건만 가져오려면 아래 메서드를 사용합니다.
            // string latestImageFile = radar.FindLatestImage(imagePathInfo);

            // 특정 일자 이후의 이미지 삭제 방법:
            // - 예: 오늘 이후 생성된 파일 삭제
            // int deletedCount = radar.DeleteImagesCreatedAfter(imagePathInfo, DateTime.Today);

            // 여러 스레드에서 갱신해야 할 경우 아래와 같이 호출하면 됩니다.
            // 내부에서 UI 스레드로 안전하게 전달되므로 작업 스레드에서 직접 호출해도 됩니다.
            // radar.Update(delegate(LightningRadar chart)
            // {
            //     chart.SetSeries(CreateUpdatedSeries());
            // });
        }

        private static LightningRadarOptions CreateOptions()
        {
            return new LightningRadarOptions
            {
                // 차트 상단 제목입니다.
                Title = "LightningRadar Common Sample",

                // 차트 바깥 기본 여백입니다. 값이 커질수록 내부 차트 영역은 작아집니다.
                ChartPadding = 24,

                // 제목/범례를 위한 상단 여백입니다.
                TopOffset = 90,

                // 카테고리 라벨과 차트 외곽의 간격입니다.
                // 라벨이 겹치면 이 값을 늘리면 됩니다.
                CategoryLabelOffset = 10f,

                // 최상단 카테고리 라벨은 가로 이동 없이,
                // 12시 방향으로만 추가 간격을 둡니다.
                TopCategoryLabelHorizontalOffset = 0f,
                TopCategoryLabelVerticalOffset = 14f,

                // 레이더 실제 반지름에서 추가로 줄이는 내부 여백입니다.
                // 값이 크면 라벨과 데이터 영역 사이가 더 벌어집니다.
                RadiusPadding = 2f,

                // 내부 원형 가이드 개수입니다.
                // 예: 10 이면 0~100을 10단계로 표현합니다.
                GridRingCount = 10,

                // 시리즈 외곽선 두께입니다.
                SeriesLineWidth = 2f,

                // 데이터 포인트 마커 크기입니다.
                SeriesPointSize = 8f,

                // 카테고리 라벨 글자 크기입니다.
                CategoryFontSize = 9f,

                // 눈금 라벨 글자 크기입니다.
                ScaleFontSize = 9f,

                // 배경색과 라벨 색도 필요에 따라 쉽게 변경할 수 있습니다.
                BackgroundColor = Color.White,
                CategoryLabelColor = Color.FromArgb(95, 95, 95),
                ScaleLabelColor = Color.FromArgb(95, 95, 95),

                // 이미지 저장 옵션입니다.
                // AppData, Documents, Desktop, 사용자 지정 경로를 모두 지원합니다.
                ImageStorage = new LightningRadarImageStorageOptions
                {
                    // 기본 저장 위치 선택
                    RootType = LightningRadarStorageRootType.Documents,

                    // RootType 이 Custom 일 때 사용할 경로입니다.
                    // 예: @"D:\RadarImages"
                    CustomRootPath = string.Empty,

                    // 기본 루트 아래에 생성할 상위 폴더명입니다.
                    RootFolderName = "LightningRadarImages",

                    // 저장 파일 포맷입니다. PNG/JPEG/BMP/GIF 선택 가능
                    ImageFormat = LightningRadarImageFormat.Png,

                    // 저장 이미지 크기입니다.
                    // 0 이하이면 현재 차트 컨트롤 크기를 그대로 사용합니다.
                    ImageWidth = 1280,
                    ImageHeight = 720,

                    // JPEG 저장 시 품질입니다. PNG/BMP/GIF 사용 시에는 무시됩니다.
                    JpegQuality = 90L
                }
            };
        }

        private static LightningRadarImagePathInfo CreateImagePathInfo()
        {
            return new LightningRadarImagePathInfo
            {
                // 저장 폴더 구조는 아래 순서로 생성됩니다.
                // [RootFolder]\FAB_ID\LOT_CD\DRAFT_NO
                FabId = "FAB1",
                LotCd = "LOT1001",
                DraftNo = "DRAFT01",

                // 파일명 접두어입니다.
                // 최종 파일명 예: radar_20260101_101530123.png
                FileNamePrefix = "radar"
            };
        }

        private static IEnumerable<string> CreateCategories()
        {
            // 카테고리 데이터 입력 방법:
            // - 차트 축 이름을 순서대로 문자열 배열 또는 IEnumerable<string> 으로 전달합니다.
            // - 아래 순서가 각 시리즈 값의 인덱스와 1:1로 매핑됩니다.
            // - 예를 들어 0번 값은 "품질", 1번 값은 "생산성" 에 표시됩니다.
            return new[]
            {
                "품질",
                "생산성",
                "안전",
                "원가",
                "납기"
            };
        }

        private static IEnumerable<LightningRadarSeries> CreateSeries()
        {
            // 시리즈 데이터 입력 방법:
            // 1. Name 에 범례 이름을 넣습니다.
            // 2. Values 에 카테고리 순서와 동일한 순서로 값을 넣습니다.
            // 3. 값은 보통 0~100 범위를 권장합니다.
            // 4. 카테고리 개수보다 값이 적으면 내부에서 남는 값은 0으로 채워집니다.
            // 5. 값이 더 많으면 카테고리 개수까지만 사용됩니다.
            return new[]
            {
                new LightningRadarSeries
                {
                    Name = "설비 A",
                    Values = new[] { 88f, 82f, 91f, 79f, 95f },
                    FillColor = Color.FromArgb(110, 255, 196, 214),
                    LineColor = Color.FromArgb(230, 225, 104, 150)
                },
                new LightningRadarSeries
                {
                    Name = "설비 B",
                    Values = new[] { 76f, 73f, 86f, 70f, 84f },
                    FillColor = Color.FromArgb(95, 186, 235, 255),
                    LineColor = Color.FromArgb(230, 74, 166, 224)
                }
            };
        }

        private static IEnumerable<LightningRadarSeries> CreateUpdatedSeries()
        {
            // 실시간 갱신 샘플용 예시 데이터입니다.
            return new[]
            {
                new LightningRadarSeries
                {
                    Name = "설비 A",
                    Values = new[] { 90f, 84f, 94f, 80f, 96f },
                    FillColor = Color.FromArgb(110, 255, 196, 214),
                    LineColor = Color.FromArgb(230, 225, 104, 150)
                },
                new LightningRadarSeries
                {
                    Name = "설비 B",
                    Values = new[] { 78f, 75f, 89f, 72f, 86f },
                    FillColor = Color.FromArgb(95, 186, 235, 255),
                    LineColor = Color.FromArgb(230, 74, 166, 224)
                }
            };
        }

        private void btnSaveImage_Click(object sender, EventArgs e)
        {
            try
            {
                string savedFilePath = radarCanvas.Radar.SaveImage(imagePathInfo);
                MessageBox.Show(this, string.Format("이미지가 저장되었습니다.\n{0}", savedFilePath), "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format("이미지 저장 중 오류가 발생했습니다.\n{0}", ex.Message), "저장 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
