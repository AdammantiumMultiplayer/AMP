using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_OWNER)]
    public class CreatureOwnerPacket : NetPacket {
        [SyncedVar] public long creatureId;
        [SyncedVar] public bool owning;
    }
}
