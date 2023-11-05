using AMP.Datatypes;
using AMP.Network.Data;
using AMP.Network.Helper;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using ThunderRoad;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.MAGIC_UPDATE)]
    internal class MagicUpdatePacket : AMPPacket {
        [SyncedVar]       public byte   handIndex;
        [SyncedVar]       public long   casterNetworkId;
        [SyncedVar]       public byte   casterType;
        [SyncedVar(true)] public float  currentCharge;

        public MagicUpdatePacket() { }
        
        public MagicUpdatePacket(byte handIndex, long casterNetworkId, ItemHolderType casterType, float currentCharge) {
            this.handIndex       = handIndex;
            this.casterNetworkId = casterNetworkId;
            this.casterType      = (byte) casterType;
            this.currentCharge   = currentCharge;
        }

        public override bool ProcessClient(NetamiteClient client) {
            Creature c = SyncFunc.GetCreature((ItemHolderType) casterType, casterNetworkId);
            if(c != null) {
                SpellCaster caster = c.GetHand((Side) handIndex).caster;

                if(caster != null && caster.spellInstance != null) {
                    Dispatcher.Enqueue(() => {
                        ((SpellCastCharge) caster.spellInstance).currentCharge = this.currentCharge;
                    });
                }
            }
            return base.ProcessClient(client);
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            server.SendToAllExcept(this, client.ClientId);
            return true;
        }
    }
}
