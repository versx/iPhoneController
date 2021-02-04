namespace iPhoneController.Models
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    class TetheredDhcpLease
    {
        // name
        public string Name { get; set; }

        // ip_address
        public string IpAddress { get; set; }

        // hw_address
        public string MacAddress { get; set; }

        public string Identifier { get; set; }

        // lease
        public string Lease { get; set; }


        public static List<TetheredDhcpLease> ParseDhcpLeases()
        {
            var list = new List<TetheredDhcpLease>();
            var leasesPath = "/var/db/dhcpd_leases";
            if (File.Exists(leasesPath))
            {
                var lines = File.ReadAllLines(leasesPath);
                TetheredDhcpLease lease = null;
                foreach (var line in lines)
                {
                    if (line.Contains("{"))
                    {
                        lease = new TetheredDhcpLease();
                    }
                    else if (line.Contains("name="))
                    {
                        lease.Name = line.Replace("name=", "").Trim('\t');
                    }
                    else if (line.Contains("ip_address="))
                    {
                        lease.IpAddress = line.Replace("ip_address=", "").Trim('\t');
                    }
                    else if (line.Contains("hw_address="))
                    {
                        lease.MacAddress = line.Replace("hw_address=", "").Trim('\t');
                    }
                    else if (line.Contains("identifier="))
                    {
                        lease.Identifier = line.Replace("identifier=", "").Trim('\t');
                    }
                    else if (line.Contains("lease="))
                    {
                        lease.Lease = line.Replace("lease=", "").Trim('\t');
                    }
                    else if (line.Contains("}"))
                    {
                        list.Add(lease);
                    }
                }
            }
            return list;
        }
    }
}