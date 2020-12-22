namespace iPhoneController.Utils
{
    using System;
    using System.Diagnostics;

    using iPhoneController.Diagnostics;

    public static class Shell
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("SHELL");

        public static string Execute(string cmd, string args, out int exitCode, bool includeErrorOutput = false)
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
            if (includeErrorOutput)
            {
                output += '\n' + p.StandardError.ReadToEndAsync().GetAwaiter().GetResult();
            }
            p.OutputDataReceived += (sender, e) => _logger.Debug($"[OUT] {e.Data}");
            p.ErrorDataReceived += (sender, e) => _logger.Error($"[ERR] {e.Data}");
            p.WaitForExit();
            exitCode = p.ExitCode;
            return output;
        }

        public static bool CommandExists(string command)
        {
            return CommandExists(command, "--version");
        }

        public static bool CommandExists(string command, string args)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}