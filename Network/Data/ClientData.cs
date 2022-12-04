using AMP.Data;
using AMP.Network.Connection;
using AMP.Network.Data.Sync;
using System;
using System.Threading;

namespace AMP.Network.Data {
    public class ClientData {

        public static ClientData SERVER = new ClientData(-1) {
              name = Defines.SERVER
        };

        public long playerId = 1;
        public string name = "Unnamed";

        internal bool greeted = false;
        internal long last_time = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        internal TcpSocket tcp;
        internal UdpSocket udp;

        internal PlayerNetworkData playerSync;

        internal Thread disconnectThread = null;

        internal bool isHost {
            get {
                return (ModManager.clientInstance != null && ModManager.clientInstance.myPlayerId == playerId);
            }
        }

        internal ClientData(long playerId) {
            this.playerId = playerId;
        }

        internal void Disconnect() {
            tcp.onDisconnect = null;
            udp.onDisconnect = null;

            if(tcp != null) tcp.Disconnect();
            if(udp != null) udp.Disconnect();
            tcp = null;
            udp = null;
        }
    }
}
