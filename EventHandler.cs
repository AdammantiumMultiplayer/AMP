using AMP.Extension;
using AMP.GameInteraction;
using AMP.Network.Client;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using AMP.SupportFunctions;
using AMP.Threading;
using AMP.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using ThunderRoad;
using UnityEngine;

namespace AMP {
    internal class EventHandler : MonoBehaviour {

        #region Global Event Registering
        private static bool registered = false;

        public static void RegisterGlobalEvents() {
            if(registered) return;
            EventManager.onLevelLoad           += EventManager_onLevelLoad;
            EventManager.onLevelUnload         += EventManager_onLevelUnload;
            EventManager.onItemSpawn           += EventManager_onItemSpawn;
            EventManager.onCreatureSpawn       += EventManager_onCreatureSpawn;
            EventManager.onCreatureAttacking   += EventManager_onCreatureAttacking;
            //EventManager.OnSpellUsed           += EventManager_OnSpellUsed;
            EventManager.onPossess             += EventManager_onPossess;
            EventManager.OnPlayerPrefabSpawned += EventManager_OnPlayerSpawned;
            EventManager.OnItemBrokenEnd       += EventManager_OnItemBrokenEnd;
            registered = true;
        }

        public static void UnRegisterGlobalEvents() {
            if(!registered) return;
            EventManager.onLevelLoad           -= EventManager_onLevelLoad;
            EventManager.onLevelUnload         -= EventManager_onLevelUnload;
            EventManager.onItemSpawn           -= EventManager_onItemSpawn;
            EventManager.onCreatureSpawn       -= EventManager_onCreatureSpawn;
            EventManager.onCreatureAttacking   -= EventManager_onCreatureAttacking;
            //EventManager.OnSpellUsed           -= EventManager_OnSpellUsed;
            EventManager.onPossess             -= EventManager_onPossess;
            EventManager.OnPlayerPrefabSpawned -= EventManager_OnPlayerSpawned;
            EventManager.OnItemBrokenEnd       -= EventManager_OnItemBrokenEnd;
            registered = false;
        }
        #endregion

        #region Global Event Handlers
        private static void EventManager_onLevelLoad(LevelData levelData, EventTime eventTime) {
            if(ModManager.clientInstance == null) return;

            if(eventTime == EventTime.OnEnd) {
                foreach(PlayerNetworkData playerSync in ModManager.clientSync.syncData.players.Values) {
                    playerSync.receivedPos = false;
                }

                LevelFunc.EnableRespawning();
            } else if(eventTime == EventTime.OnStart) {
                //foreach(PlayerNetworkData playerSync in ModManager.clientSync.syncData.players.Values) { // Will despawn all player creatures and respawn them after level has changed
                //    if(playerSync.creature == null) continue;
                //
                //    Creature c = playerSync.creature;
                //    playerSync.creature = null;
                //    playerSync.isSpawning = false;
                //    try {
                //        c.Despawn();
                //    }catch(Exception) { }
                //}
                ModManager.clientInstance.allowTransmission = false;
            }

            string currentLevel, currentMode;
            Dictionary<string, string> options = new Dictionary<string, string>();

            bool levelInfoSuccess;
            if(eventTime == EventTime.OnStart) {
                levelInfoSuccess = LevelInfo.ReadLevelInfo(levelData, out currentLevel, out currentMode);
                if(!levelInfoSuccess) return;
            } else {
                levelInfoSuccess = LevelInfo.ReadLevelInfo(out currentLevel, out currentMode, out options);
                if(!levelInfoSuccess) return;
            }

            new LevelChangePacket(currentLevel, currentMode, options, eventTime).SendToServerReliable();
        }

        private static void EventManager_onLevelUnload(LevelData levelData, EventTime eventTime) {
            //ModManager.clientInstance.allowTransmission = false;
        }

        private static void EventManager_onItemSpawn(Item item) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync     == null) return;
            if(!ModManager.clientInstance.allowTransmission) return;

            //ModManager.clientSync.SyncItemIfNotAlready(item);
            Dispatcher.Enqueue(() => {
                ModManager.clientSync.synchronizationThreadWait = 0f;
            });
        }

        private static void EventManager_onCreatureSpawn(Creature creature) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync     == null) return;
            if(!ModManager.clientInstance.allowTransmission) return;

            //ModManager.clientSync.SyncCreatureIfNotAlready(creature);
            Dispatcher.Enqueue(() => {
                ModManager.clientSync.synchronizationThreadWait = 0f;
            });
        }

        private static void EventManager_onCreatureAttacking(Creature attacker, Creature targetCreature, Transform targetTransform, BrainModuleAttack.AttackType type, BrainModuleAttack.AttackStage stage) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync     == null) return;
            //if(ModManager.safeFile.modSettings.useAdvancedNpcSyncing) return; // Always syncing the ragdoll, so no need for animations

            if(stage == BrainModuleAttack.AttackStage.WindUp) {
                CreatureNetworkData cnd = null;
                try {
                    cnd = ModManager.clientSync.syncData.creatures.First(entry => entry.Value.creature == attacker).Value;
                } catch(InvalidOperationException) { return; } // Creature is not synced

                if(cnd == null) return;
                if(cnd.networkedId <= 0) return;

                //AnimatorStateInfo animatorStateInfo = creatureSync.clientsideCreature.animator.GetCurrentAnimatorStateInfo(creatureSync.clientsideCreature.animator.layerCount - 1);

                new CreatureAnimationPacket(cnd.networkedId, cnd.creature.GetAttackAnimation()).SendToServerReliable();
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


        private static void EventManager_OnItemBrokenEnd(Breakable breakable, PhysicBody[] pieces) {
            if(breakable.LinkedItem != null) {
                NetworkItem networkItem = breakable.LinkedItem.GetComponent<NetworkItem>();
                if(networkItem != null) {
                    networkItem.OnBreak(breakable, pieces);
                }
            }
        }

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
            if(AreaManager.Instance == null) return;
            if(AreaManager.Instance.CurrentTree == null) return;
            if(AreaManager.Instance.CurrentTree.Count == 0) return;
            if(AreaManager.Instance.CurrentArea == AreaManager.Instance.CurrentTree[0]) return;

            AreaManager.Instance.CurrentTree[0].OnPlayerEnter(AreaManager.Instance.CurrentArea);
        }
    }
}
