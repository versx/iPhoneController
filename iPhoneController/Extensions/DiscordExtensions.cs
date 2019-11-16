namespace iPhoneController.Extensions
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using iPhoneController.Diagnostics;

    public static class DiscordExtensions
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("DISCORD_EXT");

        public static async Task<DiscordMessage> SendDirectMessage(this DiscordClient client, DiscordUser user, DiscordEmbed embed)
        {
            if (embed == null)
                return null;

            return await client.SendDirectMessage(user, string.Empty, embed);
        }

        public static async Task<DiscordMessage> SendDirectMessage(this DiscordClient client, DiscordUser user, string message, DiscordEmbed embed)
        {
            try
            {
                var dm = await client.CreateDmAsync(user);
                if (dm != null)
                {
                    var msg = await dm.SendMessageAsync(message, false, embed);
                    return msg;
                }
            }
            catch (Exception)
            {
                //_logger.Error(ex);
                _logger.Error($"Failed to send DM to user {user.Username}.");
            }

            return null;
        }
    }
}