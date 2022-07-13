using System.Net;

namespace AMP.Network.Helper {
    internal class NetworkUtil {
        public static string GetIP(string ip) {
            if(!ip.Equals("127.0.0.1")) {
                IPHostEntry entry = Dns.GetHostEntry(ip);
                if(entry.AddressList.Length > 0) {
                    ip = entry.AddressList[0].ToString();
                }
            }
            return ip;
        }
    }
}
