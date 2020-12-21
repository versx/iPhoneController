namespace iPhoneController
{
    using System.IO;

    public static class Strings
    {
        public const string BotName = "iPhoneController";

        public const string ConfigFileName = "config.json";

        public const string LogsFolder = "logs";

        public static readonly string ReleasesFolder = Path.Combine
        (
            Directory.GetCurrentDirectory(),
            "releases"
        );

        public static readonly string ProfilesFolder = Path.Combine
        (
            Directory.GetCurrentDirectory(),
            "profiles"
        );

        public static readonly string ConfigsFolder = Path.Combine
        (
            Directory.GetCurrentDirectory(),
            "configs"
        );

        public const string CrashMessage = "iPhoneController JUST CRASHED!";

        public const string PokemonGoBundleIdentifier = "com.nianticlabs.pokemongo";

        public const string XCodeUITestsBundleIdentifier = "com.apple.test.RealDeviceMap-UIControlUITests-Runner";

        public const string DefaultResponseMessage = "iPhoneController running...";

        public const string PlistBuddyPath = "/usr/libexec/PlistBuddy";

        public const string CodesignPath = "/usr/bin/codesign";

        public const string All = "All";
    }
}