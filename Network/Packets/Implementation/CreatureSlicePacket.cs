using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using ThunderRoad;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_SLICE)]
    public class CreatureSlicePacket : NetPacket {
        [SyncedVar] public long creatureId;
        [SyncedVar] public int  slicedPart;

        public CreatureSlicePacket() { }

        public CreatureSlicePacket(long creatureId, int slicedPart) {
            this.creatureId = creatureId;
            this.slicedPart = slicedPart;
        }

        public CreatureSlicePacket(long creatureId, RagdollPart.Type slicedPart)
            : this( creatureId: creatureId
                  , slicedPart: (int) slicedPart
                  ) {
        }
    }
}
