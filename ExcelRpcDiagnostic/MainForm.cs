using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExcelRpcDiagnostic
{
    internal sealed class MainForm : Form
    {
        private readonly Button runButton;
        private readonly Button saveButton;
        private readonly CheckBox workbookCheck;
        private readonly TextBox logBox;
        private readonly Label statusLabel;
        private readonly StringBuilder report = new StringBuilder();

        public MainForm()
        {
            Text = "Excel COM/RPC PC 진단";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(760, 520);
            ClientSize = new Size(900, 650);

            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(12, 8, 12, 0),
                Text = "PC별 Excel RPC 오류 진단\r\nExcel을 모두 닫은 뒤 실행하면 가장 정확합니다. 기존 Excel 프로세스는 종료하지 않습니다."
            };

            var commandPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(8, 6, 8, 4),
                FlowDirection = FlowDirection.LeftToRight
            };
            runButton = new Button { Text = "전체 진단 실행", Width = 130, Height = 28 };
            saveButton = new Button { Text = "결과 저장", Width = 100, Height = 28, Enabled = false };
            workbookCheck = new CheckBox
            {
                Text = "통합문서 생성/저장 테스트",
                Checked = true,
                AutoSize = true,
                Padding = new Padding(8, 6, 0, 0)
            };
            commandPanel.Controls.Add(runButton);
            commandPanel.Controls.Add(saveButton);
            commandPanel.Controls.Add(workbookCheck);

            statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Padding = new Padding(10, 5, 0, 0),
                Text = "대기 중"
            };
            logBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9F)
            };

            Controls.Add(logBox);
            Controls.Add(statusLabel);
            Controls.Add(commandPanel);
            Controls.Add(header);
            runButton.Click += RunButton_Click;
            saveButton.Click += SaveButton_Click;
        }

        private async void RunButton_Click(object sender, EventArgs e)
        {
            runButton.Enabled = false;
            saveButton.Enabled = false;
            report.Clear();
            logBox.Clear();
            statusLabel.Text = "진단 실행 중...";
            bool testWorkbook = workbookCheck.Checked;

            try
            {
                await RunStaAsync(() => RunDiagnostics(testWorkbook));
                statusLabel.Text = "진단 완료 - 정상 PC 결과와 로그를 비교하십시오.";
            }
            catch (Exception ex)
            {
                Append("FATAL", "진단 프로그램 오류", FormatException(ex));
                statusLabel.Text = "진단 중 오류 발생";
            }
            finally
            {
                runButton.Enabled = true;
                saveButton.Enabled = report.Length > 0;
            }
        }

        private static Task RunStaAsync(Action action)
        {
            var completion = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                    completion.SetResult(null);
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            });
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return completion.Task;
        }

        private void RunDiagnostics(bool testWorkbook)
        {
            Append("INFO", "진단 시작", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));
            Append("INFO", "PC", Environment.MachineName);
            Append("INFO", "사용자", Environment.UserDomainName + "\\" + Environment.UserName);
            Append("INFO", "OS", Environment.OSVersion + ", " + GetOsCaption());
            Append("INFO", "프로세스", (Environment.Is64BitProcess ? "64-bit" : "32-bit") + ", CLR " + Environment.Version);
            Append("INFO", "OS 비트수", Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");
            Append("INFO", "권한", IsAdministrator() ? "관리자" : "일반 사용자");
            Append("INFO", "실행 중 EXCEL", GetExcelProcessSummary());

            CheckService("RpcSs", "Remote Procedure Call (RPC)");
            CheckService("DcomLaunch", "DCOM Server Process Launcher");
            CheckService("RpcEptMapper", "RPC Endpoint Mapper");
            CheckExcelRegistration();
            TestExcelCom(testWorkbook);
            Append("INFO", "진단 종료", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));
        }

        private void CheckService(string serviceName, string displayName)
        {
            try
            {
                using (var service = new ServiceController(serviceName))
                    Append(service.Status == ServiceControllerStatus.Running ? "PASS" : "FAIL", "서비스 " + displayName, service.Status.ToString());
            }
            catch (Exception ex)
            {
                Append("FAIL", "서비스 " + displayName, FormatException(ex));
            }
        }

        private void CheckExcelRegistration()
        {
            Type excelType = Type.GetTypeFromProgID("Excel.Application", false);
            Append(excelType == null ? "FAIL" : "PASS", "Excel.Application ProgID", excelType == null ? "등록되지 않음" : excelType.GUID.ToString("B"));

            string[] registryPaths =
            {
                @"SOFTWARE\Microsoft\Office\ClickToRun\Configuration",
                @"SOFTWARE\Microsoft\Office\16.0\Excel\InstallRoot",
                @"SOFTWARE\WOW6432Node\Microsoft\Office\16.0\Excel\InstallRoot"
            };
            foreach (string path in registryPaths)
            {
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key == null) continue;
                        object version = key.GetValue("VersionToReport") ?? key.GetValue("Path") ?? key.GetValue("ClientVersionToReport");
                        Append("INFO", "Office 등록 " + path, version == null ? "키 존재" : version.ToString());
                    }
                }
                catch (Exception ex) { Append("WARN", "Office 레지스트리 읽기", FormatException(ex)); }
            }
        }

        private void TestExcelCom(bool testWorkbook)
        {
            object excel = null;
            object workbooks = null;
            object workbook = null;
            object worksheets = null;
            object worksheet = null;
            object cell = null;
            string tempFile = Path.Combine(Path.GetTempPath(), "ExcelRpcDiagnostic_" + Guid.NewGuid().ToString("N") + ".xlsx");

            try
            {
                Type excelType = Type.GetTypeFromProgID("Excel.Application", true);
                var timer = Stopwatch.StartNew();
                excel = Activator.CreateInstance(excelType);
                timer.Stop();
                Append("PASS", "Excel COM 인스턴스 생성", timer.ElapsedMilliseconds + " ms");

                dynamic app = excel;
                Append("INFO", "Excel 버전/경로", Convert.ToString(app.Version) + " / " + Convert.ToString(app.Path));
                app.Visible = false;
                app.DisplayAlerts = false;

                if (!testWorkbook) return;
                workbooks = app.Workbooks;
                workbook = ((dynamic)workbooks).Add();
                worksheets = ((dynamic)workbook).Worksheets;
                worksheet = ((dynamic)worksheets)[1];
                cell = ((dynamic)worksheet).Cells[1, 1];
                ((dynamic)cell).Value2 = "RPC_TEST_" + Environment.MachineName;
                string readValue = Convert.ToString(((dynamic)cell).Value2);
                Append(readValue.StartsWith("RPC_TEST_") ? "PASS" : "FAIL", "셀 쓰기/읽기", readValue);

                ((dynamic)workbook).SaveAs(tempFile, 51);
                Append(File.Exists(tempFile) ? "PASS" : "FAIL", "통합문서 저장", tempFile);
                ((dynamic)workbook).Close(false);
                ReleaseCom(cell); cell = null;
                ReleaseCom(worksheet); worksheet = null;
                ReleaseCom(worksheets); worksheets = null;
                ReleaseCom(workbook); workbook = null;
                ReleaseCom(workbooks); workbooks = null;
                app.Quit();
                Append("PASS", "Excel 종료 호출", "정상");
            }
            catch (Exception ex)
            {
                Append("FAIL", "Excel COM/RPC 테스트", FormatException(ex));
            }
            finally
            {
                TryCloseWorkbook(workbook);
                TryQuitExcel(excel);
                ReleaseCom(cell);
                ReleaseCom(worksheet);
                ReleaseCom(worksheets);
                ReleaseCom(workbook);
                ReleaseCom(workbooks);
                ReleaseCom(excel);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                try { if (File.Exists(tempFile)) File.Delete(tempFile); }
                catch (Exception ex) { Append("WARN", "임시 파일 삭제", FormatException(ex)); }
            }
        }

        private static void TryCloseWorkbook(object workbook)
        {
            if (workbook == null) return;
            try { ((dynamic)workbook).Close(false); } catch { }
        }

        private static void TryQuitExcel(object excel)
        {
            if (excel == null) return;
            try { ((dynamic)excel).Quit(); } catch { }
        }

        private static void ReleaseCom(object value)
        {
            if (value == null || !Marshal.IsComObject(value)) return;
            try { Marshal.FinalReleaseComObject(value); } catch { }
        }

        private static string FormatException(Exception ex)
        {
            var target = ex as TargetInvocationException;
            if (target != null && target.InnerException != null) ex = target.InnerException;
            var com = ex as COMException;
            string hresult = "0x" + ex.HResult.ToString("X8");
            if (com != null) hresult += " (COM)";
            return ex.GetType().Name + ", HRESULT=" + hresult + ", " + ex.Message;
        }

        private static string GetExcelProcessSummary()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("EXCEL");
                if (processes.Length == 0) return "없음";
                var result = new StringBuilder();
                foreach (Process process in processes)
                {
                    if (result.Length > 0) result.Append(", ");
                    result.Append("PID ").Append(process.Id).Append(" (").Append(process.SessionId).Append(" session)");
                    process.Dispose();
                }
                return result.ToString();
            }
            catch (Exception ex) { return "조회 실패: " + ex.Message; }
        }

        private static string GetOsCaption()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Caption, Version, BuildNumber FROM Win32_OperatingSystem"))
                    foreach (ManagementObject item in searcher.Get())
                        return item["Caption"] + " " + item["Version"] + " (Build " + item["BuildNumber"] + ")";
            }
            catch { }
            return "상세 정보 조회 실패";
        }

        private static bool IsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        private void Append(string level, string test, string detail)
        {
            string line = string.Format("{0:HH:mm:ss.fff} [{1,-5}] {2}: {3}", DateTime.Now, level, test, detail);
            lock (report) report.AppendLine(line);
            if (!IsHandleCreated || IsDisposed) return;
            BeginInvoke((Action)(() =>
            {
                logBox.AppendText(line + Environment.NewLine);
                logBox.SelectionStart = logBox.TextLength;
                logBox.ScrollToCaret();
            }));
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog
            {
                Filter = "Text file (*.txt)|*.txt",
                FileName = "ExcelRpcDiagnostic_" + Environment.MachineName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                File.WriteAllText(dialog.FileName, report.ToString(), new UTF8Encoding(true));
                statusLabel.Text = "결과 저장: " + dialog.FileName;
            }
        }
    }
}
