using AMP.Data;
using AMP.Datatypes;
using AMP.Extension;
using AMP.GameInteraction;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Helper;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.MAGIC_CHARGE)]
    internal class MagicChargePacket : AMPPacket {
        [SyncedVar]       public byte   handIndex;
        [SyncedVar]       public int    casterNetworkId;
        [SyncedVar]       public byte   casterType;
        [SyncedVar(true)] public float  currentCharge;
        [SyncedVar(true)] public Vector3 direction;

        public MagicChargePacket() { }
        
        public MagicChargePacket(byte handIndex, int casterNetworkId, ItemHolderType casterType, float currentCharge, Vector3 direction) {
            this.handIndex       = handIndex;
            this.casterNetworkId = casterNetworkId;
            this.casterType      = (byte) casterType;
            this.currentCharge   = currentCharge;
            this.direction       = direction;
        }

        public override bool ProcessClient(NetamiteClient client) {
            Creature c = SyncFunc.GetCreature((ItemHolderType) casterType, casterNetworkId);
            if(c != null) {
                SpellCaster caster;
                if(handIndex == byte.MaxValue) { // Merged Spell
                    caster = c.GetHand(Side.Left).caster;
                } else {
                    caster = c.GetHand((Side)handIndex).caster;
                }

                if(caster != null && caster.spellInstance != null) {
                    Dispatcher.Enqueue(() => {
                        if(handIndex == byte.MaxValue) {
                            // Loads and starts the merge if needed (RemoteSpellMerge), the merge itself runs every frame in NetworkPlayerCreature
                            if(!RemoteSpellMerge.StartMerge(caster.mana)) return;
                            caster.mana.mergeInstance.currentCharge = currentCharge;
                        } else {
                            SpellCastCharge scc = (SpellCastCharge) caster.spellInstance;
                            scc.currentCharge = this.currentCharge;
                        }
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
