using AMP.Data;
using AMP.Datatypes;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Helper;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using System;
using System.Reflection;
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
                            if(caster.mana.mergeInstance == null) {

                                // Load the spell from the current player model
                                if(caster.spellInstance != null && caster.spellInstance != null) {
                                    foreach(ContainerData.Content content in Player.currentCreature.container.contents) {
                                        ItemModuleSpell module = content.itemData.GetModule<ItemModuleSpell>();
                                        if(module != null && module.spellData is SpellMergeData) {
                                            SpellMergeData spellMergeData = module.spellData as SpellMergeData;
                                            if((spellMergeData.leftSpellId == caster.spellInstance.id && spellMergeData.rightSpellId == caster.spellInstance.id) || (spellMergeData.rightSpellId == caster.spellInstance.id && spellMergeData.leftSpellId == caster.spellInstance.id)) {
                                                caster.mana.mergeData = spellMergeData;
                                                caster.mana.mergeInstance = caster.mana.mergeData.Clone() as SpellMergeData;
                                                caster.mana.mergeInstance.Load(caster.mana);
                                                caster.mana.mergeCastLoaded = true;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if(caster.mana.mergeInstance == null) {
                                    Log.Err(Defines.CLIENT, "Unable to merge spell " + caster.spellInstance.id);
                                    return;
                                }

                                // This doesnt work as haptic feedback is hardcoded and crashing
                                //caster.mana.mergeInstance.Merge(true);

                                EffectData ed = GetFieldValue<EffectData>(caster.mana.mergeInstance, "chargeEffectData");
                                EffectInstance chargeEffectCreation = ed.Spawn(caster.mana.mergePoint);
                                chargeEffectCreation.Play();
                                chargeEffectCreation.SetIntensity(0f);
                                SetFieldValue(caster.mana.mergeInstance, "chargeEffect", chargeEffectCreation);
                                chargeEffectCreation.SetIntensity(currentCharge);

                                caster.mana.casterLeft.Fire(false);
                                caster.mana.casterRight.Fire(false);
                                return;
                            }

                            EffectInstance chargeEffect = GetFieldValue<EffectInstance>(caster.mana.mergeInstance, "chargeEffect");
                            chargeEffect?.SetIntensity(currentCharge);
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



        private static System.Object GetFieldValue(System.Object obj, string name) {
            if(obj == null) { return null; }

            Type type = obj.GetType();

            foreach(string part in name.Split('.')) {
                FieldInfo info = type.GetField(part, BindingFlags.Instance | BindingFlags.NonPublic);
                if(info == null) { return null; }

                obj = info.GetValue(obj);
            }
            return obj;
        }

        internal static T GetFieldValue<T>(System.Object obj, string name) {
            System.Object retval = GetFieldValue(obj, name);
            if(retval == null) { return default(T); }

            // throws InvalidCastException if types are incompatible
            return (T)retval;
        }

        internal static void SetFieldValue<T>(System.Object obj, string name, T val) {
            if(obj == null) { return; }

            foreach(string part in name.Split('.')) {
                Type type = obj.GetType();
                FieldInfo info = type.GetField(part, BindingFlags.Instance | BindingFlags.NonPublic);
                if(info == null) { return; }

                info.SetValue(obj, val);
                return;
            }
        }
    }
}
