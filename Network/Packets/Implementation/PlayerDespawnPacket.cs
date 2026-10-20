using AMP.Discord;
using AMP.GameInteraction;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_DESPAWN)]
    public class PlayerDespawnPacket : AMPPacket {
        [SyncedVar]       public int     clientId;

        public PlayerDespawnPacket() { }

        public PlayerDespawnPacket(int clientId) {
            this.clientId   = clientId;
        }

        public PlayerDespawnPacket(PlayerNetworkData pnd) 
            : this( clientId:   pnd.clientId
                  ) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.players.ContainsKey(clientId)) {
                PlayerNetworkData pnd = ModManager.clientSync.syncData.players[clientId];
                pnd.Despawn();
                pnd.receivedPos = false;
            }
            
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            // Do nothing, as no client is allowed to send such a packet
            return true;
        }
    }
}
