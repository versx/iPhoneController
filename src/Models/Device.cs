namespace iPhoneController.Models
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    using iPhoneController.Diagnostics;
    using iPhoneController.Extensions;
    using iPhoneController.Utils;

    /*
{
	"Command": "list",
	"Output": {
		"0xDEADBEAF80085": {
			"locationID": 1234567,
			"UDID": "9abc99999999999a9a9a9a9999ab999999a9abc99",
			"ECID": "0xDEADBEAF80085",
			"name": "9SE",
			"deviceType": "iPhone8,4"
		}
	},
	"Type": "CommandOutput",
	"Devices": ["0xDEADBEAF80085"]
}
     */

    class DeviceManifest
    {
        [JsonProperty("Command")]
        public string Command { get; set; }

        [JsonProperty("Output")]
        public Dictionary<string, DeviceInfo> Output { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }
    }

    class DeviceInfo
    {
        [JsonProperty("locationID")]
        public string LocationId { get; set; }

        [JsonProperty("UDID")]
        public string Uuid { get; set; }

        [JsonProperty("ECID")]
        public string Ecid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("deviceType")]
        public string DeviceType { get; set; }
    }

    internal class Device
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("DEVICE");

        /// <summary>
        /// Device name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Device UDID/UUID
        /// </summary>
        public string Uuid { get; set; }

        /// <summary>
        /// Device IP address
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// Device unique chip ID
        /// </summary>
        public string Ecid { get; set; }

        /// <summary>
        /// Get all devices connected
        /// </summary>
        /// <returns>Returns a dictionary of device names and devices</returns>
        public static Dictionary<string, Device> GetAll()
        {
            var devices = new Dictionary<string, Device>();
            var output = Shell.Execute("cfgutil", "--format JSON list", out var exitCode);
            if (string.IsNullOrEmpty(output) || exitCode != 0)
            {
                return null;
            }

            var leases = TetheredDhcpLease.ParseDhcpLeases();
            var split = output.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var obj = JsonConvert.DeserializeObject<DeviceManifest>(output);
            var deviceManifest = obj.Output;
            try
            {
                foreach (var device in deviceManifest)
                {
                    // Look for WiFi addresses since it's quick
                    var ipData = Shell.Execute("ping", $"-t 1 {device.Value.Name}", out var ipExitCode);
                    var ipAddress = ParseIPAddress(ipData);
                    if (string.IsNullOrEmpty(ipAddress))
                    {
                        // Look for tethered addresses and blanks. This takes a while
                        ipAddress = leases.FirstOrDefault(x => x.Name.ToLower().Contains(device.Value.Name.ToLower()))?.IpAddress;
                    }
                    devices.Add(device.Value.Name, new Device
                    {
                        Name = device.Value.Name,
                        Uuid = device.Value.Uuid,
                        IPAddress = ipAddress,
                        Ecid = device.Value.Ecid,
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"ERROR: {ex}");
            }
            return devices;
        }

        /// <summary>
        /// Reapply Single App Mode profile
        /// </summary>
        /// <param name="device">Device to reapply SAM profile</param>
        /// <returns>Returns whether the action succeeded or not</returns>
        public static async Task<bool> ReapplySingleAppModeProfile(Device device)
        {
            // Remove the SAM profile
            var result = await CfgUtil(device, "remove-profile", "com.apple.configurator.singleappmode", true);
            if (!result)
            {
                return false;
            }

            // Apply the PokemonGo profile to relaunch the game
            result = await CfgUtil(device, "install-profile", "sam_pogo.mobileconfig");
            if (!result)
            {
                return false;
            }

            // Reapplication was successful
            _logger.Info($"Reapplied the SAM profile to {device.Name} ({device.Uuid}): {result}");
            return true;
        }

        private static async Task<bool> CfgUtil(Device device, string command, string profile, bool isIdentifier = false)
        {
            var orgCrt = Path.Combine(Directory.GetCurrentDirectory(), "org.crt");
            var orgDer = Path.Combine(Directory.GetCurrentDirectory(), "org.der");
            if (!isIdentifier)
            {
                profile = Path.Combine(Directory.GetCurrentDirectory(), profile);
            }
            var output = await Shell.ExecuteAsync("cfgutil", $"-e {device.Ecid} -K \"{orgDer}\" -C \"{orgCrt}\" {command} \"{profile}\"", true);
            if (output.ToLower().Contains("error"))
            {
                _logger.Error($"Failed to {command} profile {profile} for device {device.Name} ({device.Uuid}): {output}");
                return false;
            }
            _logger.Info($"Successfully {command} profile {profile} to {device.Name} ({device.Uuid}): {output}");
            return true;
        }

        private static string ParseIPAddress(string data)
        {
            if (string.IsNullOrEmpty(data))
                return null;

            if (data.ToLower().Contains("ping"))
            {
                return data.GetBetween("(", ")");
            }

            var ipaddr = string.Empty;
            var ipLine = string.Empty;
            var lines = data.ToLower().Split("\n");
            foreach (var line in lines)
            {
                _logger.Debug($"Line: {line}");
                if (line.Contains("<->ipv4"))
                {
                    ipLine = line;
                    break;
                }
            }
            var split = ipLine.Split(' ');
            if (split.Length != 3)
                return null;

            var ipSplit = split[2].Split(new string[] { "<->" }, StringSplitOptions.None);
            foreach (var line in ipSplit)
            {
                _logger.Debug($"IP line: {line}");
                if (line.Contains("192.168."))
                {
                    ipaddr = line;
                    break;
                }
            }
            return ipaddr;
        }
    }
}
