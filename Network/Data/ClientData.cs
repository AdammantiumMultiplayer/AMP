using AMP.Network.Data.Sync;
using AMP.Network.Helper;

namespace AMP.Network.Data {
    internal class ClientData {
        internal long playerId = 1;
        internal string name = "Unnamed";

        internal bool greeted = false;

        internal TcpSocket tcp;
        internal UdpSocket udp;

        internal PlayerNetworkData playerSync;

        internal bool isHost {
            get {
                return (ModManager.clientInstance != null && ModManager.clientInstance.myPlayerId == playerId);
            }
        }

        internal ClientData(long playerId) {
            this.playerId = playerId;
        }

        internal void Disconnect() {
            if(tcp != null) tcp.Disconnect();
            if(udp != null) udp.Disconnect();
            tcp = null;
            udp = null;
        }
    }
}
