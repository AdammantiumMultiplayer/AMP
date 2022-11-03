using AMP.Network.Data;
using UnityEngine;

namespace AMP.Network {
    internal class NetworkStats {

        internal static float sentKbs = 0;
        internal static float receiveKbs = 0;

        internal static void UpdatePacketCount() {
            long bytesReceived = 0;
            long bytesSent = 0;

            if(ModManager.serverInstance != null) {
                foreach(ClientData cd in ModManager.serverInstance.clients.Values) {
                    bytesSent += (cd.tcp != null ? cd.tcp.GetBytesSent() : 0)
                                    + (cd.udp != null ? cd.udp.GetBytesSent() : 0);
                    bytesReceived += (cd.tcp != null ? cd.tcp.GetBytesReceived() : 0)
                                        + (cd.udp != null ? cd.udp.GetBytesReceived() : 0);
                }
            }
            if(ModManager.clientInstance != null) {
                bytesSent += ModManager.clientInstance.nw.GetBytesSent();
                bytesReceived += ModManager.clientInstance.nw.GetBytesReceive();
            }

            sentKbs = Mathf.Round((bytesSent / 1024f) * 100) / 100;
            receiveKbs = Mathf.Round((bytesReceived / 1024f) * 100) / 100;
        }

    }
}
