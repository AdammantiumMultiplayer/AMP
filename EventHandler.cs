using AMP.Data;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP {
    public class EventHandler : MonoBehaviour {

        void Start() {
            //WaveSpawner.OnWaveSpawnerEnabledEvent.AddListener((spawner) => {
            //    if(ModManager.clientSync != null) {
            //        foreach(PlayerSync ps in ModManager.clientSync.syncData.players.Values) {
            //            if(ps.creature == null) continue;
            //            spawner.RemoveFromWave(ps.creature);
            //        }
            //    }
            //
            //    Debug.Log("A " + spawner.creatureQueue.Count);
            //    Debug.Log("B " + spawner.spawnedCreatures.Count);
            //    Debug.Log("C " + spawner.waveData);
            //    Debug.Log("D " + spawner.waveData.factions.Count);
            //    foreach(WaveData.WaveFaction f in spawner.waveData.factions) {
            //        Debug.Log(f.factionID + " - " + f.factionName + " - " + f.factionMaxAlive);
            //    }
            //});
            //WaveSpawner.OnWaveSpawnerStartRunningEvent.AddListener((spawner) => {
            //    if(ModManager.clientSync != null) {
            //        foreach(PlayerSync ps in ModManager.clientSync.syncData.players.Values) {
            //            if(ps.creature == null) continue;
            //            spawner.RemoveFromWave(ps.creature);
            //        }
            //    }
            //
            //    Debug.Log("A " + spawner.creatureQueue.Count);
            //    Debug.Log("B " + spawner.spawnedCreatures.Count);
            //    Debug.Log("C " + spawner.waveData);
            //    Debug.Log("D " + spawner.waveData.factions.Count);
            //    foreach(WaveData.WaveFaction f in spawner.waveData.factions) {
            //        Debug.Log(f.factionID + " - " + f.factionName + " - " + f.factionMaxAlive);
            //    }
            //});

            EventManager.onLevelLoad += (levelData, eventTime) => {
                if(eventTime == EventTime.OnStart) {
                    if(ModManager.clientInstance == null) return;

                    ModManager.clientInstance.tcp.SendPacket(PacketWriter.LoadLevel(levelData.id));
                }else if(eventTime == EventTime.OnEnd) {
                    //if(levelData.id != "Home") return;
                    //
                    //UIMap map = FindObjectOfType<UIMap>();
                    //GameObject meep = Instantiate(GameObject.Find("WorldmapBoard"));
                    //meep.isStatic = false;
                    //map.transform.position = Player.local.transform.position + Vector3.right * 3;
                    //meep.transform.position = map.transform.position;
                }
            };

            EventManager.onCreatureSpawn += (creature) => {
                if(ModManager.clientInstance == null) return;
                if(ModManager.clientSync == null) return;
                if(!creature.pooled) return;

                foreach(CreatureSync cs in ModManager.clientSync.syncData.creatures.Values) {
                    if(cs.clientsideCreature == creature) return; // If creature already exists, just exit
                }

                // Check if the creature aims for the player
                bool isPlayerTheTaget = creature.brain.currentTarget == null ? false : creature.brain.currentTarget == Player.currentCreature;

                int currentCreatureId = ModManager.clientSync.syncData.currentClientCreatureId++;
                CreatureSync creatureSync = new CreatureSync() {
                    clientsideCreature = creature,
                    clientsideId = currentCreatureId,

                    clientTarget = isPlayerTheTaget ? ModManager.clientInstance.myClientId : 0, // If the player is the target, let the server know it

                    creatureId = creature.creatureId,
                    containerID = creature.container.containerID,
                    factionId = creature.factionId,
                };

                ModManager.clientInstance.tcp.SendPacket(creatureSync.CreateSpawnPacket());
                ModManager.clientSync.syncData.creatures.Add(-currentCreatureId, creatureSync);

                creature.OnDamageEvent += (collisionInstance) => {
                    if(creatureSync.health > 0) {
                        ModManager.clientInstance.udp.SendPacket(creatureSync.CreateHealthPacket());
                    } else {
                        ModManager.clientInstance.tcp.SendPacket(creatureSync.CreateHealthPacket());
                    }
                };

                creature.OnDespawnEvent += (eventTime) => {
                    if(creatureSync.networkedId > 0 && creatureSync.clientsideId > 0) {
                        ModManager.clientInstance.tcp.SendPacket(creatureSync.CreateDespawnPacket());
                        Log.Debug($"[Client] Creature {creatureSync.creatureId} ({creatureSync.networkedId}) is despawned.");

                        ModManager.clientSync.syncData.creatures.Remove(creatureSync.networkedId);

                        creatureSync.networkedId = 0;
                    }
                };

                creature.brain.OnStateChangeEvent += (state) => {
                    // TODO: Sync state if necessary
                };

                Log.Debug($"[Client] Creature {creature.creatureId} has been spawned.");
            };

            EventManager.onItemSpawn += (item) => {
                if(Config.ignoredTypes.Contains(item.data.type)) return;
                if(ModManager.clientInstance == null) return;
                if(ModManager.clientSync == null) return;

                //ModManager.clientSync.SyncItemIfNotAlready(item);
            };

            EventManager.onItemEquip += (item) => {
                if(Config.ignoredTypes.Contains(item.data.type)) return;
                if(ModManager.clientInstance == null) return;
                if(ModManager.clientSync == null) return;

                //ModManager.clientSync.SyncItemIfNotAlready(item);

                //Log.Debug(item);

                // TODO: Sync equipment
            };
        }

        public static void AddEventsToItem(ItemSync itemSync) {
            if(itemSync.clientsideItem == null) return;
            if(itemSync.registeredEvents) return;

            itemSync.clientsideItem.OnDespawnEvent += (item) => {
                if(itemSync.networkedId > 0 && itemSync.clientsideId > 0) { // Check if the item is already networked and is in ownership of the client
                    ModManager.clientInstance.tcp.SendPacket(itemSync.DespawnPacket());
                    Log.Debug($"[Client] Item {itemSync.dataId} ({itemSync.networkedId}) is despawned.");

                    ModManager.clientSync.syncData.items.Remove(itemSync.networkedId);

                    itemSync.networkedId = 0;
                }
            };

            itemSync.clientsideItem.OnGrabEvent += (handle, ragdollHand) => {
                if(itemSync.clientsideId > 0) return;
                
                ModManager.clientInstance.tcp.SendPacket(itemSync.TakeOwnershipPacket());
            };

            itemSync.clientsideItem.OnTelekinesisGrabEvent += (handle, teleGrabber) => {
                if(itemSync.clientsideId > 0) return;

                ModManager.clientInstance.tcp.SendPacket(itemSync.TakeOwnershipPacket());
            };

            itemSync.clientsideItem.OnSnapEvent += (holder) => {
                if(itemSync.creatureNetworkId > 0) return;

                if(itemSync.clientsideId <= 0) ModManager.clientInstance.tcp.SendPacket(itemSync.TakeOwnershipPacket());

                itemSync.UpdateFromHolder();

                Log.Debug($"[Client] Snapped item {itemSync.dataId} to {itemSync.creatureNetworkId} with slot {itemSync.drawSlot}.");

                if(itemSync.creatureNetworkId > 0) {
                    ModManager.clientInstance.tcp.SendPacket(itemSync.SnapItemPacket());
                }
            };

            itemSync.clientsideItem.OnUnSnapEvent += (holder) => {
                if(itemSync.creatureNetworkId <= 0) return;

                if(itemSync.clientsideId <= 0) ModManager.clientInstance.tcp.SendPacket(itemSync.TakeOwnershipPacket());

                ModManager.clientInstance.tcp.SendPacket(itemSync.UnSnapItemPacket());
            };

            if(itemSync.clientsideItem.holder != null) {
                if(itemSync.clientsideId <= 0) ModManager.clientInstance.tcp.SendPacket(itemSync.TakeOwnershipPacket());

                itemSync.UpdateFromHolder();

                ModManager.clientInstance.tcp.SendPacket(itemSync.SnapItemPacket());
            }

            itemSync.registeredEvents = true;
        }
    }
}
