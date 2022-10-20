using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_SLICE)]
    public class CreatureSlicePacket : NetPacket {
        [SyncedVar] public long creatureId;
        [SyncedVar] public int  slicedPart;
    }
}
