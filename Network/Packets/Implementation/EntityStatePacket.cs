using AMP.Network.Data.Sync;
using AMP.Network.Data;
using Netamite.Client.Definition;
using Netamite.Network.Packet.Attributes;
using Netamite.Network.Packet;
using Netamite.Server.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AMP.Logging;
using AMP.Data;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte)PacketType.ENTITY_STATE)]
    internal class EntityStatePacket : AMPPacket {
        [SyncedVar] public int entityId;
        [SyncedVar] public int state;
        [SyncedVar] public int value;
        [SyncedVar] public int[] vars;


        public EntityStatePacket() { }

        public EntityStatePacket(int entityId, int state, int value, int[] vars) {
            this.entityId = entityId;
            this.state = state;
            this.value = value;
            this.vars = vars;
        }

        public EntityStatePacket(int entityId, int state, int value) : this(entityId, state, value, new int[0]) { }


        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.entities.ContainsKey(entityId)) {
                ModManager.clientSync.syncData.entities[entityId].networkEntity.HandleEntityStateChange(this);
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            server.SendToAllExcept(this, client.ClientId);
            return true;
        }
    }
}
