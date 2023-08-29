using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using Netamite.Server.Data;
using UnityEngine;

namespace AMP.Network.Data {
    public class ClientData : ClientInformation {

        public static ClientData SERVER = new ClientData() {
              
        };

        internal bool greeted = false;

        internal PlayerNetworkData _player = null;

        public PlayerNetworkData player {
            get {
                if(_player == null) {
                    _player = new PlayerNetworkData() { clientId = ClientId };
                }

                return _player;
            }
            set { _player = value; }
        }

        public ClientData() { }

        public void Teleport(Vector3 position, float rotation = 0f) {
            ModManager.serverInstance.netamiteServer.SendTo(this, new PlayerTeleportPacket(position, rotation));
        }

    }
}
