using System;
using System.Net;

namespace AMP.Network.Helper {
    internal class NetworkUtil {
        internal static string GetIP(string ip) {
            if(!ip.Equals("127.0.0.1")) {
                try {
                    IPAddress.Parse(ip);
                }catch(Exception) {
                    IPHostEntry entry = Dns.GetHostEntry(ip);
                    if(entry.AddressList.Length > 0) {
                        ip = entry.AddressList[0].ToString();
                    }
                }
            }
            return ip;
        }
    }
}
