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

    using iPhoneController.Deployment;
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
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
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
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
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
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
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
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
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
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
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
            Command("resign"),
            Description("Download IPA, resign, and deploy to device(s)."),
        ]
        public async Task ResignPoGoAsync(CommandContext ctx,
            [Description("Mega download link")] string megaLink,
            [Description("Version")] string version,
            [Description("iPhone names i.e. `iPhoneAB1SE`. Comma delimiter supported `iPhoneAB1SE,iPhoneCD2SE`"), RemainingText]
            string phoneNames = Strings.All)
        {
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
                return;

            var deployer = new IpaDeployer(_dep.Config.Developer, _dep.Config.ProvisioningProfile)
            {
                ResignApp = true,
            };
            await ctx.RespondAsync("Starting resign...");
            if (!deployer.Resign(megaLink, version))
            {
                await ctx.RespondAsync($"Failed to resign IPA");
                return;
            }
            await ctx.RespondAsync($"Resign complete, starting deployment to {phoneNames}...");

            var result = deployer.Deploy(deployer.SignedReleaseFileName, phoneNames);
            var successful = result.Item1.Count > 0 ? $"Successfully deployed app to:\n{string.Join(", ", result.Item1)}" : null;
            var failed = result.Item2.Count > 0 ? $"Failed to deploy app to:\n{string.Join(", ", result.Item2)}" : null;
            // TODO: Check for content length over 2048 and split messages if so
            await ctx.RespondAsync($"{successful}\n{failed}");
        }

        [
            Command("deploy"),
            Description("Deploy Pokemon Go application to device(s).")
        ]
        public async Task DeployPoGoAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneAB1SE`. Comma delimiter supported `iPhoneAB1SE,iPhoneCD2SE`"), RemainingText]
            string phoneNames = Strings.All)
        {
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
                return;

            var devices = Device.GetAll();
            var deployAppDevices = new List<string>(phoneNames.RemoveSpaces());
            if (string.Compare(phoneNames, Strings.All, true) == 0)
                deployAppDevices = devices.Keys.ToList();
            var appPath = IpaDeployer.GetLatestAppPath();
            if (string.IsNullOrEmpty(appPath))
            {
                await ctx.RespondAsync($"No signed app found, make sure to run 'resign' command first.");
                return;
            }

            var deployer = new IpaDeployer(_dep.Config.Developer, _dep.Config.ProvisioningProfile);
            _logger.Debug($"Using app {appPath} for deployment.");
            //deployer.Deploy(appPath);
            Parallel.ForEach(deployAppDevices, async deviceName =>
            {
                if (!devices.ContainsKey(deviceName))
                {
                    _logger.Warn($"{deviceName} does not exist in device list, skipping deploy pogo.");
                }
                else
                {
                    var device = devices[deviceName];
                    var args = $"--id {device.Uuid} --bundle {appPath}";
                    _logger.Info($"Deploying to device {device.Name} ({device.Uuid})...");
                    var output = Shell.Execute("ios-deploy", args, out var exitCode, true);
                    _logger.Debug($"{device.Name} ({device.Uuid}) Deployment output: {output}");
                    var success = output.ToLower().Contains($"[100%] installed package {appPath}");
                    if (success)
                    {
                        await ctx.RespondAsync($"Deployed {appPath} to {device.Name} ({device.Uuid}) successfully.");
                    }
                    else
                    {
                        if (output.Length > 2000)
                        {
                            output = string.Join("", output.TakeLast(1900));
                        }
                        await ctx.RespondAsync($"Failed to deploy {appPath} to {device.Name} ({device.Uuid})\nOutput: {output}");
                    }
                }
            });
        }

        [
            Command("rm-pogo"),
            Description("Remove Pokemon Go application from device(s).")
        ]
        public async Task RemovePoGoAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneAB1SE`. Comma delimiter supported `iPhoneAB1SE,iPhoneCD2SE`"), RemainingText]
            string phoneNames = Strings.All)
        {
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
                return;

            var devices = Device.GetAll();
            var removeAppDevices = phoneNames.RemoveSpaces();
            if (string.Compare(phoneNames, Strings.All, true) == 0)
                removeAppDevices = devices.Keys.ToArray();
            foreach (var name in removeAppDevices)
            {
                if (!devices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping remove pogo.");
                    continue;
                }

                var device = devices[name];
                var args = $"--id {device.Uuid} --uninstall_only --bundle_id {Strings.PokemonGoBundleIdentifier}";
                var output = Shell.Execute("ios-deploy", args, out var _);
                await ctx.RespondAsync($"Removed Pokemon Go from {device.Name}\r\nOutput: {output}");
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

            return requiredRoles.Any(x => memberRoles.Contains(x));
        }

        private bool IsValidChannel(ulong channelId)
        {
            //If no channel id is specified allow the command to execute in all channels, otherwise only the channel specified.
            return _dep.Config.ChannelIds.Count == 0 || _dep.Config.ChannelIds.Contains(channelId);
        }

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