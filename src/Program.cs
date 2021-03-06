namespace iPhoneController
{
    using System;
    using System.Diagnostics;

    using iPhoneController.Configuration;
    using iPhoneController.Diagnostics;
    using iPhoneController.Utils;

    class Program
    {
        static readonly IEventLogger _logger = EventLogger.GetLogger("APP");

        static void Main(string[] args)
        {
            // https://blog.magnusmontin.net/2018/11/05/platform-conditional-compilation-in-net-core/
            //#if Linux
            //        Console.WriteLine("Built on Linux!"); 
            //#elif OSX
            //        Console.WriteLine("Built on macOS!"); 
            //#elif Windows
            //        Console.WriteLine("Built in Windows!"); 
            //#endif

            var config = Config.Load(Strings.ConfigFileName);
            if (config == null)
            {
                _logger.Error($"Failed to load config {Strings.ConfigFileName}.");
                return;
            }

            var allFound = ValidateCommandsExist();
            if (!allFound)
            {
                _logger.Error($"Requirements not found on machine, exiting...");
                return;
            }


            var bot = new Bot(config);
            bot.Start();

            Process.GetCurrentProcess().WaitForExit();
        }

        static bool ValidateCommandsExist()
        {
            var iosDeploy = Shell.CommandExists("ios-deploy");
            if (!iosDeploy)
            {
                _logger.Warn("ios-deploy not found on machine.");
            }
            var ideviceDiagnostics = Shell.CommandExists("idevicediagnostics");
            if (!ideviceDiagnostics)
            {
                _logger.Warn("idevicediagnostics not found on machine.");
            }
            var ideviceScreenshot = Shell.CommandExists("idevicescreenshot");
            if (!ideviceScreenshot)
            {
                _logger.Warn("idevicescreenshot not found on machine.");
            }
            var ideviceInfo = Shell.CommandExists("ideviceinfo");
            if (!ideviceInfo)
            {
                _logger.Warn("ideviceinfo not found on machine.");
            }
            var ideviceSyslog = Shell.CommandExists("idevicesyslog");
            if (!ideviceSyslog)
            {
                _logger.Warn("idevicesyslog not found on machine.");
            }
            var megatools = Shell.CommandExists("megadl");
            if (!megatools)
            {
                _logger.Warn("megadl not found on machine");
            }
            var cfgutil = Shell.CommandExists("cfgutil");
            if (!cfgutil)
            {
                _logger.Warn($"cfgutil (AC2 Automatation Tools) not found on machine.");
            }
            return iosDeploy && ideviceDiagnostics && ideviceScreenshot && ideviceInfo && ideviceSyslog && megatools;
        }
    }
}