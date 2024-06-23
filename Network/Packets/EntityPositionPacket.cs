using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition(false, (byte)PacketType.ENTITY_POSITION)]
    public class EntityPositionPacket : AMPPacket {
        [SyncedVar] public int entityId;
        [SyncedVar] public Vector3 position;
        [SyncedVar(true)] public Vector3 rotation;

        public EntityPositionPacket() { }

        public EntityPositionPacket(int entityId, Vector3 position, Vector3 rotation) {
            this.entityId = entityId;
            this.position = position;
            this.rotation = rotation;
        }

        public EntityPositionPacket(EntityNetworkData end)
            : this(entityId: end.networkedId
                  , position: end.position
                  , rotation: end.rotation
                  ) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.entities.ContainsKey(entityId)) {
                EntityNetworkData end = ModManager.clientSync.syncData.entities[entityId];
                
                Dispatcher.Enqueue(() => {
                    end.Apply(this);
                    end.ApplyPositionToEntity();
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(ModManager.serverInstance.entities.ContainsKey(entityId)) {
                if(ModManager.serverInstance.entity_owner[entityId] != client.ClientId) return true;

                EntityNetworkData end = ModManager.serverInstance.entities[entityId];
                end.Apply(this);

                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
