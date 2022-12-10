using AMP.Network.Data.Sync;
using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_HEALTH_SET)]
    public class CreatureHealthSetPacket : NetPacket {
        [SyncedVar] public long  creatureId;
        [SyncedVar] public float health;

        public CreatureHealthSetPacket() { }

        public CreatureHealthSetPacket(long creatureId, float health) {
            this.creatureId = creatureId;
            this.health     = health;
            if(this.health <= 0) this.health = -1;
        }

        public CreatureHealthSetPacket(CreatureNetworkData cnd)
            : this( creatureId: cnd.networkedId
                  , health:     cnd.health
                  ) {

        }
    }
}
