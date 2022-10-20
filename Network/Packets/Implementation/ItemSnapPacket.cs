using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_SNAPPING_SNAP)]
    public class ItemSnapPacket : NetPacket {
        [SyncedVar] public long itemId;
        [SyncedVar] public long holderCreatureId;
        [SyncedVar] public byte drawSlot;
        [SyncedVar] public byte holdingSide;
        [SyncedVar] public bool holderIsPlayer;
    }
}
