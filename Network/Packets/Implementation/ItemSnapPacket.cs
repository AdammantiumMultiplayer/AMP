﻿using AMP.Network.Data.Sync;
using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_SNAPPING_SNAP)]
    public class ItemSnapPacket : NetPacket {
        [SyncedVar] public long itemId;
        [SyncedVar] public long holderCreatureId;
        [SyncedVar] public byte drawSlot;
        [SyncedVar] public byte holdingIndex;
        [SyncedVar] public byte holdingSide;
        [SyncedVar] public bool holderIsPlayer;

        public ItemSnapPacket() { }

        public ItemSnapPacket(long itemId, long holderCreatureId, byte drawSlot, byte holdingIndex, byte holdingSide, bool holderIsPlayer) {
            this.itemId           = itemId;
            this.holderCreatureId = holderCreatureId;
            this.drawSlot         = drawSlot;
            this.holdingIndex     = holdingIndex;
            this.holdingSide      = holdingSide;
            this.holderIsPlayer   = holderIsPlayer;
        }

        public ItemSnapPacket(ItemNetworkData ind) 
            : this( itemId:           ind.networkedId
                  , holderCreatureId: ind.creatureNetworkId
                  , drawSlot:         (byte) ind.drawSlot
                  , holdingIndex:     ind.holdingIndex
                  , holdingSide:      (byte) ind.holdingSide
                  , holderIsPlayer:   ind.holderIsPlayer
                  ){

        }
    }
}
