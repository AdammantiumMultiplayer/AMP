using AMP.Network.Data.Sync;
using AMP.Network.Packets.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_POSITION)]
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
    }
}
