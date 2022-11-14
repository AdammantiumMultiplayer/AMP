using AMP.Data;
using AMP.Extension;
using AMP.GameInteraction;
using AMP.Network.Client;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using AMP.SupportFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace AMP {
    internal class EventHandler : MonoBehaviour {

        #region Global Event Registering
        private static bool registered = false;
        public static void RegisterGlobalEvents() {
            if(registered) return;
            EventManager.onLevelLoad         += EventManager_onLevelLoad;
            //EventManager.onItemSpawn         += EventManager_onItemSpawn;
            //EventManager.onCreatureSpawn     += EventManager_onCreatureSpawn;
            EventManager.onCreatureAttacking += EventManager_onCreatureAttacking;
            EventManager.OnSpellUsed         += EventManager_OnSpellUsed;
            EventManager.onPossess           += EventManager_onPossess;
            EventManager.OnPlayerSpawned     += EventManager_OnPlayerSpawned;
            registered = true;
        }

        public static void UnRegisterGlobalEvents() {
            if(!registered) return;
            EventManager.onLevelLoad         -= EventManager_onLevelLoad;
            //EventManager.onItemSpawn         -= EventManager_onItemSpawn;
            //EventManager.onCreatureSpawn     -= EventManager_onCreatureSpawn;
            EventManager.onCreatureAttacking -= EventManager_onCreatureAttacking;
            EventManager.OnSpellUsed         -= EventManager_OnSpellUsed;
            EventManager.onPossess           -= EventManager_onPossess;
            EventManager.OnPlayerSpawned     -= EventManager_OnPlayerSpawned;
            registered = false;
        }
        #endregion

        #region Global Event Handlers
        private static void EventManager_onLevelLoad(LevelData levelData, EventTime eventTime) {
            if(eventTime == EventTime.OnEnd) {
                if(ModManager.clientInstance == null) return;

                string currentLevel = "", currentMode = "";
                Dictionary<string, string> options = new Dictionary<string, string>();

                bool levelInfoSuccess = LevelInfo.ReadLevelInfo(ref currentLevel, ref currentMode, ref options);

                if(!levelInfoSuccess) return;

                new LevelChangePacket(currentLevel, currentMode, options).SendToServerReliable();

                LevelFunc.EnableRespawning();
            } else if(eventTime == EventTime.OnStart) {
                foreach(PlayerNetworkData playerSync in ModManager.clientSync.syncData.players.Values) { // Will despawn all player creatures and respawn them after level has changed
                    if(playerSync.creature == null) continue;

                    Creature c = playerSync.creature;
                    playerSync.creature = null;
                    playerSync.isSpawning = false;
                    try {
                        c.Despawn();
                    }catch(Exception) { }
                }
                ModManager.clientInstance.allowTransmission = false;
            }
        }

        private static void EventManager_onItemSpawn(Item item) {
            if(Config.ignoredTypes.Contains(item.data.type)) return;
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;
            if(!ModManager.clientInstance.allowTransmission) return;

            ModManager.clientSync.SyncItemIfNotAlready(item);
        }

        private static void EventManager_onCreatureSpawn(Creature creature) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;
            if(!ModManager.clientInstance.allowTransmission) return;
            if(!creature.pooled) return;

            ModManager.clientSync.SyncCreatureIfNotAlready(creature);
        }

        private static void EventManager_onCreatureAttacking(Creature attacker, Creature targetCreature, Transform targetTransform, BrainModuleAttack.AttackType type, BrainModuleAttack.AttackStage stage) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;
            if(GameConfig.useAdvancedNpcSyncing) return; // Always syncing the ragdoll, so no need for animations

            if(stage == BrainModuleAttack.AttackStage.WindUp) {
                CreatureNetworkData cnd = null;
                try {
                    cnd = ModManager.clientSync.syncData.creatures.First(entry => entry.Value.creature == attacker).Value;
                } catch(InvalidOperationException) { return; } // Creature is not synced

                if(cnd == null) return;
                if(cnd.networkedId <= 0) return;

                //AnimatorStateInfo animatorStateInfo = creatureSync.clientsideCreature.animator.GetCurrentAnimatorStateInfo(creatureSync.clientsideCreature.animator.layerCount - 1);

                new  CreatureAnimationPacket(cnd.networkedId, cnd.creature.GetAttackAnimation()).SendToServerReliable();
            }
        }

        private static void EventManager_OnSpellUsed(string spellId) {
            // Log.Warn(spellId);

            switch(spellId) {
                case "SlowTime":
                    // Log.Warn(Time.timeScale);
                    // TODO: Find way to sync time properly, probably start a coroutine here that checks if the timeScale changed and stops itself when GameManager.slowMotionState = SlowMotionState.Disabled
                    break;

                default: break;
            }
        }
        #endregion

        private static void EventManager_onPossess(Creature creature, EventTime eventTime) {
            foreach(NetworkLocalPlayer nlp in FindObjectsOfType<NetworkLocalPlayer>()) {
                Destroy(nlp);
            }
            ModManager.clientSync.syncData.myPlayerData.creature = null;

            //if(ModManager.clientSync != null && creature != null) {
            //    NetworkLocalPlayer nlp = creature.gameObject.GetElseAddComponent<NetworkLocalPlayer>();
            //    nlp.creature = creature;
            //    ModManager.clientSync.syncData.myPlayerData.creature = creature;
            //    NetworkLocalPlayer.Instance.SendHealthPacket();
            //}
        }

        private static void EventManager_OnPlayerSpawned() {
            if(Level.current.dungeon != null) {
                if(Level.current.dungeon.rooms[0].isHidden) {
                    Level.current.dungeon.rooms[0].OnPlayerEnter();
                }
            }
        }
    }
}
