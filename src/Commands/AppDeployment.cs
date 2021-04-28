namespace iPhoneController.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;

    using iPhoneController.Deployment;
    using iPhoneController.Diagnostics;
    using iPhoneController.Extensions;
    using iPhoneController.Models;
    using iPhoneController.Utils;

    public class AppDeployment
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("DEPLOYMENT");
        private readonly Dependencies _dep;

        public AppDeployment(Dependencies dep)
        {
            _dep = dep;
        }

        #region App Management

        [
            Command("resign"),
            Description("Download IPA, resign, and deploy to device(s)."),
        ]
        public async Task ResignPoGoAsync(CommandContext ctx,
            [Description("Mega download link")] string megaLink,
            [Description("Version")] string version,
            [Description("iPhone names i.e. `iPhoneAB1SE`. Comma delimiter supported `iPhoneAB1SE,iPhoneCD2SE`"), RemainingText]
            string phoneNames)
        {
            if (!ctx.Member.HasRequiredRoles(_dep.Config.RequiredRoles))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!ctx.Channel.Id.IsValidChannel(_dep.Config.ChannelIds))
                return;

            var deployer = new AppDeployer(_dep.Config.Developer, _dep.Config.ProvisioningProfile)
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
            Description("Deploy app application to device(s).")
        ]
        public async Task DeployPoGoAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneAB1SE`. Comma delimiter supported `iPhoneAB1SE,iPhoneCD2SE`"), RemainingText]
            string phoneNames)
        {
            if (!ctx.Member.HasRequiredRoles(_dep.Config.RequiredRoles))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!ctx.Channel.Id.IsValidChannel(_dep.Config.ChannelIds))
                return;

            var devices = Device.GetAll();
            var deployAppDevices = new List<string>(phoneNames.RemoveSpaces());
            if (string.Compare(phoneNames, Strings.All, true) == 0)
                deployAppDevices = devices.Keys.ToList();
            var appPath = AppDeployer.GetLatestAppPath();
            if (string.IsNullOrEmpty(appPath))
            {
                await ctx.RespondAsync($"No signed app found, make sure to run 'resign' command first.");
                return;
            }

            var deployer = new AppDeployer(_dep.Config.Developer, _dep.Config.ProvisioningProfile);
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
            string phoneNames)
        {
            if (!ctx.Member.HasRequiredRoles(_dep.Config.RequiredRoles))
            {
                await ctx.RespondAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!ctx.Channel.Id.IsValidChannel(_dep.Config.ChannelIds))
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
    }
}