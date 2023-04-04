using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition(false, (byte) PacketType.CREATURE_POSITION)]
    public class CreaturePositionPacket : NetPacket {
        [SyncedVar]       public long    creatureId;
        [SyncedVar]       public Vector3 position;
        [SyncedVar(true)] public float   rotationY;

        public CreaturePositionPacket() { }

        public CreaturePositionPacket(long creatureId, Vector3 position, float rotationY) {
            this.creatureId = creatureId;
            this.position   = position;
            this.rotationY   = rotationY;
        }

        public CreaturePositionPacket(CreatureNetworkData cnd)
            : this(creatureId: cnd.networkedId
                  , position:  cnd.position
                  , rotationY: cnd.rotationY
                  ) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureId];
                if(cnd.isSpawning) return true;

                cnd.Apply(this);

                Dispatcher.Enqueue(() => {
                    cnd.ApplyPositionToCreature();
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            if(ModManager.serverInstance.creatures.ContainsKey(creatureId)) {
                if(ModManager.serverInstance.creature_owner[creatureId] != client.ClientId) return true;

                CreatureNetworkData cnd = ModManager.serverInstance.creatures[creatureId];
                cnd.Apply(this);

                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
