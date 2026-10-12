using AMP.Data;
using AMP.Datatypes;
using AMP.Extension;
using AMP.GameInteraction;
using AMP.GameInteraction.Components;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using AMP.Network.Packets.Implementation;
using AMP.Resources;
using AMP.SupportFunctions;
using AMP.Threading;
using Netamite.Unity.Voice;
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

        private float baseTickThreadTime = 0;
        private float playerTickThreadTime = 0;
        private float synchronizationThreadTime = 0;

        private Coroutine baseTickCoroutine;
        private Coroutine playerTickCoroutine;
        private Coroutine synchronizationCoroutine;

        private const int hangTime = 5;
        private const int restartTime = 20;

        public void StartThreads () {
            baseTickCoroutine        = StartCoroutine(BaseTickThread());
            playerTickCoroutine      = StartCoroutine(PlayerTickThread());
            synchronizationCoroutine = StartCoroutine(SynchronizationThread());

            ModManager.clientSync.UpdateVoiceChatState();
        }

        internal int packetsSentPerSec = 0;
        internal int packetsReceivedPerSec = 0;

        private bool stopped = false;

        float time = 0f;
        void Update() {
            if(ModManager.clientInstance.netclient.ClientId <= 0) return;

            // Unscaled, so slow motion doesn't stretch the timer
            time += Time.unscaledDeltaTime;
            if(time > 1f) {
                time = 0f;

                checkCoroutines();

                // Packet Stats
#if DEBUG_NETWORK
                packetsSentPerSec = (ModManager.clientInstance.tcp != null ? ModManager.clientInstance.tcp.GetPacketsSent() : 0)
                                  + (ModManager.clientInstance.udp != null ? ModManager.clientInstance.udp.GetPacketsSent() : 0);
                packetsReceivedPerSec = (ModManager.clientInstance.tcp != null ? ModManager.clientInstance.tcp.GetPacketsReceived() : 0)
                                      + (ModManager.clientInstance.udp != null ? ModManager.clientInstance.udp.GetPacketsReceived() : 0);
#endif
            }
        }

        /// <summary>
        /// This function checks if all coroutines are current and up to date, if they haven't updated for "hangTime" it will log it.
        /// If they still haven't updated for "restartTime", they will be restarted.
        /// </summary>
        private void checkCoroutines() {
            CheckCoroutine("Base Tick",   ref baseTickThreadTime,        ref baseTickCoroutine,        BaseTickThread);
            CheckCoroutine("Player Tick", ref playerTickThreadTime,      ref playerTickCoroutine,      PlayerTickThread);
            CheckCoroutine("Sync",        ref synchronizationThreadTime, ref synchronizationCoroutine, SynchronizationThread);
        }

        private void CheckCoroutine(string name, ref float lastHeartbeat, ref Coroutine coroutine, Func<IEnumerator> start) {
            if(lastHeartbeat <= 0) return;

            float now = Time.unscaledTime;
            if(now > lastHeartbeat + restartTime) {
                Log.Err(Defines.AMP, $"{name} Coroutine restarting. It probably crashed.");
                lastHeartbeat = now;
                // Stop the old instance in case it's only hanging, otherwise both would keep running
                if(coroutine != null) StopCoroutine(coroutine);
                coroutine = StartCoroutine(start());
            } else if(now > lastHeartbeat + hangTime) {
                Log.Warn(Defines.AMP, $"{name} Coroutine stopped responding for {(int)(now - lastHeartbeat)} seconds...");
            }
        }

        private bool playerDataSent = false;
        // Send the local player position about 30/sec
        IEnumerator PlayerTickThread() {
            float nextTick = Time.unscaledTime;
            while(!threadCancel.Token.IsCancellationRequested) {
                playerTickThreadTime = Time.unscaledTime;

                playerTickThreadTime = Time.time;

                float wait = 1f / Config.PLAYER_TICK_RATE;
                if (wait > Time.time - time) wait -= Time.time - time;
                if (wait > 0) yield return new WaitForSeconds(wait);
                time = Time.time;

                if (ModManager.clientInstance.netclient.ClientId <= 0) continue;
                if(!ModManager.clientInstance.allowTransmission) continue;
                if(LevelInfo.IsLoading()) continue;
                if(threadCancel.Token.IsCancellationRequested) yield break;

                try {
                    if(syncData.myPlayerData == null) syncData.myPlayerData = new PlayerNetworkData();
                    if(Player.local != null && Player.currentCreature != null) {
                        if(syncData.myPlayerData.creature == null) {
                            syncData.myPlayerData.creature = Player.currentCreature;

                            syncData.myPlayerData.clientId = ModManager.clientInstance.netclient.ClientId;

                            syncData.myPlayerData.height = Player.currentCreature.GetHeight();
                            syncData.myPlayerData.creatureId = Player.currentCreature.creatureId;

                            syncData.myPlayerData.position = Player.currentCreature.transform.position;
                            syncData.myPlayerData.rotationY = Player.local.head.transform.eulerAngles.y;

                            syncData.myPlayerData.ethnicGroup = Player.currentCreature.currentEthnicGroup?.id ?? "";

                            syncData.myPlayerData.uniqueId = SystemInfo.deviceUniqueIdentifier;

                            new PlayerDataPacket(syncData.myPlayerData, SystemInfo.deviceUniqueIdentifier).SendToServerReliable();

                            CreatureEquipment.Read(syncData.myPlayerData);
                            new PlayerEquipmentPacket(syncData.myPlayerData).SendToServerReliable();

                            SendModListToServer();

                            Player.currentCreature.gameObject.GetElseAddComponent<NetworkLocalPlayer>();

                            SendMyPos(true);

                            playerDataSent = true;
                        } else {
                            SendMyPos();
                        }
                    } else {
                        Log.Err("No player creature found.");
                    }
                } catch(Exception e) {
                    Log.Err(Defines.CLIENT, $"Error: {e}");
                }
            }
        }

        // Check item, creature and entity position about 10/sec
        IEnumerator BaseTickThread() {
            float nextTick = Time.unscaledTime;
            while(!threadCancel.Token.IsCancellationRequested) {
                baseTickThreadTime = Time.unscaledTime;

                nextTick += 1f / Config.BASE_TICK_RATE;
                float wait = nextTick - Time.unscaledTime;
                if(wait > 0) {
                    yield return new WaitForSecondsRealtime(wait);
                } else {
                    nextTick = Time.unscaledTime;
                    yield return null;
                }

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
                SyncHeartbeat();
                
                if (ModManager.clientInstance.allowTransmission) {
                    if(!skipRespawning) {
                        if(ModManager.clientInstance.allowTransmission) yield return TryRespawningItems();
                        if(ModManager.clientInstance.allowTransmission) yield return TryRespawningCreatures();
                    }

                    if(ModManager.clientInstance.allowTransmission) yield return CheckUnsynchedItems();
                    if(ModManager.clientInstance.allowTransmission) yield return CheckUnsynchedCreatures();

                    if(ModManager.clientInstance.allowTransmission) yield return TryRespawningPlayers();

                    if(ModManager.clientInstance.allowTransmission) yield return CheckEntities();

                    //if(ModLoader._EnableVoiceChat && ModLoader._EnableProximityChat) yield return UpdateProximityChat();
                }
                synchronizationThreadWait = 1f;
                skipRespawning = false;

                while(synchronizationThreadWait > 0f) {
                    if(synchronizationThreadWait > 1f) synchronizationThreadWait = 1f;
                    yield return null;
                    synchronizationThreadWait -= Time.unscaledDeltaTime;
                }
            }
        }

        // The sync passes can take a while with many objects, this keeps the watchdog from restarting them meanwhile
        private void SyncHeartbeat() {
            synchronizationThreadTime = Time.unscaledTime;
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
            stopped = true;
            
            threadCancel.Cancel();
            StopAllCoroutines();

            foreach(PlayerNetworkData ps in syncData.players.Values.ToList()) {
                LeavePlayer(ps);
            }

            foreach(ItemNetworkData ind in syncData.items.Values.ToList()) {
                if(ind.networkItem != null) {
                    Destroy(ind.networkItem);
                }
            }
            foreach(CreatureNetworkData cnd in syncData.creatures.Values.ToList()) {
                if(cnd.networkCreature != null) {
                    cnd.SetOwnership(true);
                    Destroy(cnd.networkCreature);
                }
            }
            TextDisplay.ClearText();

            UpdateVoiceChatState();
        }

        /// <summary>
        /// Checking if the player has any unsynched items that the server needs to know about
        /// </summary>
        internal IEnumerator CheckUnsynchedItems() {
            List<Item> unsynced_items = GetUnsyncedItems();

            //Log.Debug(unfoundItemMode);
            foreach(Item item in unsynced_items) {
                SyncHeartbeat();
                if(item == null) continue;
                if(!item.enabled) continue;
                if(item.data == null) continue;
                if(item.isBrokenPiece) continue;

                if(item.holder != null && item.holder.parentItem != null) {
                    if(item.holder.parentItem.itemId.Equals("Quiver")) continue; // fix the duplication with quivers
                }

                if(Config.ignoredItems.Contains(item.data.id.ToLower())) continue;

                if(!Config.ignoredTypes.Contains(item.data.type)) {
                    SyncItemIfNotAlready(item);
                    
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
                SyncHeartbeat();
                ind.clientsideItem = null;

                Spawner.TrySpawnItem(ind, false);

                yield return new WaitForSecondsRealtime(Config.LONG_WAIT_DELAY);
            }
        }

        private int unsynced_creature_skip = 0;
        /// <summary>
        /// Checking if the player has any unsynched creatures that the server needs to know about
        /// </summary>
        internal IEnumerator CheckUnsynchedCreatures() {
            // Creatures that are already synced or belong to other players
            HashSet<Creature> known_creatures = new HashSet<Creature>(syncData.creatures.Values.Select(cnd => cnd.creature)
                                                              .Concat(syncData.players.Values.Select(pnd => pnd.creature))
                                                              .Where(creature => creature != null));

            // Get all creatures that are not synched
            List<Creature> unsynced_creatures = Creature.allActive.Where(creature => (Player.currentCreature == null || !creature.Equals(Player.currentCreature)) // Check if creature is the player
                                                                                  && !known_creatures.Contains(creature)
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
                SyncHeartbeat();
                if(creature == null) continue;
                if(creature.data == null) continue;

                SyncCreatureIfNotAlready(creature);
                
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
                SyncHeartbeat();
                cnd.creature = null;

                Spawner.TrySpawnCreature(cnd);

                yield return new WaitForSecondsRealtime(Config.LONG_WAIT_DELAY);
            }
        }

        private const float playerSpawnTimeout = 15f;
        private IEnumerator TryRespawningPlayers() {
            foreach(PlayerNetworkData pnd in syncData.players.Values.ToList()) {
                SyncHeartbeat();

                // Fallback in case the spawn coroutine died without calling back
                if(pnd.isSpawning && pnd.creature == null && Time.unscaledTime - pnd.spawnStartTime > playerSpawnTimeout) {
                    Log.Warn(Defines.CLIENT, $"Spawning Player {pnd.name} ({pnd.clientId}) timed out, trying again.");
                    pnd.spawnRequest++; // Throw the creature away if the old spawn still finishes
                    pnd.isSpawning = false;
                }

                // Spawner.TrySpawnPlayer only spawns if the creature is gone, so there is no point checking anything else
                if(pnd.creature == null && !pnd.isSpawning && pnd.receivedPos) {
                    //Log.Warn(Defines.CLIENT, "Player despawned, trying to respawn!");
                    Spawner.TrySpawnPlayer(pnd);

                    yield return new WaitForSecondsRealtime(Config.LONG_WAIT_DELAY);
                }
            }
        }

        private IEnumerator CheckEntities() {
            HashSet<ThunderEntity> synced_entities = new HashSet<ThunderEntity>(syncData.entities.Values.Select(end => end.entity).Where(entity => entity != null));

            // Get all entities that are not synched
            List<ThunderEntity> unsynced_entities = ThunderEntity.allEntities.Where(entity => !(entity is Item) && !(entity is Creature) && !synced_entities.Contains(entity)).ToList();

            foreach(ThunderEntity entity in unsynced_entities) {
                SyncEntityIfNotAlready(entity);
            }
            yield break;
        }

        /// <summary>
        /// Returns all active items that aren't known to the sync data yet
        /// </summary>
        private List<Item> GetUnsyncedItems() {
            HashSet<Item> synced_items = new HashSet<Item>(syncData.items.Values.Select(ind => ind.clientsideItem).Where(item => item != null));
            return Item.allActive.Where(item => !synced_items.Contains(item)).ToList();
        }


        private const float forceResendTime = 5f;
        private float lastPosSent = 0;
        private float lastHandPoseSentLeft = 0;
        private float lastHandPoseSentRight = 0;

        // Called from the player tick coroutine, so we are already on the main thread
        internal void SendMyPos(bool force = false) {
            if(Player.currentCreature == null) return;
            //if(Player.currentCreature.ragdoll.ik.handLeftTarget == null) return;

            SendMyRagdollPosition(force);
            SendMyHandPose(Player.currentCreature.handLeft, Side.Left, force);
            SendMyHandPose(Player.currentCreature.handRight, Side.Right, force);
        }

        private void SendMyRagdollPosition(bool force) {
            if(Time.unscaledTime - lastPosSent > forceResendTime) force = true;
            if(!force && !SyncFunc.hasPlayerMoved()) return;

            lastPosSent = Time.unscaledTime;

            syncData.myPlayerData.UpdatePositionFromCreature();
            if(Config.PLAYER_FULL_BODY_SYNCING) {
                new PlayerRagdollPacket(syncData.myPlayerData).SendToServerUnreliable();
            } else {
                new PlayerPositionPacket(syncData.myPlayerData).SendToServerUnreliable();
            }
        }

        private void SendMyHandPose(RagdollHand hand, Side side, bool force) {
            if(hand == null || hand.poser == null) return;
            if(hand.grabbedHandle != null) { // Grip state/held items are synced separately
                // Forget the last pose, so the first pose after letting go always gets sent
                if(side == Side.Left) syncData.myPlayerData.lastSentHandWeightsLeft = null;
                else syncData.myPlayerData.lastSentHandWeightsRight = null;
                return;
            }

            // Hand poses are sent unreliable, so resend them every now and then in case one got lost
            float lastSent = side == Side.Left ? lastHandPoseSentLeft : lastHandPoseSentRight;
            if(Time.unscaledTime - lastSent > forceResendTime) force = true;

            float[] lastSentWeights = side == Side.Left ? syncData.myPlayerData.lastSentHandWeightsLeft : syncData.myPlayerData.lastSentHandWeightsRight;
            if(!force && !SyncFunc.hasHandPoseChanged(hand, lastSentWeights)) return;

            lastSentWeights = new float[] { hand.poser.thumbCloseWeight, hand.poser.indexCloseWeight, hand.poser.middleCloseWeight, hand.poser.ringCloseWeight, hand.poser.littleCloseWeight };
            if(side == Side.Left) {
                syncData.myPlayerData.lastSentHandWeightsLeft = lastSentWeights;
                lastHandPoseSentLeft = Time.unscaledTime;
            } else {
                syncData.myPlayerData.lastSentHandWeightsRight = lastSentWeights;
                lastHandPoseSentRight = Time.unscaledTime;
            }

            new HandPositionPacket(syncData.myPlayerData, side).SendToServerUnreliable();
        }

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
            foreach(CreatureNetworkData cnd in syncData.creatures.Values.ToList()) {
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
            foreach(EntityNetworkData end in syncData.entities.Values.ToList()) {
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
            
            if(!stopped) Dispatcher.Enqueue(() => { UI.IngameModUI.currentUI?.RefreshPage(); });
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

            foreach(CreatureNetworkData cs in ModManager.clientSync.syncData.creatures.Values.ToList()) {
                if(cs.creature == creature) yield break; // If creature already exists, just exit
            }
            foreach(PlayerNetworkData playerSync in ModManager.clientSync.syncData.players.Values.ToList()) {
                if(playerSync.creature == creature) yield break;
            }
            if(Player.currentCreature != null && Player.currentCreature == creature) yield break;
            #endregion

            // Check if the creature aims for the player
            bool isPlayerTheTarget = creature.brain.currentTarget == null ? false : creature.brain.currentTarget == Player.currentCreature;

            string creatureEthnicGroup = "";
            if(creature.currentEthnicGroup != null)
                creatureEthnicGroup = creature.currentEthnicGroup.id;

            int currentCreatureId = ModManager.clientSync.syncData.currentClientCreatureId++;
            CreatureNetworkData cnd = new CreatureNetworkData() {
                creature = creature,
                clientsideId = currentCreatureId,

                clientTarget = isPlayerTheTarget ? ModManager.clientInstance.netclient.ClientId : 0, // If the player is the target, let the server know it

                creatureType = creature.creatureId,
                containerID = (creature.container != null ? creature.container.containerID : ""),
                factionId = (byte)creature.factionId,

                maxHealth = creature.maxHealth,
                health = creature.currentHealth,

                height = creature.GetHeight(),

                equipment = wardrobe,
                colors    = colors,

                ethnicGroup = creatureEthnicGroup,

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
            if(item == null) return;
            if(item.data == null) return;
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;
            if(!Item.allActive.Contains(item)) return;
            if(item.GetComponent<NetworkItem>() != null) return;
            if(Config.ignoredTypes.Contains(item.data.type)) return;
            if(!ModManager.clientInstance.allowTransmission) return;
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
            foreach(ItemNetworkData ind in ModManager.clientSync.syncData.items.Values.ToList()) {
                if(ind.holdingStates == null) continue;
                if(ind.holdingStates.Any(state => state != null && state.holderNetworkId == id && state.holderType == holderType)) {
                    ind.UpdateHoldState();
                }
            }
        }

        internal void CleanCollidingItems() {
            int i = 0;

            List<ItemNetworkData> known_items = syncData.items.Values.ToList();
            List<Item> unsynced_items = GetUnsyncedItems();
            foreach(Item item in unsynced_items) {
                //float range = SyncFunc.getCloneDistance(item.itemId);
                foreach(ItemNetworkData ind in known_items) {
                    if(item.transform.position.CloserThan(ind.position, 5f)) {
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

            float vol = ModLoader._VoiceChatVolume;
            vol = Mathf.Clamp(vol, 0, 1);

            voiceClient.SetVolume(vol);
            voiceClient.SetProximity(ModLoader._EnableProximityChat);

            UpdateVoiceChatState();

            yield break;
        }

        internal UnityVoiceClient voiceClient = null;
        public void UpdateVoiceChatState() {
            if(!threadCancel.IsCancellationRequested && ModLoader._EnableVoiceChat && (syncData.server_config == null || syncData.server_config.allow_voicechat)) {
                if(voiceClient == null) {
                    voiceClient = new UnityVoiceClient(ModManager.clientInstance.netclient, null, true);
                    voiceClient.SetRecordingThreshold(ModLoader._RecordingCutoffVolume);
                    voiceClient.SetInputDevice(ModLoader.currentRecordingDevice);
                    voiceClient.Start();
                    voiceClient.OnTalkingStateChanged += OnVoiceTalkingStateChanged;

                    Dispatcher.Enqueue(() => {
                        OnTalkingStateChanged(false);
                    });

                    Log.Debug(Defines.CLIENT, $"Started voice chat client with input {ModLoader.currentRecordingDevice}.");
                }
            } else {
                StopVoiceClient();
            }
        }

        private void StopVoiceClient() {
            if(voiceClient == null) return;

            voiceClient.OnTalkingStateChanged -= OnVoiceTalkingStateChanged;
            if(microphoneIcon != null) {
                Destroy(microphoneIcon.gameObject);
                microphoneIcon = null;
            }
            voiceClient.Stop();
            voiceClient = null;
            Log.Debug(Defines.CLIENT, "Stopped voice chat client.");
        }

        // The voice client might raise this outside the main thread, so move it over before touching any GameObjects
        private void OnVoiceTalkingStateChanged(bool talking) {
            Dispatcher.Enqueue(() => {
                OnTalkingStateChanged(talking);
            });
        }

        private static Sprite microphoneSprite;
        private SpriteRenderer microphoneIcon;
        private void OnTalkingStateChanged(bool talking) {
            if(voiceClient == null) return; // Voice chat got stopped while this was queued
            if(microphoneIcon == null) {
                if(Player.local == null) return;
                LoadMicrophoneIcon();

                microphoneIcon = new GameObject("MicrophoneIcon").AddComponent<SpriteRenderer>();

                microphoneIcon.transform.SetParent(Player.local.handLeft.ragdollHand.wristStats.baseTransform);

                if (microphoneSprite) {
                    microphoneIcon.sprite = microphoneSprite;
                }

                microphoneIcon.transform.localPosition = new Vector3(0.011f, -0.01f, -0.005f);
                microphoneIcon.transform.localScale = Vector3.one * 0.0025f;
                microphoneIcon.transform.localEulerAngles = new Vector3(0, 0, 15);
            }
            
            microphoneIcon.color = talking ? Color.green : Color.red;
        }

        // Read the microphone texture from dll bytes
        private static void LoadMicrophoneIcon() {
            if (microphoneSprite == null) {
                try {
                    Texture2D tex2d = new Texture2D(2, 2);
                    //tex2d.LoadImage(Properties.Resources.Microphone);
                    tex2d.LoadImage(AMPResources.microphone);

                    microphoneSprite = Sprite.Create(tex2d, new Rect(0, 0, tex2d.width, tex2d.height), Vector2.one / 2);
                    return;
                } catch (Exception) { }

                microphoneSprite = Sprite.Create(new Texture2D(512, 512), new Rect(0, 0, 512, 512), Vector2.one / 2);
            }
        }

        void OnDestroy() {
            StopVoiceClient();
            threadCancel.Dispose();
        }
    }
}
