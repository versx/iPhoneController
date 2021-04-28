namespace iPhoneController.Configuration
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class DiscordConfig
    {
        [JsonProperty("ownerId")]
        public ulong OwnerId { get; set; }

        [JsonProperty("channelIds")]
        public List<ulong> ChannelIds { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("commandPrefix")]
        public string CommandPrefix { get; set; }

        [JsonProperty("requiredRoles")]
        public List<ulong> RequiredRoles { get; set; }

        public DiscordConfig()
        {
            ChannelIds = new List<ulong>();
            RequiredRoles = new List<ulong>();
        }
    }
}