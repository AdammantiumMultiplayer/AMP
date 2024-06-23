using AMP.Data;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ENTITY_SPAWN)]
    public class EntitySpawnPacket : AMPPacket {

        [SyncedVar] public int entityId;
        [SyncedVar] public string type;
        [SyncedVar] public int clientsideId;
        [SyncedVar] public Vector3 position;
        [SyncedVar(true)] public Vector3 rotation;

        public EntitySpawnPacket() { }

        public EntitySpawnPacket(int entityId, string type, Vector3 position, Vector3 rotation) {
            this.entityId = entityId;
            this.type = type;
            this.position = position;
            this.rotation = rotation;
        }

        public EntitySpawnPacket(EntityNetworkData end) : this(end.clientsideId, end.type, end.position, end.rotation) { }

        public override bool ProcessClient(NetamiteClient client) {
            EntityNetworkData end = new EntityNetworkData();
            end.Apply(this);

            ModManager.clientSync.syncData.entities.TryAdd(entityId, end);
            
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            EntityNetworkData end = new EntityNetworkData();
            end.Apply(this);

            // TODO: Prevent duplication

            end.networkedId = ModManager.serverInstance.NextEntityId;
            ModManager.serverInstance.entities.TryAdd(end.networkedId, end);
            ModManager.serverInstance.entity_owner.TryAdd(end.networkedId, client.ClientId);

            Log.Debug(Defines.SERVER, $"{client.ClientName} has spawned entity {type} ({end.networkedId})");

            server.SendTo(client, new EntitySpawnPacket(end));

            end.clientsideId = 0;

            server.SendToAllExcept(new EntitySpawnPacket(end), client.ClientId);
            return true;
        }
    }
}
