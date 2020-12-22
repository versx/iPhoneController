namespace iPhoneController.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;

    using iPhoneController.Diagnostics;
    using iPhoneController.Extensions;
    using iPhoneController.Models;
    using iPhoneController.Utils;

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
            if (!ctx.Member.HasRequiredRoles(_dep.Config.RequiredRoles))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!ctx.Channel.Id.IsValidChannel(_dep.Config.ChannelIds))
                return;

            if (!string.IsNullOrEmpty(machineName) && string.Compare(machineName, Environment.MachineName, true) != 0)
                return;

            var devices = Device.GetAll();
            var keys = devices.Keys.ToList();
            var pages = SplitDevicePages();
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

        [
            Command("screen"),
            Description("Grab a screenshot of a specific device.")
        ]
        public async Task ScreenshotAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneAB1SE`. Comma delimiter supported `iPhoneAB1SE,iPhoneCD2SE`"), RemainingText]
            string phoneNames = Strings.All)
        {
            if (!ctx.Member.HasRequiredRoles(_dep.Config.RequiredRoles))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!ctx.Channel.Id.IsValidChannel(_dep.Config.ChannelIds))
                return;

            if (!Shell.CommandExists("idevicescreenshot"))
            {
                await ctx.RespondAsync($"Cannot take screenshot, `idevicescreenshot` not installed.");
                return;
            }

            var devices = Device.GetAll();
            var rebootDevices = phoneNames.RemoveSpaces();
            if (string.Compare(phoneNames, Strings.All, true) == 0)
                rebootDevices = devices.Keys.ToArray();

            var devicesFailed = new Dictionary<string, string>();
            foreach (var name in rebootDevices)
            {
                if (!devices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping screenshot.");
                    continue;
                }

                var device = devices[name];
                var fileName = $"{device.Uuid}.jpg";
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                var output = Shell.Execute("idevicescreenshot", $"-u {device.Uuid} {fileName}", out var exitCode);
                if (exitCode == 0)
                {
                    //var message = exitCode == 0 ? $"Restarting device {name} ({uuid})" : output;
                    await ctx.RespondWithFileAsync(fileName, $"Screenshot for device **{device.Name}** ({device.Uuid})");
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
            if (!ctx.Member.HasRequiredRoles(_dep.Config.RequiredRoles))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!ctx.Channel.Id.IsValidChannel(_dep.Config.ChannelIds))
                return;

            if (!string.IsNullOrEmpty(machineName) && string.Compare(machineName, Environment.MachineName, true) != 0)
                return;

            var dict = new Dictionary<string, string>();
            var devices = Device.GetAll();
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
                var device = devices[name];
                var args = $"-u {device.Uuid} -k ProductVersion";
                var output = Shell.Execute("ideviceinfo", args, out var exitCode);
                if (exitCode != 0)
                {
                    _logger.Warn($"Failed to get device info from {device.Name} ({device.Uuid}).");
                    continue;
                }
                if (dict.ContainsKey(name))
                {
                    _logger.Warn($"Duplicate device {name} ({device.Uuid}).");
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
            Command("reopen"),
            Description("Reopen games for specific device(s).")
        ]
        public async Task ReopenAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneHV1SE`. Comma delimiter supported `iPhoneHV1SE,iPhoneHV2SE`"), RemainingText]
            string phoneNames = Strings.All)
        {
            if (!ctx.Member.HasRequiredRoles(_dep.Config.RequiredRoles))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!ctx.Channel.Id.IsValidChannel(_dep.Config.ChannelIds))
                return;

            var devices = Device.GetAll();
            var reopenDevices = phoneNames.RemoveSpaces();
            if (string.Compare(phoneNames, Strings.All, true) == 0)
                reopenDevices = devices.Keys.ToArray();
            foreach (var name in reopenDevices)
            {
                if (!devices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping reboot.");
                    continue;
                }

                var device = devices[name];
                _logger.Debug($"Using IP address '{device.IPAddress}' to send restart game request to {device.Name}");
                // Check if we have IP address for device
                if (string.IsNullOrEmpty(device.IPAddress))
                {
                    await ctx.RespondAsync($"Failed to get IP address for device {device.Name} ({device.Uuid}) to restart game");
                    continue;
                }
                // Send HTTP GET request to device IP address
                var data = NetUtils.Get($"http://{device.IPAddress}:8080/restart");
                await ctx.RespondAsync
                (
                    string.IsNullOrEmpty(data)
                    ? $"Reopening game for device {device.Name} ({device.Uuid})"
                    : $"Error response: {data}"
                );
            }
        }

        [
            Command("reboot"),
            Description("Reboot specific device(s).")
        ]
        public async Task RebootAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneHV1SE`. Comma delimiter supported `iPhoneHV1SE,iPhoneHV2SE`"), RemainingText]
            string phoneNames = Strings.All)
        {
            if (!ctx.Member.HasRequiredRoles(_dep.Config.RequiredRoles))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!ctx.Channel.Id.IsValidChannel(_dep.Config.ChannelIds))
                return;

            if (!Shell.CommandExists("idevicediagnostics"))
            {
                await ctx.RespondAsync($"Cannot reboot device, `idevicediagnostics` not installed.");
                return;
            }

            var devices = Device.GetAll();
            var rebootDevices = phoneNames.RemoveSpaces();
            if (string.Compare(phoneNames, Strings.All, true) == 0)
                rebootDevices = devices.Keys.ToArray();
            foreach (var name in rebootDevices)
            {
                if (!devices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping reboot.");
                    continue;
                }

                var device = devices[name];
                var output = Shell.Execute("idevicediagnostics", $"-u {device.Uuid} restart", out var exitCode);
                var message = exitCode == 0 ? $"Restarting device {device.Name} ({device.Uuid})" : output;
                await ctx.RespondAsync(message);
            }
        }

        [
            Command("shutdown"),
            Description("Shutdown specific device(s).")
        ]
        public async Task ShutdownAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneHV1SE`. Comma delimiter supported `iPhoneHV1SE,iPhoneHV2SE`"), RemainingText]
            string phoneNames = Strings.All)
        {
            if (!ctx.Member.HasRequiredRoles(_dep.Config.RequiredRoles))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!ctx.Channel.Id.IsValidChannel(_dep.Config.ChannelIds))
                return;

            //TODO: Check if idevicediagnostics is installed.
            var devices = Device.GetAll();
            var shutdownDevices = phoneNames.RemoveSpaces();
            if (string.Compare(phoneNames, Strings.All, true) == 0)
                shutdownDevices = devices.Keys.ToArray();
            foreach (var name in shutdownDevices)
            {
                if (!devices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping shutdown.");
                    continue;
                }

                var device = devices[name];
                var output = Shell.Execute("idevicediagnostics", $"-u {device.Uuid} shutdown", out var exitCode);
                var message = exitCode == 0 ? $"Shutting down device {device.Name} ({device.Uuid})" : output;
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
            if (!ctx.Member.HasRequiredRoles(_dep.Config.RequiredRoles))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!ctx.Channel.Id.IsValidChannel(_dep.Config.ChannelIds))
                return;

            // Machine name is not null and machine name does not match current machine name, skip.
            if (!string.IsNullOrEmpty(machineName) && string.Compare(machineName, Environment.MachineName, true) != 0)
                return;

            // REVIEW: Possibly use managed Process class instead of relying on command line.
            var output = Shell.Execute("killall", processName, out var exitCode);
            await ctx.RespondAsync(exitCode == 0 ? $"{processName} killed." : $"{Environment.MachineName} Result: {output}");
        }

        #endregion

        #region Private Methods

        private List<string> SplitDevicePages()
        {
            var devices = Device.GetAll();
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

        #endregion
    }
}