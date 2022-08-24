using AMP.Network.Data.Sync;
using AMP.Network.Helper;

namespace AMP.Network.Data {
    public class ClientData {
        public long playerId = 1;
        public string name = "Unnamed";
        
        public TcpSocket tcp;
        public UdpSocket udp;

        public PlayerNetworkData playerSync;

        public bool isHost {
            get {
                return (ModManager.clientInstance != null && ModManager.clientInstance.myClientId == playerId);
            }
        }

        public ClientData(long playerId) {
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
