using AMP.Data;
using AMP.Datatypes;
using AMP.Extension;
using AMP.GameInteraction;
using AMP.GameInteraction.Components;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Client.NetworkComponents.Parts;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using AMP.Network.Packets.Implementation;
using AMP.SupportFunctions;
using AMP.Threading;
using Koenigz.PerfectCulling;
using Netamite.Voice;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ThunderRoad;
using UnityEngine;
using Item = ThunderRoad.Item;

namespace AMP.Network.Client {
    internal class ClientSync : MonoBehaviour {
        internal SyncData syncData = new SyncData();

        private CancellationTokenSource threadCancel = new CancellationTokenSource();

        public void StartThreads () {
            StartCoroutine(BaseTickThread());
            StartCoroutine(PlayerTickThread());
            StartCoroutine(SynchronizationThread());

            ModManager.clientSync.UpdateVoiceChatState();
        }

        internal int packetsSentPerSec = 0;
        internal int packetsReceivedPerSec = 0;

        float time = 0f;
        void FixedUpdate() {
            if(ModManager.clientInstance.netclient.ClientId <= 0) return;

            time += Time.fixedDeltaTime;
            if(time > 1f) {
                time = 0f;

                // Packet Stats
                #if DEBUG_NETWORK
                packetsSentPerSec = (ModManager.clientInstance.tcp != null ? ModManager.clientInstance.tcp.GetPacketsSent() : 0)
                                  + (ModManager.clientInstance.udp != null ? ModManager.clientInstance.udp.GetPacketsSent() : 0);
                packetsReceivedPerSec = (ModManager.clientInstance.tcp != null ? ModManager.clientInstance.tcp.GetPacketsReceived() : 0)
                                      + (ModManager.clientInstance.udp != null ? ModManager.clientInstance.udp.GetPacketsReceived() : 0);
                #endif
            }
        }


        private bool playerDataSent = false;
        // Check player and item position about 10/sec
        IEnumerator PlayerTickThread() {
            float time = 0;
            while(!threadCancel.Token.IsCancellationRequested) {
                float wait = 1f / Config.PLAYER_TICK_RATE;
                if(wait > Time.time - time) wait -= Time.time - time;
                if(wait > 0) yield return new WaitForSeconds(wait);
                time = Time.time;

                if(ModManager.clientInstance.netclient.ClientId <= 0) continue;
                if(!ModManager.clientInstance.allowTransmission) continue;
                if(LevelInfo.IsLoading()) continue;
                if(threadCancel.Token.IsCancellationRequested) yield break;

                if(syncData.myPlayerData == null) syncData.myPlayerData = new PlayerNetworkData();
                if(Player.local != null && Player.currentCreature != null) {
                    if(syncData.myPlayerData.creature == null) {
                        syncData.myPlayerData.creature = Player.currentCreature;

                        syncData.myPlayerData.clientId = ModManager.clientInstance.netclient.ClientId;

                        syncData.myPlayerData.height = Player.currentCreature.GetHeight();
                        syncData.myPlayerData.creatureId = Player.currentCreature.creatureId;

                        syncData.myPlayerData.position = Player.currentCreature.transform.position;
                        syncData.myPlayerData.rotationY = Player.local.head.transform.eulerAngles.y;

                        new PlayerDataPacket(syncData.myPlayerData) {
                            uniqueId = SystemInfo.deviceUniqueIdentifier
                        }.SendToServerReliable();

                        CreatureEquipment.Read(syncData.myPlayerData);
                        new PlayerEquipmentPacket(syncData.myPlayerData).SendToServerReliable();

                        SendModListToServer();

                        Dispatcher.Enqueue(() => {
                            Player.currentCreature.gameObject.GetElseAddComponent<NetworkLocalPlayer>();
                        });

                        SendMyPos(true);

                        playerDataSent = true;
                    } else {
                        SendMyPos();
                    }
                } else {
                    Log.Err("No player creature found.");
                }
            }
        }

        // Check player and item position about 10/sec
        IEnumerator BaseTickThread() {
            float time = Time.time;
            while(!threadCancel.Token.IsCancellationRequested) {
                float wait = 1f / Config.BASE_TICK_RATE;
                if(wait > Time.time - time) wait -= Time.time - time;
                if(wait > 0) yield return new WaitForSeconds(wait);
                time = Time.time;

                if(ModManager.clientInstance.netclient.ClientId <= 0) continue;
                if(!ModManager.clientInstance.allowTransmission) continue;
                if(LevelInfo.IsLoading()) continue;
                if(!playerDataSent) continue;
                if(threadCancel.Token.IsCancellationRequested) yield break;

                try {
                    SendMovedItems();
                    SendMovedCreatures();
                    SendMovedEntities();
                }catch(Exception e) {
                    Log.Err(Defines.CLIENT, $"Error: {e}");
                }
            }
        }

        private void SendModListToServer() {
            string[] modlist = ThunderRoad.ModManager.loadedMods.Select(m => m.Name).ToArray();
            new ModListPacket(modlist).SendToServerReliable();
        }

        public float synchronizationThreadWait = 1f;
        public bool skipRespawning = false;
        IEnumerator SynchronizationThread() {
            while(!threadCancel.Token.IsCancellationRequested) {
                if(ModManager.clientInstance.allowTransmission) {
                    if(!skipRespawning) {
                        if(ModManager.clientInstance.allowTransmission) yield return TryRespawningItems();
                        if(ModManager.clientInstance.allowTransmission) yield return TryRespawningCreatures();
                    }

                    if(ModManager.clientInstance.allowTransmission) yield return CheckUnsynchedItems();
                    if(ModManager.clientInstance.allowTransmission) yield return CheckUnsynchedCreatures();

                    if(ModManager.clientInstance.allowTransmission) yield return TryRespawningPlayers();

                    if(ModManager.clientInstance.allowTransmission) yield return CheckEntities();

                    if(ModLoader._EnableVoiceChat && ModLoader._EnableProximityChat) yield return UpdateProximityChat();

                    //CleanupAreas();
                }
                synchronizationThreadWait = 1f;
                skipRespawning = false;

                while(synchronizationThreadWait > 0f) {
                    yield return new WaitForEndOfFrame();
                    synchronizationThreadWait -= Time.deltaTime;
                }
            }
        }

        private void CleanupAreas() {
            if(AreaManager.Instance != null) {
                foreach(SpawnableArea area in AreaManager.Instance.CurrentTree) {
                    if(area.SpawnedArea == null) continue;
                    area.SpawnedArea.items.RemoveAll(item => item == null);
                    area.SpawnedArea.creatures.RemoveAll(creature => creature == null);

                    List<ParticleSystem> systems = area.SpawnedArea.particlesSystem.ToList();
                    systems.RemoveAll(system => system == null || system.gameObject == null);
                    area.SpawnedArea.particlesSystem = systems.ToArray();

                    PerfectCullingVolume.AllVolumes.RemoveAll(volume => volume == null);
                }
            }
        }

        internal static void PrintAreaStuff(string part) {
            #if FULL_DEBUG
            Dispatcher.Enqueue(() => {
                int cnt_i = 0;
                int cnt_c = 0;
                if(AreaManager.Instance != null) {
                    foreach(SpawnableArea area in AreaManager.Instance.CurrentTree) {
                        cnt_i += area.SpawnedArea.items.Count(item => item == null);
                        cnt_c += area.SpawnedArea.creatures.Count(creature => creature == null);
                    }
                }
                Log.Debug("Despawn " + part + " | I: " + cnt_i + " / C: " + cnt_c);
            });
            #else
            return;
            #endif
        }

        internal void Stop() {
            threadCancel.Cancel();
            StopAllCoroutines();

            foreach(PlayerNetworkData ps in syncData.players.Values) {
                LeavePlayer(ps);
            }

            foreach(ItemNetworkData ind in syncData.items.Values    ) {
                if(ind.networkItem != null) {
                    Destroy(ind.networkItem);
                }
            }
            foreach(CreatureNetworkData cnd in syncData.creatures.Values) {
                if(cnd.networkCreature != null) {
                    cnd.SetOwnership(true);
                    Destroy(cnd.networkCreature);
                }
            }
            foreach(PlayerNetworkData pnd in syncData.players.Values) {
                if(pnd.networkCreature != null) {
                    if(pnd.creature != null) {
                        pnd.isSpawning = true; // To prevent the player from respawning
                        pnd.creature.Despawn();
                    }
                    Destroy(pnd.networkCreature);
                    ClientSync.PrintAreaStuff("Creature 2");
                }
            }
            TextDisplay.ClearText();
        }

        /// <summary>
        /// Checking if the player has any unsynched items that the server needs to know about
        /// </summary>
        internal IEnumerator CheckUnsynchedItems() {
            // Get all items that are not synched
            List<Item> unsynced_items = Item.allActive.Where(item => syncData.items.All(entry => !item.Equals(entry.Value.clientsideItem))).ToList();

            //Log.Debug(unfoundItemMode);
            foreach(Item item in unsynced_items) {
                if(item == null) continue;
                if(!item.enabled) continue;
                if(item.data == null) continue;
                if(item.isBrokenPiece) continue;

                if(item.holder != null && item.holder.parentItem != null) {
                    if(item.holder.parentItem.itemId.Equals("Quiver")) continue; // fix the duplication with quivers
                }

                if(Config.ignoredItems.Contains(item.data.id.ToLower())) continue;

                if(!Config.ignoredTypes.Contains(item.data.type)) {
                    Dispatcher.Enqueue(() => {
                        SyncItemIfNotAlready(item);
                    });
                    yield return new WaitForFixedUpdate();
                } else {
                    // Despawn all props until better syncing system, so we dont spam the other clients
                    //item.Despawn();
                }
            }

            // Shouldn't really be needed
            //List<ItemNetworkData> weird_stuff = syncData.items.Values.Where(ind => ind.networkedId > 0 && ind.clientsideId > 0 && ind.clientsideItem != null && ind.networkItem == null).ToList();
            //foreach(ItemNetworkData weird in weird_stuff) {
            //    Log.Debug(weird.dataId + " " + weird.clientsideId);
            //    weird.StartNetworking();
            //}
        }

        /// <summary>
        /// Tries to spawn or respawn items that are on the server but not in the clients game world
        /// </summary>
        internal IEnumerator TryRespawningItems() {
            List<ItemNetworkData> unspawned_items = syncData.items.Values.Where(ind => (ind.clientsideItem == null || !ind.clientsideItem.enabled)
                                                                                    && !ind.isSpawning
                                                                                    && ind.clientsideId <= 0
                                                                               ).ToList();

            foreach(ItemNetworkData ind in unspawned_items) {
                ind.clientsideItem = null;

                Dispatcher.Enqueue(() => {
                    Spawner.TrySpawnItem(ind, false);
                });

                yield return new WaitForSeconds(Config.LONG_WAIT_DEALY);
            }
        }

        private int unsynced_creature_skip = 0;
        /// <summary>
        /// Checking if the player has any unsynched creatures that the server needs to know about
        /// </summary>!
        internal IEnumerator CheckUnsynchedCreatures() {
            // Get all creatures that are not synched
            List<Creature> unsynced_creatures = Creature.allActive.Where(creature => (Player.currentCreature == null || !creature.Equals(Player.currentCreature)) // Check if creature is the player
                                                                                  && !syncData.creatures.Any(entry => creature.Equals(entry.Value.creature))     // Check if creature is synced already
                                                                                  && !syncData.players.Any(entry => creature.Equals(entry.Value.creature))       // Check if creature is an other player
                                                                                  ).ToList();

            List<CreatureNetworkData> not_spawned_creatures = syncData.creatures.Values.Where(cnd => (cnd.creature == null || !cnd.creature.enabled)
                                                                                                  && !cnd.isSpawning
                                                                                                  && cnd.clientsideId <= 0
                                                                                             ).ToList();

            if(unsynced_creature_skip > 2) {
                // If our game still has unspawned creatures, don't sync any new
                if(not_spawned_creatures.Count > 0) {
                    unsynced_creature_skip++;
                    yield break;
                }
            }
            unsynced_creature_skip = 0;

            foreach(Creature creature in unsynced_creatures) {
                if(creature == null) continue;
                if(creature.data == null) continue;

                Dispatcher.Enqueue(() => {
                    SyncCreatureIfNotAlready(creature);
                });
                yield return new WaitForEndOfFrame();
            }
        }

        /// <summary>
        /// Tries to spawn or respawn creatures that are on the server but not in the clients game world
        /// </summary>
        internal IEnumerator TryRespawningCreatures() {
            List<CreatureNetworkData> unspawned_creatures = syncData.creatures.Values.Where(cnd => (cnd.creature == null/* || !cnd.creature.enabled*/)
                                                                                                && !cnd.isSpawning
                                                                                                && cnd.clientsideId <= 0
                                                                                           ).ToList();

            foreach(CreatureNetworkData cnd in unspawned_creatures) {
                cnd.creature = null;

                Dispatcher.Enqueue(() => {
                    Spawner.TrySpawnCreature(cnd);
                });

                yield return new WaitForSeconds(Config.LONG_WAIT_DEALY);
            }
        }

        private IEnumerator TryRespawningPlayers() {
            foreach(PlayerNetworkData pnd in syncData.players.Values) {
                if(  (pnd.creature == null ||/* !pnd.creature.enabled ||*/ !pnd.creature.loaded || !pnd.creature.isCulled)
                  && !pnd.isSpawning
                  && pnd.receivedPos
                   ) {
                    //Log.Warn(Defines.CLIENT, "Player despawned, trying to respawn!");
                    Dispatcher.Enqueue(() => {
                        Spawner.TrySpawnPlayer(pnd);
                    });

                    yield return new WaitForSeconds(Config.LONG_WAIT_DEALY);
                }
            }
        }

        private IEnumerator CheckEntities() {
            // Get all entities that are not synched
            List<ThunderEntity> unsynced_entities = ThunderEntity.allEntities.Where(entity => !(entity is Item) && !(entity is Creature) && !syncData.entities.Any(entry => entity.Equals(entry.Value.entity))).ToList();
            
            foreach(ThunderEntity entity in unsynced_entities) {
                Dispatcher.Enqueue(() => {
                    SyncEntityIfNotAlready(entity);
                });
            }
            yield break;
        }


        private float lastPosSent = 0;

        internal void SendMyPos(bool force = false) {
            if(Time.time - lastPosSent > 5f) force = true;

            if(Player.currentCreature == null) return;
            //if(Player.currentCreature.ragdoll.ik.handLeftTarget == null) return;

            Dispatcher.Enqueue(() => {
                if(!force) {
                    if(!SyncFunc.hasPlayerMoved()) return;
                }
                lastPosSent = Time.time;

                syncData.myPlayerData.UpdatePositionFromCreature();
                if(Config.PLAYER_FULL_BODY_SYNCING) {
                    new PlayerRagdollPacket(syncData.myPlayerData).SendToServerUnreliable();

                    /*
                    if(Player.currentCreature.handLeft.grabbedHandle == null) {
                        Log.Warn("handLeft");
                        new HandPositionPacket(syncData.myPlayerData, Side.Left).SendToServerUnreliable();
                    }
                    if(Player.currentCreature.handRight.grabbedHandle == null) {
                        Log.Warn("handRight");
                        new HandPositionPacket(syncData.myPlayerData, Side.Right).SendToServerUnreliable();
                    }
                    */
                } else {
                    new PlayerPositionPacket(syncData.myPlayerData).SendToServerUnreliable();
                }
            });

            /*
            if(Player.currentCreature.handLeft?.poser != null) {
                CheckHandPose(Player.currentCreature.handLeft);
            }
            if(Player.currentCreature.handRight?.poser != null) {
                CheckHandPose(Player.currentCreature.handRight);
            }
            */
        }

        /*
        private void CheckHandPose(RagdollHand hand) {
            if(hand == null) return;
            if(hand.poser == null) return;

            Log.Debug(hand.poser.thumbCloseWeight + " - " +
                        hand.poser.indexCloseWeight + " - " +
                        hand.poser.middleCloseWeight + " - " +
                        hand.poser.ringCloseWeight + " - " +
                        hand.poser.littleCloseWeight);
        }
        */

        internal void SendMovedItems() {
            foreach(ItemNetworkData ind in syncData.items.Values.ToArray()) {
                if(!ModManager.clientInstance.allowTransmission) continue;
                if(ind.networkItem == null) continue;
                if(ind.networkedId <= 0) continue;
                if(!ind.networkItem.IsSending()) continue;

                if(SyncFunc.hasItemMoved(ind)) {
                    ind.UpdatePositionFromItem();
                    new ItemPositionPacket(ind).SendToServerUnreliable();
                }
            }
        }

        internal void SendMovedCreatures() {
            foreach(CreatureNetworkData cnd in syncData.creatures.Values) {
                if(!ModManager.clientInstance.allowTransmission) continue;
                if(cnd.networkCreature == null) continue;
                if(cnd.networkedId <= 0) continue;
                if(!cnd.networkCreature.IsSending()) continue;

                if(SyncFunc.hasCreatureMoved(cnd)) {
                    cnd.UpdatePositionFromCreature();
                    if(cnd.ragdollPositions != null) {
                        new CreatureRagdollPacket(cnd).SendToServerUnreliable();
                    } else {
                        new CreaturePositionPacket(cnd).SendToServerUnreliable();
                    }
                }
            }
        }

        internal void SendMovedEntities() {
            foreach(EntityNetworkData end in syncData.entities.Values) {
                if(!ModManager.clientInstance.allowTransmission) continue;
                if(end.networkEntity == null) continue;
                if(end.entity == null) continue;
                if(end.clientsideId == 0) continue;

                if(SyncFunc.hasEntityMoved(end)) {
                    end.UpdatePositionFromEntity();
                    new EntityPositionPacket(end).SendToServerUnreliable();
                }
                end.networkEntity?.CheckChanges();
            }
        }

        internal void LeavePlayer(PlayerNetworkData ps) {
            if(ps == null) return;

            if(ps.creature != null) {
                Dispatcher.Enqueue(() => {
                    Destroy(ps.creature.gameObject);
                });
            }

            ModManager.clientSync.syncData.players.TryRemove(ps.clientId, out _);
        }

        internal void MovePlayer(PlayerNetworkData pnd) {
            if(pnd != null && pnd.creature != null) {
                if(pnd.networkCreature == null) pnd.StartNetworking();

                pnd.networkCreature.targetPos = pnd.position;
                pnd.networkCreature.targetRotation = pnd.rotationY;

                if(pnd.ragdollPositions == null) { // Old syncing
                    pnd.networkCreature.handLeftTargetPos = pnd.handLeftPos;
                    pnd.networkCreature.handLeftTargetRot = Quaternion.Euler(pnd.handLeftRot);

                    pnd.networkCreature.handRightTargetPos = pnd.handRightPos;
                    pnd.networkCreature.handRightTargetRot = Quaternion.Euler(pnd.handRightRot);
                
                    pnd.networkCreature.headTargetPos = pnd.headPos;
                    pnd.networkCreature.headTargetRot = Quaternion.Euler(pnd.headRot);
                } else {
                    pnd.networkCreature.SetRagdollInfo(pnd.ragdollPositions, pnd.ragdollRotations);
                    //pnd.networkCreature.ragdollPartsVelocity = pnd.ragdollVelocity;
                    //pnd.networkCreature.rotationVelocity = pnd.ragdollAngularVelocity.Select(v => v.ConvertToQuaternion()).ToArray();
                }
            }
        }

        internal List<Creature> waitingForSpawn = new List<Creature>();
        internal void SyncCreatureIfNotAlready(Creature creature) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;
            if(!ModManager.clientInstance.allowTransmission) return;
            if(waitingForSpawn.Contains(creature)) return;
            //if(!LevelInfo.IsInActiveArea(creature.transform.position)) return; // TODO: Might not be nessesary, this will just let the server know the creature is there, the client will decide if it only needs to remember or spawn it

            // Now we wait for the Creature to get a position so we dont spawn them at 0,0,0 and teleport them afterwards

            waitingForSpawn.Add(creature);
            StartCoroutine(WaitForCreature(creature));
        }

        private IEnumerator WaitForCreature(Creature creature) {
            do {
                yield return new WaitForSeconds(0.1f);
            } while(creature != null && creature.transform.position == Vector3.zero);

            waitingForSpawn.Remove(creature);
            if(creature == null) yield break;
            if(ModManager.clientInstance == null) yield break;

            #region Make sure its not already synced
            if(creature.GetComponent<NetworkCreature>() != null) yield break;
            if(creature.GetComponent<NetworkPlayerCreature>() != null) yield break;
            if(creature.GetComponent<NetworkLocalPlayer>() != null) yield break;

            string[] wardrobe = new string[0];
            Color[] colors = new Color[0];
            CreatureEquipment.Read(creature, ref colors, ref wardrobe);

            foreach(CreatureNetworkData cs in ModManager.clientSync.syncData.creatures.Values) {
                if(cs.creature == creature) yield break; // If creature already exists, just exit
            }
            foreach(PlayerNetworkData playerSync in ModManager.clientSync.syncData.players.Values) {
                if(playerSync.creature == creature) yield break;
            }
            if(Player.currentCreature != null && Player.currentCreature == creature) yield break;
            #endregion

            // Check if the creature aims for the player
            bool isPlayerTheTaget = creature.brain.currentTarget == null ? false : creature.brain.currentTarget == Player.currentCreature;

            int currentCreatureId = ModManager.clientSync.syncData.currentClientCreatureId++;
            CreatureNetworkData cnd = new CreatureNetworkData() {
                creature = creature,
                clientsideId = currentCreatureId,

                clientTarget = isPlayerTheTaget ? ModManager.clientInstance.netclient.ClientId : 0, // If the player is the target, let the server know it

                creatureType = creature.creatureId,
                containerID = (creature.container != null ? creature.container.containerID : ""),
                factionId = (byte)creature.factionId,

                maxHealth = creature.maxHealth,
                health = creature.currentHealth,

                height = creature.GetHeight(),

                equipment = wardrobe,
                colors    = colors,

                isSpawning = false,
            };
            cnd.UpdatePositionFromCreature();

            Log.Debug(Defines.CLIENT, $"Event: Creature {creature.creatureId} has been spawned.");

            ModManager.clientSync.syncData.creatures.TryAdd(-currentCreatureId, cnd);
            new CreatureSpawnPacket(cnd).SendToServerReliable();
        }

        internal void SyncEntityIfNotAlready(ThunderEntity entity) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;
            if(entity.GetComponent<NetworkEntity>() != null) return;
            if(!ModManager.clientInstance.allowTransmission) return;
            if(!LevelInfo.IsInActiveArea(entity.transform.position)) return;

            NetworkEntity ne = entity.gameObject.AddComponent<NetworkEntity>();
        }

        internal void SyncItemIfNotAlready(Item item) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;
            if(!Item.allActive.Contains(item)) return;
            if(item.GetComponent<NetworkItem>() != null) return;
            if(Config.ignoredTypes.Contains(item.data.type)) return;
            if(!ModManager.clientInstance.allowTransmission) return;
            if(item == null) return;
            if(item.data == null) return;
            if(item.isBrokenPiece) return;
            if(!LevelInfo.IsInActiveArea(item.transform.position)) return;

            //foreach(ItemNetworkData sync in ModManager.clientSync.syncData.items.Values) {
            //    if(item.Equals(sync.clientsideItem)) {
            //        return;
            //    }
            //}

            ItemNetworkData itemSync = new ItemNetworkData() {
                dataId = item.data.id,
                category = item.data.type,
                clientsideItem = item,
                position = item.transform.position,
                rotation = item.transform.eulerAngles
            };

            ItemNetworkData foundItem = SyncFunc.DoesItemAlreadyExist(itemSync, ModManager.clientSync.syncData.items.Values.ToList());
            if(foundItem != null) { // Item already exists in the world / spawned by server
                bool keep = true;
                if(foundItem.clientsideItem != null) { // The item is already spawned
                    keep = false;
                }else if(foundItem.isSpawning) { // The item is already preparing to spawn
                    keep = false;
                }
                
                if(!keep) { // If the item shouldnt be kept, just despawn it, otherwise mark it as the item thats spawned
                    item.Despawn();
                } else {
                    foundItem.clientsideItem = item;

                    foundItem.StartNetworking();
                }

                return;
            }

            itemSync.clientsideId = Interlocked.Increment(ref ModManager.clientSync.syncData.currentClientItemId);


            Log.Debug(Defines.CLIENT, $"Found new item {item.data.id} ({itemSync.clientsideId}) - Trying to sync...");

            ModManager.clientSync.syncData.items.TryAdd(-itemSync.clientsideId, itemSync);

            new ItemSpawnPacket(itemSync).SendToServerReliable();
        }

        internal static void EquipItemsForCreature(int id, ItemHolderType holderType) {
            foreach(ItemNetworkData ind in ModManager.clientSync.syncData.items.Values) {
                if(ind.holdingStates == null) continue;
                try {
                    if(ind.holdingStates.First(state => state.holderNetworkId == id && state.holderType == holderType) != null) {
                        ind.UpdateHoldState();
                    }
                } catch(InvalidOperationException) {
                    // Ignore it
                }
            }
        }

        internal void CleanCollidingItems() {
            int i = 0;

            List<ItemNetworkData> known_items = syncData.items.Values.ToList();
            List<Item> unsynced_items = Item.allActive.Where(item => syncData.items.All(entry => !item.Equals(entry.Value.clientsideItem))).ToList();
            foreach(Item item in unsynced_items) {
                //float range = SyncFunc.getCloneDistance(item.itemId);
                foreach(ItemNetworkData ind in syncData.items.Values) {
                    if(item.transform.position.CloserThan(ind.position, Config.BIG_ITEM_CLONE_MAX_DISTANCE)) {
                        i++;
                        try {
                            item.Despawn();
                        }catch(Exception ex) {
                            Log.Err(Defines.CLIENT, ex);
                        }
                        break;
                    }
                }
            }

            Log.Debug(Defines.CLIENT, $"Despawned {i} items that would collide with the server items.");
        }

        internal IEnumerator UpdateProximityChat() {
            if(voiceClient == null) yield break;
            
            foreach(KeyValuePair<int, PlayerNetworkData> player in syncData.players) {
                float vol = ModLoader._VoiceChatVolume;
                if(ModLoader._EnableProximityChat) {
                    float dist = Player.local.head.transform.position.Distance(player.Value.position);
                    vol *= 1 - ((dist - 3) / 25);
                }
                vol = Mathf.Clamp(vol, 0, 1);
                voiceClient.SetClientVolume(player.Key, vol);
            }
            yield break;
        }

        internal VoiceClient voiceClient = null;
        public void UpdateVoiceChatState() {
            if(ModLoader._EnableVoiceChat && (syncData.server_config == null || syncData.server_config.allow_voicechat)) {
                if(voiceClient == null) {
                    voiceClient = new VoiceClient(ModManager.clientInstance.netclient);

                    voiceClient.SetInputDevice(ModLoader._RecordingDevice);
                    voiceClient.SetRecordingThreshold(ModLoader._RecordingCutoffVolume);

                    voiceClient.Start();
                    Log.Debug(Defines.CLIENT, "Started voice chat client.");
                }
            } else {
                if(voiceClient != null) {
                    voiceClient.Stop();
                    voiceClient = null;
                    Log.Debug(Defines.CLIENT, "Stopped voice chat client.");
                }
            }
        }

        /*
        internal void FixStuff() {
            foreach(PlayerNetworkData pnd in syncData.players.Values) {
                if(pnd.creature != null) {
                    try { 
                        pnd.creature?.Despawn();
                    }catch(Exception) { }
                }
                pnd.isSpawning = false;
                pnd.receivedPos = true;
                pnd.creature = null;
            }
            TryRespawningPlayers();
            ModManager.clientInstance.allowTransmission = true;
        }
        */
    }
}
