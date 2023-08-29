using AMP.Events;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using System;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_HEALTH_SET)]
    public class CreatureHealthSetPacket : AMPPacket {
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
                Dispatcher.Enqueue(() => {
                    cnd.ApplyHealthToCreature();
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(ModManager.serverInstance.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.serverInstance.creatures[creatureId];
                
                ServerEvents.InvokeOnCreatureKilled(cnd, client);

                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
