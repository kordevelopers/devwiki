using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FarPoint.Win.Spread;
using System.ComponentModel;

namespace hynixTas
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SpreadGridForm());
        }
    }

    internal class SpreadGridForm : Form
    {
        public SpreadGridForm()
        {
            Text = "FarPoint Spread Grid Sample";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900, 500);

            var fpSpread = new FpSpread
            {
                Dock = DockStyle.Fill,
                BackColor = Color.WhiteSmoke
            };

            var sheet = new SheetView();
            fpSpread.Sheets.Add(sheet);

            sheet.RowCount = 5;
            sheet.ColumnCount = 4;
            sheet.ColumnHeader.RowCount = 1;

            sheet.ColumnHeader.Cells[0, 0].Text = "ID";
            sheet.ColumnHeader.Cells[0, 1].Text = "Name";
            sheet.ColumnHeader.Cells[0, 2].Text = "Department";
            sheet.ColumnHeader.Cells[0, 3].Text = "Score";

            sheet.Cells[0, 0].Text = "1001";
            sheet.Cells[0, 1].Text = "Kim";
            sheet.Cells[0, 2].Text = "Process";
            sheet.Cells[0, 3].Value = 95;

            sheet.Cells[1, 0].Text = "1002";
            sheet.Cells[1, 1].Text = "Lee";
            sheet.Cells[1, 2].Text = "QA";
            sheet.Cells[1, 3].Value = 88;

            sheet.Cells[2, 0].Text = "1003";
            sheet.Cells[2, 1].Text = "Park";
            sheet.Cells[2, 2].Text = "R&D";
            sheet.Cells[2, 3].Value = 92;

            sheet.Cells[3, 0].Text = "1004";
            sheet.Cells[3, 1].Text = "Choi";
            sheet.Cells[3, 2].Text = "Planning";
            sheet.Cells[3, 3].Value = 85;

            sheet.Cells[4, 0].Text = "1005";
            sheet.Cells[4, 1].Text = "Jung";
            sheet.Cells[4, 2].Text = "Facility";
            sheet.Cells[4, 3].Value = 90;

            sheet.Columns[0].Width = 90;
            sheet.Columns[1].Width = 130;
            sheet.Columns[2].Width = 160;
            sheet.Columns[3].Width = 90;

            Controls.Add(fpSpread);
        }

        private void InitializeFallbackGrid()
        {
            var dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true
            };

            dataGridView.Columns.Add("ID", "ID");
            dataGridView.Columns.Add("Name", "Name");
            dataGridView.Columns.Add("Department", "Department");
            dataGridView.Columns.Add("Score", "Score");

            dataGridView.Rows.Add("1001", "Kim", "Process", 95);
            dataGridView.Rows.Add("1002", "Lee", "QA", 88);
            dataGridView.Rows.Add("1003", "Park", "R&D", 92);
            dataGridView.Rows.Add("1004", "Choi", "Planning", 85);
            dataGridView.Rows.Add("1005", "Jung", "Facility", 90);

            var infoLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Text = "FarPoint 라이선스를 찾지 못해 기본 Grid로 표시 중입니다."
            };

            Controls.Add(dataGridView);
            Controls.Add(infoLabel);
        }
    }
}
