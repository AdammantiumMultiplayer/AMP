using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using Netamite.Server.Data;
using UnityEngine;

namespace AMP.Network.Data {
    public class ClientData {

        public static ClientData SERVER = new ClientData(null) {
              
        };

        internal bool greeted = false;

        public PlayerNetworkData player;

        public ClientInformation client;

        public ClientData(ClientInformation client) {
            this.client = client;
        }


        public void Teleport(Vector3 position, float rotation = 0f) {
            ModManager.serverInstance.netamiteServer.SendTo(client, new PlayerTeleportPacket(position, rotation));
        }

    }
}
