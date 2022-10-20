using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_DESPAWN)]
    public class ItemDespawnPacket : NetPacket {
        [SyncedVar] public long itemId;

        public ItemDespawnPacket(long itemId) {
            this.itemId = itemId;
        }
    }
}
