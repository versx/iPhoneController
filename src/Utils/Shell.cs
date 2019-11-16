namespace iPhoneController.Utils
{
    using System;
    using System.Diagnostics;

    public static class Shell
    {
        public static string Start(string cmd, string args, out int exitCode)
        {
            var psi = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = args,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            var p = Process.Start(psi);
            var output = p.StandardOutput.ReadToEndAsync().GetAwaiter().GetResult();
            p.OutputDataReceived += (sender, e) =>
            {
                Console.WriteLine("[OUT] " + e.Data);
            };
            p.ErrorDataReceived += (sender, e) =>
            {
                Console.WriteLine("[ERR] " + e.Data);
            };
            p.WaitForExit();
            exitCode = p.ExitCode;
            Console.WriteLine("Exit code: " + exitCode);
            return output;
        }
    }
}