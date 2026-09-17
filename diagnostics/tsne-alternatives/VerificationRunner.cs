using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

internal static class VerificationRunner
{
    private static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            if (args[0] == "--integration") return AlternativeTsneIntegrationVerification.Run();
            if (args[0] == "--performance") return TsnePerformanceVerification.Run(args.Skip(1).ToArray());
            return AlternativeTsneVerification.Run(args);
        }
        try
        {
            string executable = typeof(VerificationRunner).Assembly.Location;
            string artifacts = Path.Combine(Path.GetTempPath(), "tas-tsne-verification-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(artifacts);
            foreach (string mode in new[] { "--standalone", "--defaults", "--duplicates", "--large", "--integration" })
                RunProcess(executable, mode, artifacts);
            string isolated = Path.Combine(artifacts, "csharp-only");
            Directory.CreateDirectory(isolated);
            foreach (string file in new[] { "TsneVerification.exe", "TsneCSharp.dll", "SKhynix.TAS.Analysis.Tsne.dll" })
                File.Copy(Path.Combine(Path.GetDirectoryName(executable), file), Path.Combine(isolated, file));
            RunProcess(Path.Combine(isolated, "TsneVerification.exe"), "--csharp-only", artifacts);
            RunProcess(executable, "--performance \"" + Path.Combine(artifacts, "performance.json") + "\"", artifacts);
            Console.WriteLine("PASS: all verification groups. Results: " + artifacts);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void RunProcess(string executable, string arguments, string artifacts)
    {
        var start = new ProcessStartInfo(executable, arguments)
        {
            UseShellExecute = false, CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(executable),
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        using (var process = Process.Start(start))
        {
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(60000))
            {
                process.Kill();
                process.WaitForExit();
                throw new TimeoutException(arguments + " exceeded 60 seconds.");
            }
            string log = output.Result + error.Result;
            File.WriteAllText(Path.Combine(artifacts, arguments.Split(' ')[0].TrimStart('-') + ".log"), log);
            Console.Write(log);
            if (process.ExitCode != 0) throw new Exception(arguments + " failed: exit " + process.ExitCode);
        }
    }
}
