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

    using iPhoneController.Data;
    using iPhoneController.Diagnostics;
    using iPhoneController.Utils;

    using Microsoft.EntityFrameworkCore;

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

            var devices = await GetDevices();
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
                sb.AppendLine($"**{name}**: {uuid}");
            }
            if (sb.Length > 0)
            {
                pages.Add(sb.ToString());
            }
            for (var i = 0; i < pages.Count; i++)
            {
                var msg = pages[i];
                var count = pages.Count > 1 ? $"Page: {i + 1}/{pages.Count}" : string.Empty;
                var eb = new DiscordEmbedBuilder
                {
                    Description = msg,
                    Title = $"{Environment.MachineName} Device List ({keys.Count.ToString("N0")}) {count}",
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

            var realDevices = await GetDevices();
            var devices = phoneNames.Replace(", ", ",").Split(',');
            var devicesFailed = new Dictionary<string, string>();
            for (var i = 0; i < devices.Length; i++)
            {
                var name = devices[i];
                if (!realDevices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping screenshot.");
                    continue;
                }

                var uuid = realDevices[name];
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
            var realDevices = await GetDevices();
            var keys = realDevices.Keys.ToList();
            keys.Sort();

            for (var i = 0; i < keys.Count; i++)
            {
                var name = keys[i];
                if (!realDevices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping iOS version.");
                    continue;
                }
                var uuid = realDevices[name];
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
                Title = $"**{Environment.MachineName}** Device iOS Versions ({realDevices.Count.ToString("N0")})",
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

            var realDevices = await GetDevices();
            var devices = phoneNames.Replace(", ", ",").Split(',');
            for (var i = 0; i < devices.Length; i++)
            {
                var name = devices[i];
                if (!realDevices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping reboot.");
                    continue;
                }

                var uuid = realDevices[name];
                var output = Shell.Execute("idevicediagnostics", $"-u {uuid} restart", out var exitCode);
                var message = exitCode == 0 ? $"Restarting device {name} ({uuid})" : output;
                await ctx.RespondAsync(message);
            }
        }

        [
            Command("kill"),
            Description("Kill a specific running process.")
        ]
        public async Task KillAsync(CommandContext ctx,
            [Description("Process name to attempt to kill."), RemainingText]
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

            if (!string.IsNullOrEmpty(machineName) && string.Compare(machineName, Environment.MachineName, true) != 0)
                return;

            var output = Shell.Execute("killall", processName, out var exitCode);
            await ctx.RespondAsync(exitCode == 0 ? $"{processName} killed." : output);
        }

        #endregion

        #region Remove Apps

        [
            Command("rm-uic"),
            Description("Remove UI-Controller from device(s).")
        ]
        public async Task RemoveUICAsync(CommandContext ctx,
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

            var realDevices = await GetDevices();
            var devices = phoneNames.Replace(", ", ",").Split(',');
            for (var i = 0; i < devices.Length; i++)
            {
                var name = devices[i];
                if (!realDevices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping remove uic.");
                    continue;
                }

                var uuid = realDevices[name];
                var args = $"--id {uuid} --uninstall_only --bundle_id com.apple.test.RealDeviceMap-UIControlUITests-Runner";
                var output = Shell.Execute("ios-deploy", args, out var exitCode);
                await ctx.RespondAsync($"Removed UIC from {name}\r\nOutput: {output}");
            }
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

            var realDevices = await GetDevices();
            var devices = phoneNames.Replace(", ", ",").Split(',');
            for (var i = 0; i < devices.Length; i++)
            {
                var name = devices[i];
                if (!realDevices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping remove pogo.");
                    continue;
                }

                var uuid = realDevices[name];
                var args = $"--id {uuid} --uninstall_only --bundle_id com.nianticlabs.pokemongo";
                var output = Shell.Execute("ios-deploy", args, out var exitCode);
                await ctx.RespondAsync($"Removed Pokemon Go from {name}\r\nOutput: {output}");
            }
        }

        #endregion

        #region Logs

        [
            Command("log-full"),
            Description("Display the latest full log of a specific device.")
        ]
        public async Task FullLogAsync(CommandContext ctx,
            [Description("iPhone name"), RemainingText]
            string phoneName)
        {
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
                return;

            var realDevices = await GetDevices();
            if (!realDevices.ContainsKey(phoneName))
            {
                _logger.Warn($"{phoneName} does not exist in device list, skipping full log.");
                return;
            }

            var output = GetLatestLog(phoneName, true);
            output = string.IsNullOrEmpty(output) ? $"Failed to get latest full log for device {phoneName}." : output;
            if (File.Exists(output))
                output = File.ReadAllText(output);

            if (output.Length > 1500)
                output = output.Substring(output.Length - 1494, 1494);

            await ctx.RespondAsync($"```{output}```");
        }

        [
            Command("log-debug"),
            Description("Display the latest debug log of a specific device.")
        ]
        public async Task DebugLogAsync(CommandContext ctx,
            [Description("iPhone name"), RemainingText]
            string phoneName)
        {
            if (!HasRequiredRoles(ctx.Member))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!IsValidChannel(ctx.Channel.Id))
                return;

            var realDevices = await GetDevices();
            if (!realDevices.ContainsKey(phoneName))
            {
                _logger.Warn($"{phoneName} does not exist in device list, skipping full log.");
                return;
            }

            var output = GetLatestLog(phoneName, false);
            output = string.IsNullOrEmpty(output) ? $"Failed to get latest debug log for device {phoneName}." : output;
            if (File.Exists(output))
                output = File.ReadAllText(output);

            if (output.Length > 1500)
                output = output.Substring(output.Length - 1494, 1494);

            await ctx.RespondAsync($"```{output}```");
        }

        [
            Command("log-clear"),
            Description("Delete all old logs in the `Logs` folder of the UI-Controller Manager.")
        ]
        public async Task ClearLogsAsync(CommandContext ctx,
            [Description("Machine name to delete the old logs from, otherwise leave blank."), RemainingText]
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

            var managerFolder = Path.GetDirectoryName(_dep.Config.SQLiteFilePath);
            var logsFolder = Path.Combine(managerFolder, "Logs");
            var logFiles = Directory.GetFiles(logsFolder, "*.log");
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            for (var i = 0; i < logFiles.Length; i++)
            {
                File.Delete(logFiles[i]);
            }
            sw.Stop();
            await ctx.RespondAsync($"All log files deleted for {Environment.MachineName} (took {sw.Elapsed}).");
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

        private async Task<Dictionary<string, string>> GetDevices()
        {
            var sqliteFilePath = _dep.Config.SQLiteFilePath;
            if (!File.Exists(sqliteFilePath))
            {
                _logger.Warn($"Failed to get device list, {sqliteFilePath} does not exist.");
                return null;
            }

            using (var db = DbContextFactory.Create($"Data Source={sqliteFilePath};"))
            {
                var devices = await db.Devices
                    .Where(x => x.Uuid != "default")
                    .ToListAsync();

                if (devices?.Count == 0)
                {
                    _logger.Warn($"No devices in sqlite database.");
                    return null;
                }

                return devices.ToDictionary(x => x.Name, y => y.Uuid);
            }
        }

        private string GetLatestLog(string deviceName, bool full)
        {
            var managerFolder = Path.GetDirectoryName(_dep.Config.SQLiteFilePath);
            var logsFolder = Path.Combine(managerFolder, "Logs");
            var logs = Directory.GetFiles(logsFolder, $"*{deviceName}{(full ? "*.full.log" : "*.debug.log")}");
            var logFile = logs.FirstOrDefault(x =>
                Math.Round(DateTime.Now.Subtract(File.GetLastWriteTime(x)).TotalMinutes) <= 60); //Log was within the last hour, TODO: Change to 1-5 minutes, maybe configurable?
            return logFile;
        }

        #endregion
    }

    public class PhoneManager
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("PHONE_MGR");

        private readonly Dictionary<string, string> _devices;

        public IReadOnlyDictionary<string, string> Devices => _devices;

        public PhoneManager()
        {
            _devices = new Dictionary<string, string>();
        }

        public bool RestartDevice(string name)
        {
            return true;
        }

        public string Screenshot(string name)
        {
            return string.Empty;
        }
    }
}