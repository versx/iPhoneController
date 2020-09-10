namespace iPhoneController
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    using iPhoneController.Commands;
    using iPhoneController.Configuration;
    using iPhoneController.Diagnostics;
    using iPhoneController.Extensions;

    public class Bot
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("BOT");

        private readonly DiscordClient _client;
        private readonly CommandsNextModule _commands;
        private readonly Config _config;

        public Bot(Config config)
        {
            _logger.Trace($"WhConfig [OwnerId={config.OwnerId}, GuildId={config.GuildId}, ChannelId={config.ChannelId}]");
            _config = config;

            AppDomain.CurrentDomain.UnhandledException += async (sender, e) =>
            {
                _logger.Debug("Unhandled exception caught.");
                _logger.Error((Exception)e.ExceptionObject);

                if (e.IsTerminating)
                {
                    if (_client != null)
                    {
                        var owner = await _client.GetUserAsync(_config.OwnerId);
                        if (owner == null)
                        {
                            _logger.Warn($"Failed to get owner from id {_config.OwnerId}.");
                            return;
                        }

                        await _client.SendDirectMessage(owner, Strings.CrashMessage, null);
                    }
                }
            };

            _client = new DiscordClient(new DiscordConfiguration
            {
                AutomaticGuildSync = true,
                AutoReconnect = true,
                EnableCompression = true,
                Token = _config.Token,
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true
            });
            _client.Ready += Client_Ready;
            _client.ClientErrored += Client_ClientErrored;
            _client.DebugLogger.LogMessageReceived += DebugLogger_LogMessageReceived;

            DependencyCollection dep;
            using (var d = new DependencyCollectionBuilder())
            {
                d.AddInstance(new Dependencies(_config));
                dep = d.Build();
            }

            _commands = _client.UseCommandsNext
            (
                new CommandsNextConfiguration
                {
                    StringPrefix = _config.CommandPrefix?.ToString(),
                    EnableDms = true,
                    EnableMentionPrefix = string.IsNullOrEmpty(_config.CommandPrefix),
                    EnableDefaultHelp = false,
                    CaseSensitive = false,
                    IgnoreExtraArguments = true,
                    Dependencies = dep
                }
            );
            _commands.CommandExecuted += Commands_CommandExecuted;
            _commands.CommandErrored += Commands_CommandErrored;
            _commands.RegisterCommands<PhoneControl>();
        }

        public void Start()
        {
            _logger.Trace("Start");
            _logger.Info("Connecting to Discord...");

            _client.ConnectAsync();
        }

        public static Dictionary<string, string> GetDevices()
        {
            var devices = new Dictionary<string, string>();
            var output = Utils.Shell.Execute("ios-deploy", "-c device_identification", out var exitCode);
            if (string.IsNullOrEmpty(output) || exitCode != 0)
            {
                // Failed
                return devices;
            }

            var split = output.Split('\n');
            foreach (var line in split)
            {
                if (!line.ToLower().Contains("found"))
                    continue;

                var name = line.GetBetween("Found ", " (");
                var uuid = line.GetBetween("'", "'");
                if (!devices.ContainsKey(name))
                {
                    devices.Add(name, uuid);
                }
            }
            return devices;
        }

        #region Discord Events

        private async Task Client_Ready(ReadyEventArgs e)
        {
            _logger.Info($"[DISCORD] Connected.");
            _logger.Info($"[DISCORD] Current Application:");
            _logger.Info($"[DISCORD] Name: {e.Client.CurrentApplication.Name}");
            _logger.Info($"[DISCORD] Description: {e.Client.CurrentApplication.Description}");
            _logger.Info($"[DISCORD] Owner: {e.Client.CurrentApplication.Owner.Username}#{e.Client.CurrentApplication.Owner.Discriminator}");
            _logger.Info($"[DISCORD] Current User:");
            _logger.Info($"[DISCORD] Id: {e.Client.CurrentUser.Id}");
            _logger.Info($"[DISCORD] Name: {e.Client.CurrentUser.Username}#{e.Client.CurrentUser.Discriminator}");
            _logger.Info($"[DISCORD] Email: {e.Client.CurrentUser.Email}");
            _logger.Info($"Machine Name: {Environment.MachineName}");

            await Task.CompletedTask;
        }

        private async Task Client_ClientErrored(ClientErrorEventArgs e)
        {
            _logger.Error(e.Exception);

            await Task.CompletedTask;
        }

        private async Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            // let's log the name of the command and user
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, Strings.BotName, $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);

            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done
            await Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandErrorEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, Strings.BotName, $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? e.Context.Message.Content}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

            // let's check if the error is a result of lack of required permissions
            if (e.Exception is DSharpPlus.CommandsNext.Exceptions.ChecksFailedException)
            {
                // The user lacks required permissions, 
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

                // let's wrap the response into an embed
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Access denied",
                    Description = $"{emoji} You do not have the permissions required to execute this command.",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.RespondAsync(string.Empty, embed: embed);
            }
            else if (e.Exception is ArgumentException)
            {
                // The user lacks required permissions, 
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":x:");

                var example = $"Command Example: ```{_config.CommandPrefix}{e.Command.Name} {string.Join(" ", e.Command.Arguments.Select(x => x.IsOptional ? $"[{x.Name}]" : x.Name))}```\r\n*Parameters in brackets are optional.*";

                // let's wrap the response into an embed
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{emoji} Invalid Argument(s)",
                    Description = $"{string.Join(Environment.NewLine, e.Command.Arguments.Select(x => $"Parameter **{x.Name}** expects type **{x.Type}.**"))}.\r\n\r\n{example}",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.RespondAsync(string.Empty, embed: embed);
            }
            else if (e.Exception is DSharpPlus.CommandsNext.Exceptions.CommandNotFoundException)
            {
                _logger.Warn($"User {e.Context.User.Username} tried executing command {e.Context.Message.Content} but command does not exist.");
            }
            else
            {
                _logger.Error($"User {e.Context.User.Username} tried executing command {e.Command?.Name} and unknown error occurred.\r\n: {e.Exception}");
            }
        }

        private void DebugLogger_LogMessageReceived(object sender, DebugLogMessageEventArgs e)
        {
            if (e.Application == "REST")
            {
                _logger.Error("[DISCORD] RATE LIMITED-----------------");
                return;
            }

            //Color
            ConsoleColor color;
            switch (e.Level)
            {
                case LogLevel.Error: color = ConsoleColor.DarkRed; break;
                case LogLevel.Warning: color = ConsoleColor.Yellow; break;
                case LogLevel.Info: color = ConsoleColor.White; break;
                case LogLevel.Critical: color = ConsoleColor.Red; break;
                case LogLevel.Debug: default: color = ConsoleColor.DarkGray; break;
            }

            //Source
            var sourceName = e.Application;

            //Text
            var text = e.Message;

            //Build message
            var builder = new System.Text.StringBuilder(text.Length + (sourceName?.Length ?? 0) + 5);
            if (sourceName != null)
            {
                builder.Append('[');
                builder.Append(sourceName);
                builder.Append("] ");
            }

            for (var i = 0; i < text.Length; i++)
            {
                //Strip control chars
                var c = text[i];
                if (!char.IsControl(c))
                    builder.Append(c);
            }

            if (text != null)
            {
                builder.Append(": ");
                builder.Append(text);
            }

            text = builder.ToString();
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        #endregion
    }
}