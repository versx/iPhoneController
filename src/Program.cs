namespace iPhoneController
{
    using System;
    using System.Diagnostics;

    using iPhoneController.Configuration;
    using iPhoneController.Diagnostics;

    class Program
    {
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

            var logger = EventLogger.GetLogger();
            var config = Config.Load(Strings.ConfigFileName);
            if (config == null)
            {
                logger.Error($"Failed to load config {Strings.ConfigFileName}.");
                return;
            }

            var bot = new Bot(config);
            bot.Start();

            Process.GetCurrentProcess().WaitForExit();
        }
    }
}