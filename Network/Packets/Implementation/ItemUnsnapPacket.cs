using AMP.Network.Data.Sync;
using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_SNAPPING_UNSNAP)]
    public class ItemUnsnapPacket : NetPacket {
        [SyncedVar] public long itemId;

        public ItemUnsnapPacket(long itemId) {
            this.itemId = itemId;
        }

        public ItemUnsnapPacket(ItemNetworkData ind) 
            : this(itemId: ind.networkedId) {

        }
    }
}
