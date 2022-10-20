using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_HEALTH_SET)]
    public class CreatureHealthSetPacket : NetPacket {
        [SyncedVar] public long  creatureId;
        [SyncedVar] public float health;

        public CreatureHealthSetPacket(long creatureId, float health) {
            this.creatureId = creatureId;
            this.health     = health;
        }
    }
}
