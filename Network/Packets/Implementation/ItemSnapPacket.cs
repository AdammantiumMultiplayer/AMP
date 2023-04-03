using AMP.Data;
using AMP.Datatypes;
using AMP.Logging;
using AMP.Network.Data.Sync;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using ThunderRoad;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_SNAPPING_SNAP)]
    public class ItemSnapPacket : NetPacket {
        [SyncedVar] public long itemId;
        [SyncedVar] public long holderNetworkId;
        [SyncedVar] public byte drawSlot;
        [SyncedVar] public byte holdingIndex;
        [SyncedVar] public byte holdingSide;
        [SyncedVar] public byte holderType;

        public ItemSnapPacket() { }

        public ItemSnapPacket(long itemId, long holderNetworkId, byte drawSlot, byte holdingIndex, byte holdingSide, ItemHolderType holderType) {
            this.itemId           = itemId;
            this.holderNetworkId  = holderNetworkId;
            this.drawSlot         = drawSlot;
            this.holdingIndex     = holdingIndex;
            this.holdingSide      = holdingSide;
            this.holderType       = (byte) holderType;
        }

        public ItemSnapPacket(ItemNetworkData ind) 
            : this( itemId:           ind.networkedId
                  , holderNetworkId:  ind.holderNetworkId
                  , drawSlot:         (byte) ind.equipmentSlot
                  , holdingIndex:     ind.holdingIndex
                  , holdingSide:      (byte) ind.holdingSide
                  , holderType:       ind.holderType
                  ){

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                ItemNetworkData ind = ModManager.clientSync.syncData.items[itemId];

                ind.Apply(this);
                ind.UpdateHoldState();
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            if(itemId > 0 && ModManager.serverInstance.items.ContainsKey(itemId)) {
                ItemNetworkData ind = ModManager.serverInstance.items[itemId];

                ind.Apply(this);

                Log.Debug(Defines.SERVER, $"Snapped item {ind.dataId} to {ind.holderNetworkId} to {(ind.equipmentSlot == Holder.DrawSlot.None ? "hand " + ind.holdingSide : "slot " + ind.equipmentSlot)}.");
                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
