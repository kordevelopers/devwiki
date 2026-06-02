using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LightingChartSamples
{
    /// <summary>
    /// 레이더 차트에 필요한 단일 시리즈 데이터를 정의합니다.
    /// </summary>
    public class LightningRadarSeries
    {
        public LightningRadarSeries()
        {
            Name = string.Empty;
            Values = new float[0];
            FillColor = Color.FromArgb(110, 255, 196, 214);
            LineColor = Color.FromArgb(230, 225, 104, 150);
        }

        public string Name { get; set; }

        public float[] Values { get; set; }

        public Color FillColor { get; set; }

        public Color LineColor { get; set; }

        public LightningRadarSeries Clone()
        {
            return new LightningRadarSeries
            {
                Name = Name,
                Values = Values == null ? new float[0] : Values.ToArray(),
                FillColor = FillColor,
                LineColor = LineColor
            };
        }
    }

    /// <summary>
    /// 레이더 차트의 표시 옵션을 정의합니다.
    /// </summary>
    public class LightningRadarOptions
    {
        public LightningRadarOptions()
        {
            Title = "Radar Chart Sample";
            TitleColor = Color.FromArgb(90, 90, 90);
            TitleFontSize = 12f;
            BackgroundColor = Color.White;
            ChartPadding = 16;
            LegendWidth = 180;
            TopOffset = 52;
            RadiusPadding = 6f;
            GridRingCount = 10;
            CategoryLabelOffset = 14f;
            TopCategoryLabelVerticalOffset = 0f;
            CategoryFontSize = 8.5f;
            CategoryLabelColor = Color.FromArgb(95, 95, 95);
            ScaleFontSize = 9f;
            ScaleLabelColor = Color.FromArgb(95, 95, 95);
            MajorGridColor = Color.FromArgb(205, 205, 205);
            MinorGridColor = Color.FromArgb(230, 230, 230);
            SpokeColor = Color.FromArgb(225, 225, 225);
            LegendTextColor = Color.FromArgb(90, 90, 90);
            LegendItemSpacing = 28f;
            ShowTitle = false;
            ShowLegend = true;
            SeriesLineWidth = 2f;
            SeriesPointSize = 8f;
            ImageStorage = new LightningRadarImageStorageOptions();
        }

        /// <summary>
        /// 차트 상단에 표시할 제목입니다.
        /// 기본값: Radar Chart Sample
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 제목 텍스트 색상입니다.
        /// 기본값: 진한 회색
        /// </summary>
        public Color TitleColor { get; set; }

        /// <summary>
        /// 제목 글자 크기입니다.
        /// 기본값: 12
        /// </summary>
        public float TitleFontSize { get; set; }

        /// <summary>
        /// 차트 전체 배경색입니다.
        /// 기본값: 흰색
        /// </summary>
        public Color BackgroundColor { get; set; }

        /// <summary>
        /// 차트 영역 바깥쪽 기본 여백입니다.
        /// 기본값: 40
        /// </summary>
        public int ChartPadding { get; set; }

        /// <summary>
        /// 범례 영역 확보 폭입니다.
        /// 기본값: 180
        /// </summary>
        public int LegendWidth { get; set; }

        /// <summary>
        /// 제목/범례를 위한 상단 여백입니다.
        /// 기본값: 90
        /// </summary>
        public int TopOffset { get; set; }

        /// <summary>
        /// 계산된 반지름에서 추가로 뺄 내부 여유 공간입니다.
        /// 기본값: 15
        /// </summary>
        public float RadiusPadding { get; set; }

        /// <summary>
        /// 내부 원형 가이드 개수입니다.
        /// 기본값: 10
        /// </summary>
        public int GridRingCount { get; set; }

        /// <summary>
        /// 카테고리 라벨을 차트 외곽에서 얼마나 떨어뜨릴지 설정합니다.
        /// 기본값: 32
        /// </summary>
        public float CategoryLabelOffset { get; set; }

        /// <summary>
        /// 최상단(인덱스 0) 카테고리 라벨의 가로(X) 이동량입니다.
        /// 음수면 왼쪽, 양수면 오른쪽으로 이동합니다.
        /// 기본값: 0
        /// </summary>
        public float TopCategoryLabelHorizontalOffset { get; set; }

        /// <summary>
        /// 최상단(인덱스 0) 카테고리 라벨의 세로(Y) 이동량입니다.
        /// 양수면 위(12시 방향), 음수면 아래로 이동합니다.
        /// 기본값: 0
        /// </summary>
        public float TopCategoryLabelVerticalOffset { get; set; }

        /// <summary>
        /// 카테고리 라벨 글자 크기입니다.
        /// 기본값: 8.5
        /// </summary>
        public float CategoryFontSize { get; set; }

        /// <summary>
        /// 카테고리 라벨 색상입니다.
        /// 기본값: 회색
        /// </summary>
        public Color CategoryLabelColor { get; set; }

        /// <summary>
        /// 눈금 라벨 글자 크기입니다.
        /// 기본값: 9
        /// </summary>
        public float ScaleFontSize { get; set; }

        /// <summary>
        /// 눈금 라벨 색상입니다.
        /// 기본값: 회색
        /// </summary>
        public Color ScaleLabelColor { get; set; }

        /// <summary>
        /// 주요 가이드 선 색상입니다.
        /// 기본값: 연한 회색
        /// </summary>
        public Color MajorGridColor { get; set; }

        /// <summary>
        /// 보조 가이드 선 색상입니다.
        /// 기본값: 더 연한 회색
        /// </summary>
        public Color MinorGridColor { get; set; }

        /// <summary>
        /// 중심에서 각 축으로 뻗는 선 색상입니다.
        /// 기본값: 연한 회색
        /// </summary>
        public Color SpokeColor { get; set; }

        /// <summary>
        /// 범례 텍스트 색상입니다.
        /// 기본값: 진한 회색
        /// </summary>
        public Color LegendTextColor { get; set; }

        /// <summary>
        /// 시리즈 범례 항목 사이 가로 간격입니다.
        /// 기본값: 28
        /// </summary>
        public float LegendItemSpacing { get; set; }

        /// <summary>
        /// 차트 제목 표시 여부입니다.
        /// 기본값: true
        /// </summary>
        public bool ShowTitle { get; set; }

        /// <summary>
        /// 범례 표시 여부입니다.
        /// 기본값: true
        /// </summary>
        public bool ShowLegend { get; set; }

        /// <summary>
        /// 시리즈 외곽선 두께입니다.
        /// 기본값: 2
        /// </summary>
        public float SeriesLineWidth { get; set; }

        /// <summary>
        /// 데이터 포인트 마커 크기입니다.
        /// 기본값: 8
        /// </summary>
        public float SeriesPointSize { get; set; }

        /// <summary>
        /// 차트 이미지 저장 관련 옵션입니다.
        /// 기본값: Documents 폴더, PNG, 현재 컨트롤 크기, JPEG 품질 90
        /// </summary>
        public LightningRadarImageStorageOptions ImageStorage { get; set; }

        public LightningRadarOptions Clone()
        {
            LightningRadarOptions clone = (LightningRadarOptions)MemberwiseClone();
            clone.ImageStorage = ImageStorage == null ? new LightningRadarImageStorageOptions() : ImageStorage.Clone();
            return clone;
        }
    }

    /// <summary>
    /// 기본 저장 루트를 지정합니다.
    /// </summary>
    public enum LightningRadarStorageRootType
    {
        AppData,
        LocalAppData,
        Documents,
        Desktop,
        Custom
    }

    /// <summary>
    /// 저장할 이미지 포맷을 지정합니다.
    /// </summary>
    public enum LightningRadarImageFormat
    {
        Png,
        Jpeg,
        Bmp,
        Gif
    }

    /// <summary>
    /// 차트 이미지 저장 옵션을 정의합니다.
    /// </summary>
    public class LightningRadarImageStorageOptions
    {
        public LightningRadarImageStorageOptions()
        {
            RootType = LightningRadarStorageRootType.Documents;
            CustomRootPath = string.Empty;
            RootFolderName = "LightningRadarImages";
            ImageFormat = LightningRadarImageFormat.Png;
            ImageWidth = 0;
            ImageHeight = 0;
            JpegQuality = 90L;
        }

        /// <summary>
        /// 기본 저장 루트 위치입니다.
        /// Documents, AppData, Desktop, Custom 중에서 선택합니다.
        /// 기본값: Documents
        /// </summary>
        public LightningRadarStorageRootType RootType { get; set; }

        /// <summary>
        /// RootType 이 Custom 일 때 사용할 사용자 지정 루트 경로입니다.
        /// 기본값: 빈 문자열
        /// </summary>
        public string CustomRootPath { get; set; }

        /// <summary>
        /// 기본 루트 아래에 생성할 상위 폴더 이름입니다.
        /// 기본값: LightningRadarImages
        /// </summary>
        public string RootFolderName { get; set; }

        /// <summary>
        /// 저장할 파일 포맷입니다.
        /// 기본값: PNG
        /// </summary>
        public LightningRadarImageFormat ImageFormat { get; set; }

        /// <summary>
        /// 저장 이미지 너비입니다.
        /// 0 이하이면 현재 차트 컨트롤 너비를 사용합니다.
        /// 기본값: 0
        /// </summary>
        public int ImageWidth { get; set; }

        /// <summary>
        /// 저장 이미지 높이입니다.
        /// 0 이하이면 현재 차트 컨트롤 높이를 사용합니다.
        /// 기본값: 0
        /// </summary>
        public int ImageHeight { get; set; }

        /// <summary>
        /// JPEG 저장 시 사용할 품질 값입니다.
        /// 1~100 범위를 권장합니다.
        /// 기본값: 90
        /// </summary>
        public long JpegQuality { get; set; }

        public LightningRadarImageStorageOptions Clone()
        {
            return (LightningRadarImageStorageOptions)MemberwiseClone();
        }
    }

    /// <summary>
    /// FAB_ID, LOT_CD, DRAFT_NO 기준으로 이미지 저장/검색 경로를 식별합니다.
    /// </summary>
    public class LightningRadarImagePathInfo
    {
        public LightningRadarImagePathInfo()
        {
            FabId = string.Empty;
            LotCd = string.Empty;
            DraftNo = string.Empty;
            FileNamePrefix = "radar";
        }

        /// <summary>
        /// 공장 식별자입니다. 최상위 하위 폴더명으로 사용됩니다.
        /// </summary>
        public string FabId { get; set; }

        /// <summary>
        /// LOT 코드입니다. 두 번째 하위 폴더명으로 사용됩니다.
        /// </summary>
        public string LotCd { get; set; }

        /// <summary>
        /// DRAFT 번호입니다. 세 번째 하위 폴더명으로 사용됩니다.
        /// </summary>
        public string DraftNo { get; set; }

        /// <summary>
        /// 저장 파일명 앞에 붙일 접두어입니다.
        /// 기본값: radar
        /// </summary>
        public string FileNamePrefix { get; set; }

        public LightningRadarImagePathInfo Clone()
        {
            return (LightningRadarImagePathInfo)MemberwiseClone();
        }
    }

    /// <summary>
    /// 코드만으로 생성하여 여러 WinForms 폼에서 재사용할 수 있는 레이더 차트 컨트롤입니다.
    /// </summary>
    public class LightningRadar : Control
    {
        private readonly object syncRoot = new object();
        private string[] categories = new string[0];
        private List<LightningRadarSeries> series = new List<LightningRadarSeries>();
        private LightningRadarOptions options = new LightningRadarOptions();
        // 마지막 레전드가 그려진 영역을 보관하여 스케일/라벨이 그 영역과 겹치지 않도록 처리합니다.
        private RectangleF lastLegendBounds = RectangleF.Empty;

        public LightningRadar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            DoubleBuffered = true;
            BackColor = Color.White;
            Size = new Size(820, 540);
        }

        [Browsable(false)]
        public string[] Categories
        {
            get
            {
                lock (syncRoot)
                {
                    return categories.ToArray();
                }
            }
        }

        [Browsable(false)]
        public LightningRadarSeries[] Series
        {
            get
            {
                lock (syncRoot)
                {
                    return series.Select(item => item.Clone()).ToArray();
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public LightningRadarOptions Options
        {
            get
            {
                lock (syncRoot)
                {
                    return options.Clone();
                }
            }
        }

        public static T AttachTo<T>(Control parent, DockStyle dockStyle, Rectangle? bounds, LightningRadarOptions options)
            where T : LightningRadar, new()
        {
            T radar = new T();

            if (options != null)
            {
                radar.SetOptions(options);
            }

            if (bounds.HasValue)
            {
                radar.Bounds = bounds.Value;
            }

            radar.Dock = dockStyle;
            radar.AddTo(parent);
            return radar;
        }

        public static LightningRadar Create(Control parent, IEnumerable<string> newCategories, IEnumerable<LightningRadarSeries> newSeries, LightningRadarOptions options)
        {
            LightningRadar radar = AttachTo<LightningRadar>(parent, DockStyle.Fill, null, options);
            radar.SetData(newCategories, newSeries);
            return radar;
        }

        public void AddTo(Control parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            ExecuteOnUiThread(parent, delegate
            {
                if (Parent == parent)
                {
                    return;
                }

                parent.Controls.Add(this);
                BringToFront();
            });
        }

        public void SetData(IEnumerable<string> newCategories, IEnumerable<LightningRadarSeries> newSeries)
        {
            string[] categoryArray = newCategories == null ? new string[0] : newCategories.ToArray();
            List<LightningRadarSeries> seriesList = newSeries == null
                ? new List<LightningRadarSeries>()
                : newSeries.Select(item => item == null ? new LightningRadarSeries() : item.Clone()).ToList();

            NormalizeSeries(categoryArray, seriesList);

            lock (syncRoot)
            {
                categories = categoryArray;
                series = seriesList;
            }

            RefreshSafe();
        }

        public void SetCategories(IEnumerable<string> newCategories)
        {
            SetData(newCategories, Series);
        }

        public void SetSeries(IEnumerable<LightningRadarSeries> newSeries)
        {
            SetData(Categories, newSeries);
        }

        public void UpdateData(IEnumerable<string> newCategories, IEnumerable<LightningRadarSeries> newSeries, LightningRadarOptions newOptions)
        {
            string[] categoryArray = newCategories == null ? new string[0] : newCategories.ToArray();
            List<LightningRadarSeries> seriesList = newSeries == null
                ? new List<LightningRadarSeries>()
                : newSeries.Select(item => item == null ? new LightningRadarSeries() : item.Clone()).ToList();
            LightningRadarOptions nextOptions = newOptions == null ? new LightningRadarOptions() : newOptions.Clone();

            NormalizeSeries(categoryArray, seriesList);

            lock (syncRoot)
            {
                categories = categoryArray;
                series = seriesList;
                options = nextOptions;
            }

            RefreshSafe();
        }

        public void SetOptions(LightningRadarOptions newOptions)
        {
            if (newOptions == null)
            {
                throw new ArgumentNullException("newOptions");
            }

            lock (syncRoot)
            {
                options = newOptions.Clone();
            }

            RefreshSafe();
        }

        public void Update(Action<LightningRadar> updateAction)
        {
            if (updateAction == null)
            {
                throw new ArgumentNullException("updateAction");
            }

            ExecuteOnUiThread(this, delegate { updateAction(this); });
        }

        public string SaveImage(LightningRadarImagePathInfo pathInfo)
        {
            return SaveImage(pathInfo, Options.ImageStorage);
        }

        public string SaveImage(LightningRadarImagePathInfo pathInfo, LightningRadarImageStorageOptions storageOptions)
        {
            if (pathInfo == null)
            {
                throw new ArgumentNullException("pathInfo");
            }

            LightningRadarImageStorageOptions effectiveOptions = storageOptions == null ? new LightningRadarImageStorageOptions() : storageOptions.Clone();
            string folderPath = GetImageFolderPath(pathInfo, effectiveOptions, true);
            string fileExtension = GetFileExtension(effectiveOptions.ImageFormat);
            string safePrefix = SanitizePathSegment(pathInfo.FileNamePrefix, "radar");
            string fileName = string.Format("{0}_{1:yyyyMMdd_HHmmssfff}{2}", safePrefix, DateTime.Now, fileExtension);
            string fullPath = Path.Combine(folderPath, fileName);

            ExecuteOnUiThread(this, delegate
            {
                using (Bitmap bitmap = CreateChartBitmap(effectiveOptions))
                {
                    SaveBitmapToFile(bitmap, fullPath, effectiveOptions);
                }
            }, true);

            return fullPath;
        }

        public string GetImageFolderPath(LightningRadarImagePathInfo pathInfo)
        {
            return GetImageFolderPath(pathInfo, Options.ImageStorage, false);
        }

        public string[] FindImages(LightningRadarImagePathInfo pathInfo)
        {
            return FindImages(pathInfo, Options.ImageStorage);
        }

        public string[] FindImages(LightningRadarImagePathInfo pathInfo, LightningRadarImageStorageOptions storageOptions)
        {
            string folderPath = GetImageFolderPath(pathInfo, storageOptions, false);
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return new string[0];
            }

            string[] allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
            return Directory.GetFiles(folderPath)
                .Where(file => allowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(File.GetCreationTime)
                .ToArray();
        }

        public string FindLatestImage(LightningRadarImagePathInfo pathInfo)
        {
            return FindLatestImage(pathInfo, Options.ImageStorage);
        }

        public string FindLatestImage(LightningRadarImagePathInfo pathInfo, LightningRadarImageStorageOptions storageOptions)
        {
            return FindImages(pathInfo, storageOptions).FirstOrDefault();
        }

        public int DeleteImagesCreatedAfter(LightningRadarImagePathInfo pathInfo, DateTime threshold)
        {
            return DeleteImagesCreatedAfter(pathInfo, threshold, Options.ImageStorage);
        }

        public int DeleteImagesCreatedAfter(LightningRadarImagePathInfo pathInfo, DateTime threshold, LightningRadarImageStorageOptions storageOptions)
        {
            string[] files = FindImages(pathInfo, storageOptions);
            int deletedCount = 0;

            foreach (string file in files)
            {
                DateTime createdTime = File.GetCreationTime(file);
                if (createdTime <= threshold)
                {
                    continue;
                }

                File.Delete(file);
                deletedCount++;
            }

            return deletedCount;
        }

        public virtual LightningRadarOptions CreateDefaultOptions()
        {
            return new LightningRadarOptions();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            string[] snapshotCategories;
            LightningRadarSeries[] snapshotSeries;
            LightningRadarOptions snapshotOptions;

            lock (syncRoot)
            {
                snapshotCategories = categories.ToArray();
                snapshotSeries = series.Select(item => item.Clone()).ToArray();
                snapshotOptions = options.Clone();
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(snapshotOptions.BackgroundColor);

            // 먼저 레전드가 차지할 영역을 계산하여 다른 요소(스케일 라벨 등)가 겹치지 않도록 합니다.
            lastLegendBounds = snapshotOptions.ShowLegend
                ? CalculateLegendBounds(e.Graphics, snapshotSeries, snapshotOptions, ClientSize)
                : RectangleF.Empty;

            if (snapshotOptions.ShowTitle)
            {
                DrawTitle(e.Graphics, snapshotOptions);
            }

            DrawRadarChart(e.Graphics, snapshotCategories, snapshotSeries, snapshotOptions);

            // 레전드를 실제로 그립니다.
            if (snapshotOptions.ShowLegend)
            {
                DrawLegend(e.Graphics, snapshotSeries, snapshotOptions);
            }
        }

        /// <summary>
        /// 레전드가 실제로 차지할 영역을 계산하여 반환합니다.
        /// DrawLegend와 동일한 좌표 계산을 사용합니다.
        /// </summary>
        protected virtual RectangleF CalculateLegendBounds(Graphics graphics, LightningRadarSeries[] currentSeries, LightningRadarOptions currentOptions, Size renderSize)
        {
            if (currentSeries == null || currentSeries.Length == 0)
            {
                return RectangleF.Empty;
            }

            const float markerWidth = 20f;
            const float labelSpacing = 8f;
            float sectionSpacing = Math.Max(0f, currentOptions.LegendItemSpacing);

            using (var legendFont = new Font(Font.FontFamily, 9f, FontStyle.Regular))
            {
                float totalLegendWidth = 0f;
                float maxHeight = 0f;
                foreach (LightningRadarSeries radarSeries in currentSeries)
                {
                    SizeF textSize = graphics.MeasureString(radarSeries.Name ?? string.Empty, legendFont);
                    totalLegendWidth += markerWidth + labelSpacing + textSize.Width + sectionSpacing;
                    maxHeight = Math.Max(maxHeight, Math.Max(textSize.Height, 14f));
                }

                totalLegendWidth = Math.Max(0f, totalLegendWidth - sectionSpacing);
                float legendX = (renderSize.Width - totalLegendWidth) / 2f;
                float legendY = 12f;

                return new RectangleF(legendX, legendY - 2f, totalLegendWidth, maxHeight + 4f);
            }
        }

        protected virtual void DrawTitle(Graphics graphics, LightningRadarOptions currentOptions)
        {
            using (var titleFont = new Font(Font.FontFamily, currentOptions.TitleFontSize, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(currentOptions.TitleColor))
            {
                graphics.DrawString(currentOptions.Title ?? string.Empty, titleFont, titleBrush, 20f, 15f);
            }
        }

        protected virtual void DrawRadarChart(Graphics graphics, string[] currentCategories, LightningRadarSeries[] currentSeries, LightningRadarOptions currentOptions)
        {
            DrawRadarChart(graphics, currentCategories, currentSeries, currentOptions, ClientSize);
        }

        protected virtual void DrawRadarChart(Graphics graphics, string[] currentCategories, LightningRadarSeries[] currentSeries, LightningRadarOptions currentOptions, Size renderSize)
        {
            if (currentCategories.Length == 0)
            {
                return;
            }

            int availableWidth = renderSize.Width - (currentOptions.ChartPadding * 2);
            int availableHeight = renderSize.Height - currentOptions.TopOffset - currentOptions.ChartPadding;
            int diameter = Math.Min(availableWidth, availableHeight);
            if (diameter <= 100)
            {
                return;
            }

            float radius = (diameter / 2f) - currentOptions.RadiusPadding;
            PointF center = new PointF(renderSize.Width / 2f, currentOptions.TopOffset + (diameter / 2f));

            DrawGrid(graphics, center, radius, currentCategories, currentOptions);
            DrawScaleLabels(graphics, center, radius, currentOptions);
            DrawCategories(graphics, center, radius, currentCategories, currentOptions);

            foreach (LightningRadarSeries radarSeries in currentSeries)
            {
                DrawSeries(graphics, center, radius, currentCategories, radarSeries, currentOptions);
            }
        }

        protected virtual void DrawGrid(Graphics graphics, PointF center, float radius, string[] currentCategories, LightningRadarOptions currentOptions)
        {
            int ringCount = Math.Max(1, currentOptions.GridRingCount);

            using (var majorGridPen = new Pen(currentOptions.MajorGridColor, 1.4f))
            using (var minorGridPen = new Pen(currentOptions.MinorGridColor, 1f))
            using (var spokePen = new Pen(currentOptions.SpokeColor, 1f))
            {
                for (int i = 1; i <= ringCount; i++)
                {
                    float currentRadius = radius * i / ringCount;
                    Pen currentPen = (i % 2 == 0 || i == ringCount) ? majorGridPen : minorGridPen;
                    graphics.DrawEllipse(currentPen, center.X - currentRadius, center.Y - currentRadius, currentRadius * 2f, currentRadius * 2f);
                }

                for (int i = 0; i < currentCategories.Length; i++)
                {
                    PointF outerPoint = GetRadarPoint(center, radius, i, currentCategories.Length);
                    graphics.DrawLine(spokePen, center, outerPoint);
                }
            }
        }

        protected virtual void DrawScaleLabels(Graphics graphics, PointF center, float radius, LightningRadarOptions currentOptions)
        {
            int ringCount = Math.Max(1, currentOptions.GridRingCount);
            float ringValueStep = 100f / ringCount;

            using (var labelFont = new Font(Font.FontFamily, currentOptions.ScaleFontSize, FontStyle.Bold))
            using (var labelBrush = new SolidBrush(currentOptions.ScaleLabelColor))
            {
                // 동적 간격 계산: 라벨 높이와 반지름 기반 픽셀 간격을 비교해 표시할 라벨 간격(skip)을 결정합니다.
                SizeF sampleSize = graphics.MeasureString("100", labelFont);
                float labelHeight = sampleSize.Height;
                float ringPixelStep = radius / ringCount;
                int skip = (int)Math.Ceiling((labelHeight + 4f) / Math.Max(0.5f, ringPixelStep));
                if (skip < 1) skip = 1;

                // 0은 항상 표시 (단, 레전드와 겹치면 표시 생략)
                SizeF zeroLabelSize = graphics.MeasureString("0", labelFont);
                var zeroRect = new RectangleF(center.X - (zeroLabelSize.Width / 2f), center.Y - (zeroLabelSize.Height / 2f), zeroLabelSize.Width, zeroLabelSize.Height);
                if (!lastLegendBounds.IntersectsWith(zeroRect))
                {
                    graphics.DrawString("0", labelFont, labelBrush, zeroRect.X, zeroRect.Y);
                }

                // 간격(skip)에 따라 라벨을 출력
                for (int ring = skip; ring <= ringCount; ring += skip)
                {
                    int value = (int)Math.Round(ring * ringValueStep);
                    float currentRadius = radius * ring / ringCount;
                    PointF point = new PointF(center.X, center.Y - currentRadius);
                    SizeF labelSize = graphics.MeasureString(value.ToString(), labelFont);
                    var labelRect = new RectangleF(point.X - (labelSize.Width / 2f), point.Y - labelSize.Height - 4f, labelSize.Width, labelSize.Height);
                    // 레전드와 겹치면 표시하지 않음
                    if (!lastLegendBounds.IntersectsWith(labelRect))
                    {
                        graphics.DrawString(value.ToString(), labelFont, labelBrush, labelRect.X, labelRect.Y);
                    }
                }

                // 최상단(100)이 출력되지 않았을 경우 보장 출력
                if ((ringCount % skip) != 0)
                {
                    int value = 100;
                    float currentRadius = radius;
                    PointF point = new PointF(center.X, center.Y - currentRadius);
                    SizeF labelSize = graphics.MeasureString(value.ToString(), labelFont);
                    var labelRect = new RectangleF(point.X - (labelSize.Width / 2f), point.Y - labelSize.Height - 4f, labelSize.Width, labelSize.Height);
                    if (!lastLegendBounds.IntersectsWith(labelRect))
                    {
                        graphics.DrawString(value.ToString(), labelFont, labelBrush, labelRect.X, labelRect.Y);
                    }
                }
            }
        }

        protected virtual void DrawCategories(Graphics graphics, PointF center, float radius, string[] currentCategories, LightningRadarOptions currentOptions)
        {
            using (var labelFont = new Font(Font.FontFamily, currentOptions.CategoryFontSize, FontStyle.Regular))
            using (var labelBrush = new SolidBrush(currentOptions.CategoryLabelColor))
            {
                for (int i = 0; i < currentCategories.Length; i++)
                {
                    float angle = GetRadarAngle(i, currentCategories.Length);
                    PointF point = GetRadarPoint(center, radius + currentOptions.CategoryLabelOffset, i, currentCategories.Length);

                    if (i == 0 && Math.Abs(currentOptions.TopCategoryLabelHorizontalOffset) > float.Epsilon)
                    {
                        point = new PointF(point.X + currentOptions.TopCategoryLabelHorizontalOffset, point.Y);
                    }

                    if (i == 0 && Math.Abs(currentOptions.TopCategoryLabelVerticalOffset) > float.Epsilon)
                    {
                        point = new PointF(point.X, point.Y - currentOptions.TopCategoryLabelVerticalOffset);
                    }

                    using (var format = CreateCategoryLabelFormat(angle))
                    {
                        graphics.DrawString(currentCategories[i], labelFont, labelBrush, point, format);
                    }
                }
            }
        }

        protected virtual void DrawSeries(Graphics graphics, PointF center, float radius, string[] currentCategories, LightningRadarSeries radarSeries, LightningRadarOptions currentOptions)
        {
            if (radarSeries == null || radarSeries.Values == null || radarSeries.Values.Length == 0 || currentCategories.Length == 0)
            {
                return;
            }

            PointF[] points = radarSeries.Values
                .Take(currentCategories.Length)
                .Select((value, index) => GetRadarPoint(center, radius * Math.Max(0f, Math.Min(100f, value)) / 100f, index, currentCategories.Length))
                .ToArray();

            if (points.Length < 3)
            {
                return;
            }

            using (var fillBrush = new SolidBrush(radarSeries.FillColor))
            using (var linePen = new Pen(radarSeries.LineColor, currentOptions.SeriesLineWidth))
            using (var pointBrush = new SolidBrush(radarSeries.LineColor))
            {
                graphics.FillPolygon(fillBrush, points);
                graphics.DrawPolygon(linePen, points);

                float markerRadius = currentOptions.SeriesPointSize / 2f;
                foreach (PointF point in points)
                {
                    graphics.FillEllipse(pointBrush, point.X - markerRadius, point.Y - markerRadius, currentOptions.SeriesPointSize, currentOptions.SeriesPointSize);
                }
            }
        }

        protected virtual void DrawLegend(Graphics graphics, LightningRadarSeries[] currentSeries, LightningRadarOptions currentOptions)
        {
            DrawLegend(graphics, currentSeries, currentOptions, ClientSize);
        }

        protected virtual void DrawLegend(Graphics graphics, LightningRadarSeries[] currentSeries, LightningRadarOptions currentOptions, Size renderSize)
        {
            if (currentSeries == null || currentSeries.Length == 0)
            {
                return;
            }

            const float markerWidth = 20f;
            const float labelSpacing = 8f;
            float sectionSpacing = Math.Max(0f, currentOptions.LegendItemSpacing);

            using (var legendFont = new Font(Font.FontFamily, 9f, FontStyle.Regular))
            using (var textBrush = new SolidBrush(currentOptions.LegendTextColor))
            {
                float totalLegendWidth = 0f;
                foreach (LightningRadarSeries radarSeries in currentSeries)
                {
                    SizeF textSize = graphics.MeasureString(radarSeries.Name ?? string.Empty, legendFont);
                    totalLegendWidth += markerWidth + labelSpacing + textSize.Width + sectionSpacing;
                }

                totalLegendWidth = Math.Max(0f, totalLegendWidth - sectionSpacing);
                float legendX = (renderSize.Width - totalLegendWidth) / 2f;
                float legendY = 12f;

                foreach (LightningRadarSeries radarSeries in currentSeries)
                {
                    SizeF textSize = graphics.MeasureString(radarSeries.Name ?? string.Empty, legendFont);
                    DrawLegendItem(graphics, legendFont, textBrush, legendX, legendY, radarSeries.FillColor, radarSeries.LineColor, radarSeries.Name ?? string.Empty);
                    legendX += markerWidth + labelSpacing + textSize.Width + sectionSpacing;
                }
            }
        }

        protected virtual void DrawLegendItem(Graphics graphics, Font font, Brush textBrush, float x, float y, Color fillColor, Color lineColor, string text)
        {
            RectangleF markerRect = new RectangleF(x, y, 20f, 14f);

            using (var fillBrush = new SolidBrush(fillColor))
            using (var borderPen = new Pen(lineColor, 1.5f))
            {
                graphics.FillRectangle(fillBrush, markerRect);
                graphics.DrawRectangle(borderPen, markerRect.X, markerRect.Y, markerRect.Width, markerRect.Height);
            }

            graphics.DrawString(text, font, textBrush, x + 28f, y - 2f);
        }

        protected virtual PointF GetRadarPoint(PointF center, float radius, int index, int totalCount)
        {
            double angle = GetRadarAngle(index, totalCount);
            float x = center.X + (float)(Math.Cos(angle) * radius);
            float y = center.Y + (float)(Math.Sin(angle) * radius);
            return new PointF(x, y);
        }

        protected virtual float GetRadarAngle(int index, int totalCount)
        {
            return (float)((-Math.PI / 2d) + ((Math.PI * 2d * index) / totalCount));
        }

        protected virtual StringFormat CreateCategoryLabelFormat(float angle)
        {
            var format = new StringFormat();
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            if (cos > 0.25f)
            {
                format.Alignment = StringAlignment.Near;
            }
            else if (cos < -0.25f)
            {
                format.Alignment = StringAlignment.Far;
            }
            else
            {
                format.Alignment = StringAlignment.Center;
            }

            if (sin > 0.25f)
            {
                format.LineAlignment = StringAlignment.Near;
            }
            else if (sin < -0.25f)
            {
                format.LineAlignment = StringAlignment.Far;
            }
            else
            {
                format.LineAlignment = StringAlignment.Center;
            }

            return format;
        }

        protected virtual void RefreshSafe()
        {
            ExecuteOnUiThread(this, Invalidate);
        }

        protected virtual void NormalizeSeries(string[] currentCategories, IList<LightningRadarSeries> currentSeries)
        {
            int targetCount = currentCategories == null ? 0 : currentCategories.Length;

            if (targetCount <= 0 || currentSeries == null)
            {
                return;
            }

            foreach (LightningRadarSeries radarSeries in currentSeries)
            {
                if (radarSeries == null)
                {
                    continue;
                }

                float[] values = radarSeries.Values ?? new float[0];
                if (values.Length == targetCount)
                {
                    continue;
                }

                float[] normalized = new float[targetCount];
                Array.Copy(values, normalized, Math.Min(values.Length, targetCount));
                radarSeries.Values = normalized;
            }
        }

        protected virtual Bitmap CreateChartBitmap(LightningRadarImageStorageOptions storageOptions)
        {
            int width = Math.Max(1, storageOptions.ImageWidth > 0 ? storageOptions.ImageWidth : Width);
            int height = Math.Max(1, storageOptions.ImageHeight > 0 ? storageOptions.ImageHeight : Height);

            string[] snapshotCategories;
            LightningRadarSeries[] snapshotSeries;
            LightningRadarOptions snapshotOptions;

            lock (syncRoot)
            {
                snapshotCategories = categories.ToArray();
                snapshotSeries = series.Select(item => item.Clone()).ToArray();
                snapshotOptions = options.Clone();
            }

            Bitmap bitmap = new Bitmap(width, height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(snapshotOptions.BackgroundColor);

                lastLegendBounds = snapshotOptions.ShowLegend
                    ? CalculateLegendBounds(graphics, snapshotSeries, snapshotOptions, new Size(width, height))
                    : RectangleF.Empty;

                if (snapshotOptions.ShowTitle)
                {
                    DrawTitle(graphics, snapshotOptions);
                }

                DrawRadarChart(graphics, snapshotCategories, snapshotSeries, snapshotOptions, new Size(width, height));

                if (snapshotOptions.ShowLegend)
                {
                    DrawLegend(graphics, snapshotSeries, snapshotOptions, new Size(width, height));
                }
            }

            return bitmap;
        }

        protected virtual void SaveBitmapToFile(Bitmap bitmap, string path, LightningRadarImageStorageOptions storageOptions)
        {
            ImageFormat imageFormat = GetDrawingImageFormat(storageOptions.ImageFormat);
            if (storageOptions.ImageFormat == LightningRadarImageFormat.Jpeg)
            {
                ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(item => item.FormatID == ImageFormat.Jpeg.Guid);
                if (codec != null)
                {
                    EncoderParameters parameters = new EncoderParameters(1);
                    long quality = Math.Max(1L, Math.Min(100L, storageOptions.JpegQuality));
                    parameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                    bitmap.Save(path, codec, parameters);
                    parameters.Dispose();
                    return;
                }
            }

            bitmap.Save(path, imageFormat);
        }

        protected virtual string GetImageFolderPath(LightningRadarImagePathInfo pathInfo, LightningRadarImageStorageOptions storageOptions, bool createIfMissing)
        {
            if (pathInfo == null)
            {
                throw new ArgumentNullException("pathInfo");
            }

            LightningRadarImageStorageOptions effectiveOptions = storageOptions == null ? new LightningRadarImageStorageOptions() : storageOptions.Clone();
            string rootPath = ResolveRootPath(effectiveOptions);
            string folderPath = Path.Combine(
                rootPath,
                SanitizePathSegment(effectiveOptions.RootFolderName, "LightningRadarImages"),
                SanitizePathSegment(pathInfo.FabId, "FAB_UNKNOWN"),
                SanitizePathSegment(pathInfo.LotCd, "LOT_UNKNOWN"),
                SanitizePathSegment(pathInfo.DraftNo, "DRAFT_UNKNOWN"));

            if (createIfMissing && !Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            return folderPath;
        }

        protected virtual string ResolveRootPath(LightningRadarImageStorageOptions storageOptions)
        {
            switch (storageOptions.RootType)
            {
                case LightningRadarStorageRootType.AppData:
                    return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                case LightningRadarStorageRootType.LocalAppData:
                    return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                case LightningRadarStorageRootType.Desktop:
                    return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                case LightningRadarStorageRootType.Custom:
                    if (string.IsNullOrWhiteSpace(storageOptions.CustomRootPath))
                    {
                        throw new InvalidOperationException("CustomRootPath 값이 필요합니다.");
                    }

                    return storageOptions.CustomRootPath;
                case LightningRadarStorageRootType.Documents:
                default:
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
        }

        protected virtual ImageFormat GetDrawingImageFormat(LightningRadarImageFormat imageFormat)
        {
            switch (imageFormat)
            {
                case LightningRadarImageFormat.Jpeg:
                    return ImageFormat.Jpeg;
                case LightningRadarImageFormat.Bmp:
                    return ImageFormat.Bmp;
                case LightningRadarImageFormat.Gif:
                    return ImageFormat.Gif;
                case LightningRadarImageFormat.Png:
                default:
                    return ImageFormat.Png;
            }
        }

        protected virtual string GetFileExtension(LightningRadarImageFormat imageFormat)
        {
            switch (imageFormat)
            {
                case LightningRadarImageFormat.Jpeg:
                    return ".jpg";
                case LightningRadarImageFormat.Bmp:
                    return ".bmp";
                case LightningRadarImageFormat.Gif:
                    return ".gif";
                case LightningRadarImageFormat.Png:
                default:
                    return ".png";
            }
        }

        protected virtual string SanitizePathSegment(string value, string fallback)
        {
            string text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                text = text.Replace(invalidChar, '_');
            }

            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }

        protected virtual void ExecuteOnUiThread(Control control, Action action)
        {
            ExecuteOnUiThread(control, action, false);
        }

        protected virtual void ExecuteOnUiThread(Control control, Action action, bool forceSynchronous)
        {
            if (action == null)
            {
                return;
            }

            if (control == null || control.IsDisposed)
            {
                return;
            }

            if (!control.IsHandleCreated)
            {
                action();
                return;
            }

            if (control.InvokeRequired)
            {
                if (forceSynchronous)
                {
                    control.Invoke(action);
                }
                else
                {
                    control.BeginInvoke(action);
                }

                return;
            }

            action();
        }
    }
}
