using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using static ThunderRoad.HandPoseData;

namespace AMP.Network.Client {
    internal class NetworkLocalPlayer : NetworkCreature {

        internal static NetworkLocalPlayer Instance;

        void Awake() {
            creature = Player.currentCreature;

            Instance = this;

            //Log.Warn("INIT Local Player");

            RegisterEvents();
        }

        protected override ManagedLoops ManagedLoops => 0;

        internal override bool IsSending() {
            return true;
        }

        private bool registeredEvents = false;
        internal new void RegisterEvents() {
            if(registeredEvents) return;

            foreach(Wearable w in creature.equipment.wearableSlots) {
                w.OnItemEquippedEvent += (item) => {
                    if(ModManager.clientInstance == null) return;
                    if(ModManager.clientSync == null) return;

                    ModManager.clientSync.ReadEquipment();
                    new PlayerEquipmentPacket(ModManager.clientSync.syncData.myPlayerData).SendToServerReliable();
                };
            }

            creature.OnKillEvent += (collisionInstance, eventTime) => {
                // TODO: Figure out a way to ressurect the player

                if(eventTime == EventTime.OnEnd) return;

                //Thread t = new Thread(() => {
                //    Thread.Sleep(15000);
                //    
                //    Dispatcher.current.Enqueue(() => {
                //        Player.currentCreature.Resurrect(Player.currentCreature.maxHealth, null);
                //    });
                //});
                //t.Start();
            };

            creature.OnHealEvent += (heal, healer) => {
                SendHealthPacket();
            };
            creature.OnDamageEvent += (collisionInstance) => {
                SendHealthPacket();
            };
            creature.OnKillEvent += (collisionInstance, eventTime) => {
                if(eventTime == EventTime.OnStart) return;
                SendHealthPacket();
            };

            //Player.currentCreature.handLeft.caster.magicSource.GetComponentInChildren<Trigger>().callBack += (other, enter) => { Log.Warn(Player.currentCreature.handLeft.caster.spellInstance); };
            //Player.currentCreature.handRight.caster.magicSource.GetComponentInChildren<Trigger>().callBack += (other, enter) => { Log.Warn(Player.currentCreature.handRight.caster.spellInstance); };
            //Player.currentCreature.handLeft.caster.spellInstance

            RegisterGrabEvents();
        }
        
        internal void SendHealthPacket() {
            if(Player.currentCreature.isKilled) {
                ModManager.clientSync.syncData.myPlayerData.health = 0;
            } else {
                ModManager.clientSync.syncData.myPlayerData.health = Player.currentCreature.currentHealth / Player.currentCreature.maxHealth;
            }

            new PlayerHealthSetPacket(ModManager.clientSync.syncData.myPlayerData).SendToServerReliable();
        }

        protected override void ManagedOnDisable() {
            Destroy(this);
        }
    }
}
