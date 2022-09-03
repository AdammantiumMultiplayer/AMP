using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.SupportFunctions;
using AMP.Threading;
using AMP.Useless;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ThunderRoad;
using UnityEngine;

namespace AMP {
    public class EventHandler : MonoBehaviour {

        #region Global Event Registering
        private static bool registered = false;
        public static void RegisterGlobalEvents() {
            if(registered) return;
            EventManager.onLevelLoad         += EventManager_onLevelLoad;
            EventManager.onItemSpawn         += EventManager_onItemSpawn;
            EventManager.onCreatureSpawn     += EventManager_onCreatureSpawn;
            EventManager.onCreatureAttacking += EventManager_onCreatureAttacking;
            EventManager.OnSpellUsed         += EventManager_OnSpellUsed;
            registered = true;
        }

        public static void UnRegisterGlobalEvents() {
            if(!registered) return;
            EventManager.onLevelLoad         -= EventManager_onLevelLoad;
            EventManager.onItemSpawn         -= EventManager_onItemSpawn;
            EventManager.onCreatureSpawn     -= EventManager_onCreatureSpawn;
            EventManager.onCreatureAttacking -= EventManager_onCreatureAttacking;
            EventManager.OnSpellUsed         -= EventManager_OnSpellUsed;
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

                if(ModManager.clientSync.syncData.serverlevel.Equals(currentLevel.ToLower())) return;

                PacketWriter.LoadLevel(currentLevel, currentMode, options).SendToServerReliable();


                // Try respawning all despawned players
                foreach(long clientId in ModManager.clientSync.syncData.players.Keys) {
                    ClientSync.SpawnPlayer(clientId); // Will just stop if the creature is still spawned
                }
            } else if(eventTime == EventTime.OnStart) {
                foreach(PlayerNetworkData playerSync in ModManager.clientSync.syncData.players.Values) { // Will despawn all player creatures and respawn them after level has changed
                    if(playerSync.creature == null) continue;

                    Creature c = playerSync.creature;
                    playerSync.creature = null;
                    try {
                        c.Despawn();
                    }catch(Exception) { }
                }
            }
        }

        private static void EventManager_onItemSpawn(Item item) {
            if(Config.ignoredTypes.Contains(item.data.type)) return;
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;

            ModManager.clientSync.SyncItemIfNotAlready(item);
        }

        private static void EventManager_onCreatureSpawn(Creature creature) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;
            if(!creature.pooled) return;

            foreach(Network.Data.Sync.CreatureNetworkData cs in ModManager.clientSync.syncData.creatures.Values) {
                if(cs.clientsideCreature == creature) return; // If creature already exists, just exit
            }
            foreach(PlayerNetworkData playerSync in ModManager.clientSync.syncData.players.Values) {
                if(playerSync.creature == creature) return;
            }

            // Check if the creature aims for the player
            bool isPlayerTheTaget = creature.brain.currentTarget == null ? false : creature.brain.currentTarget == Player.currentCreature;

            int currentCreatureId = ModManager.clientSync.syncData.currentClientCreatureId++;
            CreatureNetworkData creatureSync = new CreatureNetworkData() {
                clientsideCreature = creature,
                clientsideId = currentCreatureId,

                clientTarget = isPlayerTheTaget ? ModManager.clientInstance.myClientId : 0, // If the player is the target, let the server know it

                creatureId = creature.creatureId,
                containerID = creature.container.containerID,
                factionId = creature.factionId,

                maxHealth = creature.maxHealth,
                health = creature.currentHealth,

                height = creature.GetHeight(),

                equipment = creature.ReadWardrobe(),

                isSpawning = false,
            };

            Log.Debug($"[Client] Event: Creature {creature.creatureId} has been spawned.");

            ModManager.clientSync.syncData.creatures.Add(-currentCreatureId, creatureSync);
            creatureSync.CreateSpawnPacket().SendToServerReliable();

            creatureSync.StartNetworking();
        }

        private static void EventManager_onCreatureAttacking(Creature attacker, Creature targetCreature, Transform targetTransform, BrainModuleAttack.AttackType type, BrainModuleAttack.AttackStage stage) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;

            if(stage == BrainModuleAttack.AttackStage.WindUp) {
                Network.Data.Sync.CreatureNetworkData creatureSync = null;
                try {
                    creatureSync = ModManager.clientSync.syncData.creatures.First(entry => entry.Value.clientsideCreature == attacker).Value;
                } catch(InvalidOperationException) { return; } // Creature is not synced

                if(creatureSync == null) return;
                if(creatureSync.networkedId <= 0) return;

                AnimatorStateInfo animatorStateInfo = creatureSync.clientsideCreature.animator.GetCurrentAnimatorStateInfo(creatureSync.clientsideCreature.animator.layerCount - 1);

                PacketWriter.CreatureAnimation(creatureSync.networkedId, animatorStateInfo.fullPathHash, creatureSync.clientsideCreature.GetAttackAnimation()).SendToServerReliable();
            }
        }

        private static void EventManager_OnSpellUsed(string spellId) {
            Log.Warn(spellId);

            switch(spellId) {
                case "SlowTime":
                    Log.Warn(Time.timeScale);
                    // TODO: Find way to sync time properly, probably start a coroutine here that checks if the timeScale changed and stops itself when GameManager.slowMotionState = SlowMotionState.Disabled
                    break;

                default: break;
            }
        }
        #endregion
    }
}
