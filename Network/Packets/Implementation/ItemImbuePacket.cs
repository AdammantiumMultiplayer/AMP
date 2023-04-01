using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_IMBUE)]
    public class ItemImbuePacket : NetPacket {
        [SyncedVar]       public long   itemId;
        [SyncedVar]       public string type;
        [SyncedVar]       public byte   index;
        [SyncedVar(true)] public float  amount;

        public ItemImbuePacket() { }

        public ItemImbuePacket(long itemId, string type, byte index, float amount) {
            this.itemId = itemId;
            this.type   = type;
            this.index  = index;
            this.amount = amount;
        }

        public ItemImbuePacket(long itemId, string type, int index, float amount) 
            : this( itemId: itemId
                  , type:   type
                  , index:  (byte) index
                  , amount: amount) {
        }
    }
}
