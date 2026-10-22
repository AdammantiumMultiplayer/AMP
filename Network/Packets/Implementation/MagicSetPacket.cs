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

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.MAGIC_SET)]
    internal class MagicSetPacket : AMPPacket {
        [SyncedVar] public string magicId;
        [SyncedVar] public byte   handIndex;
        [SyncedVar] public int    casterNetworkId;
        [SyncedVar] public byte   casterType;

        public MagicSetPacket() { }
        
        public MagicSetPacket(string magicId, byte handIndex, int casterNetworkId, ItemHolderType casterType) {
            this.magicId         = magicId;
            this.handIndex       = handIndex;
            this.casterNetworkId = casterNetworkId;
            this.casterType      = (byte) casterType;
        }

        public override bool ProcessClient(NetamiteClient client) {
            Creature c = SyncFunc.GetCreature((ItemHolderType) casterType, casterNetworkId);
            if(c != null) {
                if(magicId != null && magicId != "" && magicId.Length > 0) { // Started Magic
                    SpellCastData scd = Catalog.GetData<SpellData>(magicId) as SpellCastData;
                    if(scd == null) {
                        Log.Warn(Defines.CLIENT, $"Tried casting magic spell \"{magicId}\", but was not found. Using fallback spell \"{Config.FALLBACK_SPELL_ID}\" instead.");
                        scd = Catalog.GetData<SpellData>(Config.FALLBACK_SPELL_ID) as SpellCastData;
                    }

                    if(scd != null) {
                        SpellCaster caster = c.GetHand((Side) handIndex).caster;

                        if(caster != null) {
                            Dispatcher.Enqueue(() => {
                                // Spells stay loaded after casting (like on the casters side), only load it if it changed
                                if(caster.spellInstance == null || caster.spellInstance.id != scd.id) {
                                    // Loading another spell unloads the old one and with it the merge, do that first, the game can't do it for other players
                                    RemoteSpellMerge.UnloadMerge(caster.mana);
                                    caster.LoadSpell(scd);
                                }
                                caster.Fire(true);
                            });
                        }
                    }
                } else { // Stopped Magic
                    SpellCaster caster = c.GetHand((Side) handIndex).caster;

                    if(caster != null) {
                        Dispatcher.Enqueue(() => {
                            // Ends the merge like the game does, it stays loaded (things it started, like the gravity bubble, keep running)
                            RemoteSpellMerge.StopMerge(caster.mana);

                            // Only stops casting, the spell stays loaded like on the casters side. It gets unloaded when another spell gets loaded
                            caster.Fire(false);
                        });
                    }
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
