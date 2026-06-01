using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace LightingChartSamples
{
    public partial class TextDocument : Form
    {
        public TextDocument()
        {
            InitializeComponent();
            Load += TextDocument_Load;
            SizeChanged += TextDocument_SizeChanged;
            BuildDocument();
        }

        private void TextDocument_Load(object sender, EventArgs e)
        {
            ApplyWebStyle();
        }

        private void TextDocument_SizeChanged(object sender, EventArgs e)
        {
            ApplyWebStyle();
        }

        private void ApplyWebStyle()
        {
            panelHeader.Paint -= PanelHeader_Paint;
            panelHeader.Paint += PanelHeader_Paint;

            ApplyRoundedRegion(panelHeader, 18);
            ApplyRoundedRegion(panelCard, 14);
            ApplyRoundedRegion(panelBottom, 12);
        }

        private void PanelHeader_Paint(object sender, PaintEventArgs e)
        {
            using (var brush = new LinearGradientBrush(panelHeader.ClientRectangle,
                Color.FromArgb(70, 118, 210),
                Color.FromArgb(103, 158, 245),
                20f))
            {
                e.Graphics.FillRectangle(brush, panelHeader.ClientRectangle);
            }
        }

        private void ApplyRoundedRegion(Control control, int radius)
        {
            if (control.Width <= 0 || control.Height <= 0)
            {
                return;
            }

            var rect = new Rectangle(0, 0, control.Width, control.Height);
            var diameter = radius * 2;
            var path = new GraphicsPath();

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            control.Region = new Region(path);
        }

        private void BuildDocument()
        {
            richTextBoxDocument.Clear();
            richTextBoxDocument.DetectUrls = true;
            richTextBoxDocument.SelectionIndent = 0;
            richTextBoxDocument.SelectionHangingIndent = 0;
            var ivoryBackColor = Color.FromArgb(255, 255, 240);

            var titleFont = new Font("맑은 고딕", 14F, FontStyle.Bold);
            var headingFont = new Font("맑은 고딕", 12F, FontStyle.Bold);
            var bodyFont = new Font("맑은 고딕", 11F, FontStyle.Regular);
            var tipFont = new Font("맑은 고딕", 10.5F, FontStyle.Regular);
            var chipFont = new Font("맑은 고딕", 9.5F, FontStyle.Bold);

            AppendStyledText("3. 비즈니스 보고서 요약 샘플 (개조식)\n", titleFont, Color.Black);
            AppendStyledText("  LIVE  ", chipFont, Color.White, Color.FromArgb(236, 95, 80));
            AppendStyledText("  KPI TRACKING  ", chipFont, Color.White, Color.FromArgb(58, 136, 255));
            AppendStyledText("\n\n", bodyFont, Color.Black);
            AppendStyledText("실제 업무에서 사용하는 결론 중심의 핵심 보고서 텍스트 양식입니다.\n", bodyFont, Color.FromArgb(57, 67, 89));
            AppendStyledText("────────────────────────────────────────────────────\n\n", bodyFont, Color.FromArgb(225, 229, 236));

            AppendBulletHeading("[추진 배경] ", headingFont, bodyFont);
            AppendStyledText("분기별 매출 감소에 따른 온·오프라인 채널 통합 마케팅 전략 변경 필요성 대두\n\n", bodyFont, Color.Black, ivoryBackColor);

            AppendBulletHeading("[개선 방안]\n", headingFont, bodyFont);
            AppendSubBullet("모바일 앱 UI/UX 개편을 통한 사용자 편의성 증대", bodyFont, ivoryBackColor);
            AppendSubBullet("타겟 고객층(2030 세대) 맞춤형 프로모션 전개\n", bodyFont, ivoryBackColor);

            AppendBulletHeading("[기대 효과] ", headingFont, bodyFont);
            AppendStyledText("신규 고객 유입률 ", bodyFont, Color.Black, ivoryBackColor);
            AppendStyledText("15%", new Font("맑은 고딕", 11F, FontStyle.Bold), Color.FromArgb(35, 84, 148), ivoryBackColor);
            AppendStyledText(" 증가 및 기존 고객 이탈율 감소 기대\n\n", bodyFont, Color.Black, ivoryBackColor);

            AppendStyledText("📌  🚗Next Action: ", new Font("맑은 고딕", 10.5F, FontStyle.Bold), Color.FromArgb(60, 85, 128));
            AppendStyledText("다음 주까지 A/B 테스트 2종과 채널별 CAC 리포트 업데이트\n", bodyFont, Color.FromArgb(72, 84, 104));

            AppendTipBox(tipFont);

            richTextBoxDocument.SelectionStart = 0;
            richTextBoxDocument.SelectionLength = 0;
            richTextBoxDocument.ScrollToCaret();
        }

        private void AppendBulletHeading(string text, Font headingFont, Font bodyFont)
        {
            richTextBoxDocument.SelectionBullet = true;
            richTextBoxDocument.SelectionIndent = 0;
            richTextBoxDocument.SelectionHangingIndent = 0;
            AppendStyledText(text, headingFont, Color.Red);
            richTextBoxDocument.SelectionBullet = false;
            richTextBoxDocument.SelectionIndent = 0;
            richTextBoxDocument.SelectionHangingIndent = 0;
            richTextBoxDocument.SelectionFont = bodyFont;
        }

        private void AppendSubBullet(string text, Font font, Color backColor)
        {
            richTextBoxDocument.SelectionIndent = 25;
            richTextBoxDocument.SelectionHangingIndent = 15;
            richTextBoxDocument.SelectionBullet = true;
            AppendStyledText(text + "\n", font, Color.Black, backColor);
            richTextBoxDocument.SelectionBullet = false;
            richTextBoxDocument.SelectionIndent = 0;
            richTextBoxDocument.SelectionHangingIndent = 0;
        }

        private void AppendTipBox(Font tipFont)
        {
            AppendStyledText("\n", tipFont, Color.Black);
            var tipBackColor = Color.FromArgb(237, 239, 242);
            var tipForeColor = Color.FromArgb(35, 44, 62);

            AppendStyledText("   💡 Tip: ", new Font("맑은 고딕", 10.5F, FontStyle.Bold), tipForeColor, tipBackColor, false);
            AppendStyledText("한글 문장의 다양한 더미 텍스트가 필요하다면 ", tipFont, tipForeColor, tipBackColor, false);
            AppendStyledText("한글 Lorem Ipsum 생성기", new Font("맑은 고딕", 10.5F, FontStyle.Underline), Color.FromArgb(26, 64, 140), tipBackColor, false);
            AppendStyledText("를 통해 맞춤형 테스트 문자열을 생성하실 수 있습니다.", tipFont, tipForeColor, tipBackColor, false);
            AppendStyledText("   \n", tipFont, tipForeColor, tipBackColor, false);
        }

        private void AppendStyledText(string text, Font font, Color foreColor, Color? backColor = null, bool normalizeParagraph = true)
        {
            string resolvedText = normalizeParagraph ? NormalizeParagraphText(text) : text;

            richTextBoxDocument.SelectionStart = richTextBoxDocument.TextLength;
            richTextBoxDocument.SelectionLength = 0;
            richTextBoxDocument.SelectionFont = font;
            richTextBoxDocument.SelectionColor = foreColor;
            richTextBoxDocument.SelectionBackColor = backColor ?? richTextBoxDocument.BackColor;
            richTextBoxDocument.AppendText(resolvedText);
            richTextBoxDocument.SelectionBackColor = richTextBoxDocument.BackColor;
        }

        private static string NormalizeParagraphText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", " ");
            string[] lines = normalized.Split(new[] { '\n' }, StringSplitOptions.None);
            var builder = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                if (i > 0)
                {
                    line = line.TrimStart();
                }

                if (i > 0)
                {
                    builder.Append("\n");
                }

                builder.Append(line);
            }

            return builder.ToString();
        }
    }
}
