using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace AMP.Network.Helper {
    internal class NetworkUtil {
        internal static string GetIP(string address) {
            if(!address.Equals("127.0.0.1")) {
                try {
                    IPAddress.Parse(address);
                }catch(Exception) {
                    IPAddress[] addresslist = Dns.GetHostAddresses(address);
                    if(addresslist.Length > 0) {
                        address = addresslist.First(addr => addr.AddressFamily == AddressFamily.InterNetwork).ToString();
                    }
                }
            }
            return address;
        }
    }
}
