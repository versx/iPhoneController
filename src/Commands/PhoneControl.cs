namespace iPhoneController.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;

    using iPhoneController.Diagnostics;
    using iPhoneController.Extensions;
    using iPhoneController.Utils;

    //TODO: Restart all devices

    public class PhoneControl
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("PHONE_CTRL");
        private readonly Dependencies _dep;

        #region Constructor

        public PhoneControl(Dependencies dep)
        {
            _dep = dep;
        }

        #endregion

        #region Information

        [
            Command("list"),
            Description("List all available devices.")
        ]
        public async Task ListDevicesAsync(CommandContext ctx,
            [Description("Machine name to list devices from, otherwise leave blank."), RemainingText]
            string machineName = "")
        {
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
                return;

            if (!string.IsNullOrEmpty(machineName) && string.Compare(machineName, Environment.MachineName, true) != 0)
                return;

            var devices = GetDevices();
            var keys = devices.Keys.ToList();
            /*
            var pages = new List<string>();
            var maxDevicePerPage = 20;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < keys.Count; i++)
            {
                if (i % maxDevicePerPage == 0 && i != 0)
                {
                    pages.Add(sb.ToString());
                    sb.Clear();
                }
                var name = keys[i];
                var device = devices[name];
                // TODO: Add disabled indicator
                sb.AppendLine($"**{name}**: {device.Uuid}");
            }
            if (sb.Length > 0)
            {
                pages.Add(sb.ToString());
            }
            */
            var pages = SplitPages();
            for (var i = 0; i < pages.Count; i++)
            {
                var msg = pages[i];
                var count = pages.Count > 1 ? $"Page: {i + 1}/{pages.Count}" : string.Empty;
                var eb = new DiscordEmbedBuilder
                {
                    Description = msg,
                    Title = $"{Environment.MachineName} Device List ({keys.Count:N0}) {count}",
                    Color = DiscordColor.Blurple
                };
                await ctx.RespondAsync(embed: eb);
            }
        }

        private List<string> SplitPages()
        {
            var devices = GetDevices();
            var keys = devices.Keys.ToList();
            var pages = new List<string>();
            var maxDevicePerPage = 20;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < keys.Count; i++)
            {
                if (i % maxDevicePerPage == 0 && i != 0)
                {
                    pages.Add(sb.ToString());
                    sb.Clear();
                }
                var name = keys[i];
                var uuid = devices[name];
                // TODO: Add disabled indicator
                sb.AppendLine($"**{name}**: {uuid}");
            }
            if (sb.Length > 0)
            {
                pages.Add(sb.ToString());
            }
            return pages;
        }

        [
            Command("screen"),
            Description("Grab a screenshot of a specific device.")
        ]
        public async Task ScreenshotAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneAB1SE`. Comma delimiter supported `iPhoneAB1SE,iPhoneCD2SE`"), RemainingText]
            string phoneNames)
        {
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
                return;

            //TODO: Check if idevicescreenshot is installed.
            var devices = GetDevices();
            var rebootDevices = phoneNames.Replace(", ", ",").Split(',');
            var devicesFailed = new Dictionary<string, string>();
            for (var i = 0; i < rebootDevices.Length; i++)
            {
                var name = rebootDevices[i];
                if (!devices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping screenshot.");
                    continue;
                }

                var uuid = devices[name];
                var fileName = $"{uuid}.jpg";
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                var output = Shell.Execute("idevicescreenshot", $"-u {uuid} {fileName}", out var exitCode);
                if (exitCode == 0)
                {
                    //var message = exitCode == 0 ? $"Restarting device {name} ({uuid})" : output;
                    await ctx.RespondWithFileAsync(fileName, $"Screenshot for device **{name}** ({uuid})");
                    continue;
                }

                devicesFailed.Add(name, output);
            }

            if (devicesFailed.Count > 0)
            {
                var eb = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = "Screenshots failed on the following devices",
                    Description = string.Join("\r\n", devicesFailed.Select(x => $"- **{x.Key}**: {x.Value}"))
                };
                await ctx.RespondAsync(embed: eb);
            }
        }

        [
            Command("iosver"),
            Description("")
        ]
        public async Task IosVersionAsync(CommandContext ctx,
            [Description("Machine name to list iOS device versions from, otherwise leave blank."), RemainingText]
            string machineName = "")
        {
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
                return;

            if (!string.IsNullOrEmpty(machineName) && string.Compare(machineName, Environment.MachineName, true) != 0)
                return;

            var dict = new Dictionary<string, string>();
            var devices = GetDevices();
            var keys = devices.Keys.ToList();
            keys.Sort();

            for (var i = 0; i < keys.Count; i++)
            {
                var name = keys[i];
                if (!devices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping iOS version.");
                    continue;
                }
                var uuid = devices[name];
                var args = $"-u {uuid} -k ProductVersion";
                var output = Shell.Execute("ideviceinfo", args, out var exitCode);
                if (exitCode != 0)
                {
                    _logger.Warn($"Failed to get device info from {name} ({uuid}).");
                    continue;
                }
                if (dict.ContainsKey(name))
                {
                    _logger.Warn($"Duplicate device {name} ({uuid}).");
                    continue;
                }
                dict.Add(name, output);
            }

            if (dict.Count == 0)
            {
                await ctx.RespondAsync($"Failed to get device info from any devices.");
                return;
            }

            var eb = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Blurple,
                Title = $"**{Environment.MachineName}** Device iOS Versions ({devices.Count:N0})",
                Description = string.Join("\r\n", dict.Select(x => $"- **{x.Key}**: {x.Value.Replace("\n", null)}"))
            };
            await ctx.RespondAsync(embed: eb);
        }

        #endregion

        #region Management

        [
            Command("reboot"),
            Description("Reboot specific device(s).")
        ]
        public async Task RebootAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneHV1SE`. Comma delimiter supported `iPhoneHV1SE,iPhoneHV2SE`"), RemainingText]
            string phoneNames)
        {
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
                return;

            //TODO: Check if idevicediagnostics is installed.

            var devices = GetDevices();
            var rebootDevices = phoneNames.Replace(", ", ",").Split(',');
            for (var i = 0; i < rebootDevices.Length; i++)
            {
                var name = rebootDevices[i];
                if (!devices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping reboot.");
                    continue;
                }

                var uuid = devices[name];
                var output = Shell.Execute("idevicediagnostics", $"-u {uuid} restart", out var exitCode);
                var message = exitCode == 0 ? $"Restarting device {name} ({uuid})" : output;
                await ctx.RespondAsync(message);
            }
        }

        [
            Command("shutdown"),
            Description("Shutdown specific device(s).")
        ]
        public async Task ShutdownAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneHV1SE`. Comma delimiter supported `iPhoneHV1SE,iPhoneHV2SE`"), RemainingText]
            string phoneNames)
        {
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
                return;

            //TODO: Check if idevicediagnostics is installed.
            var devices = GetDevices();
            var shutdownDevices = phoneNames.Replace(", ", ",").Split(',');
            for (var i = 0; i < shutdownDevices.Length; i++)
            {
                var name = shutdownDevices[i];
                if (!devices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping shutdown.");
                    continue;
                }

                var uuid = devices[name];
                var output = Shell.Execute("idevicediagnostics", $"-u {uuid} shutdown", out var exitCode);
                var message = exitCode == 0 ? $"Shutting down device {name} ({uuid})" : output;
                await ctx.RespondAsync(message);
            }
        }

        [
            Command("kill"),
            Description("Kill a specific running process.")
        ]
        public async Task KillAsync(CommandContext ctx,
            [Description("Process name to attempt to kill.")]
            string processName,
            [Description("Machine name to kill the process on, otherwise leave blank."), RemainingText]
            string machineName = "")
        {
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
                return;

            // Machine name is not null and machine name does not match current machine name, skip.
            if (!string.IsNullOrEmpty(machineName) && string.Compare(machineName, Environment.MachineName, true) != 0)
                return;

            // REVIEW: Possibly use managed Process class instead of relying on command line.
            var output = Shell.Execute("killall", processName, out var exitCode);
            await ctx.RespondAsync(exitCode == 0 ? $"{processName} killed." : $"{Environment.MachineName} Result: {output}");
        }

        #endregion

        #region App Management

        [
            Command("deploy"),
            Description("Remove Pokemon Go application from device(s).")
        ]
        public async Task DeployPoGoAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneAB1SE`. Comma delimiter supported `iPhoneAB1SE,iPhoneCD2SE`"), RemainingText]
            string phoneNames)
        {
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
                return;

            var devices = GetDevices();
            var deployAppDevices = phoneNames.Replace(", ", "").Split(",");
            var list = new List<string>(phoneNames.Replace(", ", "").Split(","));
            Parallel.ForEach(list, async x =>
            {
                for (var i = 0; i < deployAppDevices.Length; i++)
                {
                    var name = deployAppDevices[i];
                    if (!devices.ContainsKey(name))
                    {
                        _logger.Warn($"{name} does not exist in device list, skipping deploy pogo.");
                        continue;
                    }

                    var uuid = devices[name];
                    var args = $"--id {uuid} --bundle {_dep.Config.PokemonGoAppPath}";
                    var output = Shell.Execute("ios-deploy", args, out var exitCode);
                    await ctx.RespondAsync($"Deployed Pokemon Go to {name} ({uuid})\r\nOutput: {output}");
                }
            });
        }

        [
            Command("rm-pogo"),
            Description("Remove Pokemon Go application from device(s).")
        ]
        public async Task RemovePoGoAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneAB1SE`. Comma delimiter supported `iPhoneAB1SE,iPhoneCD2SE`"), RemainingText]
            string phoneNames)
        {
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
                return;

            var devices = GetDevices();
            var removeAppDevices = phoneNames.Replace(", ", ",").Split(',');
            for (var i = 0; i < removeAppDevices.Length; i++)
            {
                var name = removeAppDevices[i];
                if (!devices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping remove pogo.");
                    continue;
                }

                var uuid = devices[name];
                var args = $"--id {uuid} --uninstall_only --bundle_id {Strings.PokemonGoBundleIdentifier}";
                var output = Shell.Execute("ios-deploy", args, out var exitCode);
                await ctx.RespondAsync($"Removed Pokemon Go from {name}\r\nOutput: {output}");
            }
        }

        #endregion

        #region Private Methods

        private bool HasRequiredRoles(DiscordMember member)
        {
            var requiredRoles = _dep.Config.RequiredRoles;
            if (requiredRoles.Count == 0)
                return true;

            var memberRoles = member.Roles?.Select(x => x.Id)?.ToList();
            if (memberRoles == null)
                return false;

            for (var i = 0; i < requiredRoles.Count; i++)
            {
                var requiredRole = requiredRoles[i];
                if (memberRoles.Contains(requiredRole))
                    return true;
            }

            return false;
        }

        private bool IsValidChannel(ulong channelId)
        {
            //If no channel id is specified allow the command to execute in all channels, otherwise only the channel specified.
            return _dep.Config.ChannelId == 0 || _dep.Config.ChannelId == channelId;
        }

        private Dictionary<string, string> GetDevices()
        {
            var devices = new Dictionary<string, string>();
            var output = Shell.Execute("ios-deploy", "-c device_identification", out var exitCode);
            if (string.IsNullOrEmpty(output))// || exitCode != 0)
            {
                // Failed
                return devices;
            }

            var split = output.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in split)
            {
                if (!line.ToLower().Contains("found"))
                    continue;

                var uuid = line.GetBetween("Found ", " (");
                var name = line.GetBetween("'", "'");
                if (!devices.ContainsKey(name))
                {
                    devices.Add(name, uuid);
                }
            }
            return devices;
        }

        #endregion
    }
}