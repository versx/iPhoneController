namespace iPhoneController.Utils
{
    using System;
    using System.Net;

    public static class NetUtils
    {
        public static string Get(string url)
        {
            using (var wc = new WebClient())
            {
                wc.Proxy = null;
                return wc.DownloadString(url);
            }
        }
    }
}