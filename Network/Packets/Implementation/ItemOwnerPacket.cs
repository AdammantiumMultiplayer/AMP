﻿using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_OWNER)]
    public class ItemOwnerPacket : NetPacket {
        [SyncedVar] public long itemId;
        [SyncedVar] public bool owning;

        public ItemOwnerPacket() { }

        public ItemOwnerPacket(long itemId, bool owning) {
            this.itemId = itemId;
            this.owning = owning;
        }
    }
}
