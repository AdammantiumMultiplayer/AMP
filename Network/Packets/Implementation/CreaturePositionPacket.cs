using AMP.Network.Data.Sync;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
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
    }
}
