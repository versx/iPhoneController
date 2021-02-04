namespace iPhoneController.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using iPhoneController.Diagnostics;
    using iPhoneController.Extensions;
    using iPhoneController.Utils;

    internal class Device
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("DEVICE");

        public string Name { get; set; }

        public string Uuid { get; set; }

        public string IPAddress { get; set; }


        public static Dictionary<string, Device> GetAll()
        {
            var devices = new Dictionary<string, Device>();
            var output = Shell.Execute("ios-deploy", "-c device_identification", out var exitCode);
            if (string.IsNullOrEmpty(output))// || exitCode != 0)
            {
                // Failed
                return devices;
            }

            var leases = TetheredDhcpLease.ParseDhcpLeases();
            var split = output.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                foreach (var line in split)
                {
                    if (!line.ToLower().Contains("found"))
                        continue;

                    var uuid = line.GetBetween("Found ", " (");
                    var name = line.GetBetween("'", "'");
                    // Look for WiFi addresses since it's quick
                    var ipData = Shell.Execute("ping", $"-t 1 {name}", out var ipExitCode);
                    var ipAddress = ParseIPAddress(ipData);
                    if (string.IsNullOrEmpty(ipAddress))
                    {
                        // Look for tethered addresses and blanks. This takes a while
                        ipAddress = leases.FirstOrDefault(x => x.Name.ToLower().Contains(name.ToLower()))?.IpAddress;
                    }
                    _logger.Debug($"Found device {name} ({uuid}) {ipAddress}");
                    if (!devices.ContainsKey(name))
                    {
                        devices.Add(name, new Device
                        {
                            Name = name,
                            Uuid = uuid,
                            IPAddress = ipAddress
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"ERROR: {ex}");
            }
            return devices;
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
            // TODO: Test
            foreach (var line in lines)
            {
                _logger.Debug($"Line: {line}");
                if (line.Contains("<->ipv4"))
                {
                    ipLine = line;
                    break;
                }
            }
            foreach (var line in ipLine.Split(' '))
            {
                _logger.Debug($"IP Line Split: {line}");
            }
            var split = ipLine.Split(' ');
            if (split.Length != 3)
                return null;

            var ipSplit = split[2].Split(new string[] { "<->" }, StringSplitOptions.None);//ipLine.Split(" |:");
            // TODO: Test
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