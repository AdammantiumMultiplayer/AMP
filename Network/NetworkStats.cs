using UnityEngine;

namespace AMP.Network {
    internal class NetworkStats {

        internal static float sentKbs = 0;
        internal static float receiveKbs = 0;

        internal static void UpdatePacketCount() {
            long bytesReceived = 0;
            long bytesSent = 0;

            //if(ModManager.serverInstance != null) {
            //    foreach(ClientData cd in ModManager.serverInstance.clients.Values) {
            //        bytesSent += (cd.reliable != null ? cd.reliable.GetBytesSent() : 0)
            //                        + (cd.unreliable != null ? cd.unreliable.GetBytesSent() : 0);
            //        bytesReceived += (cd.reliable != null ? cd.reliable.GetBytesReceived() : 0)
            //                            + (cd.unreliable != null ? cd.unreliable.GetBytesReceived() : 0);
            //    }
            //}
            if(ModManager.clientInstance != null) {
                bytesSent += ModManager.clientInstance.netclient.GetBytesSent();
                bytesReceived += ModManager.clientInstance.netclient.GetBytesReceive();
            }

            sentKbs = Mathf.Round((bytesSent / 1024f) * 100) / 100;
            receiveKbs = Mathf.Round((bytesReceived / 1024f) * 100) / 100;
        }

    }
}
