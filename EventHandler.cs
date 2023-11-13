using AMP.Extension;
using AMP.GameInteraction;
using AMP.Network.Client;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using AMP.SupportFunctions;
using AMP.Threading;
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
            EventManager.onLevelLoad           += EventManager_onLevelLoad;
            EventManager.onLevelUnload         += EventManager_onLevelUnload;
            EventManager.onItemSpawn           += EventManager_onItemSpawn;
            EventManager.onCreatureSpawn       += EventManager_onCreatureSpawn;
            EventManager.onCreatureAttacking   += EventManager_onCreatureAttacking;
            EventManager.OnSpellUsed           += EventManager_OnSpellUsed;
            EventManager.onPossess             += EventManager_onPossess;
            EventManager.OnPlayerPrefabSpawned += EventManager_OnPlayerSpawned;
            EventManager.OnItemBrokenEnd       += EventManager_OnItemBrokenEnd;
            EventManager.onCreatureKill        += EventManager_onCreatureKill;

            Catalog.gameData.deathSlowMoRatio = 1f;
            registered = true;
        }

        public static void UnRegisterGlobalEvents() {
            if(!registered) return;
            EventManager.onLevelLoad           -= EventManager_onLevelLoad;
            EventManager.onLevelUnload         -= EventManager_onLevelUnload;
            EventManager.onItemSpawn           -= EventManager_onItemSpawn;
            EventManager.onCreatureSpawn       -= EventManager_onCreatureSpawn;
            EventManager.onCreatureAttacking   -= EventManager_onCreatureAttacking;
            EventManager.OnSpellUsed           -= EventManager_OnSpellUsed;
            EventManager.onPossess             -= EventManager_onPossess;
            EventManager.OnPlayerPrefabSpawned -= EventManager_OnPlayerSpawned;
            EventManager.OnItemBrokenEnd       -= EventManager_OnItemBrokenEnd;
            EventManager.onCreatureKill        -= EventManager_onCreatureKill;

            Catalog.gameData.deathSlowMoRatio = 0.05f;
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
                ModManager.clientInstance.allowTransmission = false;
            }

            string currentLevel, currentMode;
            Dictionary<string, string> options = new Dictionary<string, string>();

            bool levelInfoSuccess;
            if(eventTime == EventTime.OnStart) {
                levelInfoSuccess = LevelInfo.ReadLevelInfo(levelData, out currentLevel, out currentMode);
                if(!levelInfoSuccess) return;
                LevelFunc.SetRespawning(true, levelData.GetMode());
            } else {
                levelInfoSuccess = LevelInfo.ReadLevelInfo(out currentLevel, out currentMode, out options);
                if(!levelInfoSuccess) return;
                LevelFunc.SetRespawning(true, levelData.GetMode());
            }

            new LevelChangePacket(currentLevel, currentMode, options, eventTime).SendToServerReliable();
        }

        private static void EventManager_onLevelUnload(LevelData levelData, EventTime eventTime) {
            ModManager.clientInstance.allowTransmission = false;
        }

        private static void EventManager_onItemSpawn(Item item) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync     == null) return;
            if(!ModManager.clientInstance.allowTransmission) return;

            //ModManager.clientSync.SyncItemIfNotAlready(item);
            Dispatcher.Enqueue(() => {
                ModManager.clientSync.skipRespawning = true;
                ModManager.clientSync.synchronizationThreadWait = 0f;
            });
        }

        private static void EventManager_onCreatureKill(Creature creature, Player player, CollisionInstance collisionInstance, EventTime eventTime) {
            // TODO: Remove once the game fixes it (or maybe its caused by the mp mod... dunno...)
            if(eventTime == EventTime.OnStart) {
                if(creature != null && creature.brain != null && creature.brain.instance != null) {
                    BrainModuleDetection bmd = creature.brain.instance.GetModule<BrainModuleDetection>(false);
                    if(bmd != null && bmd.defenseCollider == null) {
                        bmd.Load(creature);
                    }
                }
            }
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
                
                string clip = cnd.creature.GetAttackAnimation();
                if(clip != null && clip.Length > 0) new CreatureAnimationPacket(cnd.networkedId, clip).SendToServerReliable();
            }
        }

        private static void EventManager_OnSpellUsed(string spellId, Creature creature, Side side) {
            NetworkCreature networkCreature = creature.GetComponent<NetworkCreature>();
            if(networkCreature != null) {
                networkCreature.OnSpellUsed(spellId, side);
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
