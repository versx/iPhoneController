namespace iPhoneController.Utils
{
    using System;
    using System.Collections.Generic;

    using iPhoneController.Extensions;

    class Devices
    {
        public static Dictionary<string, string> GetAll()
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
    }
}