using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMP.Network.Data {
    public class ClientData {
        public int playerId = 1;
        public string name = "Unnamed";
        
        public TcpSocket tcp;
        public UdpSocket udp;

        public PlayerSync playerSync;

        public bool isHost {
            get {
                return (ModManager.clientInstance != null && ModManager.clientInstance.myClientId == playerId);
            }
        }

        public ClientData(int playerId) {
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
