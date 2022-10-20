using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_IMBUE)]
    public class ItemImbuePacket : NetPacket {
        [SyncedVar]       public long   itemId;
        [SyncedVar]       public string type;
        [SyncedVar]       public byte   index;
        [SyncedVar(true)] public float  amount;
    }
}
