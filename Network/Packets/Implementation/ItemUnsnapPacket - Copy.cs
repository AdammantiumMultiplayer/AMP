using AMP.Data;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_SLIDE)]
    public class ItemSlidePacket : AMPPacket {
        [SyncedVar]       public int itemId;
        [SyncedVar(true)] public float axisPosition;

        public ItemSlidePacket() { }

        public ItemSlidePacket(int itemId, float axisPosition) {
            this.itemId = itemId;
            this.axisPosition = axisPosition;
        }

        public ItemSlidePacket(ItemNetworkData ind) 
            : this(itemId:       ind.networkedId
                  ,axisPosition: ind.axisPosition) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                ItemNetworkData itemNetworkData = ModManager.clientSync.syncData.items[itemId];

                itemNetworkData.Apply(this);
                Dispatcher.Enqueue(() => {
                    itemNetworkData.UpdateSlidePos();
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(itemId > 0 && ModManager.serverInstance.items.ContainsKey(itemId)) {
                ItemNetworkData ind = ModManager.serverInstance.items[itemId];

                ind.Apply(this);

                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
