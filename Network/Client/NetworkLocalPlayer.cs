using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Data.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client {
    public class NetworkLocalPlayer : NetworkCreature {

        public static NetworkLocalPlayer Instance;

        void Awake() {
            creature = Player.currentCreature;

            Instance = this;

            //Log.Warn("INIT Local Player");

            RegisterEvents();
        }

        protected override ManagedLoops ManagedLoops => 0;

        public new bool IsOwning() {
            return true;
        }

        private bool registeredEvents = false;
        public new void RegisterEvents() {
            if(registeredEvents) return;


            foreach(Wearable w in creature.equipment.wearableSlots) {
                w.OnItemEquippedEvent += (item) => {
                    if(ModManager.clientInstance == null) return;
                    if(ModManager.clientSync == null) return;

                    ModManager.clientSync.ReadEquipment();
                    ModManager.clientSync.syncData.myPlayerData.CreateEquipmentPacket().SendToServerReliable();
                };
            }

            creature.OnKillEvent += (collisionInstance, eventTime) => {
                //TODO: Figure out a way to ressurect the player

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

            //Player.currentCreature.handLeft.caster.magicSource.GetComponentInChildren<Trigger>().callBack += (other, enter) => { Log.Warn(Player.currentCreature.handLeft.caster.spellInstance); };
            //Player.currentCreature.handRight.caster.magicSource.GetComponentInChildren<Trigger>().callBack += (other, enter) => { Log.Warn(Player.currentCreature.handRight.caster.spellInstance); };
            //Player.currentCreature.handLeft.caster.spellInstance

            RegisterGrabEvents();
        }
    }
}
