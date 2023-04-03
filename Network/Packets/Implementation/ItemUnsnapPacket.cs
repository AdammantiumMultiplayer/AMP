using AMP.Data;
using AMP.Logging;
using AMP.Network.Data.Sync;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_SNAPPING_UNSNAP)]
    public class ItemUnsnapPacket : NetPacket {
        [SyncedVar] public long itemId;

        public ItemUnsnapPacket() { }

        public ItemUnsnapPacket(long itemId) {
            this.itemId = itemId;
        }

        public ItemUnsnapPacket(ItemNetworkData ind) 
            : this(itemId: ind.networkedId) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                ItemNetworkData itemNetworkData = ModManager.clientSync.syncData.items[itemId];

                itemNetworkData.Apply(this);
                itemNetworkData.UpdateHoldState();
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
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
