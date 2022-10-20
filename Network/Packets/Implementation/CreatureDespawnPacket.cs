using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_DESPAWN)]
    public class CreatureDepawnPacket : NetPacket {
        [SyncedVar] public long creatureId;
    }
}
