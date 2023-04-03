using AMP.Data;
using AMP.Events;
using AMP.Logging;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using System;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_DESPAWN)]
    public class ItemDespawnPacket : NetPacket {
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
                        ind.clientsideItem.Despawn();
                    });
                }
                ModManager.clientSync.syncData.items.TryRemove(itemId, out _);
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            if(ModManager.serverInstance.items.ContainsKey(itemId)) {
                ItemNetworkData ind = ModManager.serverInstance.items[itemId];

                Log.Debug(Defines.SERVER, $"{client.ClientName} has despawned item {ind.dataId} ({ind.networkedId})");

                server.SendToAllExcept(this, client.ClientId);

                ModManager.serverInstance.items.TryRemove(itemId, out _);
                ModManager.serverInstance.item_owner.TryRemove(itemId, out _);

                try { if(ServerEvents.OnItemDespawned != null) ServerEvents.OnItemDespawned.Invoke(ind, client); } catch(Exception e) { Log.Err(e); }
            }
            return true;
        }
    }
}
