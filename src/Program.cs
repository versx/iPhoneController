namespace iPhoneController
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    using iPhoneController.Data;

    class Program
    {
        static void Main(string[] args)
        {
            //#if Linux
            //        Console.WriteLine("Built on Linux!"); 
            //#elif OSX
            //        Console.WriteLine("Built on macOS!"); 
            //#elif Windows
            //        Console.WriteLine("Built in Windows!"); 
            //#endif

            var logger = Diagnostics.EventLogger.GetLogger();
            var config = Configuration.Config.Load(Strings.ConfigFileName);
            if (config == null)
            {
                logger.Error($"Failed to load config {Strings.ConfigFileName}.");
                return;
            }

            var bot = new Bot(config);
            bot.Start();

            System.Diagnostics.Process.GetCurrentProcess().WaitForExit();
        }
    }
}