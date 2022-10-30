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
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client {
    internal class ClientSync : MonoBehaviour {
        internal SyncData syncData = new SyncData();

        void Start () {
            if(!ModManager.clientInstance.nw.isConnected) {
                Destroy(this);
                return;
            }
            StartCoroutine(onUpdateTick());
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
                if(!ModManager.clientInstance.readyForTransmitting) continue;
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
                        PlayerEquipment.Read();
                        new PlayerEquipmentPacket(syncData.myPlayerData).SendToServerReliable();

                        Player.currentCreature.gameObject.GetElseAddComponent<NetworkLocalPlayer>();
                        Player.onSpawn += (player) => {
                            Player.currentCreature.gameObject.GetElseAddComponent<NetworkLocalPlayer>();
                            syncData.myPlayerData.creature = Player.currentCreature;
                            NetworkLocalPlayer.Instance.SendHealthPacket();
                        };

                        SendMyPos(true);

                        yield return CheckUnsynchedItems(); // Send the item when the player first connected
                    } else {
                        SendMyPos();
                    }
                }
                try {
                    SendMovedItems();
                    SendMovedCreatures();
                }catch(Exception e) {
                    Log.Err($"[Client] Error: {e}");
                }
            }
        }

        internal void Stop() {
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

                    if(Config.FULL_BODY_SYNCING) {
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
                    Log.Err($"[Client] Error at {pos}: {e}");
                }
            });
        }

        internal void SendMovedItems() {
            foreach(KeyValuePair<long, ItemNetworkData> entry in syncData.items) {
                if(entry.Value.clientsideId <= 0 || entry.Value.networkedId <= 0) continue;

                if(SyncFunc.hasItemMoved(entry.Value)) {
                    entry.Value.UpdatePositionFromItem();
                    new ItemPositionPacket(entry.Value).SendToServerUnreliable();
                }
            }
        }

        internal void SendMovedCreatures() {
            foreach(KeyValuePair<long, CreatureNetworkData> entry in syncData.creatures) {
                if(entry.Value.clientsideId <= 0 || entry.Value.networkedId <= 0) continue;

                if(SyncFunc.hasCreatureMoved(entry.Value)) {
                    entry.Value.UpdatePositionFromCreature();
                    if(entry.Value.creature.IsRagdolled()) {
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
        }

        internal void MovePlayer(PlayerNetworkData pnd) {
            if(pnd != null && pnd.creature != null) {
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

            Log.Debug("[Client] Found new item " + item.data.id + " - Trying to spawn...");

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
