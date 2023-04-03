using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition(true, (byte) PacketType.ITEM_POSITION)]
    public class ItemPositionPacket : NetPacket {
        [SyncedVar]       public long    itemId;
        [SyncedVar]       public Vector3 position;
        [SyncedVar(true)] public Vector3 rotation;
        [SyncedVar(true)] public Vector3 velocity;
        [SyncedVar(true)] public Vector3 angularVelocity;

        public ItemPositionPacket() { }

        public ItemPositionPacket(long itemId, Vector3 position, Vector3 rotation, Vector3 velocity, Vector3 angularVelocity) {
            this.itemId          = itemId;
            this.position        = position;
            this.rotation        = rotation;
            this.velocity        = velocity;
            this.angularVelocity = angularVelocity;
        }

        public ItemPositionPacket(ItemNetworkData ind)
            : this( itemId:          ind.networkedId
                  , position:        ind.position
                  , rotation:        ind.rotation
                  , velocity:        ind.velocity
                  , angularVelocity: ind.angularVelocity
                  ) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                ItemNetworkData itemNetworkData = ModManager.clientSync.syncData.items[itemId];

                itemNetworkData.Apply(this);
                itemNetworkData.PositionChanged();

                Dispatcher.Enqueue(() => {
                    itemNetworkData.ApplyPositionToItem();
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            if(ModManager.serverInstance.items.ContainsKey(itemId)) {
                ItemNetworkData ind = ModManager.serverInstance.items[itemId];

                ind.Apply(this);

                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
