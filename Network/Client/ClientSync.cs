using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using AMP.Useless;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using static ThunderRoad.ContainerData;

namespace AMP.Network.Client {
    public class ClientSync : MonoBehaviour {
        public SyncData syncData = new SyncData();

        void Start () {
            if(!ModManager.clientInstance.isConnected) {
                Destroy(this);
                return;
            }
            StartCoroutine(onUpdateTick());
        }

        public int packetsSentPerSec = 0;
        public int packetsReceivedPerSec = 0;

        bool checkItemCoroutineRunning = false;

        float time = 0f;
        void FixedUpdate() {
            if(!ModManager.clientInstance.isConnected) {
                Destroy(this);
                return;
            }
            if(ModManager.clientInstance.myClientId <= 0) return;

            time += Time.fixedDeltaTime;
            if(time > 1f) {
                if(!checkItemCoroutineRunning)
                    StartCoroutine(CheckUnsynchedItems()); // Check for unsynched or despawned items
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
            while(true) {
                yield return new WaitForSeconds(1f / ModManager.TICK_RATE);

                if(ModManager.clientInstance.myClientId <= 0) continue;

                if(syncData.myPlayerData == null) syncData.myPlayerData = new PlayerSync();
                if(Player.local != null && Player.currentCreature != null) {
                    if(syncData.myPlayerData.creature == null) {
                        syncData.myPlayerData.creature = Player.currentCreature;

                        syncData.myPlayerData.clientId = ModManager.clientInstance.myClientId;
                        syncData.myPlayerData.name = SteamFriends.GetPersonaName(); // TODO: Maybe the game has an other way to get the name
                        syncData.myPlayerData.name = NameColorizer.FormatSpecialName(syncData.myPlayerData.name);


                        syncData.myPlayerData.height = Player.currentCreature.GetHeight();
                        syncData.myPlayerData.creatureId = Player.currentCreature.creatureId;

                        syncData.myPlayerData.playerPos = Player.local.transform.position;
                        syncData.myPlayerData.playerRot = Player.local.transform.eulerAngles.y;

                        ModManager.clientInstance.tcp.SendPacket(syncData.myPlayerData.CreateConfigPacket());

                        SendMyPos(true);
                    } else {
                        SendMyPos();
                    }
                }
                SendMovedItems();
            }
        }

        /// <summary>
        /// Checking if the player has any unsynched items that the server needs to know about
        /// </summary>
        private IEnumerator CheckUnsynchedItems() {
            checkItemCoroutineRunning = true;

            // Get all items that only the client is seeing
            List<Item> client_only_items = Item.allActive.Where(item => syncData.serverItems.All(item2 => !item.Equals(item2))).ToList();
            // Get all items that are not synched
            List<Item> unsynced_items = client_only_items.Where(item => syncData.clientItems.All(item2 => !item.Equals(item2))).ToList();

            //Debug.Log("client_only_items: " + client_only_items.Count);
            //Debug.Log("unsynced_items: " + client_only_items.Count);

            foreach(Item item in unsynced_items) {
                if(/*item.data.type != ThunderRoad.ItemData.Type.Prop &&*/ item.data.type != ThunderRoad.ItemData.Type.Body && item.data.type != ThunderRoad.ItemData.Type.Spell) {
                    syncData.currentClientItemId++;

                    ItemSync itemSync = new ItemSync() {
                        dataId = item.data.id,
                        clientsideItem = item,
                        clientsideId = syncData.currentClientItemId,
                        position = item.transform.position,
                        rotation = item.transform.eulerAngles
                    };
                    ModManager.clientInstance.tcp.SendPacket(itemSync.CreateSpawnPacket());

                    syncData.clientItems.Add(item);
                    syncData.items.Add(-syncData.currentClientItemId, itemSync);

                    Debug.Log("[Client] Found new item " + item.data.id + " - Trying to spawn...");

                    yield return new WaitForEndOfFrame();
                } else {
                    // Despawn all props until better syncing system, so we dont spam the other clients
                    item.Despawn();
                }
            }

            // Get all despawned items
            //List<Item> despawned = client_only_items.Where(item => Item.allActive.All(item2 => !item.Equals(item2))).ToList();
            //foreach(Item item in despawned) {
            //    try {
            //        ItemSync itemSync = syncData.itemDataMapping.Values.First(i => i.clientsideItem.Equals(item));
            //        if(itemSync != null) {
            //            ModManager.clientInstance.tcp.SendPacket(itemSync.DespawnPacket());
            //            Debug.Log("[Client] Item " + itemSync.networkedId + " is despawned.");
            //        }
            //    } catch { }
            //
            //    client_only_items.Remove(item);
            //}
            checkItemCoroutineRunning = false;
        }

        // TODO: Fix: Player rotation does not match with headset rotation / Current Bugfix, use head Rotation, but need to find proper way for that
        // TODO: Fix: Player position is a bit buggy and doesnt always match
        private float lastPosSent = Time.time;
        public void SendMyPos(bool force = false) {
            if(Time.time - lastPosSent > 0.25f) force = true;
            if(!force) {
                if(!SyncFunc.hasPlayerMoved()) return;
            }

            syncData.myPlayerData.handLeftPos = Player.local.handLeft.transform.position;
            syncData.myPlayerData.handLeftRot = Player.currentCreature.handLeft.transform.eulerAngles;// += new Vector3(0, 0, 90);

            syncData.myPlayerData.handRightPos = Player.local.handRight.transform.position;
            syncData.myPlayerData.handRightRot = Player.currentCreature.handRight.transform.eulerAngles;// += new Vector3(-90, 0, 0);

            syncData.myPlayerData.headRot = Player.currentCreature.ragdoll.headPart.transform.eulerAngles;

            syncData.myPlayerData.playerPos = Player.currentCreature.transform.position;
            syncData.myPlayerData.playerRot = Player.local.head.transform.eulerAngles.y;

            ModManager.clientInstance.udp.SendPacket(syncData.myPlayerData.CreatePosPacket());
            
            lastPosSent = Time.time;
        }

        public void SendMovedItems() {
            foreach(KeyValuePair<int, ItemSync> entry in syncData.items) {
                if(SyncFunc.hasItemMoved(entry.Value)) {
                    entry.Value.GetPositionFromItem();
                    ModManager.clientInstance.udp.SendPacket(entry.Value.CreatePosPacket());
                }
            }
        }

        public void SpawnPlayer(int clientId) {
            PlayerSync playerSync = ModManager.clientSync.syncData.players[clientId];

            if(playerSync.creature != null || playerSync.isSpawning) return;

            CreatureData creatureData = Catalog.GetData<CreatureData>(playerSync.creatureId);
            if(creatureData != null) {
                playerSync.isSpawning = true;
                Vector3 position = playerSync.playerPos;
                Quaternion rotation = Quaternion.Euler(0, playerSync.playerRot, 0);

                creatureData.brainId = "HumanStatic";
                creatureData.containerID = "PlayerDefault";
                creatureData.factionId = 0;

                creatureData.SpawnAsync(position, rotation, null, false, null, creature => {
                    Debug.Log("[Client] Spawned Character for Player " + playerSync.clientId);

                    playerSync.creature = creature;

                    IKControllerFIK ik = creature.GetComponentInChildren<IKControllerFIK>();



                    Transform handLeftTarget = new GameObject("HandLeftTarget" + playerSync.clientId).transform;
                    handLeftTarget.parent = creature.transform;
                    #if DEBUG_INFO
                    TextMesh textMesh = handLeftTarget.gameObject.AddComponent<TextMesh>();
                    textMesh.text = "L";
                    textMesh.alignment = TextAlignment.Center;
                    textMesh.anchor = TextAnchor.MiddleCenter;
                    #endif
                    ik.SetHandAnchor(Side.Left, handLeftTarget);
                    playerSync.leftHandTarget = handLeftTarget;

                    Transform handRightTarget = new GameObject("HandRightTarget" + playerSync.clientId).transform;
                    handRightTarget.parent = creature.transform;
                    #if DEBUG_INFO
                    textMesh = handRightTarget.gameObject.AddComponent<TextMesh>();
                    textMesh.text = "R";
                    textMesh.alignment = TextAlignment.Center;
                    textMesh.anchor = TextAnchor.MiddleCenter;
                    #endif
                    ik.SetHandAnchor(Side.Right, handRightTarget);
                    playerSync.rightHandTarget = handRightTarget;

                    //Transform headTarget = new GameObject("HeadTarget" + playerSync.clientId).transform;
                    //headTarget.parent = creature.transform;
                    //#if DEBUG
                    //textMesh = headTarget.gameObject.AddComponent<TextMesh>();
                    //textMesh.text = "H";
                    //textMesh.alignment = TextAlignment.Center;
                    //textMesh.anchor = TextAnchor.MiddleCenter;
                    //#endif
                    //ik.SetHeadAnchor(headTarget);
                    //ik.SetHeadState(false, true);
                    //ik.SetHeadWeight(0, 1);
                    //headTarget.localPosition = Vector3.zero;
                    playerSync.headTarget = creature.ragdoll.headPart.transform;


                    Transform playerNameTag = new GameObject("PlayerNameTag" + playerSync.clientId).transform;
                    playerNameTag.parent = creature.transform;
                    playerNameTag.transform.localPosition = new Vector3(0, 2.3f, 0);
                    playerNameTag.transform.localEulerAngles = new Vector3(0, 180, 0);
                    TextMesh textMesh = playerNameTag.gameObject.AddComponent<TextMesh>();
                    textMesh.text = playerSync.name;
                    textMesh.alignment = TextAlignment.Center;
                    textMesh.anchor = TextAnchor.MiddleCenter;
                    textMesh.fontSize = 500;
                    textMesh.characterSize = 0.0025f;


                    //playerSync.headTarget = ik.headTarget;
                    ik.handLeftEnabled = true;
                    ik.handRightEnabled = true;
                    //ik.headEnabled = true;

                    creature.gameObject.name = "Player #" + playerSync.clientId;

                    creature.maxHealth = 100000;
                    creature.currentHealth = creature.maxHealth;

                    creature.isPlayer = false;
                    creature.enabled = false;
                    creature.locomotion.enabled = false;
                    creature.animator.enabled = false;
                    creature.ragdoll.enabled = false;
                    foreach(RagdollPart ragdollPart in creature.ragdoll.parts) {
                        foreach(HandleRagdoll hr in ragdollPart.handles){ Destroy(hr.gameObject); }// hr.enabled = false;
                        ragdollPart.sliceAllowed = false;
                        ragdollPart.enabled = false;
                    }
                    creature.brain.Stop();
                    creature.StopAnimation();
                    creature.brain.StopAllCoroutines();
                    creature.locomotion.MoveStop();
                    //creature.animator.speed = 0f;

                    // Trying to despawn equipet items | TODO: Doesn't seem to work right now, maybe try delayed?
                    creature.equipment.UnequipWeapons();

                    GameObject.DontDestroyOnLoad(creature.gameObject);

                    Creature.all.Remove(creature);
                    Creature.allActive.Remove(creature);

                    if(creature.currentRoom != null)
                        creature.currentRoom.UnRegisterCreature(creature);

                    //File.WriteAllText("C:\\Users\\mariu\\Desktop\\log.txt", GUIManager.LogLine(creature.gameObject, ""));

                    playerSync.isSpawning = false;
                });
            }
        }

        internal void MovePlayer(int clientId, PlayerSync newPlayerSync) {
            PlayerSync playerSync = ModManager.clientSync.syncData.players[clientId];

            if(playerSync != null && playerSync.creature != null) {
                playerSync.ApplyPos(newPlayerSync);

                playerSync.creature.transform.position = playerSync.playerPos;
                playerSync.creature.transform.eulerAngles = new Vector3(0, playerSync.playerRot, 0);

                playerSync.leftHandTarget.position = playerSync.handLeftPos;
                playerSync.leftHandTarget.eulerAngles = playerSync.handLeftRot;

                playerSync.rightHandTarget.position = playerSync.handRightPos;
                playerSync.rightHandTarget.eulerAngles = playerSync.handRightRot;

                // TODO: Head movement sync if needed
                //playerSync.headTarget.eulerAngles = playerSync.headRot;
            }
        }
    }
}
