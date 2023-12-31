using AMP.Data;
using AMP.Datatypes;
using AMP.Logging;
using AMP.Network.Helper;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using ThunderRoad;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CHANGE_FACTION)]
    public class ChangeFactionPacket : AMPPacket {
        [SyncedVar] public int  networkId;
        [SyncedVar] public ItemHolderType creatureType;
        [SyncedVar] public int  factionId;

        public ChangeFactionPacket() {}

        public ChangeFactionPacket(int networkId, ItemHolderType creatureType, int factionId) {
            this.networkId = networkId;
            this.creatureType = creatureType;
            this.factionId = factionId;
        }

        public override bool ProcessClient(NetamiteClient client) {
            Creature c = SyncFunc.GetCreature(creatureType, networkId);
            if(c != null) {
                Dispatcher.Enqueue(() => {
                    c.SetFaction(factionId);
                });
            }

            return true;
        }
    }
}
