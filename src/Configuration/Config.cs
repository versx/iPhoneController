namespace iPhoneController.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Newtonsoft.Json;

    using iPhoneController.Diagnostics;

    public class Config
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("CONFIG");

        #region Properties

        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("port")]
        public ushort Port { get; set; }

        [JsonProperty("servers")]
        public Dictionary<ulong, DiscordConfig> Servers { get; set; }

        [JsonProperty("developer")]
        public string Developer { get; set; }

        [JsonProperty("provisioningProfile")]
        public string ProvisioningProfile { get; set; }

        [JsonProperty("useIosDeploy")]
        public bool UseIosDeploy { get; set; }

        #endregion

        #region Constructor

        public Config()
        {
            Host = "*";
            Port = 6542;
            Servers = new Dictionary<ulong, DiscordConfig>();
        }

        #endregion

        public void Save(string filePath)
        {
            var data = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, data);
        }

        public static Config Load(string filePath)
        {
            var config = LoadInit<Config>(filePath, typeof(Config));
            Models.Device.UseIosDeploy = config.UseIosDeploy;
            return config;
        }

        private static T LoadInit<T>(string filePath, Type type)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{filePath} file not found.", filePath);
            }

            var data = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(data))
            {
                _logger.Error($"{filePath} database is empty.");
                return default;
            }

            return (T)JsonConvert.DeserializeObject(data, type);
        }

    }
}