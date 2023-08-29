using AMP.Data;
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
    [PacketDefinition((byte) PacketType.CREATURE_DESPAWN)]
    public class CreatureDepawnPacket : AMPPacket {
        [SyncedVar] public long creatureId;

        public CreatureDepawnPacket() { }

        public CreatureDepawnPacket(long creatureId) {
            this.creatureId = creatureId;
        }

        public CreatureDepawnPacket(CreatureNetworkData cnd) 
            : this(creatureId: cnd.networkedId
                  ) {
            
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureId];

                if(cnd.creature != null) {
                    Dispatcher.Enqueue(() => {
                        cnd.creature.Despawn();
                    });
                }
                ModManager.clientSync.syncData.creatures.TryRemove(creatureId, out _);
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(ModManager.serverInstance.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.serverInstance.creatures[creatureId];

                Log.Debug(Defines.SERVER, $"{client.ClientName} has despawned creature {cnd.creatureType} ({cnd.networkedId})");
                server.SendToAllExcept(this, client.ClientId);

                ModManager.serverInstance.creatures.TryRemove(creatureId, out _);
                ModManager.serverInstance.creature_owner.TryRemove(creatureId, out _);

                ServerEvents.InvokeOnCreatureDespawned(cnd, client);
            }
            return true;
        }
    }
}
