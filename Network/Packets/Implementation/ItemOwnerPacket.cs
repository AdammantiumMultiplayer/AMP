using AMP.Network.Data;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_OWNER)]
    public class ItemOwnerPacket : AMPPacket {
        [SyncedVar] public int itemId;
        [SyncedVar] public bool owning;

        public ItemOwnerPacket() { }

        public ItemOwnerPacket(int itemId, bool owning) {
            this.itemId = itemId;
            this.owning = owning;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(owning && !ModManager.clientSync.syncData.owningItems.Contains(itemId)) ModManager.clientSync.syncData.owningItems.Add(itemId);
            if(!owning && ModManager.clientSync.syncData.owningItems.Contains(itemId)) ModManager.clientSync.syncData.owningItems.Remove(itemId);

            if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                Dispatcher.Enqueue(() => {
                    ModManager.clientSync.syncData.items[itemId].SetOwnership(owning);

                    if(owning) ModManager.clientSync.syncData.items[itemId].networkItem?.OnHoldStateChanged();
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(itemId > 0 && ModManager.serverInstance.items.ContainsKey(itemId)) {
                ModManager.serverInstance.UpdateItemOwner(ModManager.serverInstance.items[itemId], client);
            }
            return true;
        }
    }
}
