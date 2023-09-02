using AMP.Events;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_HEALTH_SET)]
    public class PlayerHealthSetPacket : AMPPacket {
        [SyncedVar] public long  playerId;
        [SyncedVar] public float health;

        public PlayerHealthSetPacket() { }

        public PlayerHealthSetPacket(long playerId, float health) {
            this.playerId = playerId;
            this.health   = health;
        }

        public PlayerHealthSetPacket(PlayerNetworkData pnd) 
            : this( playerId: pnd.clientId
                  , health:   pnd.health
                  ){

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.players.ContainsKey(playerId)) {
                ModManager.clientSync.syncData.players[playerId].Apply(this);
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(client.player.Apply(this)) {
                ServerEvents.InvokeOnPlayerKilled(client, client.player.lastDamager);
            }

            #if DEBUG_SELF
            // Just for debug to see yourself
            server.SendToAll(new PlayerHealthSetPacket(client.player));
            #else
            server.SendToAllExcept(new PlayerHealthSetPacket(client.player), client.ClientId);
            #endif
            return true;
        }
    }
}
