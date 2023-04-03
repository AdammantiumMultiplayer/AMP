using AMP.Events;
using AMP.Logging;
using AMP.Network.Data.Sync;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using System;
using ThunderRoad;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_HEALTH_SET)]
    public class CreatureHealthSetPacket : NetPacket {
        [SyncedVar] public long  creatureId;
        [SyncedVar] public float health;

        public CreatureHealthSetPacket() { }

        public CreatureHealthSetPacket(long creatureId, float health) {
            this.creatureId = creatureId;
            this.health     = health;
            if(this.health <= 0) this.health = -1;
        }

        public CreatureHealthSetPacket(CreatureNetworkData cnd)
            : this( creatureId: cnd.networkedId
                  , health:     cnd.health
                  ) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureId];
                cnd.Apply(this);
                cnd.ApplyHealthToCreature();
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            if(ModManager.serverInstance.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.serverInstance.creatures[creatureId];
                if(cnd.Apply(this)) {
                    try { if(ServerEvents.OnCreatureKilled != null) ServerEvents.OnCreatureKilled.Invoke(cnd, client); } catch(Exception e) { Log.Err(e); }
                }

                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
