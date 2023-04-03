using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;

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

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                ModManager.clientSync.syncData.items[itemId].SetOwnership(owning);

                ModManager.clientSync.syncData.items[itemId].networkItem?.OnHoldStateChanged();
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            if(itemId > 0 && ModManager.serverInstance.items.ContainsKey(itemId)) {
                ModManager.serverInstance.UpdateItemOwner(ModManager.serverInstance.items[itemId], client);
            }
            return true;
        }
    }
}
