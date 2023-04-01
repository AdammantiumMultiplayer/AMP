using AMP.Network.Data.Sync;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_DESPAWN)]
    public class CreatureDepawnPacket : NetPacket {
        [SyncedVar] public long creatureId;

        public CreatureDepawnPacket() { }

        public CreatureDepawnPacket(long creatureId) {
            this.creatureId = creatureId;
        }

        public CreatureDepawnPacket(CreatureNetworkData cnd) 
            : this(creatureId: cnd.networkedId
                  ) {
            
        }
    }
}
