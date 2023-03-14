using AMP.Datatypes;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_SNAPPING_SNAP)]
    public class ItemSnapPacket : NetPacket {
        [SyncedVar] public long itemId;
        [SyncedVar] public long holderNetworkId;
        [SyncedVar] public byte drawSlot;
        [SyncedVar] public byte holdingIndex;
        [SyncedVar] public byte holdingSide;
        [SyncedVar] public byte holderType;

        public ItemSnapPacket() { }

        public ItemSnapPacket(long itemId, long holderNetworkId, byte drawSlot, byte holdingIndex, byte holdingSide, ItemHolderType holderType) {
            this.itemId           = itemId;
            this.holderNetworkId  = holderNetworkId;
            this.drawSlot         = drawSlot;
            this.holdingIndex     = holdingIndex;
            this.holdingSide      = holdingSide;
            this.holderType       = (byte) holderType;
        }

        public ItemSnapPacket(ItemNetworkData ind) 
            : this( itemId:           ind.networkedId
                  , holderNetworkId:  ind.holderNetworkId
                  , drawSlot:         (byte) ind.equipmentSlot
                  , holdingIndex:     ind.holdingIndex
                  , holdingSide:      (byte) ind.holdingSide
                  , holderType:       ind.holderType
                  ){

        }
    }
}
