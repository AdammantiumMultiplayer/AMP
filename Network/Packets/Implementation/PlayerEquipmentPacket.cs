using AMP.GameInteraction;
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
        [SyncedVar] public int      clientId;
        [SyncedVar] public Color[]  colors;
        [SyncedVar] public string[] equipment;
        [SyncedVar] public string ethnicGroup;

        public PlayerEquipmentPacket() { }

        public PlayerEquipmentPacket(int clientId, Color[] colors, string[] equipment, string ethnicGroup) {
            this.clientId  = clientId;
            this.colors    = colors;
            this.equipment = equipment;
            this.ethnicGroup = ethnicGroup;
        }

        public PlayerEquipmentPacket(PlayerNetworkData pnd)
            : this(clientId:  pnd.clientId
                  , colors:    pnd.colors
                  , equipment: pnd.equipment
                  , ethnicGroup: pnd.ethnicGroup
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
