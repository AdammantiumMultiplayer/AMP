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
    [PacketDefinition((byte) PacketType.MAGIC_CHARGE)]
    internal class MagicChargePacket : AMPPacket {
        [SyncedVar]       public byte   handIndex;
        [SyncedVar]       public int    casterNetworkId;
        [SyncedVar]       public byte   casterType;
        [SyncedVar(true)] public float  currentCharge;

        public MagicChargePacket() { }
        
        public MagicChargePacket(byte handIndex, int casterNetworkId, ItemHolderType casterType, float currentCharge) {
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
                        SpellCastCharge scc = (SpellCastCharge)caster.spellInstance;
                        scc.currentCharge = this.currentCharge;
                        //scc.UpdateCaster();
                        //scc.FixedUpdateCaster();
                        //caster.ManaUpdate();
                        //scc.UpdateSpray(); // TODO: Fix so the particles will show up
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
