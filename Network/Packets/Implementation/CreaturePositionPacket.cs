using AMP.Network.Data.Sync;
using AMP.Network.Packets.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_POSITION)]
    public class CreaturePositionPacket : NetPacket {
        [SyncedVar]       public long    creatureId;
        [SyncedVar]       public Vector3 position;
        [SyncedVar(true)] public float   rotationY;

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
    }
}
