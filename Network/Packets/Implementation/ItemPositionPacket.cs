using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition(true, (byte) PacketType.ITEM_POSITION)]
    public class ItemPositionPacket : AMPPacket {
        [SyncedVar]       public long    timestamp; // This Timestamp is the client timestamp including the server time offset, so its basically the server time
        [SyncedVar]       public int     itemId;
        [SyncedVar]       public Vector3 position;
        [SyncedVar(true)] public Vector3 rotation;
        [SyncedVar(true)] public Vector3 velocity;
        [SyncedVar(true)] public Vector3 angularVelocity;

        public ItemPositionPacket() { }

        public ItemPositionPacket(long timestamp, int itemId, Vector3 position, Vector3 rotation, Vector3 velocity, Vector3 angularVelocity) {
            this.timestamp       = timestamp;
            this.itemId          = itemId;
            this.position        = position;
            this.rotation        = rotation;
            this.velocity        = velocity;
            this.angularVelocity = angularVelocity;
        }

        public ItemPositionPacket(ItemNetworkData ind)
            : this( timestamp:       ind.dataTimestamp
                  , itemId:          ind.networkedId
                  , position:        ind.position
                  , rotation:        ind.rotation
                  , velocity:        ind.velocity
                  , angularVelocity: ind.angularVelocity
                  ) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                ItemNetworkData itemNetworkData = ModManager.clientSync.syncData.items[itemId];

                float compensationFactor = NetworkData.GetCompensationFactor(timestamp);

                if(ModManager.safeFile.modSettings.ShouldPredict(compensationFactor)) {
                    Vector3 estimatedPos = position;
                    Vector3 estimatedRotation = rotation;

                    estimatedPos += velocity * compensationFactor;
                    estimatedRotation += angularVelocity * compensationFactor;

                    position = estimatedPos;
                    rotation = estimatedRotation;
                }

                itemNetworkData.Apply(this);
                itemNetworkData.PositionChanged();

                itemNetworkData.ApplyPositionToItem();
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(ModManager.serverInstance.items.ContainsKey(itemId)) {
                ItemNetworkData ind = ModManager.serverInstance.items[itemId];

                ind.Apply(this);

                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
