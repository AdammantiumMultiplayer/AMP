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
    [PacketDefinition((byte) PacketType.ITEM_SNAPPING_SNAP)]
    public class ItemSnapPacket : AMPPacket {
        [SyncedVar] public int  itemId;
        [SyncedVar] public ItemHoldingState[] itemHoldingStates;

        public ItemSnapPacket() { }

        public ItemSnapPacket(int itemId, ItemHoldingState[] itemHoldingStates) {
            this.itemId            = itemId;
            this.itemHoldingStates = itemHoldingStates;
        }

        public ItemSnapPacket(ItemNetworkData ind) 
            : this( itemId:           ind.networkedId
                  , itemHoldingStates:ind.holdingStates
                  ){

        }

        public override bool ProcessClient(NetamiteClient client) {
            Dispatcher.Enqueue(() => {
                if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                    ItemNetworkData ind = ModManager.clientSync.syncData.items[itemId];

                    ind.Apply(this);
                    ind.UpdateHoldState();
                }
            });
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(itemId > 0 && ModManager.serverInstance.items.ContainsKey(itemId)) {
                ItemNetworkData ind = ModManager.serverInstance.items[itemId];

                ind.Apply(this);

                Log.Debug(Defines.SERVER, $"Snapped item {ind.dataId} to {ind.holdingStatesInfo}.");
                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
