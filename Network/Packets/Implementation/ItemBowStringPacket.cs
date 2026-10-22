using AMP.Network.Data;
using AMP.Network.Data.Sync;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    /// <summary>
    /// How far the string of a bow is drawn (0 = rest, 1 = fully drawn), see NetworkBowString
    /// </summary>
    [PacketDefinition(false, (byte) PacketType.ITEM_BOW_STRING)]
    public class ItemBowStringPacket : AMPPacket {
        [SyncedVar]       public int   itemId;
        [SyncedVar(true)] public float pullRatio;

        public ItemBowStringPacket() { }

        public ItemBowStringPacket(int itemId, float pullRatio) {
            this.itemId    = itemId;
            this.pullRatio = pullRatio;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                // Gets applied every frame by NetworkBowString
                ModManager.clientSync.syncData.items[itemId].bowPullRatio = pullRatio;
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(itemId > 0 && ModManager.serverInstance.items.ContainsKey(itemId)) {
                int owner;
                if(!ModManager.serverInstance.item_owner.TryGetValue(itemId, out owner) || owner != client.ClientId) return true;

                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
