namespace iPhoneController.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;

    using iPhoneController.Configuration;
    using iPhoneController.Deployment;
    using iPhoneController.Diagnostics;
    using iPhoneController.Extensions;
    using iPhoneController.Models;
    using iPhoneController.Utils;

    public class AppDeployment : BaseCommandModule
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("DEPLOYMENT");
        private readonly Config _config;

        public AppDeployment(Config config)
        {
            _config = config;
        }

        #region App Management

        [
            Command("resign"),
            Description("Download IPA, resign, and deploy to device(s). If no devices are provided, it will only resign and not deploy."),
        ]
        public async Task ResignPoGoAsync(CommandContext ctx,
            [Description("Mega download link")] string megaLink,
            [Description("Version")] string version,
            [Description("iPhone names i.e. `iPhoneAB1SE`. Comma delimiter supported `iPhoneAB1SE,iPhoneCD2SE`"), RemainingText]
            string phoneNames)
        {
            if (ctx.Guild?.Id == null || !_config.Servers.ContainsKey(ctx.Guild.Id))
                return;

            if (!ctx.Member.HasRequiredRoles(_config.Servers.Values.ToList()))
            {
                await ctx.Channel.SendMessageAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!ctx.Channel.Id.IsValidChannel(_config.Servers.Values.ToList()))
                return;

            var deployer = new AppDeployer(_config.Developer, _config.ProvisioningProfile)
            {
                ResignApp = true,
            };
            deployer.DeployCompleted += async (object sender, DeployEventArgs e) =>
            {
                var response = e.Success
                    ? $"Successfully deployed app to: {e.Device.Name}"
                    : $"Failed to deploy app to: {e.Device.Name}";
                await ctx.Channel.SendMessageAsync(response);
            };
            await ctx.Channel.SendMessageAsync("Starting resign...");
            if (!deployer.Resign(megaLink, version))
            {
                await ctx.Channel.SendMessageAsync($"Failed to resign IPA");
                return;
            }

            var response = $"Resign complete, saved to {deployer.SignedReleaseFileName}.";
            if (!string.IsNullOrEmpty(phoneNames))
            {
                await ctx.Channel.SendMessageAsync($"{response} Starting deployment to {phoneNames}...");
                deployer.Deploy(deployer.SignedReleaseFileName, phoneNames);
                return;
            }

            await ctx.Channel.SendMessageAsync(response);
        }

        [
            Command("deploy"),
            Description("Deploy app application to device(s).")
        ]
        public async Task DeployPoGoAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneAB1SE`. Comma delimiter supported `iPhoneAB1SE,iPhoneCD2SE`"), RemainingText]
            string phoneNames)
        {
            if (ctx.Guild?.Id == null || !_config.Servers.ContainsKey(ctx.Guild.Id))
                return;

            if (!ctx.Member.HasRequiredRoles(_config.Servers.Values.ToList()))
            {
                await ctx.Channel.SendMessageAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!ctx.Channel.Id.IsValidChannel(_config.Servers.Values.ToList()))
                return;

            if (string.IsNullOrEmpty(phoneNames))
            {
                await ctx.Channel.SendMessageAsync($"No devices provided, command not executed.");
                return;
            }

            var devices = Device.GetAll();
            var deployAppDevices = new List<string>(phoneNames.RemoveSpaces());
            if (string.Compare(phoneNames, Strings.All, true) == 0)
                deployAppDevices = devices.Keys.ToList();
            var appPath = AppDeployer.GetLatestAppPath();
            if (string.IsNullOrEmpty(appPath))
            {
                await ctx.Channel.SendMessageAsync($"No signed app found, make sure to run 'resign' command first.");
                return;
            }

            var deployer = new AppDeployer(_config.Developer, _config.ProvisioningProfile);
            deployer.DeployCompleted += async (object sender, DeployEventArgs e) =>
            {
                var message = e.Success
                    ? $"Successfully deployed app to: {e.Device.Name}"
                    : $"Failed to deploy app to: {e.Device.Name}";
                await ctx.Channel.SendMessageAsync(message);
            };
            _logger.Debug($"Using app {appPath} for deployment.");
            await ctx.Channel.SendMessageAsync($"Starting deployment to {phoneNames}...");
            deployer.Deploy(appPath, phoneNames);
        }

        [
            Command("rm-pogo"),
            Description("Remove Pokemon Go application from device(s).")
        ]
        public async Task RemovePoGoAsync(CommandContext ctx,
            [Description("iPhone names i.e. `iPhoneAB1SE`. Comma delimiter supported `iPhoneAB1SE,iPhoneCD2SE`"), RemainingText]
            string phoneNames)
        {
            if (ctx.Guild?.Id == null || !_config.Servers.ContainsKey(ctx.Guild.Id))
                return;

            if (!ctx.Member.HasRequiredRoles(_config.Servers.Values.ToList()))
            {
                await ctx.Channel.SendMessageAsync($":no_entry: {ctx.User.Username} Unauthorized permissions.");
                return;
            }

            if (!ctx.Channel.Id.IsValidChannel(_config.Servers.Values.ToList()))
                return;

            if (string.IsNullOrEmpty(phoneNames))
            {
                await ctx.Channel.SendMessageAsync($"No devices provided, command not executed.");
                return;
            }

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
                var message = $"Removed Pokemon Go from {device.Name}\r\nOutput: ";
                await ctx.Channel.SendMessageAsync(message + string.Join("", output.TakeLast(1900)));
            }
        }

        #endregion
    }
}