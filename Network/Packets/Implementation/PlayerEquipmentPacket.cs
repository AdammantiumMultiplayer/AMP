using AMP.GameInteraction;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_EQUIPMENT)]
    public class PlayerEquipmentPacket : AMPPacket {
        [SyncedVar] public long     clientId;
        [SyncedVar] public Color[]  colors;
        [SyncedVar] public string[] equipment;

        public PlayerEquipmentPacket() { }

        public PlayerEquipmentPacket(long clientId, Color[] colors, string[] equipment) {
            this.clientId  = clientId;
            this.colors    = colors;
            this.equipment = equipment;
        }

        public PlayerEquipmentPacket(PlayerNetworkData pnd)
            : this(clientId:  pnd.clientId
                  , colors:    pnd.colors
                  , equipment: pnd.equipment
                  ) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            #if !DEBUG_SELF
            if(clientId == client.ClientId) return true;
            #endif

            PlayerNetworkData pnd = ModManager.clientSync.syncData.players.GetOrAdd(clientId, new PlayerNetworkData());
            pnd.Apply(this);

            if(pnd.isSpawning) return true;
            if(pnd.clientId <= 0) return true; // No player data received yet
            Dispatcher.Enqueue(() => {
                if(pnd.creature == null) {
                    Spawner.TrySpawnPlayer(pnd);
                }
                CreatureEquipment.Apply(pnd);
            });
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            client.player.Apply(this);

            #if DEBUG_SELF
            // Just for debug to see yourself
            server.SendToAll(new PlayerEquipmentPacket(client.player));
            #else
            server.SendToAllExcept(new PlayerEquipmentPacket(client.player), client.ClientId);
            #endif
            return true;
        }
    }
}
