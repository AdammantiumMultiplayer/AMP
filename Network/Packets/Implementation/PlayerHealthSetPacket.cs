using AMP.Events;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using System;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_HEALTH_SET)]
    public class PlayerHealthSetPacket : NetPacket {
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

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            ClientData cd = client.GetData();

            if(cd.player.Apply(this)) {
                try { if(ServerEvents.OnPlayerKilled != null) ServerEvents.OnPlayerKilled.Invoke(cd.player, client); } catch(Exception e) { Log.Err(e); }
            }

            #if DEBUG_SELF
            // Just for debug to see yourself
            server.SendToAll(new PlayerHealthSetPacket(cd.player));
            #else
            server.SendToAllExcept(new PlayerHealthSetPacket(cd.player), client.ClientId);
            #endif
            return true;
        }
    }
}
