namespace iPhoneController.Models
{
    using System;
    using System.Collections.Generic;

    using iPhoneController.Extensions;
    using iPhoneController.Utils;

    internal class Device
    {
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

            var split = output.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
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
                    var tetherData = Shell.Execute("idevicesyslog", $"-u {uuid} -m '192.168.' -T 'IPv4'", out var tetherExitCode);
                    ipAddress = ParseIPAddress(tetherData);
                }
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
            return devices;
        }

        private static string ParseIPAddress(string data)
        {
            if (data.ToLower().Contains("ping"))
            {
                return data.GetBetween("(", ")");
            }

            var ipaddr = string.Empty;
            var ipLine = string.Empty;
            var lines = data.Split("\n");
            // TODO: Test
            foreach (var line in lines)
            {
                if (line.Contains("<->IPv4"))
                {
                    ipLine = line;
                    break;
                }
            }
            var ipSplit = ipLine.Split(" |:");
            // TODO: Test
            foreach (var line in ipSplit)
            {
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