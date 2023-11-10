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
    [PacketDefinition((byte) PacketType.ITEM_SNAPPING_UNSNAP)]
    public class ItemUnsnapPacket : AMPPacket {
        [SyncedVar] public int itemId;

        public ItemUnsnapPacket() { }

        public ItemUnsnapPacket(int itemId) {
            this.itemId = itemId;
        }

        public ItemUnsnapPacket(ItemNetworkData ind) 
            : this(itemId: ind.networkedId) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                ItemNetworkData itemNetworkData = ModManager.clientSync.syncData.items[itemId];

                itemNetworkData.Apply(this);
                Dispatcher.Enqueue(() => {
                    itemNetworkData.UpdateHoldState();
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(itemId > 0 && ModManager.serverInstance.items.ContainsKey(itemId)) {
                ItemNetworkData ind = ModManager.serverInstance.items[itemId];
                Log.Debug(Defines.SERVER, $"Unsnapped item {ind.dataId} from {ind.holderNetworkId}.");

                ind.Apply(this);

                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
