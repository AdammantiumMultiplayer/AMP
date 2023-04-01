using AMP.Network.Data.Sync;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_DESPAWN)]
    public class ItemDespawnPacket : NetPacket {
        [SyncedVar] public long itemId;

        public ItemDespawnPacket() { }

        public ItemDespawnPacket(long itemId) {
            this.itemId = itemId;
        }

        public ItemDespawnPacket(ItemNetworkData ind) 
            : this(itemId: ind.networkedId){

        }
    }
}
