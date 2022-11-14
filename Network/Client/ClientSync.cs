using AMP.Data;
using AMP.Extension;
using AMP.GameInteraction;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using AMP.Network.Packets.Implementation;
using AMP.SupportFunctions;
using AMP.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client {
    internal class ClientSync : MonoBehaviour {
        internal SyncData syncData = new SyncData();

        private Thread stayAlivePingThread = null;
        void Start () {
            if(!ModManager.clientInstance.nw.isConnected) {
                Destroy(this);
                return;
            }
            StartCoroutine(onUpdateTick());
            StartCoroutine(spawnerThread());

            stayAlivePingThread = new Thread(() => {
                Thread.Sleep(10000);
                while(ModManager.clientInstance.nw.isConnected) {
                    new PingPacket().SendToServerUnreliable();
                    Thread.Sleep(10000);
                }
            });
            stayAlivePingThread.Start();
        }

        internal int packetsSentPerSec = 0;
        internal int packetsReceivedPerSec = 0;

        float time = 0f;
        void FixedUpdate() {
            if(ModManager.clientInstance == null || !ModManager.clientInstance.nw.isConnected) {
                Destroy(this);
                return;
            }
            if(ModManager.clientInstance.myPlayerId <= 0) return;

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

        // Check player and item position about 60/sec
        IEnumerator onUpdateTick() {
            float time = Time.time;
            while(true) {
                float wait = 1f / Config.TICK_RATE;
                if(wait > Time.time - time) wait -= Time.time - time;
                if(wait > 0) yield return new WaitForSeconds(wait);
                time = Time.time;

                if(ModManager.clientInstance.myPlayerId <= 0) continue;
                if(!ModManager.clientInstance.allowTransmission) continue;
                if(Level.current != null && !Level.current.loaded) continue;

                if(syncData.myPlayerData == null) syncData.myPlayerData = new PlayerNetworkData();
                if(Player.local != null && Player.currentCreature != null) {
                    if(syncData.myPlayerData.creature == null) {
                        syncData.myPlayerData.creature = Player.currentCreature;

                        syncData.myPlayerData.clientId = ModManager.clientInstance.myPlayerId;
                        syncData.myPlayerData.name = UserData.GetUserName();

                        syncData.myPlayerData.height = Player.currentCreature.GetHeight();
                        syncData.myPlayerData.creatureId = Player.currentCreature.creatureId;

                        syncData.myPlayerData.position = Player.currentCreature.transform.position;
                        syncData.myPlayerData.rotationY = Player.local.head.transform.eulerAngles.y;

                        new PlayerDataPacket(syncData.myPlayerData).SendToServerReliable();
                        CreatureEquipment.Read(syncData.myPlayerData);

                        new PlayerEquipmentPacket(syncData.myPlayerData).SendToServerReliable();

                        Player.currentCreature.gameObject.GetElseAddComponent<NetworkLocalPlayer>();

                        SendMyPos(true);
                    } else {
                        SendMyPos();
                    }
                }
                try {
                    SendMovedItems();
                    SendMovedCreatures();
                }catch(Exception e) {
                    Log.Err(Defines.CLIENT, $"Error: {e}");
                }
            }
        }


        IEnumerator spawnerThread() {
            while(true) {
                if(ModManager.clientInstance.allowTransmission) {
                    yield return TryRespawningItems();
                    yield return TryRespawningCreatures();

                    yield return CheckUnsynchedItems();
                    yield return CheckUnsynchedCreatures();

                    yield return TryRespawningPlayers();
                }

                yield return new WaitForSeconds(1);
            }
        }

        internal void Stop() {
            if(stayAlivePingThread != null) stayAlivePingThread.Abort();
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
                }
            }
        }

        /// <summary>
        /// Checking if the player has any unsynched items that the server needs to know about
        /// </summary>
        private IEnumerator CheckUnsynchedItems() {
            // Get all items that are not synched
            List<Item> unsynced_items = Item.allActive.Where(item => syncData.items.All(entry => !item.Equals(entry.Value.clientsideItem))).ToList();

            foreach(Item item in unsynced_items) {
                if(item == null) continue;
                if(item.data == null) continue;

                if(!Config.ignoredTypes.Contains(item.data.type)) {
                    SyncItemIfNotAlready(item);

                    yield return new WaitForEndOfFrame();
                } else {
                    // Despawn all props until better syncing system, so we dont spam the other clients
                    item.Despawn();
                }
            }
        }

        /// <summary>
        /// Tries to spawn or respawn items that are on the server but not in the clients game world
        /// </summary>
        private IEnumerator TryRespawningItems() {
            List<ItemNetworkData> unspawned_items = syncData.items.Values.Where(ind => (ind.clientsideItem == null || !ind.clientsideItem.enabled)
                                                                                    && !ind.isSpawning
                                                                                    && ind.clientsideId <= 0
                                                                               ).ToList();

            foreach(ItemNetworkData ind in unspawned_items) {
                ind.clientsideItem = null;

                Spawner.TrySpawnItem(ind);
                yield return new WaitForFixedUpdate();
            }
        }

        private int unsynced_creature_skip = 0;
        /// <summary>
        /// Checking if the player has any unsynched creatures that the server needs to know about
        /// </summary>!
        private IEnumerator CheckUnsynchedCreatures() {
            // Get all items that are not synched
            List<Creature> unsynced_creatures = Creature.allActive.Where(creature => syncData.creatures.All(entry => !creature.Equals(entry.Value.creature))).ToList();

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

                SyncCreatureIfNotAlready(creature);

                yield return new WaitForEndOfFrame();
            }
        }

        /// <summary>
        /// Tries to spawn or respawn creatures that are on the server but not in the clients game world
        /// </summary>
        private IEnumerator TryRespawningCreatures() {
            List<CreatureNetworkData> unspawned_creatures = syncData.creatures.Values.Where(cnd => (cnd.creature == null || !cnd.creature.enabled)
                                                                                                && !cnd.isSpawning
                                                                                                && cnd.clientsideId <= 0
                                                                                           ).ToList();

            foreach(CreatureNetworkData cnd in unspawned_creatures) {
                cnd.creature = null;

                Spawner.TrySpawnCreature(cnd);
                yield return new WaitForFixedUpdate();
            }
        }

        private IEnumerator TryRespawningPlayers() {
            foreach(PlayerNetworkData pnd in syncData.players.Values) {
                if(  (pnd.creature == null || !pnd.creature.enabled)
                  && !pnd.isSpawning
                   ) {
                    //Log.Warn(Defines.CLIENT, "Player despawned, trying to respawn!");
                    Spawner.TrySpawnPlayer(pnd);
                    yield return new WaitForFixedUpdate();
                }
            }
        }


        private float lastPosSent = 0;
        internal void SendMyPos(bool force = false) {
            if(Time.time - lastPosSent > 5f) force = true;

            if(Player.currentCreature == null) return;
            //if(Player.currentCreature.ragdoll.ik.handLeftTarget == null) return;

            string pos = "init";
            Dispatcher.Enqueue(() => {
                try {
                    if(!force) {
                        if(!SyncFunc.hasPlayerMoved()) return;
                    }
                    lastPosSent = Time.time;

                    pos = "position";
                    syncData.myPlayerData.position = Player.currentCreature.transform.position;
                    syncData.myPlayerData.rotationY = Player.local.head.transform.eulerAngles.y;

                    if(Config.PLAYER_FULL_BODY_SYNCING) {
                        pos = "ragdoll";
                        syncData.myPlayerData.ragdollParts = Player.currentCreature.ReadRagdoll();

                        pos = "send-ragdoll";
                        new PlayerRagdollPacket(syncData.myPlayerData).SendToServerUnreliable();
                    } else {
                        pos = "velocity";
                        syncData.myPlayerData.velocity = Player.local.locomotion.rb.velocity;
                
                        pos = "handLeft";
                        syncData.myPlayerData.handLeftPos = Player.currentCreature.ragdoll.ik.handLeftTarget.position - syncData.myPlayerData.position;
                        syncData.myPlayerData.handLeftRot = Player.currentCreature.ragdoll.ik.handLeftTarget.eulerAngles;

                        pos = "handRight";
                        syncData.myPlayerData.handRightPos = Player.currentCreature.ragdoll.ik.handRightTarget.position - syncData.myPlayerData.position;
                        syncData.myPlayerData.handRightRot = Player.currentCreature.ragdoll.ik.handRightTarget.eulerAngles;

                        pos = "head";
                        syncData.myPlayerData.headPos = Player.currentCreature.ragdoll.headPart.transform.position;
                        syncData.myPlayerData.headRot = Player.currentCreature.ragdoll.headPart.transform.eulerAngles;

                        pos = "send-pos";
                        new PlayerPositionPacket(syncData.myPlayerData).SendToServerUnreliable();
                    }
                } catch(Exception e) {
                    Log.Err(Defines.CLIENT, $"Error at {pos}: {e}");
                }
            });
        }

        internal void SendMovedItems() {
            foreach(KeyValuePair<long, ItemNetworkData> entry in syncData.items) {
                if(entry.Value.networkItem == null) continue;
                if(!entry.Value.networkItem.IsSending()) continue;
                if(entry.Value.networkedId <= 0) continue;

                if(SyncFunc.hasItemMoved(entry.Value)) {
                    entry.Value.UpdatePositionFromItem();
                    new ItemPositionPacket(entry.Value).SendToServerUnreliable();
                }
            }
        }

        internal void SendMovedCreatures() {
            foreach(KeyValuePair<long, CreatureNetworkData> entry in syncData.creatures) {
                if(entry.Value.networkCreature == null) continue;
                if(entry.Value.networkedId <= 0) continue;
                if(!entry.Value.networkCreature.IsSending()) continue;

                if(SyncFunc.hasCreatureMoved(entry.Value)) {
                    entry.Value.UpdatePositionFromCreature();
                    if(entry.Value.ragdollParts != null) {
                        new CreatureRagdollPacket(entry.Value).SendToServerUnreliable();
                    } else {
                        new CreaturePositionPacket(entry.Value).SendToServerUnreliable();
                    }
                }
            }
        }

        internal void LeavePlayer(PlayerNetworkData ps) {
            if(ps == null) return;

            if(ps.creature != null) {
                Destroy(ps.creature.gameObject);
            }

            ModManager.clientSync.syncData.players.Remove(ps.clientId);
        }

        internal void MovePlayer(PlayerNetworkData pnd) {
            if(pnd != null && pnd.creature != null) {
                if(pnd.networkCreature == null) pnd.StartNetworking();

                pnd.networkCreature.targetPos = pnd.position;
                pnd.networkCreature.targetRotation = pnd.rotationY;

                pnd.networkCreature.SetRagdollInfo(pnd.ragdollParts);

                if(pnd.ragdollParts == null) { // Old syncing
                    pnd.networkCreature.handLeftTargetPos = pnd.handLeftPos;
                    pnd.networkCreature.handLeftTargetRot = Quaternion.Euler(pnd.handLeftRot);

                    pnd.networkCreature.handRightTargetPos = pnd.handRightPos;
                    pnd.networkCreature.handRightTargetRot = Quaternion.Euler(pnd.handRightRot);
                
                    pnd.networkCreature.headTargetPos = pnd.headPos;
                    pnd.networkCreature.headTargetRot = Quaternion.Euler(pnd.headRot);
                }
            }
        }

        internal void SyncCreatureIfNotAlready(Creature creature) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;

            string[] wardrobe = new string[0];
            Color[] colors = new Color[0];
            CreatureEquipment.Read(creature, ref colors, ref wardrobe);

            // Now we wait for the Creature to get a position so we dont spawn them at 0,0,0 and teleport them afterwards
            Thread awaitSpawnThread = new Thread(() => {
                do {
                    Thread.Sleep(100);
                } while(creature.transform.position == Vector3.zero);

                #region Make sure its not already synced
                if(creature.GetComponent<NetworkCreature>() != null) return;
                if(creature.GetComponent<NetworkPlayerCreature>() != null) return;
                if(creature.GetComponent<NetworkLocalPlayer>() != null) return;

                foreach(CreatureNetworkData cs in ModManager.clientSync.syncData.creatures.Values) {
                    if(cs.creature == creature) return; // If creature already exists, just exit
                }
                foreach(PlayerNetworkData playerSync in ModManager.clientSync.syncData.players.Values) {
                    if(playerSync.creature == creature) return;
                }
                if(Player.currentCreature != null && Player.currentCreature == creature) return;
                #endregion

                // Check if the creature aims for the player
                bool isPlayerTheTaget = creature.brain.currentTarget == null ? false : creature.brain.currentTarget == Player.currentCreature;

                int currentCreatureId = ModManager.clientSync.syncData.currentClientCreatureId++;
                CreatureNetworkData cnd = new CreatureNetworkData() {
                    creature = creature,
                    clientsideId = currentCreatureId,

                    clientTarget = isPlayerTheTaget ? ModManager.clientInstance.myPlayerId : 0, // If the player is the target, let the server know it

                    creatureType = creature.creatureId,
                    containerID = creature.container.containerID,
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

                ModManager.clientSync.syncData.creatures.Add(-currentCreatureId, cnd);
                new CreatureSpawnPacket(cnd).SendToServerReliable();
            });
            awaitSpawnThread.Start();
        }

        internal void SyncItemIfNotAlready(Item item) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;
            if(!Item.allActive.Contains(item)) return;
            if(item.GetComponent<NetworkItem>() != null) return;

            foreach(ItemNetworkData sync in ModManager.clientSync.syncData.items.Values) {
                if(item.Equals(sync.clientsideItem)) {
                    return;
                }
            }

            Log.Debug(Defines.CLIENT, $"Found new item " + item.data.id + " - Trying to spawn...");

            ModManager.clientSync.syncData.currentClientItemId++;

            ItemNetworkData itemSync = new ItemNetworkData() {
                dataId = item.data.id,
                category = item.data.type,
                clientsideItem = item,
                clientsideId = ModManager.clientSync.syncData.currentClientItemId,
                position = item.transform.position,
                rotation = item.transform.eulerAngles
            };

            ModManager.clientSync.syncData.items.Add(-ModManager.clientSync.syncData.currentClientItemId, itemSync);

            new ItemSpawnPacket(itemSync).SendToServerReliable();
        }

        internal static void EquipItemsForCreature(long id, bool holderIsPlayer) {
            foreach(ItemNetworkData ind in ModManager.clientSync.syncData.items.Values) {
                if(ind.creatureNetworkId == id && ind.holderIsPlayer == holderIsPlayer) {
                    ind.UpdateHoldState();
                }
            }
        }
    }
}
