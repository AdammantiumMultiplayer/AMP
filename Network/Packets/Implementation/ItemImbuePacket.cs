using AMP.Network.Data;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_IMBUE)]
    public class ItemImbuePacket : AMPPacket {
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

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                Dispatcher.Enqueue(() => {
                    ModManager.clientSync.syncData.items[itemId].Apply(this);
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            server.SendToAllExcept(this, client.ClientId); // Just forward them atm
            return true;
        }
    }
}
