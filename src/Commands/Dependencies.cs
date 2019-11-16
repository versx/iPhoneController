namespace iPhoneController.Commands
{
    using iPhoneController.Configuration;

    public class Dependencies
    {
        public Config Config { get; }

        public Dependencies(Config config)
        {
            Config = config;
        }
    }
}