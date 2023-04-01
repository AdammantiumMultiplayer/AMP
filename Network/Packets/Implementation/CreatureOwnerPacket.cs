using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_OWNER)]
    public class CreatureOwnerPacket : NetPacket {
        [SyncedVar] public long creatureId;
        [SyncedVar] public bool owning;

        public CreatureOwnerPacket() { }

        public CreatureOwnerPacket(long creatureId, bool owning) {
            this.creatureId = creatureId;
            this.owning     = owning;
        }
    }
}
