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

    public class PhoneControl
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("PHONE_CTRL");
        private readonly Dependencies _dep;

        public PhoneControl(Dependencies dep)
        {
            _dep = dep;
        }

        [
            Command("list"),
            Description("List all available devices.")
        ]
        public async Task ListDevicesAsync(CommandContext ctx)
        {
            if (!HasRequiredRoles(ctx.Member))
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");

            if (!IsValidChannel(ctx.Channel.Id))
                await ctx.RespondAsync($":warning: {ctx.User.Username} Invalid channel.");

            var realDevices = GetDevices();
            var devices = realDevices.Keys.ToList();
            var devicesString = string.Empty;
            for (var i = 0; i < devices.Count; i++)
            {
                var name = devices[i];
                var uuid = realDevices[name];
                devicesString += $"**{name}**: {uuid}\r\n";
            }

            var eb = new DiscordEmbedBuilder
            {
                Description = devicesString,
                Title = "Device List",
                Color = DiscordColor.Blurple
            };
            await ctx.RespondAsync(embed: eb);
        }

        [
            Command("reboot"),
            Description("Reboot specific device(s).")
        ]
        public async Task RebootAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneHV1SE`. Comma delimiter supported `iPhoneHV1SE,iPhoneHV2SE`"), RemainingText]
            string phoneNames)
        {
            if (!HasRequiredRoles(ctx.Member))
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");

            if (!IsValidChannel(ctx.Channel.Id))
                await ctx.RespondAsync($":warning: {ctx.User.Username} Invalid channel.");

            var realDevices = GetDevices();
            var devices = phoneNames.Split(',');
            for (var i = 0; i < devices.Length; i++)
            {
                var name = devices[i];
                if (!realDevices.ContainsKey(name))
                {
                    _logger.Warn($"{name} does not exist in device list, skipping reboot.");
                    continue;
                }

                var uuid = realDevices[name];
                var output = Shell.Start("idevicediagnostics", $"-u {uuid} restart", out var exitCode);
                var message = exitCode == 0 ? $"Restarting device {name} ({uuid})" : output;
                await ctx.RespondAsync(message);
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
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");

            if (!IsValidChannel(ctx.Channel.Id))
                await ctx.RespondAsync($":warning: {ctx.User.Username} Invalid channel.");

            var realDevices = GetDevices();
            var devices = phoneNames.Split(',');
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

                var output = Shell.Start("idevicescreenshot", $"-u {uuid} {uuid}.jpg", out var exitCode);
                if (exitCode == 0)
                {
                    //var message = exitCode == 0 ? $"Restarting device {name} ({uuid})" : output;
                    await ctx.RespondWithFileAsync($"{uuid}.jpg");
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
            Command("rm-uic"),
            Description("Remove UI-Controller from device(s).")
        ]
        public async Task RemoveUICAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneAB1SE`. Comma delimiter supported `iPhoneAB1SE,iPhoneCD2SE`"), RemainingText]
            string phoneNames)
        {
            if (!HasRequiredRoles(ctx.Member))
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");

            if (!IsValidChannel(ctx.Channel.Id))
                await ctx.RespondAsync($":warning: {ctx.User.Username} Invalid channel.");

            var realDevices = GetDevices();
            var devices = phoneNames.Split(',');
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
                Shell.Start("ios-deploy", args, out var exitCode);
                //TODO: Proper remove uic response
                await ctx.RespondAsync($"Removed UIC from: {phoneNames}");
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
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");

            if (!IsValidChannel(ctx.Channel.Id))
                await ctx.RespondAsync($":warning: {ctx.User.Username} Invalid channel.");

            var realDevices = GetDevices();
            var devices = phoneNames.Split(',');
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
                Shell.Start("ios-deploy", args, out var exitCode);
                //TODO: Proper remove pogo response
                await ctx.RespondAsync($"Removed Pokemon Go from: {phoneNames}");
            }
        }

        [
            Command("log-full"),
            Description("")
        ]
        public async Task FullLogAsync(CommandContext ctx,
            [Description(""), RemainingText]
            string phoneName)
        {
            if (!HasRequiredRoles(ctx.Member))
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");

            if (!IsValidChannel(ctx.Channel.Id))
                await ctx.RespondAsync($":warning: {ctx.User.Username} Invalid channel.");

            //if (!_dep.Config.Devices.ContainsKey(phoneName))
            //{
            //    _logger.Warn($"{phoneName} does not exist in device list, skipping full log.");
            //    return;
            //}

            //var args = $". -name \"*{phoneName}*.full.log*\" -mtime -30m | head -1";
            var args = $". -name *{phoneName}*.full*.log | head -1";
            //var args = $". -amin 1 -name \"*{phoneName}*.full*.log\" ! -name \"*full\"* -print";
            var output = Shell.Start("find", args, out var exitCode);
            output = string.IsNullOrEmpty(output) ? $"Failed to get latest full log for device {phoneName}." : output;
            await ctx.RespondAsync(output);
        }

        [
            Command("log-debug"),
            Description("")
        ]
        public async Task DebugLogAsync(CommandContext ctx,
            [Description(""), RemainingText]
            string phoneName)
        {
            if (!HasRequiredRoles(ctx.Member))
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");

            if (!IsValidChannel(ctx.Channel.Id))
                await ctx.RespondAsync($":warning: {ctx.User.Username} Invalid channel.");

            //if (!_dep.Config.Devices.ContainsKey(phoneName))
            //{
            //    _logger.Warn($"{phoneName} does not exist in device list, skipping full log.");
            //    return;
            //}

            var args = $". -amin 1 -name \"*{phoneName}*.debug*.log\" -print";
            var output = Shell.Start("find", args, out var exitCode);
            output = string.IsNullOrEmpty(output) ? $"Failed to get latest debug log for device {phoneName}." : output;
            await ctx.RespondAsync(output);
        }

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
            return _dep.Config.RequiredRoles.Count == 0 || _dep.Config.RequiredRoles.Contains(channelId);
        }

        private Dictionary<string, string> GetDevices()
        {
            var sqliteFilePath = _dep.Config.SQLiteFilePath;
            if (!File.Exists(sqliteFilePath))
            {
                _logger.Warn($"Failed to get device list, {sqliteFilePath} does not exist.");
                return null;
            }

            using (var db = DbContextFactory.Create($"Data Source={sqliteFilePath};"))
            {
                var devices = db.Devices
                    .Where(x => x.Uuid != "default")
                    .ToListAsync()
                    .GetAwaiter()
                    .GetResult();

                if (devices.Count == 0)
                {
                    _logger.Warn($"No devices in sqlite database.");
                    return null;
                }

                return devices.ToDictionary(x => x.Name, y => y.Uuid);
            }
        }
    }
}