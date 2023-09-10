using AMP.Data;
using AMP.Events;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_DESPAWN)]
    public class ItemDespawnPacket : AMPPacket {
        [SyncedVar] public long itemId;

        public ItemDespawnPacket() { }

        public ItemDespawnPacket(long itemId) {
            this.itemId = itemId;
        }

        public ItemDespawnPacket(ItemNetworkData ind) 
            : this(itemId: ind.networkedId){

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                ItemNetworkData ind = ModManager.clientSync.syncData.items[itemId];

                if(ind.clientsideItem != null) {
                    Dispatcher.Enqueue(() => {
                        if(ind.clientsideItem != null) {
                            ind.clientsideItem.Despawn();
                        }
                    });
                }
                ModManager.clientSync.syncData.items.TryRemove(itemId, out _);
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(ModManager.serverInstance.items.ContainsKey(itemId)) {
                ItemNetworkData ind = ModManager.serverInstance.items[itemId];

                Log.Debug(Defines.SERVER, $"{client.ClientName} has despawned item {ind.dataId} ({ind.networkedId})");

                server.SendToAllExcept(this, client.ClientId);

                ModManager.serverInstance.items.TryRemove(itemId, out _);
                ModManager.serverInstance.item_owner.TryRemove(itemId, out _);

                ServerEvents.InvokeOnItemDespawned(ind, client);
            }
            return true;
        }
    }
}
