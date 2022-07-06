using AMP.Network.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMP.Network.Data {
    public class ClientData {
        public int id = 1;
        public string name = "Unnamed";
        
        public TcpSocket tcp;
        public UdpSocket udp;
        private int playerId;

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
